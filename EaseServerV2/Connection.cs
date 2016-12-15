using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;
using CommonLib;
using EaseServer.Configuration;
using EaseServer.Interface;
using EaseServer.Performance;
using EaseServer.SocketServer;

namespace EaseServer
{

    /// <summary>
    /// 服务端连接对象,包含Socket相关信息。
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
    public class Connection : MarshalByRefObject, IServerConnection, IDisposable
    {
        /// <summary>
        /// 当前连接绑定参数
        /// </summary>
        private SocketAsyncEventArgs eventArgs;

        Server _server; Socket _serverSocket;
        string _localServerIP;
        /// <summary>
        /// 网络交换字节序列
        /// </summary>
        Stream _exchangeStream = null;

        // raw request data
        public const int MaxHeaderBytes = 8 * 1024;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="socket">The socket.</param>
        /// <param name="args">The <see cref="System.Net.Sockets.SocketAsyncEventArgs"/> instance containing the event data.</param>
        /// <param name="dataReceived">The data received.</param>
        /// <param name="disconnectedCallback">The disconnected callback.</param>
        public Connection(Server server, Socket socket, SocketAsyncEventArgs args, DataReceivedCallback dataReceived, DisconnectedCallback disconnectedCallback)
            : this(server, socket, args, NewSessionId(), FileAccess.ReadWrite, null, dataReceived, disconnectedCallback)
        {

        }

        /// <summary>
        /// A connection to our server, always listening asynchronously.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="socket">The Socket for the connection.</param>
        /// <param name="args">The SocketAsyncEventArgs for asyncronous recieves.</param>
        /// <param name="seesionID">会话标识</param>
        /// <param name="monitorAccess">The monitor access.</param>
        /// <param name="monitorDump">The monitor dump.</param>
        /// <param name="dataReceived">A callback invoked when data is recieved.</param>
        /// <param name="disconnectedCallback">A callback invoked on disconnection.</param>
        public Connection(Server server, Socket socket, SocketAsyncEventArgs args,
             string seesionID, FileAccess monitorAccess, Stream monitorDump,
            DataReceivedCallback dataReceived, DisconnectedCallback disconnectedCallback)
        {
            lock (this)
            {
                _server = server;
                _serverSocket = socket;
                _sid = seesionID;

                SocketMonitorStream sms = new SocketMonitorStream(socket, FileAccess.ReadWrite, false);
                sms.OnActiveFire += s => { this.LastInteractive = DateTime.Now; };
                if (monitorDump != null)
                {
                    sms.RecordAccess = monitorAccess;
                    sms.DumpStream = monitorDump;
                }
                _exchangeStream = sms; //初始化绑定的交换字节序列
                InitialIP();

                BindingState state = new BindingState()
                {
                    BindSocket = socket,
                    DataReceived = dataReceived,
                    DisconnectedCallback = disconnectedCallback
                };

                eventArgs = args;
                //eventArgs.DisconnectReuseSocket = true;
                eventArgs.RemoteEndPoint = args.RemoteEndPoint;
                eventArgs.Completed += ReceivedCompleted;  //挂接接收事件
                eventArgs.UserToken = state;
            }
        }
        #endregion

        string LocalServerIP
        {
            get
            {
                if (_localServerIP == null)
                {
                    var hostEntry = Dns.GetHostEntry(Environment.MachineName);
                    var localAddress = hostEntry.AddressList[0];
                    _localServerIP = localAddress.ToString();
                }

                return _localServerIP;
            }
        }


        void InitialIP()
        {
            LastInteractive = DateTime.Now; //初始化IP信息

            IPEndPoint ep = (IPEndPoint)_serverSocket.LocalEndPoint;
            localEP = (ep != null) ? ep.ToString() : "0.0.0.0";
            localIp = (ep != null && ep.Address != null) ? ep.Address.ToString() : "127.0.0.1";

            ep = (IPEndPoint)_serverSocket.RemoteEndPoint;
            remoteEP = (ep != null) ? ep.ToString() : "0.0.0.0";
            remoteIp = (ep != null && ep.Address != null) ? ep.Address.ToString() : "127.0.0.1";
        }

        void showErrorAndClose(string errorMessage)
        {
            CommonLib.ExtensionUtil.CatchAll(() => SendLessData(Encoding.Default.GetBytes(errorMessage)));
            closeAndDispose(); //操作错误之后关闭
        }

        #region Public Methods
        /// <summary>
        /// 连接客户端并异步接收数据
        /// </summary>
        public void Connect()
        {
            lock (this)
            {
                LastInteractive = DateTime.Now; //建立连接及活动时间
                ConnectedTime = DateTime.Now;

                BeginCounter(PerformancePoint.WholeTime);  //连接起始时间[开始]
                //Server.Log.DebugFormat("☆{1} {0}", "ListenForData", remoteEP);

                ListenForData(eventArgs); //首次监听数据

                #region 在默认等待时间后，如果没有发送数据则断开或执行应答式处理。
                if (ServerSessionSupport.ConfigInstance.EnableMixedSession)
                {
                    receiveWaitHandler.WaitOne((currentType.FullName + ".WaitMilliseconds").AppSettings<int>(1000));
                    if (!hasSendClientBytes)
                    {
                        if (processor == null)
                        {
                            processor = Server.GetAppliedProcessor(this);
                            if (processor == null)
                            {
                                showErrorAndClose(string.Format("Connection timeout, disconnecting automatically. ({0} v{1})",
                                    Server.ServerName, Messages.VersionString));
                            }
                            else
                            {
                                if (processor is IHELOFirstRnrProcessor)
                                    ((IHELOFirstRnrProcessor)processor).SayHello();
                            }
                        }
                    }
                }
                #endregion

                #region 最长闲置时间(每秒检查)
                int idleSeconds = "EaseServer.MaxIdleTimeSeconds".AppSettings<int>(120);
                HeartBeartTimer = new Timer(new TimerCallback(o =>
                {
                    if (KeepAliveSeconds == 0 && KeepAlive) return;

                    bool idleClose = false;
                    if (KeepAliveSeconds > 0)
                    {
                        idleClose = DateTime.Now.Subtract(LastInteractive).TotalSeconds > KeepAliveSeconds;
                    }
                    else
                    {
                        idleClose = DateTime.Now.Subtract(LastInteractive).TotalSeconds > Convert.ToInt32(o);
                    }

                    if (idleClose || MaxAccessCount < 1)
                        closeConnection(eventArgs); //闲置或达到最多会话次数关闭

                }), idleSeconds, idleSeconds, 1000);
                #endregion

            }
        }

        Timer HeartBeartTimer = null;

        /// <summary>
        /// Sends data to the client.
        /// </summary>
        /// <param name="data">The data to send.</param>
        public void SendLessData(Byte[] data)
        {
            SendLessData(data, 0, data.Length);
        }

        /// <summary>
        /// Sends data to the client.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="offset">The offset into the data.</param>
        /// <param name="count">The ammount of data to send.</param>
        public void SendLessData(Byte[] data, Int32 offset, Int32 count)
        {
            lock (this)
            {
                BindingState state = eventArgs.UserToken as BindingState;
                if (state != null)
                {
                    Socket socket = state.BindSocket;
                    if (socket != null && socket.Connected)
                        socket.Send(data, offset, count, SocketFlags.None);
                }
            }
        }

        /// <summary>
        /// 带缓冲发送大数据
        /// </summary>
        /// <param name="totalBytes">目标字节序列</param>
        /// <param name="bufferSize">缓冲字节数</param>
        /// <param name="hasSendError">是否发送错误</param>
        /// <returns>返回已发送的字节数</returns>
        public int SendWithBuffer(byte[] totalBytes, int bufferSize, ref bool hasSendError)
        {
            lock (this)
            {
                BindingState state = eventArgs.UserToken as BindingState;
                if (state != null)
                {
                    Socket socket = state.BindSocket;
                    if (socket != null && socket.Connected)
                        return sendSocketFragement(socket, totalBytes, bufferSize, ref hasSendError, Server.Log.DebugFormat);
                }
            }
            return 0;
        }
        #endregion

        /// <summary>
        /// 发送Socket字节序列,并返回发送了多少字节数据。
        /// </summary>
        /// <param name="clientSocket">客户端连接的Socket对象</param>
        /// <param name="currentSendBytes">当前发送字节序列</param>
        /// <param name="bufferSize">发送字节序列缓冲</param>
        /// <param name="hasSendError">是否出现发送错误</param>
        /// <param name="senderWriter">Socket发送数据监控委托</param>
        /// <returns>总共发送了的字节总数</returns>
        int sendSocketFragement(Socket clientSocket, byte[] currentSendBytes, int bufferSize, ref bool hasSendError, ListClientWriter senderWriter)
        {
            int totalSent = 0, curentSent = 0;
            hasSendError = false;
            while (totalSent < currentSendBytes.Length)
            {
                LastInteractive = DateTime.Now; //发回处理数据

                if (currentSendBytes.Length - totalSent < bufferSize)
                    bufferSize = currentSendBytes.Length - totalSent;

                try
                {
                    curentSent = clientSocket.Send(currentSendBytes, totalSent, bufferSize, SocketFlags.None);
                    if (senderWriter != null)
                        senderWriter("* 本次发送:{0}字节，缓冲大小为{1}字节。", curentSent, bufferSize);
                }
                catch (SocketException err)
                {
                    //重新连接一次(10035)
                    if (err.ErrorCode == (int)SocketError.WouldBlock)
                    {
                        if (clientSocket != null) continue;
                    }
                    else
                    {
                        hasSendError = true;
                        break;
                    }
                }
                totalSent += curentSent;
            }
            return totalSent;
        }

        #region Private Methods
        /// <summary>
        /// Starts and asynchronous recieve.
        /// </summary>
        /// <param name="args">The SocketAsyncEventArgs to use.</param>
        private void ListenForData(SocketAsyncEventArgs args)
        {
            lock (this)
            {
                if (state == ConnectionState.ReceiveClientData || state == ConnectionState.Closing)
                    return;

                state = ConnectionState.ReceiveClientData;
                BeginCounter(PerformancePoint.ReceiveData); //解析接收数据[开始]
                Socket socket = (args.UserToken as BindingState).BindSocket;
                if (socket != null && socket.Connected)
                {
                    try
                    {
                        ExtMethods.InvokeAsyncMethod(socket, socket.ReceiveAsync, ReceivedCompleted, args);
                    }
                    catch (Exception ivkEx)
                    {
                        //System.InvalidOperationException 现在已经正在使用此 SocketAsyncEventArgs 实例进行异步套接字操作。
                        Server.Log.Error("*ReceiveAsync Error:" + ivkEx.Message);
                        closeAndDispose();
                    }
                }
            }
        }

        /// <summary>
        /// Called when an asynchronous receive has completed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The SocketAsyncEventArgs for the operation.</param>
        private void ReceivedCompleted(Object sender, SocketAsyncEventArgs args)
        {
            if (args == null)
                return;

            lock (this)
            {
                try
                {
                    if (args.BytesTransferred == 0 || args.SocketError != SocketError.Success)
                    {
                        closeConnection(args); //Graceful disconnect
                        return;
                    }

                    Byte[] data = new Byte[args.BytesTransferred];
                    Array.Copy(args.Buffer, args.Offset, data, 0, data.Length);

                    BindingState bdState = args.UserToken as BindingState;
                    //Server.Log.DebugFormat("☆{1} {0}", "ReceivedCompleted", bdState.socket.RemoteEndPoint);
                    OnDataReceived(data, args.RemoteEndPoint as IPEndPoint, bdState.DataReceived);

                    ListenForData(args);  //监听下一次数据发送
                }
                catch (Exception recEx)
                {
                    Server.Log.Error("* ReceivedCompleted Exception", recEx);
                }
            }
        }


        #endregion

        #region Events
        /// <summary>
        /// Fires the DataReceivedCallback.
        /// </summary>
        /// <param name="data">The data which was received.</param>
        /// <param name="remoteEndPoint">The address the data came from.</param>
        /// <param name="callback">The callback.</param>
        private void OnDataReceived(Byte[] data, IPEndPoint remoteEndPoint, DataReceivedCallback callback)
        {
            callback(this, new DataEventArgs() { RemoteEndPoint = remoteEndPoint, Data = data, Offset = 0, Length = data.Length });
        }

        /// <summary>
        /// Fires the DisconnectedCallback.
        /// </summary>
        /// <param name="args">The SocketAsyncEventArgs for this connection.</param>
        /// <param name="callback">The callback.</param>
        private void OnDisconnected(SocketAsyncEventArgs args, DisconnectedCallback callback)
        {
            callback(this, args);
        }
        #endregion


        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString() { return string.Format("{2} {1}<=>{0}", RemoteEP, LocalEP, Protocol); }

        string localIp, localEP, remoteIp, remoteEP;

        /// <summary>
        /// 当前处理连接处理对象
        /// </summary>
        IConnectionProcessor processor = null;

        /// <summary>
        /// 当前实例类型
        /// </summary>
        Type currentType = typeof(Connection);

        /// <summary>
        /// 等待接收数据事件
        /// </summary>
        ManualResetEvent receiveWaitHandler = new ManualResetEvent(false);

        /// <summary>
        /// 客户端连接已发送连接数据
        /// </summary>
        bool hasSendClientBytes = false;

        /// <summary>
        /// 同一实例对象的线程锁
        /// </summary>
        object threadLocker = new object();
        /// <summary>
        /// 获取同一对象的线程同步锁
        /// </summary>
        public object ThreadSynRoot { get { return threadLocker; } }


        #region IServerConnection 成员

        /// <summary>
        /// 获取客户端与服务端通信的字节序列封装(网络字节序列)
        /// </summary>
        public Stream ExchangeStream
        {
            get { return _exchangeStream; }
        }

        private bool _keepAlive = false;
        /// <summary>
        /// 是否保持连接不主动断开
        /// </summary>
        public bool KeepAlive
        {
            get { return _keepAlive; }
            set { _keepAlive = value; }
        }

        /// <summary>
        /// 客户端要求的保持连接长度（单位秒）
        /// </summary>
        public int KeepAliveSeconds { get; set; }


        int _maxAccessCount = 100;
        /// <summary>
        /// 最对交互次数(默认100)
        /// </summary>
        public int MaxAccessCount
        {
            get { return _maxAccessCount; }
            set { _maxAccessCount = value; }
        }

        /// <summary>
        /// 当前Socket上的缓冲数据
        /// </summary>
        protected byte[] socketBuffer = new byte[MaxHeaderBytes];

        /// <summary>
        /// 单次Socket上发送的字节数
        /// </summary>
        public byte[] SocketBufferData
        {
            get { return socketBuffer; }
            internal set { socketBuffer = value; }
        }

        /// <summary>
        /// 服务器端的IP绑定地址
        /// </summary>
        public string LocalIP { get { return localIp; } }

        /// <summary>
        /// 服务器端的IP绑定端点信息
        /// </summary>
        public string LocalEP { get { return localEP; } }

        /// <summary>
        /// 客户端远程IP地址
        /// </summary>
        public string RemoteIP { get { return remoteIp; } }

        /// <summary>
        /// 远程连接端点，用户连接客户端的标识。形如：192.168.8.119:3456
        /// </summary>
        public string RemoteEP { get { return remoteEP; } }

        /// <summary>
        /// 分配新的会话标识
        /// </summary>
        public static string NewSessionId()
        {
            return DateTime.Now.ToString("MMdd_HHmmss_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        string _sid = null;
        /// <summary>
        /// 获取单次应用请求的会话标识
        /// </summary>
        public string SessionId
        {
            get
            {
                if (_sid == null) _sid = NewSessionId();
                return _sid;
            }
        }

        /// <summary>
        /// 获取或设置当前协议的值，默认为HTTP。
        /// </summary>
        /// <value></value>
        public string Protocol
        {
            get
            {
                if (processor != null) return processor.ProtocolIdentity;
                return "HTTP";
            }
        }

        /// <summary>
        /// 获取当前客户端的连接时间
        /// </summary>
        public DateTime ConnectedTime { get; private set; }

        /// <summary>
        /// 获取当前客户端的最近活动时间
        /// </summary>
        public DateTime LastInteractive { get; private set; }

        /// <summary>
        /// 获取连接模型
        /// </summary>
        /// <value></value>
        public ConnectionMode SocketMode
        {
            get
            {
                if (processor == null) return ConnectionMode.Auto;
                return processor.SocketMode;
            }
        }

        void releaseExchangeDumpStream()
        {
            if (_exchangeStream is SocketMonitorStream)
            {
                SocketMonitorStream sms = (SocketMonitorStream)_exchangeStream;
                if (sms.DumpStream != null)
                {
                    try
                    {
                        sms.DumpStream.Close();
                        sms.DumpStream.Dispose();
                    }
                    catch (Exception) { }
                    finally
                    {
                        sms.DumpStream = null;
                    }
                }
            }
        }

        /// <summary>
        /// 当前连接状态
        /// </summary>
        volatile ConnectionState state = ConnectionState.UNSetting;

        /// <summary>
        /// 获取服务端API暴露接口引用
        /// </summary>
        public IServerAPI GetServerAPI() { return _server; }

        /// <summary>
        /// 获取当前连接的Socket对象
        /// </summary>
        public Socket GetClientSocket() { return _serverSocket; }

        /// <summary>
        /// 数据通信次数
        /// </summary>
        int _accesTimes = 0;

        /// <summary>
        /// 判断是否是首次获取Socket上新请求的数据
        /// </summary>
        public bool IsFirstAccess { get { return _accesTimes == 1; } }

        /// <summary>
        /// 增加单次会话Socket上的数据发送次数
        /// </summary>
        public virtual void IncrementAccessCount() { _accesTimes++; }

        /// <summary>
        /// 重置Socket上的单次数据读取计数
        /// </summary>
        public virtual void ResetAccessCount() { _accesTimes = 0; }

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// [主要发送数据入口]发送应答序列视图
        /// </summary>
        public void SendResponseStream(RnRStream resp, int bufferSize)
        {
            long totalSent = 0, curentSent = 0;
            bool hasSendError = false;
            string protoID = processor.ProtocolIdentity;
            long offsePos = resp.Position;

            sw.Reset();
            sw.Start();

            int sendTimes = 0;
            byte[] currentSendBytes = new byte[0];
            if (resp.HasFinished())
            {
                totalSent = sendSocketFragement(_serverSocket, resp.SessionBuffer.ToArray(), bufferSize, ref hasSendError, Server.Log.DebugFormat);
            }
            else
            {
                while (_serverSocket != null && !resp.HasFinished())
                {
                    if (resp.Length - totalSent < bufferSize)
                    {
                        bufferSize = (int)(resp.Length - totalSent);
                    }

                    if (sendTimes == 0 && offsePos > 0)
                    {
                        currentSendBytes = resp.ReadTotalLength(resp.SessionBuffer.ToArray(), bufferSize);
                    }
                    else
                    {
                        currentSendBytes = resp.ReadSpecialLength(bufferSize);
                    }

                    curentSent = sendSocketFragement(_serverSocket, currentSendBytes, bufferSize, ref hasSendError, Server.Log.DebugFormat);
                    totalSent += curentSent;
                    sendTimes++;

                    Server.Log.DebugFormat("* [{4}] 向<{0}>发送{3}字节, 总{1}/{2}字节, 余{5}字节.",
                        RemoteEP, totalSent, resp.Length,
                        curentSent, protoID,
                        resp.Length - totalSent);

                    if (hasSendError) break;
                }
            }

            sw.Stop();
            Server.Log.DebugFormat("* [{3}] 向<{0}>总共发送{1}/{2}字节数据,共计{4}ms,正常完成[{5}].",
                    RemoteEP, totalSent, resp.Length, protoID,
                    sw.ElapsedMilliseconds, !hasSendError);

        }

        /// <summary>
        /// 如果存在字节监控则写入读取的字节数据
        /// </summary>
        public void MonitorDump()
        {
            if (ExchangeStream is SocketMonitorStream)
            {
                SocketMonitorStream sms = (SocketMonitorStream)ExchangeStream;
                if (sms.DumpStream != null
                               && (FileAccess.Read & sms.RecordAccess) == FileAccess.Read)
                {
                    sms.SetReadFlag();
                    sms.DumpStream.Write(sms.reqIDBytes, 0, sms.reqIDBytes.Length);
                    sms.DumpStream.Write(socketBuffer, 0, socketBuffer.Length);
                    sms.DumpStream.Flush();
                    sms.ResetReadFlag();
                }
            }
        }

        #region 第三方连接日志调试辅助
        /// <summary>
        /// 服务端记录日志(INFO)
        /// </summary>
        public void ServerLog(string format, params object[] args) { Server.Log.InfoFormat(format, args); }

        /// <summary>
        /// 服务端记录日志(DEBUG)
        /// </summary>
        public void ServerDebug(string format, params object[] args) { Server.Log.DebugFormat(format, args); }

        /// <summary>
        /// 服务端记录日志(ERROR)
        /// </summary>
        public void ServerError(string format, params object[] args) { Server.Log.ErrorFormat(format, args); }
        #endregion

        #region 性能计数统计
        /// <summary>
        /// 重置性能计数器
        /// </summary>
        public void ResetCounter()
        {
            _connCounter = null;
        }

        PerformanceCounter _connCounter = null;
        /// <summary>
        /// 获取当前连接的性能计数器
        /// </summary>
        /// <value></value>
        public PerformanceCounter PerfCounter
        {
            get
            {
                if (_connCounter == null)
                {
                    _connCounter = new PerformanceCounter()
                    {
                        LocalEndPoint = LocalEP.ToString(),
                        RemoteEndpoint = remoteEP,
                        RootPerfData = new PerfData()
                    };
                }
                return _connCounter;
            }
        }

        /// <summary>
        /// 判断是否进行性能统计
        /// </summary>
        /// <value></value>
        public bool EnablePerformanceCounter
        {
            get { return (currentType.FullName + ".EnablePerformanceCounter").AppSettings<bool>(false); }
        }

        PerformancePoint? _pointSetting;

        /// <summary>
        /// 性能计数的统计点配置
        /// </summary>
        /// <value></value>
        public PerformancePoint PerfCounterPoint
        {
            get
            {
                if (_pointSetting.HasValue)
                    return _pointSetting.Value;

                string pCfg = ConfigurationManager.AppSettings[currentType.FullName + ".PerformancePoint"];
                if (string.IsNullOrEmpty(pCfg))
                {
                    _pointSetting = PerformancePoint.ALL;
                }
                else
                {
                    string[] cfgArr = pCfg.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string item in cfgArr)
                    {
                        if (_pointSetting.HasValue)
                        {
                            _pointSetting = _pointSetting.Value.Include<PerformancePoint>((PerformancePoint)Enum.Parse(typeof(PerformancePoint), item));
                        }
                        else
                        {
                            _pointSetting = (PerformancePoint)Enum.Parse(typeof(PerformancePoint), item);
                        }
                    }
                }
                return _pointSetting.Value;
            }
        }

        /// <summary>
        /// 最近的性能计数对象
        /// </summary>
        PerfData lastestPerfData = null;

        /// <summary>
        /// 开始统计特定的点
        /// </summary>
        /// <param name="point">统计点</param>
        /// <param name="appendActions">附加的其他操作</param>
        /// <returns></returns>
        public PerfData BeginCounter(PerformancePoint point, params Action<PerfData>[] appendActions)
        {
            PerfData tarPerf = new PerfData() { Point = point };
            if (EnablePerformanceCounter && PerfCounterPoint.Has<PerformancePoint>(point))
            {
                if (point == PerformancePoint.WholeTime)
                {
                    tarPerf = PerfCounter.RootPerfData.BeginCounter(point, appendActions);
                    lastestPerfData = PerfCounter.RootPerfData;
                }
                else
                {
                    PerfData pDat = PerfCounter.RootPerfData.GetOrCreateSubPerfData(point, false, lastestPerfData);
                    pDat = pDat.BeginCounter(point, appendActions);
                    lastestPerfData = pDat;
                    tarPerf = pDat;
                }
            }
            return tarPerf;
        }

        /// <summary>
        /// 结束统计特定的点
        /// </summary>
        /// <param name="point">统计点</param>
        /// <param name="prefixActions">前置的其他操作</param>
        /// <returns></returns>
        public PerfData EndCounter(PerformancePoint point, params Action<PerfData>[] prefixActions)
        {
            PerfData tarPerf = new PerfData() { Point = point };
            if (EnablePerformanceCounter && PerfCounterPoint.Has<PerformancePoint>(point))
            {
                if (point == PerformancePoint.WholeTime)
                {
                    tarPerf = PerfCounter.RootPerfData.EndCounter(prefixActions);
                    lastestPerfData = PerfCounter.RootPerfData;
                }
                else
                {
                    PerfData pDat = PerfCounter.RootPerfData.GetOrCreateSubPerfData(point, true, lastestPerfData);
                    pDat = pDat.EndCounter(prefixActions);
                    lastestPerfData = pDat;
                    tarPerf = pDat;
                }
            }
            return tarPerf;
        }
        #endregion

        #endregion

        /// <summary>
        /// 获取当前连接<see cref="Connection"/> 是否可用。
        /// </summary>
        /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
        public bool Connected { get { return (_serverSocket != null && _serverSocket.Connected); } }

        /// <summary>
        /// Obtains a lifetime service object to control the lifetime policy for this instance.
        /// </summary>
        /// <returns>
        /// An object of type <see cref="T:System.Runtime.Remoting.Lifetime.ILease"/> used to control the lifetime policy for this instance. This is the current lifetime service object for this instance if one exists; otherwise, a new lifetime service object initialized to the value of the <see cref="P:System.Runtime.Remoting.Lifetime.LifetimeServices.LeaseManagerPollTime"/> property.
        /// </returns>
        /// <exception cref="T:System.Security.SecurityException">
        /// The immediate caller does not have infrastructure permission.
        /// </exception>
        /// <PermissionSet>
        /// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="RemotingConfiguration, Infrastructure"/>
        /// </PermissionSet>
        public override object InitializeLifetimeService()
        {
            // never expire the license
            return null;
        }

        #region ASPXHost
        /// <summary>
        /// Waits for request bytes.
        /// </summary>
        /// <returns></returns>
        public int WaitForRequestBytes()
        {
            int availBytes = 0;
            try
            {
                if (_serverSocket.Available == 0)
                {
                    // poll until there is data
                    _serverSocket.Poll(100000 /* 100ms */, SelectMode.SelectRead);
                    if (_serverSocket.Available == 0 && _serverSocket.Connected)
                    {
                        _serverSocket.Poll(30000000 /* 30sec */, SelectMode.SelectRead);
                    }
                }
                availBytes = _serverSocket.Available;
            }
            catch { }
            return availBytes;
        }

        /// <summary>
        /// Reads the request bytes.
        /// </summary>
        /// <param name="maxBytes">The max bytes.</param>
        /// <returns></returns>
        public byte[] ReadRequestBytes(int maxBytes)
        {
            LastInteractive = DateTime.Now; //读取未完成的头部数据

            try
            {
                if (WaitForRequestBytes() == 0) return null;

                int numBytes = _serverSocket.Available;
                if (numBytes > maxBytes)
                    numBytes = maxBytes;

                int numReceived = 0;
                byte[] buffer = new byte[numBytes];

                if (numBytes > 0)
                {
                    numReceived = _exchangeStream.Read(buffer, 0, numBytes);
                }

                if (numReceived < numBytes)
                {
                    byte[] tempBuffer = new byte[numReceived];
                    if (numReceived > 0)
                    {
                        Buffer.BlockCopy(buffer, 0, tempBuffer, 0, numReceived);
                    }
                    buffer = tempBuffer;
                }

                return buffer;
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Writes the headers.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="extraHeaders">The extra headers.</param>
        /// <param name="keepAlive">是否保持连接</param>
        public void WriteHeaders(int statusCode, String extraHeaders, bool keepAlive)
        {
            string headers = MakeResponseHeaders(statusCode, extraHeaders, -1, keepAlive);
            try
            {
                byte[] headBytes = Encoding.UTF8.GetBytes(headers);
                _exchangeStream.Write(headBytes, 0, headBytes.Length);
                LastInteractive = DateTime.Now; //向客户端发送HTTP头部数据
            }
            catch (SocketException) { }
        }

        /// <summary>
        /// 忽略错误向已连接的客户端发送数据
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void WriteBody(byte[] data, int offset, int length)
        {
            try
            {
                LastInteractive = DateTime.Now; //向客户端发送数据
                _exchangeStream.Write(data, offset, length);
                LastInteractive = DateTime.Now;//向客户端发送数据
            }
            catch (Exception) { }
        }


        /// <summary>
        /// 通过文件名路径获取文件类型
        /// </summary>
        internal static String MakeContentTypeHeader(string fileName)
        {
            System.Diagnostics.Debug.Assert(File.Exists(fileName));
            string contentType = null;

            var info = new FileInfo(fileName);
            string extension = info.Extension.ToLowerInvariant();

            switch (extension)
            {

                #region 文本型
                case ".txt":
                    contentType = "text/css";
                    break;
                case ".css":
                    contentType = "text/plain";
                    break;
                case ".xml":
                case ".rss":
                    contentType = "text/xml";
                    break;
                case ".htm":
                case ".html":
                    contentType = "text/html";
                    break;
                case ".js":
                    contentType = "application/x-javascript";
                    break;
                case ".xaml":
                    contentType = "application/xaml+xml";
                    break;
                #endregion

                #region 图片型
                case ".bmp":
                    contentType = "image/bmp";
                    break;
                case ".gif":
                    contentType = "image/gif";
                    break;
                case ".png":
                    contentType = "image/png";
                    break;
                case ".ico":
                    contentType = "image/x-icon";
                    break;
                case ".jpe":
                case ".jpeg":
                case ".jpg":
                    contentType = "image/jpeg";
                    break;
                #endregion

                #region 压缩包
                case ".zip":
                case ".xap":
                case ".jar":
                case ".slvx":
                    contentType = "application/x-zip-compressed";
                    break;
                #endregion

                default:
                    break;
            }

            if (contentType == null)
            {
                return ConfigurationManager.AppSettings[Server.ServerName + ".UnkownContentType"];
            }
            return "Content-Type: " + contentType + "\r\n";
        }

        /// <summary>
        /// 创建响应头内容字符维护连接状态
        /// </summary>
        /// <param name="statusCode">响应状态码</param>
        /// <param name="moreHeaders">附加的其他头信息</param>
        /// <param name="contentLength">响应内容体长度</param>
        /// <param name="keepAlive">是否保持连接</param>
        /// <returns></returns>
        public virtual string MakeResponseHeaders(int statusCode, string moreHeaders, int contentLength, bool keepAlive)
        {
            var sb = new StringBuilder();

            sb.Append("HTTP/1.1 " + statusCode + " " + HttpWorkerRequest.GetStatusDescription(statusCode) + "\r\n");
            sb.Append("Server: " + Server.ServerName + "/" + Messages.VersionString + "\r\n");
            sb.Append("Date: " + DateTime.Now.ToUniversalTime().ToString("R", DateTimeFormatInfo.InvariantInfo) + "\r\n");
            if (contentLength >= 0)
                sb.Append("Content-Length: " + contentLength + "\r\n");

            if (moreHeaders != null)
                sb.Append(moreHeaders);

            if (!this.KeepAlive)
            {
                if (!keepAlive)
                    sb.Append("Connection: close\r\n");
            }
            else
            {
                if (MaxAccessCount < 1)
                {
                    sb.Append("Connection: close\r\n");
                }
                else
                {
                    if (KeepAliveSeconds > 0)
                    {
                        sb.AppendFormat("Keep-Alive: timeout={0}, max={1}\r\n", KeepAliveSeconds, MaxAccessCount);
                    }
                    else
                    {
                        sb.AppendFormat("Keep-Alive: max={0}\r\n", MaxAccessCount);
                    }
                }
            }

            sb.Append("\r\n");

            return sb.ToString();
        }

        /// <summary>
        /// Write100s the continue.
        /// </summary>
        public void Write100Continue()
        {
            WriteEntireResponseFromString(100, null, null, true);
        }

        public void WriteEntireResponseFromString(int statusCode, String extraHeaders, String body, bool keepAlive)
        {
            try
            {
                int bodyLength = (body != null) ? Encoding.UTF8.GetByteCount(body) : 0;
                string headers = MakeResponseHeaders(statusCode, extraHeaders, bodyLength, keepAlive);
                byte[] sendBuf = Encoding.UTF8.GetBytes(headers + body);
                _exchangeStream.Write(sendBuf, 0, sendBuf.Length);
            }
            catch (SocketException) { }
            finally
            {
                if (!keepAlive)
                {
                    closeAndDispose();  //输出HTML之后关闭
                }
            }
        }

        /// <summary>
        /// Writes the entire response from file.(404, 403, 200)
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="keepAlive">if set to <c>true</c> [keep alive].</param>
        public void WriteEntireResponseFromFile(String fileName, bool keepAlive)
        {
            if (!File.Exists(fileName))
            {
                WriteErrorAndClose(404);
                return;
            }

            // Deny the request if the contentType cannot be recognized.
            string contentTypeHeader = MakeContentTypeHeader(fileName);
            if (contentTypeHeader == null)
            {
                WriteErrorAndClose(403);
                return;
            }

            bool completed = false;
            FileStream fs = null;

            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                int len = (int)fs.Length;
                byte[] fileBytes = new byte[len];
                int bytesRead = fs.Read(fileBytes, 0, len);

                String headers = MakeResponseHeaders(200, contentTypeHeader, bytesRead, keepAlive);
                byte[] headBytes = Encoding.UTF8.GetBytes(headers);
                _exchangeStream.Write(headBytes, 0, headBytes.Length);
                _exchangeStream.Write(fileBytes, 0, bytesRead);
                completed = true;
            }
            catch (SocketException) { }
            finally
            {
                if (!keepAlive || !completed)
                    closeAndDispose();  //发送文件出错或不保持连接

                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// Gets the error response body.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        string GetErrorResponseBody(int statusCode, string message)
        {
            string body = Messages.FormatErrorMessageBody(statusCode, _server.VirtualPath);
            if (message != null && message.Length > 0)
            {
                body += "\r\n<!--\r\n" + message + "\r\n-->";
            }
            return body;
        }

        /// <summary>
        /// 连接错误处理
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="message">The message.</param>
        public void WriteErrorAndClose(int statusCode, string message)
        {
            if (Protocol.Equals("HTTP", StringComparison.InvariantCultureIgnoreCase))
            {
                WriteEntireResponseFromString(statusCode, null, GetErrorResponseBody(statusCode, message), false);
            }
            else
            {
                if (this.processor != null) processor.WriteErrorAndClose(statusCode, message);
            }
        }

        /// <summary>
        /// Writes the error and close.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        public void WriteErrorAndClose(int statusCode)
        {
            WriteErrorAndClose(statusCode, null);
        }

        #endregion

        /// <summary>
        /// HTTP头部与内容分隔字节
        /// </summary>
        internal static readonly byte[] HttpHeadBreakBytes = new byte[] { 13, 10, 13, 10 };

        /// <summary>
        /// 当前连接的HTTP头部数据
        /// </summary>
        byte[] _currentHttpHeadBytes = null;

        /// <summary>
        /// 是否需要附加缓冲字节数据
        /// </summary>
        bool _needAppendBuffer = false;

        /// <summary>
        /// 在当前线程处理接收到的数据
        /// </summary>
        /// <param name="receivBytes">当前接收到的数据</param>
        public void ProcessReceivedData(byte[] receivBytes)
        {
            #region 设置已接收到数据信号
            lock (threadLocker)
            {
                if (!hasSendClientBytes)
                {
                    hasSendClientBytes = true;
                    receiveWaitHandler.Set();
                }
                state = ConnectionState.ExecuteForResponse;
            }
            #endregion

            IncrementAccessCount(); //通过处理接收数据增加交互次数

            if (_needAppendBuffer)  //增加发送的字节数据
            {
                if (_currentHttpHeadBytes != null)  //更新头部字节缓冲数据
                {
                    byte[] newBuffer = new byte[_currentHttpHeadBytes.Length + receivBytes.Length];
                    Buffer.BlockCopy(_currentHttpHeadBytes, 0, newBuffer, 0, _currentHttpHeadBytes.Length); //保留原始数据
                    Buffer.BlockCopy(receivBytes, 0, newBuffer, _currentHttpHeadBytes.Length, receivBytes.Length); //新增数据
                    socketBuffer = newBuffer;
                }
            }
            else
            {
                socketBuffer = receivBytes;
            }

            #region 处理当前发送数据
            if (processor != null)
            {
                //Server.Log.DebugFormat("* [{0}] 第{1}次接收到数据{2}字节...", Protocol, AccesTimes, SocketBufferData.Length);
                //Server.Log.DebugFormat("{0}", SocketBufferData.GetHexViewString());
                processCurrent(); //根据现有的处理器进行处理
            }
            else
            {
                if (IsFirstAccess)  //还没有找到任何处理器时
                {
                    #region 首次交互处理
                    MonitorDump(); //添加首次读取的字节缓冲
                    BeginCounter(PerformancePoint.ParseProcessor); //解析连接处理器[开始]
                    IConnectionProcessor process = Server.GetRespectiveProcess(this, socketBuffer);
                    if (process != null)
                    {
                        processor = process;
                        EndCounter(PerformancePoint.ParseProcessor); //连接解析器处理[结束]
                        //if (process.SocketMode != ConnectionMode.SingleCall) { KeepAlive = true; }
                        processCurrent(); //首次连接处理
                        return;
                    }
                    #endregion
                }

                int? crLFIdx = null;
                string assertHTTPRawHeader = null;
                bool needSendErrorBytes = false;

                #region 当发送数据为HTTP头部数据时
                byte[] httpBreakBytes = new byte[] { 13, 10 };
                crLFIdx = socketBuffer.LocateFirst(httpBreakBytes, 0);
                if (!crLFIdx.HasValue) needSendErrorBytes = true;
                if (!needSendErrorBytes)
                {
                    int? headBreakIndex = socketBuffer.LocateFirst(HttpHeadBreakBytes, 0);
                    if (!headBreakIndex.HasValue)
                    {
                        _currentHttpHeadBytes = new byte[socketBuffer.Length];
                        Buffer.BlockCopy(socketBuffer, 0, _currentHttpHeadBytes, 0, socketBuffer.Length); //预存头部字节数据
                        _needAppendBuffer = true;
                    }
                    else
                    {
                        _needAppendBuffer = false;
                        _currentHttpHeadBytes = null;  //清空头部字节数据
                        try
                        {
                            assertHTTPRawHeader = Encoding.Default.GetString(socketBuffer);
                        }
                        catch (Exception)
                        {
                            needSendErrorBytes = true;
                        }
                    }
                }

                if (needSendErrorBytes)
                {
                    Server.Log.DebugFormat("* 接收到无效数据{0}字节，二进制内容显示为\r\n{1}",
                        socketBuffer.Length,
                        socketBuffer.GetHexViewString());

                    showErrorAndClose("Invalid client Command, disconnecting automatically. (" + Server.ServerName + " v" + Messages.VersionString + ")");
                }
                else
                {
                    if (!_needAppendBuffer && !String.IsNullOrEmpty(assertHTTPRawHeader))
                    {
                        processHttpHeader(assertHTTPRawHeader.Split('\n'));
                    }
                }
                #endregion
            }
            #endregion

        }

        /// <summary>
        /// 使用processor处理当前请求
        /// <remarks>假定一次发送了全部请求数据[TODO]</remarks>
        /// </summary>
        void processCurrent()
        {
            EndCounter(PerformancePoint.ReceiveData);//接收到数据完成[结束]
            //Debug.Assert(processor != null);
            #region 处理当前发送数据
            Action currentAct = null;
            if (processor is IRnRProcessor)
            {
                IRnRProcessor iRnr = (IRnRProcessor)processor;
                #region 处理R&R业务逻辑
                if (iRnr != null)
                {
                    if (SocketMode == ConnectionMode.SelfHosting)
                    {
                        if (IsFirstAccess)  //扩展方式处理首次连接
                        {
                            //首次接收到了数据
                            iRnr.WriteReqeustBytes(socketBuffer);
                        }
                        else
                        {
                            iRnr.ReadRequestData();
                        }
                        IncrementAccessCount(); //SelfHost => 通过处理器增加连接次数
                    }
                    else
                    {
                        iRnr.WriteReqeustBytes(socketBuffer);
                    }

                    if (iRnr.HasFinishedRequest())
                    {
                        currentAct = () =>
                        {
                            iRnr.ProcessResponse();
                            iRnr.ResetRequest();
                        };
                    }
                }
                #endregion
            }
            else
            {
                currentAct = processor.ProcessRequest;
            }

            if (currentAct != null)
            {
                try
                {
                    currentAct();
                }
                catch (Exception processEx)
                {
                    Server.Log.Error("* " + processor.GetType().FullName + "处理失败：", processEx);
                    closeAndDispose(); //业务处理错误关闭
                    return;
                }
            }
            #endregion

            if (Connected)
            {
                if (SocketMode == ConnectionMode.SelfHosting)
                {
                    processCurrent(); //重复调用当前处理
                }
                else
                {
                    socketBuffer = new byte[MaxHeaderBytes];
                }
            }
        }

        void processAspxHost()
        {
            // find or create host
            Host host = _server.GetHost();
            if (host == null)
            {
                WriteErrorAndClose(500);
                return;
            }

            Exception lastExp = null;
            if (_serverSocket != null && _serverSocket.Connected)
            {
                // process request in worker app domain
                try
                {
                    host.ProcessRequest(this);
                }
                catch (AppDomainUnloadedException unloadEx)
                {
                    lastExp = unloadEx;
                    _server.HostStopped();
                    Server.Log.Error("* AspxHost宿主服务已卸载！");
                }
                catch (Exception hostEx)
                {
                    lastExp = hostEx;
                    Server.Log.Error("* AspxHost处理出现异常：", hostEx);
                }
            }
            if (lastExp != null) closeAndDispose();//APSX处理异常关闭
        }

        /// <summary>
        /// 依据HTTP头数据进行处理逻辑
        /// </summary>
        void processHttpHeader(string[] allHeaderLines)
        {
            string firstLineString = allHeaderLines[0].Trim();
            string knownUserAgent = string.Empty;
            bool doNextProcess = true;

            #region HTTP屏蔽不兼容的UA
            string userAgent = Array.Find<string>(allHeaderLines, l => l.IndexOf("User-Agent", StringComparison.InvariantCultureIgnoreCase) != -1);
            if (!string.IsNullOrEmpty(userAgent))
            {
                bool isSkipped = false, isSupported = false;
                foreach (SessionUserAgent ua in ServerSessionSupport.ConfigInstance.Agents)
                {
                    ua.RawString = userAgent;
                    isSupported = !ua.IsNotSupported(ref isSkipped);
                    if (!isSkipped && !isSupported)
                    {
                        WriteErrorAndClose(403,
                            string.Format("您的浏览器 {0}/{1}, 版本低于系统要求版本:{2}, 请升级您的浏览器或使用其他浏览器访问！",
                            ua.FriendlyName, ua.CurrentVersion, ua.MinVersion));
                        doNextProcess = false;
                    }

                    #region 设置访问UA标识
                    if (isSkipped)
                    {
                        knownUserAgent = string.Format("[{0}]", userAgent.Trim());
                    }
                    else
                    {
                        knownUserAgent = string.Format("[UA:{0}/{1}]", ua.FriendlyName, ua.CurrentVersion);
                        break;
                    }
                    #endregion
                }
            }
            else
            {
                if (!ServerSessionSupport.ConfigInstance.EnableEmptyUserAgent)
                {
                    WriteErrorAndClose(403, "Error:系统不允许空的用户代理(User-Agent)标识访问！");
                    doNextProcess = false;
                }
            }
            #endregion

            //[HTTP] POST /cpl/service/1.3.99.1.3 HTTP/1.1
            ServerLog("* [{4}:{0}][{1}]{3} {2}", Protocol, socketBuffer.Length, firstLineString, knownUserAgent,
                _server.Port);

            if (!doNextProcess) return;
            if (ServerSessionSupport.ConfigInstance.EnableInternalAspxHost)
            {
                processAspxHost();
            }
            else
            {
                if (firstLineString.EndsWith("HTTP/1.1", StringComparison.InvariantCultureIgnoreCase)
                    || firstLineString.EndsWith("HTTP/1.0", StringComparison.InvariantCultureIgnoreCase))
                {
                    WriteErrorAndClose(503);
                }
                else
                {
                    showErrorAndClose("Service Unavailable. (" + Server.ServerName + " v" + Messages.VersionString + ")");
                }
            }
        }


        /// <summary>
        /// 关闭建立的相关连接
        /// </summary>
        public void Close()
        {
            lock (this)
            {
                closeConnection(eventArgs); //接口
            }
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="args">The SocketAsyncEventArgs for the connection.</param>
        private void closeConnection(SocketAsyncEventArgs args)
        {
            lock (this)
            {
                if (state == ConnectionState.Closed)
                    return;

                state = ConnectionState.Closing;
                //Server.Log.DebugFormat("☆{1} {0} @[{2}]", "SocketClose", RemoteEP, _accesTimes);

                try
                {
                    BindingState bdState = args.UserToken as BindingState;
                    Socket socket = bdState.BindSocket;
                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                    }
                    catch { } // throws if client process has already closed
                    socket.Close();
                    socket = null;

                    OnDisconnected(args, bdState.DisconnectedCallback);
                }
                catch (Exception closeEx)
                {
                    Server.Log.Error("* closeConnection Exception", closeEx);
                }

                Dispose(true);  //连接断开后释放资源
                state = ConnectionState.Closed;
            }
        }

        #region IDisposable Members
        private Boolean disposed = false;

        ~Connection()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _currentHttpHeadBytes = null;
                    if (HeartBeartTimer != null)
                    {
                        ExtMethods.StopTimer(HeartBeartTimer);
                        HeartBeartTimer.Dispose();
                        HeartBeartTimer = null;
                    }

                    #region 释放相关资源
                    if (_exchangeStream != null)
                    {
                        releaseExchangeDumpStream();

                        try
                        {
                            if (_exchangeStream != null)
                            {
                                _exchangeStream.Close();
                                _exchangeStream.Dispose();
                            }
                        }
                        catch (Exception) { }
                        finally
                        {
                            _exchangeStream = null;
                        }
                    }
                    #endregion

                    if (eventArgs != null)
                    {
                        eventArgs.AcceptSocket = null;
                        eventArgs.Completed -= ReceivedCompleted;
                        eventArgs = null;
                    }
                }
                disposed = true;
            }
        }
        #endregion

        /// <summary>
        /// 关闭并释放资源
        /// </summary>
        void closeAndDispose()
        {
            Close();
            Dispose(true);
        }
    }

    /// <summary>
    /// 绑定状态对象
    /// </summary>
    internal class BindingState
    {
        /// <summary>
        /// 数据接收回调
        /// </summary>
        public DataReceivedCallback DataReceived;

        /// <summary>
        /// 连接断开回调
        /// </summary>
        public DisconnectedCallback DisconnectedCallback;

        /// <summary>
        /// 绑定套接字
        /// </summary>
        public Socket BindSocket;
    }

    /// <summary>
    /// 连接状态
    /// </summary>
    public enum ConnectionState : int
    {
        /// <summary>
        /// 未设置
        /// </summary>
        UNSetting = 0,

        /// <summary>
        /// 等待接收数据
        /// </summary>
        ReceiveClientData = 1,

        /// <summary>
        /// 处理数据结果
        /// </summary>
        ExecuteForResponse = 2,

        /// <summary>
        /// 正在断开连接
        /// </summary>
        Closing = 3,

        /// <summary>
        /// 连接已关闭
        /// </summary>
        Closed = 4
    }

}