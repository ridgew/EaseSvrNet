/* **********************************************************************************
 *
 * Copyright (c) Microsoft Corporation. All rights reserved.
 *
 * This source code is subject to terms and conditions of the Microsoft Public
 * License (Ms-PL). A copy of the license can be found in the license.htm file
 * included in this distribution.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * **********************************************************************************/

using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Web;
using CommonLib;
using EaseServer.Configuration;
using EaseServer.Interface;
using EaseServer.Performance;
using EaseServer.Security;

namespace EaseServer
{
    /// <summary>
    /// ��������Ӷ���,����Socket�����Ϣ��
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
    public class Connection : MarshalByRefObject, IServerConnection, IDisposable
    {
        Server _server; Socket _serverSocket;
        string _localServerIP;

        /// <summary>
        /// ���罻���ֽ�����
        /// </summary>
        Stream _exchangeStream = null;

        // raw request data
        public const int MaxHeaderBytes = 8 * 1024;

        /// <summary>
        /// ��ʼ��һ�� <see cref="Connection"/> class ʵ����
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="socket">The socket.</param>
        public Connection(Server server, Socket socket)
            : this(server, socket, NewSessionID(), FileAccess.ReadWrite, null)
        {

        }

        /// <summary>
        /// ��ʼ��һ�� <see cref="Connection"/> class ʵ����ָ��������ѡ�
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="socket">The socket.</param>
        /// <param name="seesionID">The seesion ID.</param>
        /// <param name="monitorAccess">The monitor access.</param>
        /// <param name="monitorDump">The monitor dump.</param>
        public Connection(Server server, Socket socket, string seesionID, FileAccess monitorAccess, Stream monitorDump)
        {
            IsSecured = false;

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
            _exchangeStream = sms;
            InitialIP();
        }


        /// <summary>
        /// ��ʼ��һ�� <see cref="Connection"/> class ʵ����
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="socket">The socket.</param>
        /// <param name="monitorAccess">The monitor access.</param>
        /// <param name="monitorDump">The monitor dump.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="requireClientCert">if set to <c>true</c> [require client cert].</param>
        public Connection(Server server, Socket socket, FileAccess monitorAccess, Stream monitorDump,
            X509Certificate certificate, SslProtocols protocol, bool requireClientCert)
        {
            IsSecured = true;

            _server = server;
            _serverSocket = socket;
            _sid = NewSessionID();

            SocketMonitorStream sms = new SocketMonitorStream(socket, FileAccess.ReadWrite, false);
            if (monitorDump != null)
            {
                sms.RecordAccess = monitorAccess;
                sms.DumpStream = monitorDump;
            }

            ClientCertificate clientCertificate = null;
            SslStream sslStream = new SslStream(sms, false, delegate(object sender, X509Certificate receivedCertificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    clientCertificate = new ClientCertificate(receivedCertificate, chain, sslPolicyErrors);
                    return !(requireClientCert && receivedCertificate == null);
                }
            );

            try
            {
                sslStream.AuthenticateAsServer(certificate, requireClientCert, protocol, false);
            }
            catch (IOException err)
            {
                Server.Log.Error(err);
            }
            catch (ObjectDisposedException err)
            {
                Server.Log.Error(err);
            }
            catch (AuthenticationException err)
            {
                Server.Log.Error(err);
            }

            _clientCertificate = clientCertificate;
            _exchangeStream = sslStream;
            InitialIP();
        }

        /// <summary>
        /// ��ȡ�ͻ���������ͨ�ŵ��ֽ����з�װ(�����ֽ�����)
        /// </summary>
        public Stream ExchangeStream
        {
            get { return _exchangeStream; }
        }

        /// <summary>
        /// ��ǰSocket�ϵĻ�������
        /// </summary>
        protected byte[] socketBuffer = new byte[MaxHeaderBytes];

        /// <summary>
        /// ����Socket�Ϸ��͵��ֽ���
        /// </summary>
        public byte[] SocketBufferData
        {
            get { return socketBuffer; }
            internal set { socketBuffer = value; }
        }

        /// <summary>
        /// ��ȡ��ǰ���ӵ�Socket����
        /// </summary>
        public Socket GetClientSocket() { return _serverSocket; }

        /// <summary>
        /// ����ͨ�Ŵ���
        /// </summary>
        internal int _accesTimes = 0;
        /// <summary>
        /// �ж��Ƿ����״λ�ȡSocket�������������
        /// </summary>
        public bool IsFirstAccess { get { return _accesTimes == 1; } }

        /// <summary>
        /// ���ӵ��λỰSocket�ϵ����ݷ��ʹ���
        /// </summary>
        public virtual void IncrementAccessCount() { _accesTimes++; }

        /// <summary>
        /// ����Socket�ϵĵ������ݶ�ȡ����
        /// </summary>
        public virtual void ResetAccessCount() { _accesTimes = 0; }

        /// <summary>
        /// ��ȡ����ͨ�Ŵ���
        /// </summary>
        public int GetAccessCount() { return _accesTimes; }

        ClientCertificate _clientCertificate = null;
        /// <summary>
        /// Gets the client's security certificate.
        /// </summary>
        public ClientCertificate ClientCertificate { get { return _clientCertificate; } }

        /// <summary>
        /// Using SSL or other encryption method.
        /// </summary>
        public bool IsSecured { get; internal set; }

        Timer HeartBeartTimer = null;

        #region ����˸���

        /// <summary>
        /// ����Socket�ֽ�����,�����ط����˶����ֽ����ݡ�
        /// </summary>
        /// <param name="clientSocket">�ͻ������ӵ�Socket����</param>
        /// <param name="currentSendBytes">��ǰ�����ֽ�����</param>
        /// <param name="bufferSize">�����ֽ����л���</param>
        /// <param name="hasSendError">�Ƿ���ַ��ʹ���</param>
        /// <param name="senderWriter">Socket�������ݼ��ί��</param>
        /// <returns>�ܹ������˵��ֽ�����</returns>
        int SendSocketFragment(Socket clientSocket, byte[] currentSendBytes, int bufferSize, ref bool hasSendError, ListClientWriter senderWriter)
        {
            int totalSent = 0, curentSent = 0;
            hasSendError = false;
            while (totalSent < currentSendBytes.Length)
            {
                LastInteractive = DateTime.Now; //���ش�������
                if (currentSendBytes.Length - totalSent < bufferSize) bufferSize = currentSendBytes.Length - totalSent;
                try
                {
                    //SocketException: �޷��������һ������ֹ���׽��ֲ���
                    //clientSocket.Blocking = true;
                    curentSent = clientSocket.Send(currentSendBytes, totalSent, bufferSize, SocketFlags.None);
                    if (senderWriter != null)
                        senderWriter("* ���η���:{0}�ֽڣ������СΪ{1}�ֽڡ�", curentSent, bufferSize);
                }
                catch (SocketException err) //��ͻ��˷������ݴ���
                {
                    //��������һ��(10035)
                    if (err.SocketErrorCode == SocketError.WouldBlock)
                    {
                        if (clientSocket != null) continue;
                    }
                    else
                    {
                        hasSendError = true;
                        Server.Log.ErrorFormat("* ��[{2}]�������ݳ��ִ���({0}), {1}", err.SocketErrorCode, err.Message, clientSocket.RemoteEndPoint);
                        break;
                    }
                }
                totalSent += curentSent;
            }
            return totalSent;
        }

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// [��Ҫ�����������]����Ӧ��������ͼ
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
                totalSent = SendSocketFragment(_serverSocket, resp.SessionBuffer.ToArray(), bufferSize, ref hasSendError, Server.Log.DebugFormat);
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

                    curentSent = SendSocketFragment(_serverSocket, currentSendBytes, bufferSize, ref hasSendError, Server.Log.DebugFormat);
                    totalSent += curentSent;
                    sendTimes++;

                    Server.Log.DebugFormat("* [{4}] ��<{0}>����{3}�ֽ�, ��{1}/{2}�ֽ�, ��{5}�ֽ�.",
                        RemoteEP, totalSent, resp.Length,
                        curentSent, protoID,
                        resp.Length - totalSent);

                    if (hasSendError) break;
                }
            }

            sw.Stop();
            Server.Log.DebugFormat("* [{3}] ��<{0}>�ܹ�����{1}/{2}�ֽ�����,����{4}ms,�������[{5}].",
                    RemoteEP, totalSent, resp.Length, protoID,
                    sw.ElapsedMilliseconds, !hasSendError);

        }

        /// <summary>
        /// ��ȡ�����API��¶�ӿ�����
        /// </summary>
        public IServerAPI GetServerAPI() { return _server; }

        /// <summary>
        /// ��ȡ��ǰ�ͻ��˵�����ʱ��
        /// </summary>
        public DateTime ConnectedTime { get; private set; }

        /// <summary>
        /// ��ȡ��ǰ�ͻ��˵�����ʱ��
        /// </summary>
        public DateTime LastInteractive { get; private set; }

        /// <summary>
        /// ��ȡ����ģ��
        /// </summary>
        public ConnectionMode SocketMode
        {
            get
            {
                if (processor == null) return ConnectionMode.Auto;
                return processor.SocketMode;
            }
        }

        private bool _keepAlive = false;
        /// <summary>
        /// �Ƿ񱣳����Ӳ������Ͽ�
        /// </summary>
        public bool KeepAlive
        {
            get { return _keepAlive; }
            set { _keepAlive = value; }
        }

        /// <summary>
        /// �ͻ���Ҫ��ı������ӳ��ȣ���λ�룩
        /// </summary>
        public int KeepAliveSeconds { get; set; }

        int _maxAccessCount = 100;
        /// <summary>
        /// ��Խ�������(Ĭ��20)
        /// </summary>
        public int MaxAccessCount
        {
            get { return _maxAccessCount; }
            set { _maxAccessCount = value; }
        }

        /// <summary>
        /// �ͻ��˶Ͽ�ʱ�Ĳ���
        /// </summary>
        Action<Connection> _fireOnDisconnect = delegate { };

        /// <summary>
        /// ��ȡ�����ÿͻ��˶Ͽ�ʱ�Ĳ���
        /// </summary>
        public event Action<Connection> FireOnDisconnect
        {
            add { _fireOnDisconnect += value; }
            remove { _fireOnDisconnect -= value; }
        }

        /// <summary>
        /// ����������ӽ��յ��ͻ��˷��͹���������ʱ
        /// </summary>
        public event Action<long> OnReceivedData;


        /// <summary>
        /// ��ǰ�������Ӵ������
        /// </summary>
        IConnectionProcessor processor = null;
        /// <summary>
        /// ʹ��processor����ǰ����
        /// <remarks>�ٶ�һ�η�����ȫ����������</remarks>
        /// </summary>
        void processCurrent()
        {
            EndCounter(PerformancePoint.ReceiveData); //���յ��������[����]
            //Debug.Assert(processor != null);
            #region ����ǰ��������
            Action currentAct = null;
            if (processor is IRnRProcessor)
            {
                IRnRProcessor iRnr = (IRnRProcessor)processor;
                #region ����R&Rҵ���߼�
                if (iRnr != null)
                {
                    if (SocketMode == ConnectionMode.SelfHosting)
                    {
                        if (IsFirstAccess)  //��չ��ʽ�����״�����
                        {
                            //�״ν��յ�������
                            iRnr.WriteReqeustBytes(socketBuffer);
                        }
                        else
                        {
                            iRnr.ReadRequestData();
                        }
                        IncrementAccessCount(); //SelfHost => ͨ���������������Ӵ���
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
                    Server.Log.Error("* " + processor.GetType().FullName + "����ʧ�ܣ�", processEx);
                    CloseAndDispose();
                    return;
                }
            }
            #endregion

            if (Connected)
            {
                if (SocketMode == ConnectionMode.SelfHosting)
                {
                    processCurrent(); //�ظ����õ�ǰ����
                }
                else
                {
                    socketBuffer = new byte[MaxHeaderBytes];
                    ReceiveClientData(); //��������ǰ����
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
                    Server.Log.Error("* AspxHost����������ж�أ�");
                }
                catch (Exception hostEx)
                {
                    lastExp = hostEx;
                    Server.Log.Error("* AspxHost��������쳣��", hostEx);
                }
            }
            if (lastExp != null) CloseAndDispose();
        }

        /// <summary>
        /// ����С�����ݲ������ѷ��͵��ֽ���
        /// </summary>
        /// <param name="responseBytes">The response bytes.</param>
        /// <returns></returns>
        int SendData(byte[] responseBytes)
        {
            if (_serverSocket != null && _serverSocket.Connected)
            {
                return _serverSocket.Send(responseBytes, responseBytes.Length, SocketFlags.None);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// ��������ֽڼ����д���ȡ���ֽ�����
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

        void showErrorAndClose(string errorMessage)
        {
            CommonLib.ExtensionUtil.CatchAll(() => SendData(Encoding.Default.GetBytes(errorMessage)));
            CloseAndDispose();
        }

        ConnectionState state = ConnectionState.UnSetting;

        /// <summary>
        /// �첽���տͻ��˷��͵�����
        /// </summary>
        public void ReceiveClientData()
        {
            lock (threadLocker)
            {
                if (state == ConnectionState.ReceiveClientData || state == ConnectionState.Closing)
                    return;

                //ServerDebug("#{0}�����첽������������", Thread.CurrentThread.ManagedThreadId);
                state = ConnectionState.ReceiveClientData;

                BeginCounter(PerformancePoint.ReceiveData); //������������[��ʼ]
                if (_serverSocket != null && _serverSocket.Connected)
                {
                    try
                    {
                        ContextExchangeWrap wrap = ContextExchangeWrap.CreateBufferRef(ref socketBuffer, this, _serverSocket.EndReceive);
                        _serverSocket.BeginReceive(wrap.DataBuffer, 0, wrap.DataBuffer.Length, SocketFlags.None, new AsyncCallback(connectionDataArrived), wrap);

                        //ContextExchangeWrap wrap = ContextExchangeWrap.CreateBufferRef(ref socketBuffer, this, _exchangeStream.EndRead);
                        //_exchangeStream.BeginRead(wrap.DataBuffer, 0, wrap.DataBuffer.Length, new AsyncCallback(connectionDataArrived), wrap);
                    }
                    catch (Exception)
                    {
                        CloseAndDispose();
                    }

                    //if (state == ConnectionState.ReceiveClientData)
                    //    ServerDebug("#{0}�����첽����ɹ�", Thread.CurrentThread.ManagedThreadId);
                }
            }
        }

        /// <summary>
        /// ���첽���յ�����ʱ
        /// </summary>
        static void connectionDataArrived(IAsyncResult arg)
        {
            if (arg == null)
                return;

            ContextExchangeWrap wrap = arg.AsyncState as ContextExchangeWrap;
            if (wrap != null)
            {
                int singleRevLength = 0;
                #region ȷ�����յ�����
                try
                {
                    singleRevLength = wrap.ReceiveEndCallBack(arg);
                }
                catch (Exception) { }
                #endregion

                if (singleRevLength < 1)
                {
                    wrap.SocketConnection.Close();
                    wrap.SocketConnection.Dispose();
                }
                else
                {
                    #region ͬ���߳�����
                    wrap.SocketConnection.SocketBufferData = wrap.DataBuffer;
                    wrap.SocketConnection.LastInteractive = DateTime.Now; //�����������ǰ���ûʱ��
                    wrap.SocketConnection.ProcessReceivedData(singleRevLength);
                    wrap.SocketConnection.LastInteractive = DateTime.Now;//����������ݺ����ûʱ��
                    #endregion
                    wrap.SocketConnection.ReceiveClientData(); //�����ȴ��������ݷ���
                }
            }
        }

        /// <summary>
        /// ��ǰ���ӵ�HTTPͷ������
        /// </summary>
        byte[] _currentHttpHeadBytes = null;
        /// <summary>
        /// �Ƿ���Ҫ���ӻ����ֽ�����
        /// </summary>
        bool _needAppendBuffer = false;

        /// <summary>
        /// HTTPͷ�������ݷָ��ֽ�
        /// </summary>
        internal static readonly byte[] HttpHeadBreakBytes = new byte[] { 13, 10, 13, 10 };

        /// <summary>
        /// �ڵ�ǰ�̴߳�����յ�������
        /// </summary>
        /// <param name="receivLen">���յ������ݳ���</param>
        public void ProcessReceivedData(int receivLen)
        {
            #region �����ѽ��յ������ź�
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

            if (OnReceivedData != null)
                OnReceivedData(receivLen);

            IncrementAccessCount(); //ͨ����������������ӽ�������
            if (_needAppendBuffer)  //���ӷ��͵��ֽ�����
            {
                if (_currentHttpHeadBytes != null)  //����ͷ���ֽڻ�������
                {
                    byte[] newBuffer = new byte[_currentHttpHeadBytes.Length + receivLen];
                    Buffer.BlockCopy(_currentHttpHeadBytes, 0, newBuffer, 0, _currentHttpHeadBytes.Length);
                    Buffer.BlockCopy(socketBuffer, 0, newBuffer, _currentHttpHeadBytes.Length, receivLen);
                    socketBuffer = newBuffer;
                }
            }
            else
            {
                #region ������Ч�Ļ����ֽ�����
                if (receivLen < socketBuffer.Length)
                {
                    byte[] currentReceiveBytes = new byte[receivLen];
                    Array.Copy(socketBuffer, 0, currentReceiveBytes, 0, receivLen);
                    socketBuffer = currentReceiveBytes;
                }
                #endregion
            }

            #region ����ǰ��������
            if (processor != null)
            {
                //Server.Log.DebugFormat("* [{0}] ��{1}�ν��յ�����{2}�ֽ�...", Protocol, AccesTimes, SocketBufferData.Length);
                //Server.Log.DebugFormat("{0}", SocketBufferData.GetHexViewString());
                processCurrent(); //�������еĴ��������д���
            }
            else
            {
                if (IsFirstAccess)  //��û���ҵ��κδ�����ʱ
                {
                    #region �״ν�������
                    MonitorDump(); //����״ζ�ȡ���ֽڻ���
                    BeginCounter(PerformancePoint.ParseProcessor);  //�������Ӵ�����[��ʼ]
                    IConnectionProcessor process = Server.GetRespectiveProcess(this, socketBuffer);
                    if (process != null)
                    {
                        processor = process;
                        EndCounter(PerformancePoint.ParseProcessor); //���ӽ���������[����]
                        //if (process.SocketMode != ConnectionMode.SingleCall) { KeepAlive = true; }
                        processCurrent(); //�״����Ӵ���
                        return;
                    }
                    #endregion
                }

                int? crLFIdx = null;
                string assertHTTPRawHeader = null;
                bool needSendErrorBytes = false;

                #region ����������ΪHTTPͷ������ʱ
                byte[] httpBreakBytes = new byte[] { 13, 10 };
                crLFIdx = socketBuffer.LocateFirst(httpBreakBytes, 0);
                if (!crLFIdx.HasValue) needSendErrorBytes = true;
                if (!needSendErrorBytes)
                {
                    int? headBreakIndex = socketBuffer.LocateFirst(HttpHeadBreakBytes, 0);
                    if (!headBreakIndex.HasValue)
                    {
                        _currentHttpHeadBytes = new byte[socketBuffer.Length];
                        Buffer.BlockCopy(socketBuffer, 0, _currentHttpHeadBytes, 0, socketBuffer.Length); //Ԥ��ͷ���ֽ�����
                        _needAppendBuffer = true;
                    }
                    else
                    {
                        _needAppendBuffer = false;
                        _currentHttpHeadBytes = null;  //���ͷ���ֽ�����
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
                    Server.Log.ErrorFormat("* ���յ���Ч����{0}�ֽڣ�������������ʾΪ\r\n{1}", socketBuffer.Length, socketBuffer.GetHexViewString());
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
        /// ����HTTPͷ���ݽ��д����߼�
        /// </summary>
        void processHttpHeader(string[] allHeaderLines)
        {
            string firstLineString = allHeaderLines[0].Trim();
            string knownUserAgent = string.Empty;
            bool doNextProcess = true;

            #region HTTP���β����ݵ�UA
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
                            string.Format("��������� {0}/{1}, �汾����ϵͳҪ��汾:{2}, �����������������ʹ��������������ʣ�",
                            ua.FriendlyName, ua.CurrentVersion, ua.MinVersion));
                        doNextProcess = false;
                    }

                    #region ���÷���UA��ʶ
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
                    WriteErrorAndClose(403, "Error:ϵͳ������յ��û�����(User-Agent)��ʶ���ʣ�");
                    doNextProcess = false;
                }
            }
            #endregion

            //[HTTP] POST /cpl/service/1.3.99.1.3 HTTP/1.1
            ServerLog("* [{4}:{0}][{1}]{3} {2}", Protocol, socketBuffer.Length, firstLineString, knownUserAgent, _server.Port);

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
        /// �ͻ��������ѷ�����������
        /// </summary>
        bool hasSendClientBytes = false;
        /// <summary>
        /// ͬһʵ��������߳���
        /// </summary>
        object threadLocker = new object();
        /// <summary>
        /// ��ȡͬһ������߳�ͬ����
        /// </summary>
        public object ThreadSynRoot { get { return threadLocker; } }

        /// <summary>
        /// ��ǰʵ������
        /// </summary>
        Type currentType = typeof(Connection);

        /// <summary>
        /// �ȴ����������¼�
        /// </summary>
        ManualResetEvent receiveWaitHandler = new ManualResetEvent(false);

        /// <summary>
        /// ���ӿͻ��˲��첽��������
        /// </summary>
        public void Connect()
        {
            LastInteractive = DateTime.Now; //�������Ӽ��ʱ��
            ConnectedTime = DateTime.Now;

            BeginCounter(PerformancePoint.WholeTime);//������ʼʱ��[��ʼ]
            ReceiveClientData(); //����֮����״ν�������

            #region ��Ĭ�ϵȴ�ʱ������û�з���������Ͽ���ִ��Ӧ��ʽ����
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

            #region �����ʱ��(ÿ����)
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
                    CloseAndDispose(); //���û�ﵽ���Ự�����ر�

            }), idleSeconds, idleSeconds, 1000);
            #endregion

        }
        #endregion

        /// <summary>
        /// ��ȡ���ƴ�ʵ���������ڲ��Ե������ڷ������
        /// </summary>
        /// <returns>
        /// 	<see cref="T:System.Runtime.Remoting.Lifetime.ILease"/> ���͵Ķ������ڿ��ƴ�ʵ���������ڲ��ԡ����Ǵ�ʵ����ǰ�������ڷ������������ڣ�������Ϊ��ʼ��Ϊ <see cref="P:System.Runtime.Remoting.Lifetime.LifetimeServices.LeaseManagerPollTime"/> ���Ե�ֵ���������ڷ������
        /// </returns>
        /// <exception cref="T:System.Security.SecurityException">ֱ�ӵ��÷�û�л����ṹȨ�ޡ�</exception>
        /// <PermissionSet>
        /// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="RemotingConfiguration, Infrastructure"/>
        /// </PermissionSet>
        public override object InitializeLifetimeService()
        {
            // never expire the license
            return null;
        }


        /// <summary>
        /// ��ȡ��ǰ����<see cref="Connection"/> �Ƿ���á�
        /// </summary>
        /// <value><c>true</c> if connected; otherwise, <c>false</c>.</value>
        public bool Connected { get { return (_serverSocket != null && _serverSocket.Connected); } }

        /// <summary>
        /// Gets a value indicating whether this instance is local.
        /// </summary>
        /// <value><c>true</c> if this instance is local; otherwise, <c>false</c>.</value>
        public bool IsLocal
        {
            get
            {
                string remoteIP = RemoteIP;
                if (remoteIP == "127.0.0.1" || remoteIP == "::1")
                    return true;
                return LocalServerIP.Equals(remoteIP);
            }
        }

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

        string localIp, localEP, remoteIp, remoteEP;

        void InitialIP()
        {
            LastInteractive = DateTime.Now; //��ʼ��IP��Ϣ

            IPEndPoint ep = (IPEndPoint)_serverSocket.LocalEndPoint;
            localEP = (ep != null) ? ep.ToString() : "0.0.0.0";
            localIp = (ep != null && ep.Address != null) ? ep.Address.ToString() : "127.0.0.1";

            ep = (IPEndPoint)_serverSocket.RemoteEndPoint;
            remoteEP = (ep != null) ? ep.ToString() : "0.0.0.0";
            remoteIp = (ep != null && ep.Address != null) ? ep.Address.ToString() : "127.0.0.1";
        }

        /// <summary>
        /// �������˵�IP�󶨵�ַ
        /// </summary>
        public string LocalIP { get { return localIp; } }

        /// <summary>
        /// �������˵�IP�󶨶˵���Ϣ
        /// </summary>
        public string LocalEP { get { return localEP; } }

        /// <summary>
        /// �ͻ���Զ��IP��ַ
        /// </summary>
        public string RemoteIP { get { return remoteIp; } }

        /// <summary>
        /// Զ�����Ӷ˵㣬�û����ӿͻ��˵ı�ʶ�����磺192.168.8.119:3456
        /// </summary>
        public string RemoteEP { get { return remoteEP; } }

        /// <summary>
        /// �����µĻỰ��ʶ
        /// </summary>
        public static string NewSessionID()
        {
            return DateTime.Now.ToString("MMdd_HHmmss_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        string _sid = null;
        /// <summary>
        /// ��ȡ����Ӧ������ĻỰ��ʶ
        /// </summary>
        public string SessionID
        {
            get
            {
                if (_sid == null) _sid = NewSessionID();
                return _sid;
            }
        }

        /// <summary>
        /// ��ȡ�����õ�ǰЭ���ֵ��Ĭ��ΪHTTP��
        /// </summary>
        public string Protocol
        {
            get
            {
                if (processor != null) return processor.ProtocolIdentity;
                return "HTTP";
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
        /// ������Ӧͷ�����ַ�ά������״̬
        /// </summary>
        /// <param name="statusCode">��Ӧ״̬��</param>
        /// <param name="moreHeaders">���ӵ�����ͷ��Ϣ</param>
        /// <param name="contentLength">��Ӧ�����峤��</param>
        /// <param name="keepAlive">�Ƿ񱣳�����</param>
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
        /// ͨ���ļ���·����ȡ�ļ�����
        /// </summary>
        internal static String MakeContentTypeHeader(string fileName)
        {
            System.Diagnostics.Debug.Assert(File.Exists(fileName));
            string contentType = null;

            var info = new FileInfo(fileName);
            string extension = info.Extension.ToLowerInvariant();

            switch (extension)
            {

                #region �ı���
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

                #region ͼƬ��
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

                #region ѹ����
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
        /// Reads the request bytes.
        /// </summary>
        /// <param name="maxBytes">The max bytes.</param>
        /// <returns></returns>
        public byte[] ReadRequestBytes(int maxBytes)
        {
            LastInteractive = DateTime.Now; //��ȡδ��ɵ�ͷ������

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
        /// Write100s the continue.
        /// </summary>
        public void Write100Continue()
        {
            WriteEntireResponseFromString(100, null, null, true);
        }

        /// <summary>
        /// ���Դ����������ӵĿͻ��˷�������
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public void WriteBody(byte[] data, int offset, int length)
        {
            try
            {
                LastInteractive = DateTime.Now; //��ͻ��˷�������
                _exchangeStream.Write(data, offset, length);
                LastInteractive = DateTime.Now;//��ͻ��˷�������
            }
            catch (Exception) { }
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
            catch (SocketException) { } //�����ı�����
            finally
            {
                if (!keepAlive)
                {
                    CloseAndDispose();
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
                int fileLen = (int)fs.Length;
                String headers = MakeResponseHeaders(200, contentTypeHeader, fileLen, keepAlive);
                byte[] headBytes = Encoding.UTF8.GetBytes(headers);
                _exchangeStream.Write(headBytes, 0, headBytes.Length);

                byte[] fileBuffer = new byte[4096];
                int bytesRead = 0, totalRead = 0;
                try
                {
                    while ((bytesRead = fs.Read(fileBuffer, 0, fileLen)) > 0)
                    {
                        totalRead += bytesRead;
                        _exchangeStream.Write(fileBuffer, 0, bytesRead);
                    }
                }
                catch (Exception) { }
                completed = totalRead == fileLen;
            }
            catch (SocketException) { } //�����ļ�����
            finally
            {
                if (!keepAlive || !completed)
                    CloseAndDispose();

                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }

        /// <summary>
        /// ���Ӵ�����
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

        //public void WriteErrorWithExtraHeadersAndKeepAlive(int statusCode, string extraHeaders)
        //{
        //    WriteEntireResponseFromString(statusCode, extraHeaders, GetErrorResponseBody(statusCode, null), true);
        //}

        #region ������������־���Ը���
        /// <summary>
        /// ����˼�¼��־(INFO)
        /// </summary>
        public void ServerLog(string format, params object[] args) { Server.Log.InfoFormat(format, args); }

        /// <summary>
        /// ����˼�¼��־(DEBUG)
        /// </summary>
        public void ServerDebug(string format, params object[] args) { Server.Log.DebugFormat(format, args); }

        /// <summary>
        /// ����˼�¼��־(ERROR)
        /// </summary>
        public void ServerError(string format, params object[] args) { Server.Log.ErrorFormat(format, args); }
        #endregion

        #region ���ܼ���ͳ��
        /// <summary>
        /// �������ܼ�����
        /// </summary>
        public void ResetCounter()
        {
            _connCounter = null;
        }

        PerformanceCounter _connCounter = null;
        /// <summary>
        /// ��ȡ��ǰ���ӵ����ܼ�����
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
        /// �ж��Ƿ��������ͳ��
        /// </summary>
        /// <value></value>
        public bool EnablePerformanceCounter
        {
            get { return (currentType.FullName + ".EnablePerformanceCounter").AppSettings<bool>(false); }
        }

        PerformancePoint? _pointSetting;

        /// <summary>
        /// ���ܼ�����ͳ�Ƶ�����
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
        /// ��������ܼ�������
        /// </summary>
        PerfData lastestPerfData = null;

        /// <summary>
        /// ��ʼͳ���ض��ĵ�
        /// </summary>
        /// <param name="point">ͳ�Ƶ�</param>
        /// <param name="appendActions">���ӵ���������</param>
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
        /// ����ͳ���ض��ĵ�
        /// </summary>
        /// <param name="point">ͳ�Ƶ�</param>
        /// <param name="prefixActions">ǰ�õ���������</param>
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
        /// Writes the headers.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="extraHeaders">The extra headers.</param>
        /// <param name="keepAlive">�Ƿ񱣳�����</param>
        public void WriteHeaders(int statusCode, String extraHeaders, bool keepAlive)
        {
            string headers = MakeResponseHeaders(statusCode, extraHeaders, -1, keepAlive);
            try
            {
                byte[] headBytes = Encoding.UTF8.GetBytes(headers);
                _exchangeStream.Write(headBytes, 0, headBytes.Length);
                LastInteractive = DateTime.Now; //��ͻ��˷���HTTPͷ������
            }
            catch (SocketException) { } //HTTPͷ���������
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString() { return string.Format("{2} {1}<=>{0}", RemoteEP, LocalEP, Protocol); }

        /// <summary>
        /// �жϵ�ǰ�����Ƿ��ѹر�
        /// </summary>
        bool _isClosed = false;

        /// <summary>
        /// �رս������������
        /// </summary>
        public void Close()
        {
            _server.UnRegisterClient(this);
            if (_isClosed) return;

            lock (threadLocker)
            {
                state = ConnectionState.Closing;
                if (_isClosed) return;

                if (HeartBeartTimer != null)
                {
                    ExtMethods.StopTimer(HeartBeartTimer);
                    HeartBeartTimer.Dispose();
                    HeartBeartTimer = null;
                }

                if (_serverSocket != null)
                {
                    CommonLib.ExtensionUtil.CatchAll(() => _fireOnDisconnect(this));
                    try
                    {
                        _serverSocket.Shutdown(SocketShutdown.Both);
                        _serverSocket.Close();
                    }
                    catch { }
                    finally
                    {
                        _serverSocket = null;
                    }
                }
                _isClosed = true;
            }
        }


        #region IDisposable ��Ա

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
                    socketBuffer = null;

                    #region �ͷ������Դ
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
                }
                disposed = true;
            }
        }
        #endregion

        /// <summary>
        /// �رղ��ͷ���Դ
        /// </summary>
        internal void CloseAndDispose()
        {
            try
            {
                Close();
            }
            catch (Exception) { }
            finally
            {
                Dispose(true);
            }
        }

        #endregion
    }

    /// <summary>
    /// ���߳̽�����Ϣ�Ķ����װ
    /// </summary>
    internal class ContextExchangeWrap
    {
        private ContextExchangeWrap() { }

        public static ContextExchangeWrap CreateBufferRef(ref byte[] buffer, Connection conn, Func<IAsyncResult, int> callBack)
        {
            ContextExchangeWrap wrap = new ContextExchangeWrap();
            wrap.DataBuffer = buffer;
            wrap.SocketConnection = conn;
            wrap.ReceiveEndCallBack = callBack;
            return wrap;
        }

        /// <summary>
        /// �����������֮��Ļص�
        /// </summary>
        public Func<IAsyncResult, int> ReceiveEndCallBack { get; set; }

        /// <summary>
        /// ��ǰ���Ӷ���
        /// </summary>
        public Connection SocketConnection { get; set; }

        /// <summary>
        /// ��ǰ���ӵ����ݻ���
        /// </summary>
        public byte[] DataBuffer { get; set; }

    }

    /// <summary>
    /// ����״̬
    /// </summary>
    public enum ConnectionState : byte
    {
        /// <summary>
        /// δ����
        /// </summary>
        UnSetting = 0,

        /// <summary>
        /// �ȴ���������
        /// </summary>
        ReceiveClientData = 1,

        /// <summary>
        /// �������ݽ��
        /// </summary>
        ExecuteForResponse = 2,

        /// <summary>
        /// ���ڶϿ�����
        /// </summary>
        Closing = 3
    }
}
