/**********************************************
 * $Id: EaseServer.cs 1429 2010-12-29 02:20:44Z wangqj $
 * $Author: wangqj $
 * $Revision: 1429 $
 * $LastChangedRevision: 1429 $
 * $LastChangedDate: 2010-12-29 10:20:44 +0800 (Wed, 29 Dec 2010) $
 ***********************************************/
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Remoting;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using CommonLib;
using EaseServer.Configuration;
using EaseServer.Interface;
using log4net;
using Logger = log4net.ILog;

namespace EaseServer
{
    /// <summary>
    /// �������
    /// </summary>
    public class Server : ServiceBase, IServerAPI
    {
        int _port;
        string _virtualPath, _physicalPath;
        TcpListener svrListener = null;
        Host _host;

        Timer secondDataChecker = null;

        long messageCount = 0, bytesTotal = 0;

        /// <summary>
        /// ��ʼ��һ�� <see cref="Server"/> class ʵ����
        /// </summary>
        /// <param name="port">�����˿�</param>
        /// <param name="virtualPath">Ӧ�ó�������·��</param>
        /// <param name="physicalPath">��ASPX���������·��</param>
        public Server(int port, string virtualPath, string physicalPath)
        {
            _port = port;
            _virtualPath = virtualPath;
            _physicalPath = physicalPath.EndsWith("\\", StringComparison.Ordinal) ? physicalPath : physicalPath + "\\";
        }

        private readonly X509Certificate _certificate;
        private readonly SslProtocols _sslProtocol = SslProtocols.Default;
        private readonly bool _requireClientCerts = false;

        /// <summary>
        /// ��ʼ��һ�� <see cref="Server"/> class ʵ����
        /// </summary>
        /// <param name="port">�����˿�</param>
        /// <param name="virtualPath">Ӧ�ó�������·��</param>
        /// <param name="physicalPath">��ASPX���������·��</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="requireClientCerts">if set to <c>true</c> [require client certs].</param>
        public Server(int port, string virtualPath, string physicalPath, X509Certificate certificate, SslProtocols protocol, bool requireClientCerts)
            : this(port, virtualPath, physicalPath)
        {
            _certificate = certificate;
            _sslProtocol = protocol;
            _requireClientCerts = requireClientCerts;
        }

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
        /// Gets the virtual path.
        /// </summary>
        /// <value>The virtual path.</value>
        public string VirtualPath { get { return _virtualPath; } }

        /// <summary>
        /// ASPX�����������·��
        /// </summary>
        /// <value></value>
        public string PhysicalPath { get { return _physicalPath; } }

        /// <summary>
        /// ��ȡ��������˿�
        /// </summary>
        public int Port { get { return _port; } }

        /// <summary>
        /// ��ȡHTTP���ʵĸ�Ŀ¼
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
        /// ��ȡӦ�ó�������
        /// </summary>
        internal static string ServerName = typeof(Server).Assembly.GetName().Name;

        /// <summary>
        /// �����ļ����ĺ����
        /// </summary>
        internal static List<Action> ConfigChangeCallBack = new List<Action>();

        #region �Ựȫ�ָ���

        /// <summary>
        /// ȫ��֧�ֻỰ�ʵ�
        /// </summary>
        internal static readonly ThreadSafeDictionary<string, ServerSession> SupportSeesions = new ThreadSafeDictionary<string, ServerSession>();

        /// <summary>
        /// ��̬������Ϣ
        /// </summary>
        private static readonly ThreadSafeDictionary<string, SessionConfig> SupportSessionConfigFetch = new ThreadSafeDictionary<string, SessionConfig>();

        /// <summary>
        /// ����˻Ự��ʼ��
        /// </summary>
        void InitialSessionSupport()
        {
            ServerSessionSupport cusSS = ServerSessionSupport.ConfigInstance;
            if (cusSS == null) return;

            Log.DebugFormat("# �������÷�����[{5}]: {0} v{1} (AspxHost = {2}, Mixed = {3}, EmptyUserAgent = {4}).",
                Server.ServerName, Messages.VersionString,
                cusSS.EnableInternalAspxHost, cusSS.EnableMixedSession, cusSS.EnableEmptyUserAgent, Port);

            if (cusSS.SupportItems == null) return;

            #region ��վɵ�����
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
                    throw new System.Configuration.ConfigurationErrorsException("���ô��󣺻Ựʵ������[" + cusSS.SupportItems[i].ImplementTypeName + "]�����ڻ�û��ʵ�ֽӿ�[" + processType.AssemblyQualifiedName + "]��");
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

                    Log.DebugFormat("* ����֧��: {0}, ����Ϊ{1}", key, currentType);
                    if (cusSS.SupportItems[i].Config != null)
                    {
                        //ʵ����������
                        SupportSessionConfigFetch[key] = cusSS.SupportItems[i].Config;
                        Log.DebugFormat("* ���ûỰ������Ϣ: {0}", key);
                    }
                }
            }

        }

        /// <summary>
        /// ��ȡ�׸���Ӧʽ����ģ��
        /// </summary>
        internal static IConnectionProcessor GetAppliedProcessor(Connection conn)
        {
            IConnectionProcessor processor = null;
            ServerSession sSession = null;
            foreach (var key in SupportSeesions.Keys)
            {
                sSession = SupportSeesions[key];
                //����Ӧ��ʽ����ģ��
                if (!sSession.IsResponse) continue;

                processor = Activator.CreateInstance(sSession.RuntimeType) as IConnectionProcessor;
                if (processor != null)
                {
                    processor.ServerConnection = conn;
                    ApplyConfig(key, processor);
                    Log.DebugFormat("* �Ự����: {0} => {1}, ��������:{2}", key, sSession.RuntimeType.FullName, processor.SocketMode);
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
                    Log.DebugFormat("* �Ự������Ϣ����{0}", configKey);
                    SupportSessionConfigFetch[configKey].HasConfiged = true;
                }
            }
        }

        /// <summary>
        /// ��ȡ�׸�Ӧ��ʽ����ģ��
        /// </summary>
        internal static IConnectionProcessor GetRespectiveProcess(Connection conn, byte[] firstReadBytes)
        {
            ServerSession sSession = null;
            IConnectionProcessor tProcess = null;
            foreach (var key in SupportSeesions.Keys)
            {
                sSession = SupportSeesions[key];
                //������Ӧʽ����ģ��
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
                        Log.DebugFormat("* �Ự����: {0} => {1}, ��������:{2}", key, sSession.RuntimeType.FullName, tProcess.SocketMode);
                        break;
                    }
                }
            }
            return tProcess;
        }

        /// <summary>
        /// ���еļ���������
        /// </summary>
        public static readonly ThreadSafeDictionary<string, Server> AllListenServer = new ThreadSafeDictionary<string, Server>();
        #endregion

        /// <summary>
        /// ������������ʵ��ʱ�������������ִ�У��ڡ�������ƹ�������(SCM) ������͡���ʼ������ʱ�������ڲ���ϵͳ����ʱ�������Զ������ķ��񣩡�ָ����������ʱ��ȡ�Ĳ�����
        /// </summary>
        /// <param name="args">��������ݵ����ݡ�</param>
        protected override void OnStart(string[] args)
        {
            Start();
        }

        /// <summary>
        /// Socket������ʼ
        /// </summary>
        public void Start()
        {
            try
            {
                svrListener = new TcpListener(IPAddress.Any, _port);
                svrListener.ExclusiveAddressUse = true;
                svrListener.Start("EaseServer.PendingQueueCount".AppSettings<int>(2000));      //������������
            }
            catch (Exception socketExp)
            {
                svrListener = null;
                Log.Error("* ������ʧ�ܣ�", socketExp);
                if (!Environment.UserInteractive)
                {
                    return;
                }
                else
                {
                    throw socketExp;
                }
            }

            if (svrListener != null)
            {
                string crtSvrKey = "#" + _port.ToString();
                AllListenServer.MergeSafe(crtSvrKey, this);

                StartDateTime = DateTime.Now;
                //�Ự֧��
                if (SupportSeesions.Count < 1) InitialSessionSupport();

                doWorkThread();    //����ʱ���տͻ�������
                //���������Ϣ�޸�
                FileDependency refreshAppDc = new FileDependency(Assembly.GetExecutingAssembly().Location + ".config");
                refreshAppDc.OnFileChange += new FileDependency.FileChange(refreshAppDc_OnFileChange);

                secondDataChecker = new Timer(new TimerCallback(s => //ÿ��״̬����
                {
                    if (messageCount > 0 && bytesTotal > 0)
                    {
                        DateTime currentDatetime = DateTime.Now;
                        Log.InfoFormat("* [{4}]������������{0}, [{3}]����{1}����Ϣ/{2}���ݡ�",
                            currentConnectionCount, messageCount, formatBytes(bytesTotal),
                            currentDatetime.AddSeconds(-1).ToString("HH:mm:ss"),
                            currentDatetime.ToString("HH:mm:ss,fff"));

                        messageCount = bytesTotal = 0;
                    }

                    TimerTigger((Server)s);

                }), this, 0, 1000);

                Log.InfoFormat("* {0}��������������", crtSvrKey);
            }

        }

        /// <summary>
        /// ��ǰ�Ƿ������߳���Ч�ͻ���
        /// </summary>
        static bool TickClienting = false;

        /// <summary>
        /// ��ѯ������ÿͻ���
        /// </summary>
        /// <param name="Svr"></param>
        void TimerTigger(Server svr)
        {
            lock (secondDataChecker)
            {
                if (TickClienting) return;

                TickClienting = true;
                int j = svr.TotalConnections.Count;
                TimeSpan idleSpan = TimeSpan.MinValue;
                List<string> disconnectList = new List<string>();
                double idleSecondSet = "EaseServer.MaxIdleTimeSeconds".AppSettings<double>(120.00);

                try
                {
                    #region ��ȡ��Ҫ�Ͽ��Ŀͻ����б�
                    IServerConnection currentConn = null;
                    foreach (var item in svr.TotalConnections.Keys)
                    {
                        currentConn = svr.TotalConnections[item];
                        idleSpan = System.DateTime.Now - currentConn.LastInteractive;
                        if (currentConn.KeepAliveSeconds == 0 && currentConn.KeepAlive) continue;

                        //�Ƿ��б���������������
                        if (currentConn.KeepAliveSeconds > 0)
                        {
                            if (idleSpan.TotalSeconds > currentConn.KeepAliveSeconds)
                            {
                                disconnectList.Add(item);
                            }
                        }
                        else
                        {
                            if (idleSpan.TotalSeconds > idleSecondSet)
                            {
                                disconnectList.Add(item);
                            }
                        }
                    }
                    #endregion
                }
                catch (Exception) { }

                disconnectList.ForEach(k =>
                {
                    IServerConnection c = null;
                    try
                    {
                        c = svr.TotalConnections[k];
                    }
                    catch (KeyNotFoundException) { }
                    if (c != null)
                    {
                        Log.DebugFormat("* Timer����������ǿ�ƶϿ�{0}[{5}], ����{1} > {2}��, ����ʱ��:{3}, ����:{4}.",
                            c.RemoteEP,
                            (DateTime.Now - c.LastInteractive).TotalSeconds,
                            idleSecondSet,
                            c.ConnectedTime,
                            c.LastInteractive, c.Protocol);

                        c.Close();
                        c.Dispose();
                    }
                });

                TickClienting = false;
            }
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
        /// ����������ʵ��ʱ���÷�����ϵͳ�����ر�ʱִ�С��÷���ָ��Ӧ��ϵͳ�����ر�ǰִ�еĴ���
        /// </summary>
        protected override void OnShutdown()
        {
            StopService();
        }

        /// <summary>
        /// ����������ʵ��ʱ���÷����ڡ�������ƹ�������(SCM) ����ֹͣ������͵�����ʱִ�С�ָ������ֹͣ����ʱ��ȡ�Ĳ�����
        /// </summary>
        protected override void OnStop()
        {
            StopService();
        }

        /// <summary>
        /// �رյ�ǰ��������
        /// </summary>
        public void StopService()
        {
            _shutdownInProgress = true;

            if (secondDataChecker != null)
            {
                ExtMethods.StopTimer(secondDataChecker);
                secondDataChecker.Dispose();
                secondDataChecker = null;
            }

            string crtSvrKey = "#" + _port.ToString();
            AllListenServer.RemoveSafe(crtSvrKey);

            int cTotal = allConnections.Count;
            //����ȫ���Ͽ�
            while (cTotal > 0)
            {
                var er = allConnections.Values.GetEnumerator();
                if (er.MoveNext())
                {
                    if (er.Current != null) { er.Current.Close(); er.Current.Dispose(); }
                    allConnections.RemoveSafe(er.Current.RemoteEP);
                }
                cTotal--;
            }

            try
            {
                if (svrListener != null) svrListener.Stop(); //����ֹͣ�ر�
            }
            catch { }
            finally
            {
                svrListener = null;
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

            Log.InfoFormat("* " + crtSvrKey + "������ֹͣ��");
        }

        /// <summary>
        /// ��ǰ����ʵ������ʱ��
        /// </summary>
        public DateTime StartDateTime { get; private set; }

        /// <summary>
        /// ��ȡ��������
        /// </summary>
        public string GetServerName() { return Server.ServerName; }

        /// <summary>
        /// ��ȡ����汾
        /// </summary>
        /// <returns></returns>
        public string GetServerVersion() { return Messages.VersionString; }

        /// <summary>
        /// ������Ϣ�������޸�ʱ��
        /// </summary>
        public DateTime LastChangeDateTime = DateTime.Now;
        /// <summary>
        /// ��ǰ�Ƿ�����ˢ��������Ϣ
        /// </summary>
        bool onChangeConfig = false;

        void refreshAppDc_OnFileChange(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                //����Ϊ���ٳ���һ��
                if (DateTime.Now.Subtract(LastChangeDateTime).Seconds > 1)
                {
                    if (onChangeConfig) return;

                    onChangeConfig = true;
                    //�ȴ����ٸ��º�n��
                    WaittingOnLeave(e.FullPath, 2);

                    bool hasError = false;
                    int tryTimes = 0;
                    Exception errExp = null;

                    #region �ҽӸ��»ص�
                    foreach (Action fire in ConfigChangeCallBack)
                    {
                        try
                        {
                            fire();
                        }
                        catch { }
                    }
                    #endregion

                    #region ��������5��
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
                    if (!hasError) InitialSessionSupport();

                    Server.Log.InfoFormat("* Ӧ�ó���������Ϣ�Ѹ���[{2}]���ϴ��޸�ʱ��Ϊ��{0}��������ϢΪ��{1}��",
                        LastChangeDateTime,
                        hasError ? errExp.Message : "[ˢ�³ɹ�]",
                        tryTimes);

                    LastChangeDateTime = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// �ȴ����ڸ��µ��ļ��ﵽһ���������
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


        // called at the end of request processing
        // to disconnect the remoting proxy for Connection object
        // and allow GC to pick it up
        public void OnRequestEnd(Connection conn) { RemotingServices.Disconnect(conn); }

        /// <summary>
        /// ��ǰASP.NET���������ѹر�
        /// </summary>
        public void HostStopped() { _host = null; }

        /// <summary>
        /// ͬ���̳߳�����
        /// </summary>
        internal static void SynThreadPoolSeting()
        {
            #region �̳߳�ʹ������
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

        /// <summary>
        /// ����������
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

                        //�����̳߳��������޸�
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

        ThreadSafeDictionary<string, IServerConnection> allConnections = new ThreadSafeDictionary<string, IServerConnection>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// ��ȡ���пͻ�������
        /// </summary>
        public ThreadSafeDictionary<string, IServerConnection> TotalConnections { get { return allConnections; } }

        /// <summary>
        /// ���пͻ�����������
        /// </summary>
        int currentConnectionCount = 0;

        /// <summary>
        /// Ĭ�ϱ���¼�ʵ��
        /// </summary>
        ConnectionEvent allConnEvents = (conn, isConn) =>
        {
            if (isConn)
                Log.DebugFormat("<< �ͻ���<{0}>������, ������������{1}��", conn.RemoteEP, conn.GetServerAPI().ConnectionCount);
            else
                Log.DebugFormat(">>> [{2}]�ͻ���<{0}>�ѶϿ����ܼ�����ʱ��{1}, ������������{3}��",
                    conn.RemoteEP, DateTime.Now.Subtract(conn.ConnectedTime), conn.Protocol, conn.GetServerAPI().ConnectionCount);
        };

        /// <summary>
        /// ���ӱ仯ʱ�������¼�
        /// </summary>
        public event ConnectionEvent OnConnectionChange
        {
            add { allConnEvents += value; }
            remove { allConnEvents -= value; }
        }

        /// <summary>
        /// ��ǰ�����������������
        /// </summary>
        public int ConnectionCount { get { return currentConnectionCount; } }

        /// <summary>
        /// �ж��Ƿ����ڹرշ�����
        /// </summary>
        /// <returns></returns>
        public bool IsShuttingDown() { return _shutdownInProgress; }

        /// <summary>
        /// �����ͻ�������
        /// </summary>
        /// <param name="conn">�ͻ�������</param>
        public void RegisterClient(IServerConnection conn)
        {
            string cKey = conn.RemoteEP;
            if (!allConnections.ContainsKey(cKey))
            {
                Interlocked.Increment(ref currentConnectionCount);
            }
            else
            {
                try
                {
                    allConnections[cKey].Close();
                    allConnections[cKey].Dispose();
                }
                catch (Exception) { }
                allConnections.Remove(cKey);
            }
            allConnections.Add(cKey, conn);
            if (MaxClientCount > 0 && currentConnectionCount + 1 > MaxClientCount)
            {
                acceptNextSocket = false; //�ﵽ����ֹͣ
                svrListener.Stop();  //�Ѵﵽ��������
                Log.InfoFormat("* �Ѵﵽ��������ɵ����������[{0}]���µĽ��뽫����ֹ��", MaxClientCount);
            }
            allConnEvents.BeginInvoke(conn, true, null, null);
        }

        /// <summary>
        /// ȡ�������ͻ�������
        /// </summary>
        /// <param name="conn">�ͻ�������</param>
        public void UnRegisterClient(IServerConnection conn)
        {
            string cKey = conn.RemoteEP;
            if (!allConnections.ContainsKey(cKey) || currentConnectionCount < 1) //������1�����ӿͻ���(2011-9-16)
                return;

            allConnections.Remove(cKey);
            Interlocked.Decrement(ref currentConnectionCount);
            if (!_shutdownInProgress)
            {
                if (!acceptNextSocket || (MaxClientCount > 0 && currentConnectionCount < MaxClientCount))
                {
                    startReceiveClient();
                }
            }
            allConnEvents.BeginInvoke(conn, false, null, null);
        }

        Connection getSocketConnection(Socket acceptedSocket)
        {
            string dumpConfig = ConfigurationManager.AppSettings[ServerName + ".DumpAccess"];
            if (dumpConfig != null && Enum.IsDefined(typeof(FileAccess), dumpConfig))
            {
                string sid = Connection.NewSessionID();
                string dumpFormat = ConfigurationManager.AppSettings[ServerName + ".DumpFormat"];
                DumpFormat format = DumpFormat.Binary;
                if (dumpFormat != null && Enum.IsDefined(typeof(DumpFormat), dumpFormat))
                    format = (DumpFormat)Enum.Parse(typeof(DumpFormat), dumpFormat);

                Stream mStream = new FileDumpStream("dump", sid, format);
                return new Connection(this, acceptedSocket, sid, (FileAccess)Enum.Parse(typeof(FileAccess), dumpConfig), mStream);
            }
            else
            {
                return new Connection(this, acceptedSocket);
            }
        }

        /// <summary>
        /// �ڵ�ǰ�߳̽���Socket���ӣ�������ǰ���ӵ�ҵ���߼�
        /// </summary>
        /// <param name="acceptedSocket">��ǰSocket����ʵ��</param>
        public void ProcessSocketConnection(Socket acceptedSocket)
        {
            if (_shutdownInProgress)
            {
                CommonLib.ExtensionUtil.CatchAll(() =>
                {
                    acceptedSocket.Close();
                    acceptedSocket.Shutdown(SocketShutdown.Both);
                });
                return;
            }

            //δ�ܽ������Ӷ���
            //if (conn.IsSecured && conn.ClientCertificate != null) return;

            Connection conn = getSocketConnection(acceptedSocket);
            conn.OnReceivedData += CounterReceiveBytes;
            conn.FireOnDisconnect += c => c.GetServerAPI().UnRegisterClient(c);
            conn.Connect();
            RegisterClient(conn);
        }

        void CounterReceiveBytes(long recLength)
        {
            messageCount = Interlocked.Increment(ref messageCount);
            bytesTotal = Interlocked.Add(ref bytesTotal, recLength);
        }

        /// <summary>
        /// �µĿͻ��˵��������Socket���Ӷ������
        /// </summary>
        /// <param name="arg"></param>
        static void newClientArrived(IAsyncResult arg)
        {
            if (arg == null) return;

            ServerConnectionWrap wrap = arg.AsyncState as ServerConnectionWrap;
            if (wrap != null)
            {
                wrap.CallBackResult();
                Socket acceptedSocket = null;
                try
                {
                    acceptedSocket = wrap.AcceptEndCallBack(arg);
                }
                catch (Exception) { }

                if (acceptedSocket != null)
                {
                    byte[] optionInValue = FillKeepAliveStruct(1, 20000, 2000);
                    acceptedSocket.IOControl(IOControlCode.KeepAliveValues, optionInValue, null);
                    acceptedSocket.Blocking = true;                             //NetworkStreamʹ������ģʽ
                    acceptedSocket.LingerState = new LingerOption(false, 0);    //���ӳٹر�����

                    //acceptedSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, this.socketSendBuffSize);
                    //acceptedSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, this.socketReceiveBuffSize);
                    wrap.ContextServer.ProcessSocketConnection(acceptedSocket);
                }
            }
        }

        /// <summary>
        /// FillKeepAliveStruct �õ�Keep-Alive�ṹֵ
        /// </summary>
        /// <param name="onOff">�Ƿ�����Keep-Alive</param>
        /// <param name="keepAliveTimeInMSec">�����ʱ��ms</param>
        /// <param name="keepAliveIntervalInMSec">̽��ʱ����ms</param>
        /// <returns></returns>
        static byte[] FillKeepAliveStruct(int onOff, int keepAliveTimeInMSec, int keepAliveIntervalInMSec)
        {
            byte[] array = new byte[12];
            BitConverter.GetBytes(onOff).CopyTo(array, 0);
            BitConverter.GetBytes(keepAliveTimeInMSec).CopyTo(array, 4);
            BitConverter.GetBytes(keepAliveIntervalInMSec).CopyTo(array, 8);
            return array;
        }

        /// <summary>
        /// [��̬����]�ر����м�������
        /// </summary>
        public static void StopAllServer()
        {
            while (AllListenServer.Count > 0)
            {
                IEnumerator<string> er = AllListenServer.Keys.GetEnumerator();
                if (er.MoveNext())
                {
                    AllListenServer[er.Current].Stop();
                    AllListenServer.Remove(er.Current);
                }
            }
        }

        #region ��������
        private int _maxClientCount = 0;
        /// <summary>
        /// �����ܿͻ���������(Ϊ0������)
        /// </summary>
        public int MaxClientCount
        {
            get { return _maxClientCount; }
            set { _maxClientCount = value; }
        }

        ManualResetEvent acceptClientHander = new ManualResetEvent(false);
        Thread acceptClientThread = null;

        void doWorkThread()
        {
            if (acceptClientThread != null)
                acceptClientThread.Abort();

            acceptClientThread = new Thread(new ThreadStart(acceptClientConnection));
            acceptClientThread.IsBackground = true;
            acceptClientThread.Start();
        }

        void acceptClientConnection()
        {
            while (!_shutdownInProgress && acceptNextSocket)
            {
                acceptClientHander.Reset();

                if (String.IsNullOrEmpty(Thread.CurrentThread.Name))
                    Thread.CurrentThread.Name = "#" + Thread.CurrentThread.ManagedThreadId;

                ServerConnectionWrap wrap = ServerConnectionWrap.CreateConnectionRef(this, svrListener.EndAcceptSocket, acceptClientHander.Set);

                //Log.Debug("��ʼ�첽�ȴ���һ������");
                bool queResult = ThreadPool.QueueUserWorkItem(new WaitCallback(o =>
                {
                    svrListener.BeginAcceptSocket(new AsyncCallback(newClientArrived), wrap);
                }));

                if (queResult)
                {
                    acceptClientHander.WaitOne();
                }
                else
                {
                    int t1, t2, ma1, ma2, mi1, mi2;
                    ThreadPool.GetAvailableThreads(out t1, out t2);
                    ThreadPool.GetMaxThreads(out ma1, out ma2);
                    ThreadPool.GetMinThreads(out mi1, out mi2);
                    Log.InfoFormat("*������һ���ͻ������ӵĶ��д洢ʧ�ܣ�\r\n* �̳߳�״̬(����:IOCP)������{0}:{1}, ���{2}:{3}, ��С{4}:{5}��",
                        t1, t2, ma1, ma2, mi1, mi2);

                    Thread.Sleep(500);
                }
            }

            if (acceptClientThread != null)
                acceptClientThread.Abort();
        }

        /// <summary>
        /// ��ǰ�Ƿ����ڹر�
        /// </summary>
        bool _shutdownInProgress;
        /// <summary>
        /// �ж��Ƿ������һ�����ӿͻ��������У�
        /// </summary>
        bool acceptNextSocket = true;

        /// <summary>
        /// �������ӿͻ�����
        /// </summary>
        object _lock_AcceptNext = new object();

        /// <summary>
        /// ���¼����󣬿�ʼ�����µĿͻ��ˡ�
        /// </summary>
        void startReceiveClient()
        {
            lock (_lock_AcceptNext)
            {
                if (!acceptNextSocket)             //��ǰλ����ֹ��һ���ͻ�����״̬
                {
                    acceptNextSocket = true;       //����״̬
                    if (svrListener != null)
                    {
                        svrListener.Start();       //���¼���
                        doWorkThread();        //���¼����󣬽��������ӵĿͻ��ˡ�
                        Log.InfoFormat("* ��������ʼ�����µĿͻ������ӣ���ǰ������Ӱٷֱ�״̬[{0}/{1}]��", allConnections.Count, MaxClientCount);
                    }
                    else
                    {
                        Log.ErrorFormat("* [reReceiveClient()]�����ڶ˿�{0}�ķ�������쳣�����������Ѳ����ڣ�", Port);
                    }
                }
            }
        }

        /// <summary>
        /// ǿ�ƶϿ�ָ���ͻ���
        /// </summary>
        /// <param name="clientEndPointStr">�ͻ��˵�ip:port��ʶ�ַ���</param>
        public void DisconnectClient(string clientEndPointStr)
        {
            IServerConnection conn = null;
            if (allConnections.TryGetValue(clientEndPointStr, out conn))
            {
                ExtensionUtil.CatchAll(() => { conn.Close(); conn.Dispose(); });
                allConnections.RemoveSafe(clientEndPointStr);  //�Ͽ��ض����Ӷ˵�
            }
        }

        /// <summary>
        /// ʹ������ƥ��ģʽ�Ͽ����������Ŀͻ���
        /// </summary>
        /// <param name="clientEndPattern">����ƥ��ģʽ</param>
        public void DisconnectBatchClient(string clientEndPattern)
        {
            List<string> cKeys = new List<string>();
            cKeys.AddRange(allConnections.Keys); //�����������Ӹ���

            bool matchProtocol = clientEndPattern.StartsWith("[") && clientEndPattern.EndsWith("]");
            if (matchProtocol)
                clientEndPattern = clientEndPattern.Trim('[', ']');

            for (int i = 0, j = cKeys.Count; i < j; i++)
            {
                if (clientEndPattern == "*"
                    || (matchProtocol && allConnections[cKeys[i]].Protocol.Equals(clientEndPattern, StringComparison.InvariantCultureIgnoreCase))
                    || System.Text.RegularExpressions.Regex.IsMatch(cKeys[i], clientEndPattern))
                {
                    DisconnectClient(cKeys[i]);
                }
            }
        }


        /// <summary>
        /// ��������ҳ��ʽ��ȡ�ͻ������Ӽ���
        /// </summary>
        /// <param name="startIdx">��ʼ����(0��ʼ)</param>
        /// <param name="pageSize">ÿ����ʾ������</param>
        /// <param name="totalClient">��ǰ�ܹ��ж��ٿͻ���</param>
        public IServerConnection[] GetConnectionList(int startIdx, int pageSize, out int totalClient)
        {
            List<IServerConnection> rList = new List<IServerConnection>();
            var er = allConnections.Values.GetEnumerator();
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

            totalClient = allConnections.Count;
            return rList.ToArray();
        }

        /// <summary>
        /// Lists the client status.
        /// </summary>
        /// <param name="clientPattern">The client pattern.</param>
        /// <param name="listHandler">The list handler.</param>
        public void ListClientStatus(string clientPattern, ListClientWriter listHandler)
        {
            int j = allConnections.Count;
            if (j < 1)
            {
                listHandler("��û�пͻ������ӣ�");
            }
            else
            {
                int i = 0;
                bool matchProtocol = clientPattern.StartsWith("[") && clientPattern.EndsWith("]");
                if (matchProtocol)
                    clientPattern = clientPattern.Trim('[', ']');

                foreach (var tKey in allConnections.Keys)
                {
                    i++;
                    IServerConnection item = null;
                    if (allConnections.TryGetValue(tKey, out item))
                    {
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
        }

        /// <summary>
        /// ��ȡ�������֧�ֵ����лỰ��ʶ
        /// </summary>
        /// <returns></returns>
        public string[] GetSupportSessionKeys()
        {
            List<string> keyList = new List<string>();
            ServerSession ss = null;
            foreach (var key in SupportSeesions.Keys)
            {
                ss = SupportSeesions[key];
                keyList.Add(string.Format("{0} => {2}{1}", key, ss.ImplementTypeName, ss.IsResponse ? "(����Ӧ��ʽ)" : ""));
            }
            return keyList.ToArray();
        }

        /// <summary>
        /// �г��������пͻ���
        /// </summary>
        /// <param name="svr">The SVR.</param>
        public static void ListClientStatus(Server svr, ListClientWriter listHandler)
        {
            svr.ListClientStatus("*", listHandler);
        }
        #endregion

        #region ��־���з�װ
        /// <summary>
        /// ��־��¼����
        /// </summary>
        enum LogLevel : byte
        {
            /// <summary>
            /// ȫ����¼
            /// </summary>
            ALL = 255,
            /// <summary>
            /// ����
            /// </summary>
            DEBUG = 3,
            /// <summary>
            /// ��Ϣ
            /// </summary>
            INFO = 2,
            /// <summary>
            /// ����
            /// </summary>
            ERROR = 1,
            /// <summary>
            /// ����־
            /// </summary>
            NONE = 0
        }

        /// <summary>
        /// ��־���з�װ
        /// </summary>
        internal static class Log
        {
            /// <summary>
            /// ��־����
            /// </summary>
            static readonly Logger logger = LogManager.GetLogger(typeof(Server));

            /// <summary>
            /// ��־����
            /// </summary>
            static LockFreeQueue<Action> LogQue = new LockFreeQueue<Action>();

            static Timer secondDataChecker = null;

            /// <summary>
            /// ��ǰ�Ƿ�����д��־
            /// </summary>
            static volatile bool Logging = false;

            /// <summary>
            /// ������־����
            /// </summary>
            static void RefreshLogLevel()
            {
                lock (typeof(Log))
                {
                    Level = (LogLevel)Enum.Parse(typeof(LogLevel), "EaseServer.ServerLogLevel".AppSettings<string>("INFO"));
                }
            }

            /// <summary>
            /// ������־����
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
            /// ��ʼ��¼��־
            /// </summary>
            public static void LoggingNow()
            {
                if (secondDataChecker != null || Logging)
                    return;

                secondDataChecker = new Timer(new TimerCallback(q => //ÿ��״̬����
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
            /// ֹͣ��־��¼
            /// </summary>
            public static void StopLogging()
            {
                if (secondDataChecker == null || !Logging)
                    return;

                if (secondDataChecker != null)
                {
                    ExtMethods.StopTimer(secondDataChecker);
                    if (!LogQue.IsEmpty)
                        logger.InfoFormat("* ����{0}����־δд�롣", LogQue.Count);
                    secondDataChecker.Dispose();
                    secondDataChecker = null;
                }
            }

        }
        #endregion

    }

    /// <summary>
    /// �����¼�
    /// </summary>
    /// <param name="conn">���Ӷ����װ</param>
    /// <param name="isConn">�Ƿ��ǽ������ӻ��ǶϿ�����</param>
    public delegate void ConnectionEvent(IServerConnection conn, bool isConn);

    class ServerConnectionWrap
    {
        private ServerConnectionWrap() { }

        public static ServerConnectionWrap CreateConnectionRef(Server ctxServer, Func<IAsyncResult, Socket> callBack, Func<bool> resultCallBack)
        {
            ServerConnectionWrap wrap = new ServerConnectionWrap();
            wrap.ContextServer = ctxServer;
            wrap.CallBackResult = resultCallBack;
            wrap.AcceptEndCallBack = callBack;
            return wrap;
        }

        /// <summary>
        /// �����ķ�����
        /// </summary>
        public Server ContextServer { get; set; }

        /// <summary>
        /// ������һ�����ӻص�
        /// </summary>
        public Func<bool> CallBackResult { get; set; }

        /// <summary>
        /// ����Socket���ӻص�
        /// </summary>
        public Func<IAsyncResult, Socket> AcceptEndCallBack { get; set; }
    }

}
