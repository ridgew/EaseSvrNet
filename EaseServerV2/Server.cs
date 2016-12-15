using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting;
using System.ServiceProcess;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using CommonLib;
using EaseServer.Configuration;
using EaseServer.Interface;
using EaseServer.SocketServer;
using log4net;
using Logger = log4net.ILog;

namespace EaseServer
{
    /// <summary>
    /// 服务入口
    /// </summary>
    public class Server : ServiceBase, IServerAPI
    {
        SocketArgsPool socketArgsPool = null;
        BufferPool bufferManager = null;

        /// <summary>
        /// 当前服务器连接总数
        /// </summary>
        int _currentConnCount = 0;
        long messageCount = 0, bytesTotal = 0;

        int maxConnCount = 20000;//最大连接个数
        Timer secondDataChecker = null;

        int _port = 8095;
        int _backLog = 50;
        TcpSocketListener socketListener;
        string _virtualPath, _physicalPath;

        /// <summary>
        /// 获取应用程序名称
        /// </summary>
        internal static string ServerName = typeof(Server).Assembly.GetName().Name;

        /// <summary>
        /// 初始化一个 <see cref="Server"/> class 实例。
        /// </summary>
        /// <param name="port">监听端口</param>
        /// <param name="virtualPath">应用程序虚拟路径</param>
        /// <param name="physicalPath">绑定ASPX服务的物理路径</param>
        public Server(int port, string virtualPath, string physicalPath)
        {
            _backLog = "EaseServer.PendingQueueCount".AppSettings<int>(2000);
            _port = port;
            _virtualPath = virtualPath;

            if (string.IsNullOrEmpty(physicalPath))
                physicalPath = AppDomain.CurrentDomain.BaseDirectory;

            _physicalPath = physicalPath.EndsWith("\\", StringComparison.Ordinal) ? physicalPath : physicalPath + "\\";
        }

        /// <summary>
        /// 配置文件更改后调用
        /// </summary>
        internal static List<Action> ConfigChangeCallBack = new List<Action>();

        #region 会话全局辅助

        /// <summary>
        /// 全部支持会话词典
        /// </summary>
        static readonly ThreadSafeDictionary<string, ServerSession> SupportSeesions = new ThreadSafeDictionary<string, ServerSession>();

        /// <summary>
        /// 动态配置信息
        /// </summary>
        static readonly ThreadSafeDictionary<string, SessionConfig> SupportSessionConfigFetch = new ThreadSafeDictionary<string, SessionConfig>();

        /// <summary>
        /// 所有的监听服务器
        /// </summary>
        static readonly ThreadSafeDictionary<string, Server> AllListenServer = new ThreadSafeDictionary<string, Server>();

        /// <summary>
        /// 服务端会话初始化
        /// </summary>
        void InitialSessionSupport()
        {
            ServerSessionSupport cusSS = ServerSessionSupport.ConfigInstance;
            if (cusSS == null) return;

            Log.InfoFormat("# 重新配置服务器[{5}]: {0} v{1} (AspxHost = {2}, Mixed = {3}, EmptyUserAgent = {4}).",
                Server.ServerName, Messages.VersionString,
                cusSS.EnableInternalAspxHost, cusSS.EnableMixedSession, cusSS.EnableEmptyUserAgent, Port);

            if (cusSS.SupportItems == null) return;

            #region 清空旧的配置
            SupportSessionConfigFetch.Clear();
            SupportSeesions.Clear();
            #endregion

            Type currentType = null;
            Type processType = typeof(IConnectionProcessor);
            for (int i = 0, j = cusSS.SupportItems.Length; i < j; i++)
            {
                if (!cusSS.SupportItems[i].Enable)
                    continue;

                currentType = Type.GetType(cusSS.SupportItems[i].ImplementTypeName, false);
                if (currentType == null ||
                    currentType.GetInterface(processType.FullName, false) == null)
                {
                    throw new System.Configuration.ConfigurationErrorsException("配置错误：会话实现类型[" + cusSS.SupportItems[i].ImplementTypeName + "]不存在或没有实现接口[" + processType.AssemblyQualifiedName + "]！");
                }
                else
                {
                    string key = cusSS.SupportItems[i].Identity;
                    cusSS.SupportItems[i].RuntimeType = currentType;
                    if (SupportSeesions.ContainsKey(key))
                    {
                        SupportSeesions[key] = cusSS.SupportItems[i];
                    }
                    else
                    {
                        SupportSeesions.Add(key, cusSS.SupportItems[i]);
                    }

                    Log.InfoFormat("* 设置支持: {0}, 类型为{1}", key, currentType);
                    if (cusSS.SupportItems[i].Config != null)
                    {
                        //实例配置设置
                        SupportSessionConfigFetch[key] = cusSS.SupportItems[i].Config;
                        Log.InfoFormat("* 设置会话配置信息: {0}", key);
                    }
                }
            }

        }

        /// <summary>
        /// 获取首个响应式处理模块
        /// </summary>
        internal static IConnectionProcessor GetAppliedProcessor(IServerConnection conn)
        {
            IConnectionProcessor processor = null;
            ServerSession sSession = null;
            foreach (var key in SupportSeesions.Keys)
            {
                sSession = SupportSeesions[key];
                //跳过应答式处理模块
                if (!sSession.IsResponse) continue;

                processor = Activator.CreateInstance(sSession.RuntimeType) as IConnectionProcessor;
                if (processor != null)
                {
                    processor.ServerConnection = conn;
                    ApplyConfig(key, processor);
                    Log.DebugFormat("* 会话处理: {0} => {1}, 保持连接:{2}", key, sSession.RuntimeType.FullName, processor.SocketMode);
                }
                break;
            }
            return processor;
        }

        static void ApplyConfig(string configKey, IConnectionProcessor processor)
        {
            if (SupportSessionConfigFetch.ContainsKey(configKey))
            {
                if (!SupportSessionConfigFetch[configKey].ConfigOnce
                    || !SupportSessionConfigFetch[configKey].HasConfiged)
                {
                    processor.ConfigInstance(SupportSessionConfigFetch[configKey]);
                    Log.DebugFormat("* 会话配置信息调用{0}", configKey);
                    SupportSessionConfigFetch[configKey].HasConfiged = true;
                }
            }
        }

        /// <summary>
        /// 获取首个应答式处理模块
        /// </summary>
        internal static IConnectionProcessor GetRespectiveProcess(IServerConnection conn, byte[] firstReadBytes)
        {
            ServerSession sSession = null;
            IConnectionProcessor tProcess = null;
            foreach (var key in SupportSeesions.Keys)
            {
                sSession = SupportSeesions[key];
                //跳过响应式处理模块
                if (sSession.IsResponse) continue;

                tProcess = Activator.CreateInstance(sSession.RuntimeType) as IConnectionProcessor;
                if (tProcess != null)
                {
                    if (!tProcess.AcceptConnection(firstReadBytes))
                    {
                        tProcess = null;
                    }
                    else
                    {
                        tProcess.ServerConnection = conn;
                        ApplyConfig(key, tProcess);
                        Log.DebugFormat("* 会话处理: {0} => {1}, 保持连接:{2}", key, sSession.RuntimeType.FullName, tProcess.SocketMode);
                        break;
                    }
                }
            }
            return tProcess;
        }

        /// <summary>
        /// 获取所有客户端连接
        /// </summary>
        ThreadSafeDictionary<string, IServerConnection> _allConnections = new ThreadSafeDictionary<string, IServerConnection>(StringComparer.InvariantCultureIgnoreCase);

        #endregion

        #region IServerAPI 成员

        /// <summary>
        /// 获取服务监听端口
        /// </summary>
        /// <value></value>
        public int Port { get { return _port; } }

        /// <summary>
        /// 允许的最大连接数
        /// </summary>
        /// <value></value>
        public int MaxClientCount { get { return maxConnCount; } }

        /// <summary>
        /// 获取服务端所支持的所有会话标识
        /// </summary>
        /// <returns></returns>
        public string[] GetSupportSessionKeys()
        {
            List<string> keyList = new List<string>();
            ServerSession ss = null;
            foreach (var key in SupportSeesions.Keys)
            {
                ss = SupportSeesions[key];
                keyList.Add(string.Format("{0} => {2}{1}", key, ss.ImplementTypeName, ss.IsResponse ? "(主动应答式)" : ""));
            }
            return keyList.ToArray();
        }

        /// <summary>
        /// 当前服务实例启动时间
        /// </summary>
        public DateTime StartDateTime { get; private set; }

        /// <summary>
        /// 获取服务名称
        /// </summary>
        public string GetServerName() { return Server.ServerName; }

        /// <summary>
        /// 获取服务版本
        /// </summary>
        /// <returns></returns>
        public string GetServerVersion() { return Messages.VersionString; }

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

        /// <summary>
        /// Gets the virtual path.
        /// </summary>
        /// <value>The virtual path.</value>
        public string VirtualPath { get { return _virtualPath; } }

        /// <summary>
        /// ASPX服务承载物理路径
        /// </summary>
        /// <value></value>
        public string PhysicalPath { get { return _physicalPath; } }

        /// <summary>
        /// 获取HTTP访问的跟目录
        /// </summary>
        public string RootUrl
        {
            get
            {
                if (_port != 80)
                {
                    return "http://localhost:" + _port + _virtualPath;
                }
                else
                {
                    return "http://localhost" + _virtualPath;
                }
            }
        }

        /// <summary>
        /// 当前服务的所有连接总数
        /// </summary>
        /// <value></value>
        public int ConnectionCount
        {
            get { return _currentConnCount; }
        }

        /// <summary>
        /// 强制断开指定客户端
        /// </summary>
        /// <param name="clientEndPointStr">客户端的ip:port标识字符串</param>
        public void DisconnectClient(string clientEndPointStr)
        {
            IServerConnection conn = null;
            if (_allConnections.TryGetValue(clientEndPointStr, out conn))
            {
                ExtensionUtil.CatchAll(() => { conn.Close(); conn.Dispose(); });
                _allConnections.RemoveSafe(clientEndPointStr);  //断开特定连接端点
            }
        }

        /// <summary>
        /// 使用正则匹配模式断开符合条件的客户端
        /// </summary>
        /// <param name="clientEndPattern">正则匹配模式</param>
        public void DisconnectBatchClient(string clientEndPattern)
        {
            List<string> cKeys = new List<string>();
            cKeys.AddRange(_allConnections.Keys); //复制所有连接副本

            bool matchProtocol = clientEndPattern.StartsWith("[") && clientEndPattern.EndsWith("]");
            if (matchProtocol)
                clientEndPattern = clientEndPattern.Trim('[', ']');

            for (int i = 0, j = cKeys.Count; i < j; i++)
            {
                if (clientEndPattern == "*"
                    || (matchProtocol && _allConnections[cKeys[i]].Protocol.Equals(clientEndPattern, StringComparison.InvariantCultureIgnoreCase))
                    || System.Text.RegularExpressions.Regex.IsMatch(cKeys[i], clientEndPattern))
                {
                    DisconnectClient(cKeys[i]);
                }
            }
        }

        /// <summary>
        /// 以索引分页方式获取客户端连接集合
        /// </summary>
        /// <param name="startIdx">起始索引(0开始)</param>
        /// <param name="pageSize">每次显示多少条</param>
        /// <param name="totalClient">当前总共有多少客户端</param>
        /// <returns></returns>
        public IServerConnection[] GetConnectionList(int startIdx, int pageSize, out int totalClient)
        {
            List<IServerConnection> rList = new List<IServerConnection>();
            var er = _allConnections.Values.GetEnumerator(); //获取所有连接枚举值
            int curIdx = 0;
            int endIdx = startIdx + pageSize - 1;

            while (curIdx <= endIdx && er.MoveNext())
            {
                if (er.Current != null && curIdx >= startIdx)
                {
                    rList.Add(er.Current);
                }
                curIdx++;
            }

            totalClient = _allConnections.Count;
            return rList.ToArray();
        }

        /// <summary>
        /// 按照提供的客户端匹配模式列出客户端
        /// </summary>
        /// <param name="clientPattern">客户端匹配模式</param>
        /// <param name="listHandler">列表项委托</param>
        public void ListClientStatus(string clientPattern, ListClientWriter listHandler)
        {
            int j = _allConnections.Count;
            if (j < 1)
            {
                listHandler("还没有客户端连接！");
            }
            else
            {
                int i = 0;
                bool matchProtocol = clientPattern.StartsWith("[") && clientPattern.EndsWith("]");
                if (matchProtocol)
                    clientPattern = clientPattern.Trim('[', ']');

                foreach (var tKey in _allConnections.Keys)
                {
                    i++;
                    IServerConnection item = _allConnections[tKey];

                    if (clientPattern == "*"
                        || (matchProtocol && item.Protocol.Equals(clientPattern, StringComparison.InvariantCultureIgnoreCase))
                        || System.Text.RegularExpressions.Regex.IsMatch(tKey, clientPattern))
                    {
                        listHandler("{0}:{1} <{2}> Connect:{3:M-d HH:mm:ss,fff} Active:{4:M-d HH:mm:ss,fff} Mode:{5}",
                            i,
                            item.Protocol,
                            item.RemoteEP,
                            item.ConnectedTime,
                            item.LastInteractive,
                            item.SocketMode);
                    }
                }
            }
        }

        /// <summary>
        /// 建立客户端连接
        /// </summary>
        /// <param name="conn">客户端连接</param>
        public void RegisterClient(IServerConnection conn)
        {
            _allConnections.MergeSafe(conn.RemoteEP, conn);
        }

        /// <summary>
        /// 取消建立客户端连接
        /// </summary>
        /// <param name="conn">客户端连接</param>
        public void UnRegisterClient(IServerConnection conn)
        {
            lock (this)
            {
                _currentConnCount = Interlocked.Decrement(ref _currentConnCount); //新客户端断开
                _allConnections.RemoveSafe(conn.RemoteEP);
            }
        }

        #endregion

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            Start();
        }

        void prepareServerData()
        {
            maxConnCount = "EaseServer.MaxConnectionCount".AppSettings<int>(20000);
            int eachBufferSize = "EaseServer.ConnectionBufferSize".AppSettings<int>(4096);

            socketArgsPool = new SocketArgsPool(maxConnCount);
            bufferManager = new BufferPool(maxConnCount, eachBufferSize);
        }

        string formatBytes(long size)
        {
            string[] units = new string[] { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            float calcSize = Convert.ToSingle(size);
            for (i = 0; calcSize >= 1024f && i < 4; i++) calcSize /= 1024f;
            return Math.Round(calcSize, 2) + units[i];
        }

        /// <summary>
        /// Socket监听开始
        /// </summary>
        public void Start()
        {
            prepareServerData();
            Log.LoggingNow();

            socketListener = new TcpSocketListener(IPAddress.Any, _port, _backLog);
            socketListener.SocketConnected += socketListener_SocketConnected;
            socketListener.Start();

            string crtSvrKey = "#" + _port.ToString();
            if (!AllListenServer.ContainsKey(crtSvrKey))
            {
                AllListenServer.Add(crtSvrKey, this);
            }

            StartDateTime = DateTime.Now;
            //会话支持
            if (SupportSeesions.Count < 1)
                InitialSessionSupport(); //初始话会话支持信息

            //检测配置信息修改
            FileDependency refreshAppDc = new FileDependency(Assembly.GetExecutingAssembly().Location + ".config");
            refreshAppDc.OnFileChange += new FileDependency.FileChange(refreshAppDc_OnFileChange);

            secondDataChecker = new Timer(new TimerCallback(s => //每秒状态操作
            {
                if (messageCount > 0 && bytesTotal > 0)
                {
                    Log.InfoFormat("* [" + DateTime.Now.AddSeconds(-1).ToString("HH:mm:ss") + "]服务连接总数{0}, 可用连接数{3}, 处理{1}个消息/{2}数据。",
                        _currentConnCount, messageCount, formatBytes(bytesTotal), socketArgsPool.Available);
                    messageCount = bytesTotal = 0;
                }

            }), this, 0, 1000);

            Log.InfoFormat("* 服务已启动……");
        }

        /// <summary>
        /// 配置信息的最新修改时间
        /// </summary>
        public DateTime LastChangeDateTime = DateTime.Now;
        /// <summary>
        /// 当前是否正在刷新配置信息
        /// </summary>
        volatile bool onChangeConfig = false;

        void refreshAppDc_OnFileChange(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                //限制为至少超过一秒
                if (DateTime.Now.Subtract(LastChangeDateTime).Seconds > 1)
                {
                    if (onChangeConfig) return;

                    onChangeConfig = true;
                    //等待不再更新后n秒
                    WaittingOnLeave(e.FullPath, 2);

                    bool hasError = false;
                    int tryTimes = 0;
                    Exception errExp = null;

                    #region 挂接更新回调
                    foreach (Action fire in ConfigChangeCallBack)
                    {
                        try
                        {
                            fire();
                        }
                        catch { }
                    }
                    #endregion

                    #region 至少重试5次
                    do
                    {
                        try
                        {
                            CommonLib.ExtensionUtil.RefreshAppConfig("");
                            ServerSessionSupport.ConfigInstance.Refresh();
                        }
                        catch (Exception ex)
                        {
                            hasError = true;
                            errExp = ex;
                        }
                        finally
                        {
                            tryTimes++;
                            if (hasError) Thread.Sleep(1000);
                        }
                    }
                    while (hasError == true && tryTimes <= 5);
                    #endregion

                    onChangeConfig = false;
                    if (!hasError)
                        InitialSessionSupport(); //配置文件修改、重新配置会话支持信息

                    Log.InfoFormat("* 应用程序配置信息已更新[{2}]，上次修改时间为：{0}，处理消息为：{1}。",
                        LastChangeDateTime,
                        hasError ? errExp.Message : "[刷新成功]",
                        tryTimes);

                    LastChangeDateTime = DateTime.Now;

                }
            }
        }

        /// <summary>
        /// 等待正在更新的文件达到一定秒数间隔
        /// </summary>
        void WaittingOnLeave(string filePath, int wattingSeconds)
        {
            FileInfo fi = new FileInfo(filePath);
            if (fi.Exists)
            {
                while ((DateTime.Now - fi.LastWriteTime).TotalSeconds < wattingSeconds)
                {
                    Thread.Sleep(1000);
                    fi.Refresh();
                }
            }
        }

        /// <summary>
        /// 在派生类中实现时，该方法于系统即将关闭时执行。该方法指定应在系统即将关闭前执行的处理。
        /// </summary>
        protected override void OnShutdown()
        {
            StopService();
        }

        /// <summary>
        /// 在派生类中实现时，该方法于“服务控制管理器”(SCM) 将“停止”命令发送到服务时执行。指定服务停止运行时采取的操作。
        /// </summary>
        protected override void OnStop()
        {
            StopService();
        }

        /// <summary>
        /// 关闭当前监听服务
        /// </summary>
        public void StopService()
        {
            _shutdownInProgress = true;
            Log.StopLogging();

            AllListenServer.RemoveSafe("#" + _port.ToString());
            int cTotal = _allConnections.Count;

            //连接全部断开
            while (cTotal > 0)
            {
                var er = _allConnections.Values.GetEnumerator();
                if (er.MoveNext())
                {
                    if (er.Current != null)
                    {
                        er.Current.Close(); //服务停止时关闭
                        er.Current.Dispose();
                    }
                    _allConnections.Remove(er.Current.RemoteEP);
                }
                cTotal--;
            }

            if (socketArgsPool != null)
            {
                socketArgsPool.Dispose();
                socketArgsPool = null;
            }

            if (socketListener != null)
            {
                socketListener.SocketConnected -= socketListener_SocketConnected;
                socketListener.Stop();
                socketListener.Dispose();
                socketListener = null;
            }

            try
            {
                if (_host != null) _host.Shutdown();
                while (_host != null)
                {
                    Thread.Sleep(100);
                }
            }
            catch { }
            finally { _host = null; }
        }

        /// <summary>
        /// Socket连接建立
        /// </summary>
        /// <param name="sender">TcpSocketListener对象</param>
        /// <param name="e"></param>
        void socketListener_SocketConnected(object sender, SocketEventArgs e)
        {
            TcpSocketListener listener = (TcpSocketListener)sender;
            if (_shutdownInProgress && e != null && e.Socket != null)
            {
                try
                {
                    e.Socket.Close();
                    e.Socket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                return;
            }
            if (!listener.IsRunning || e == null || e.Socket == null) return;

            Socket acceptedSocket = e.Socket;
            lock (this)
            {
                _currentConnCount = Interlocked.Increment(ref _currentConnCount);  //新客户端连接

                #region Socket连接建立
                SocketAsyncEventArgs args = socketArgsPool.CheckOut();
                bufferManager.CheckOut(args);
                args.RemoteEndPoint = acceptedSocket.RemoteEndPoint;

                //args.DisconnectReuseSocket = true; //允许重用
                //Server.Log.DebugFormat("☆{1} {0}", "SocketConnected", acceptedSocket.RemoteEndPoint);
                byte[] optionInValue = FillKeepAliveStruct(1, 10000, 2000); //设定2秒钟检测一次，超过10秒检测失败时抛出异常。
                acceptedSocket.IOControl(IOControlCode.KeepAliveValues, optionInValue, null);
                acceptedSocket.Blocking = true;                             //NetworkStream使用阻塞模式
                acceptedSocket.LingerState = new LingerOption(false, 0);    //不延迟关闭连接

                Connection conn = null;
                string dumpConfig = ConfigurationManager.AppSettings[ServerName + ".DumpAccess"];
                if (dumpConfig != null && Enum.IsDefined(typeof(FileAccess), dumpConfig))
                {
                    string sid = Connection.NewSessionId();
                    string dumpFormat = ConfigurationManager.AppSettings[ServerName + ".DumpFormat"];
                    DumpFormat format = DumpFormat.Binary;
                    if (dumpFormat != null && Enum.IsDefined(typeof(DumpFormat), dumpFormat))
                        format = (DumpFormat)Enum.Parse(typeof(DumpFormat), dumpFormat);

                    Stream mStream = new FileDumpStream("dump", sid, format);
                    conn = new Connection(this, acceptedSocket, args, sid, (FileAccess)Enum.Parse(typeof(FileAccess), dumpConfig), mStream,
                        new DataReceivedCallback(DataReceived), new DisconnectedCallback(Disconnected));
                }
                else
                {
                    conn = new Connection(this, acceptedSocket, args, new DataReceivedCallback(DataReceived), new DisconnectedCallback(Disconnected));
                }

                RegisterClient(conn); //客户端连接已建立
                conn.Connect();
                #endregion
            }

        }

        /// <summary>
        /// FillKeepAliveStruct 得到Keep-Alive结构值
        /// </summary>
        /// <param name="onOff">是否启用Keep-Alive</param>
        /// <param name="keepAliveTimeInMSec">最大存活时间ms</param>
        /// <param name="keepAliveIntervalInMSec">探测时间间隔ms</param>
        /// <returns></returns>
        byte[] FillKeepAliveStruct(int onOff, int keepAliveTimeInMSec, int keepAliveIntervalInMSec)
        {
            byte[] array = new byte[12];
            BitConverter.GetBytes(onOff).CopyTo(array, 0);
            BitConverter.GetBytes(keepAliveTimeInMSec).CopyTo(array, 4);
            BitConverter.GetBytes(keepAliveIntervalInMSec).CopyTo(array, 8);
            return array;
        }

        /// <summary>
        /// 接收到客户端数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void DataReceived(Connection sender, DataEventArgs e)
        {
            messageCount = Interlocked.Increment(ref messageCount);
            bytesTotal = Interlocked.Add(ref bytesTotal, e.Data.Length);
            //Server.Log.DebugFormat("☆{1} {0}", "DataReceived", e.RemoteEndPoint);
            sender.ProcessReceivedData(e.Data);
        }

        /// <summary>
        /// 客户端连接断开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Disconnected(Connection sender, SocketAsyncEventArgs e)
        {
            UnRegisterClient(sender); //从服务器端断开

            e.AcceptSocket = null;
            e.RemoteEndPoint = null;

            bufferManager.CheckIn(e);
            socketArgsPool.CheckIn(e);
        }

        /// <summary>
        /// 当前是否正在关闭
        /// </summary>
        bool _shutdownInProgress;

        /// <summary>
        /// 判断是否正在关闭服务器
        /// </summary>
        /// <returns></returns>
        public bool IsShuttingDown() { return _shutdownInProgress; }

        #region ASPXHosting

        Host _host;

        /// <summary>
        /// 当前ASP.NET服务宿主已关闭
        /// </summary>
        public void HostStopped() { _host = null; }

        /// <summary>
        /// 宿主服务锁
        /// </summary>
        object hostLock = new object();

        internal Host GetHost()
        {
            if (_shutdownInProgress)
                return null;

            Host host = _host;
            if (host == null)
            {
                lock (hostLock)
                {
                    host = _host;
                    if (host == null)
                    {
                        string DirectoryListSetting = ConfigurationManager.AppSettings[Server.ServerName + ".DirectoryList"] ?? "false";
                        host = (Host)CreateWorkerAppDomainWithHost(_virtualPath, _physicalPath, typeof(Host));
                        host.Configure(this, _port, _virtualPath, _physicalPath);
                        host.EnableDirectoryList = Convert.ToBoolean(DirectoryListSetting);
                        _host = host;

                        //避免线程池设置遭修改
                        SynThreadPoolSeting();
                    }
                }
            }
            return host;
        }

        /// <summary>
        /// Creates the worker app domain with host.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <param name="physicalPath">The physical path.</param>
        /// <param name="hostType">Type of the host.</param>
        /// <returns></returns>
        static object CreateWorkerAppDomainWithHost(string virtualPath, string physicalPath, Type hostType)
        {
            // this creates worker app domain in a way that host doesn't need to be in GAC or bin
            // using BuildManagerHost via private reflection
            string uniqueAppString = string.Concat(virtualPath, physicalPath).ToLowerInvariant();
            string appId = string.Concat((uniqueAppString.GetHashCode()).ToString("x", CultureInfo.InvariantCulture), "@", ServerName);

            // create BuildManagerHost in the worker app domain
            var appManager = ApplicationManager.GetApplicationManager();
            var buildManagerHostType = typeof(HttpRuntime).Assembly.GetType("System.Web.Compilation.BuildManagerHost");
            var buildManagerHost = appManager.CreateObject(appId, buildManagerHostType, virtualPath, physicalPath, false);

            // call BuildManagerHost.RegisterAssembly to make Host type loadable in the worker app domain
            buildManagerHostType.InvokeMember("RegisterAssembly",
                BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic,
                null, buildManagerHost,
                new object[2] { hostType.Assembly.FullName, hostType.Assembly.Location });

            // create Host in the worker app domain
            return appManager.CreateObject(appId, hostType, virtualPath, physicalPath, false);
        }

        /// <summary>
        /// 同步线程池设置
        /// </summary>
        internal static void SynThreadPoolSeting()
        {
            #region 线程池使用设置
            string threadPoolSetting = ConfigurationManager.AppSettings["ThreadPool.SetMaxThreads"];
            int[] iThreadSetArr = new int[2];
            if (!string.IsNullOrEmpty(threadPoolSetting))
            {
                iThreadSetArr = Array.ConvertAll<string, int>(threadPoolSetting.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries), s => Convert.ToInt32(s));
                System.Threading.ThreadPool.SetMaxThreads(iThreadSetArr[0], iThreadSetArr[1]);
            }
            else
            {
                System.Threading.ThreadPool.SetMaxThreads(500, 1000);
            }
            threadPoolSetting = ConfigurationManager.AppSettings["ThreadPool.SetMinThreads"];
            if (!string.IsNullOrEmpty(threadPoolSetting))
            {
                iThreadSetArr = Array.ConvertAll<string, int>(threadPoolSetting.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries), s => Convert.ToInt32(s));
                System.Threading.ThreadPool.SetMinThreads(iThreadSetArr[0], iThreadSetArr[1]);
            }
            else
            {
                System.Threading.ThreadPool.SetMaxThreads(10, 200);
            }
            #endregion
        }

        // called at the end of request processing
        // to disconnect the remoting proxy for Connection object
        // and allow GC to pick it up
        public void OnRequestEnd(Connection conn) { RemotingServices.Disconnect(conn); }
        #endregion

        /// <summary>
        /// [静态方法]关闭所有监听服务
        /// </summary>
        public static void StopAllServer()
        {
            Program.RuningHandler.Set();

            while (AllListenServer.Count > 0)
            {
                IEnumerator<string> er = AllListenServer.Keys.GetEnumerator();
                if (er.MoveNext())
                {
                    AllListenServer[er.Current].Stop();
                    AllListenServer.RemoveSafe(er.Current);
                }
            }
        }

        #region 日志队列封装
        /// <summary>
        /// 日志记录级别
        /// </summary>
        enum LogLevel : byte
        {
            /// <summary>
            /// 全部记录
            /// </summary>
            ALL = 255,
            /// <summary>
            /// 调试
            /// </summary>
            DEBUG = 3,
            /// <summary>
            /// 消息
            /// </summary>
            INFO = 2,
            /// <summary>
            /// 错误
            /// </summary>
            ERROR = 1,
            /// <summary>
            /// 无日志
            /// </summary>
            NONE = 0
        }

        /// <summary>
        /// 日志队列封装
        /// </summary>
        internal static class Log
        {
            /// <summary>
            /// 日志辅助
            /// </summary>
            static readonly Logger logger = LogManager.GetLogger(typeof(Server));

            /// <summary>
            /// 日志队列
            /// </summary>
            static LockFreeQueue<Action> LogQue = new LockFreeQueue<Action>();

            static Timer secondDataChecker = null;

            /// <summary>
            /// 当前是否正在写日志
            /// </summary>
            static volatile bool Logging = false;

            /// <summary>
            /// 更新日志级别
            /// </summary>
            static void RefreshLogLevel()
            {
                lock (typeof(Log))
                {
                    Level = (LogLevel)Enum.Parse(typeof(LogLevel), "EaseServer.ServerLogLevel".AppSettings<string>("INFO"));
                }
            }

            /// <summary>
            /// 设置日志级别
            /// </summary>
            volatile static LogLevel Level = LogLevel.INFO;

            static Log()
            {
                ConfigChangeCallBack.Add(RefreshLogLevel);
                LoggingNow();
                RefreshLogLevel();
            }

            public static void DebugFormat(string format, params object[] args)
            {
                if ((Byte)Level >= (byte)LogLevel.DEBUG)
                    LogQue.Enqueue(() => logger.DebugFormat(format, args));
            }

            public static void InfoFormat(string format, params object[] args)
            {
                if ((Byte)Level >= (byte)LogLevel.INFO)
                    LogQue.Enqueue(() => logger.InfoFormat(format, args));
            }

            public static void ErrorFormat(string format, params object[] args)
            {
                if ((Byte)Level >= (byte)LogLevel.ERROR)
                    LogQue.Enqueue(() => logger.ErrorFormat(format, args));
            }

            public static void Error(object msg)
            {
                Error(msg, null);
            }

            public static void Error(object msg, Exception exception)
            {
                if ((Byte)Level >= (byte)LogLevel.ERROR)
                {
                    if (exception == null)
                        LogQue.Enqueue(() => logger.Error(msg));
                    else
                        LogQue.Enqueue(() => logger.Error(msg, exception));
                }
            }

            /// <summary>
            /// 开始记录日志
            /// </summary>
            public static void LoggingNow()
            {
                if (secondDataChecker != null || Logging)
                    return;

                secondDataChecker = new Timer(new TimerCallback(q => //每秒状态操作
                {
                    LockFreeQueue<Action> Que = (LockFreeQueue<Action>)q;
                    if (!Que.IsEmpty && !Logging)
                    {
                        Logging = true;
                        Action fire = null;
                        int count = 0;
                        while (Que.TryDequeue(out fire))
                        {
                            try
                            {
                                fire();
                            }
                            catch (Exception logEx)
                            {
                                logger.Error("QueLog Exception:", logEx);
                            }

                            count++;
                            if (count % 20 == 0)
                            {
                                Thread.Sleep(50);
                                count = 0;
                            }
                        }
                    }
                    Logging = false;
                }), LogQue, 0, 1000);
            }

            /// <summary>
            /// 停止日志记录
            /// </summary>
            public static void StopLogging()
            {
                if (secondDataChecker == null || !Logging)
                    return;

                if (secondDataChecker != null)
                {
                    ExtMethods.StopTimer(secondDataChecker);
                    if (!LogQue.IsEmpty)
                        logger.InfoFormat("* 还有{0}条日志未写入。", LogQue.Count);
                    secondDataChecker.Dispose();
                    secondDataChecker = null;
                }
            }

        }
        #endregion

    }
}
