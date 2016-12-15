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
    /// 服务入口
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
        /// 初始化一个 <see cref="Server"/> class 实例。
        /// </summary>
        /// <param name="port">监听端口</param>
        /// <param name="virtualPath">应用程序虚拟路径</param>
        /// <param name="physicalPath">绑定ASPX服务的物理路径</param>
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
        /// 初始化一个 <see cref="Server"/> class 实例。
        /// </summary>
        /// <param name="port">监听端口</param>
        /// <param name="virtualPath">应用程序虚拟路径</param>
        /// <param name="physicalPath">绑定ASPX服务的物理路径</param>
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
        /// 获取控制此实例的生存期策略的生存期服务对象。
        /// </summary>
        /// <returns>
        /// 	<see cref="T:System.Runtime.Remoting.Lifetime.ILease"/> 类型的对象，用于控制此实例的生存期策略。这是此实例当前的生存期服务对象（如果存在）；否则为初始化为 <see cref="P:System.Runtime.Remoting.Lifetime.LifetimeServices.LeaseManagerPollTime"/> 属性的值的新生存期服务对象。
        /// </returns>
        /// <exception cref="T:System.Security.SecurityException">直接调用方没有基础结构权限。</exception>
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
        /// 获取服务监听端口
        /// </summary>
        public int Port { get { return _port; } }

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
        /// 获取应用程序名称
        /// </summary>
        internal static string ServerName = typeof(Server).Assembly.GetName().Name;

        /// <summary>
        /// 配置文件更改后调用
        /// </summary>
        internal static List<Action> ConfigChangeCallBack = new List<Action>();

        #region 会话全局辅助

        /// <summary>
        /// 全部支持会话词典
        /// </summary>
        internal static readonly ThreadSafeDictionary<string, ServerSession> SupportSeesions = new ThreadSafeDictionary<string, ServerSession>();

        /// <summary>
        /// 动态配置信息
        /// </summary>
        private static readonly ThreadSafeDictionary<string, SessionConfig> SupportSessionConfigFetch = new ThreadSafeDictionary<string, SessionConfig>();

        /// <summary>
        /// 服务端会话初始化
        /// </summary>
        void InitialSessionSupport()
        {
            ServerSessionSupport cusSS = ServerSessionSupport.ConfigInstance;
            if (cusSS == null) return;

            Log.DebugFormat("# 重新配置服务器[{5}]: {0} v{1} (AspxHost = {2}, Mixed = {3}, EmptyUserAgent = {4}).",
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

                    Log.DebugFormat("* 设置支持: {0}, 类型为{1}", key, currentType);
                    if (cusSS.SupportItems[i].Config != null)
                    {
                        //实例配置设置
                        SupportSessionConfigFetch[key] = cusSS.SupportItems[i].Config;
                        Log.DebugFormat("* 设置会话配置信息: {0}", key);
                    }
                }
            }

        }

        /// <summary>
        /// 获取首个响应式处理模块
        /// </summary>
        internal static IConnectionProcessor GetAppliedProcessor(Connection conn)
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
        internal static IConnectionProcessor GetRespectiveProcess(Connection conn, byte[] firstReadBytes)
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
        /// 所有的监听服务器
        /// </summary>
        public static readonly ThreadSafeDictionary<string, Server> AllListenServer = new ThreadSafeDictionary<string, Server>();
        #endregion

        /// <summary>
        /// 当在派生类中实现时，在下列情况下执行：在“服务控制管理器”(SCM) 向服务发送“开始”命令时，或者在操作系统启动时（对于自动启动的服务）。指定服务启动时采取的操作。
        /// </summary>
        /// <param name="args">启动命令传递的数据。</param>
        protected override void OnStart(string[] args)
        {
            Start();
        }

        /// <summary>
        /// Socket监听开始
        /// </summary>
        public void Start()
        {
            try
            {
                svrListener = new TcpListener(IPAddress.Any, _port);
                svrListener.ExclusiveAddressUse = true;
                svrListener.Start("EaseServer.PendingQueueCount".AppSettings<int>(2000));      //服务启动监听
            }
            catch (Exception socketExp)
            {
                svrListener = null;
                Log.Error("* 监听绑定失败：", socketExp);
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
                //会话支持
                if (SupportSeesions.Count < 1) InitialSessionSupport();

                doWorkThread();    //启动时接收客户端连接
                //检测配置信息修改
                FileDependency refreshAppDc = new FileDependency(Assembly.GetExecutingAssembly().Location + ".config");
                refreshAppDc.OnFileChange += new FileDependency.FileChange(refreshAppDc_OnFileChange);

                secondDataChecker = new Timer(new TimerCallback(s => //每秒状态操作
                {
                    if (messageCount > 0 && bytesTotal > 0)
                    {
                        DateTime currentDatetime = DateTime.Now;
                        Log.InfoFormat("* [{4}]服务连接总数{0}, [{3}]处理{1}个消息/{2}数据。",
                            currentConnectionCount, messageCount, formatBytes(bytesTotal),
                            currentDatetime.AddSeconds(-1).ToString("HH:mm:ss"),
                            currentDatetime.ToString("HH:mm:ss,fff"));

                        messageCount = bytesTotal = 0;
                    }

                    TimerTigger((Server)s);

                }), this, 0, 1000);

                Log.InfoFormat("* {0}服务已启动……", crtSvrKey);
            }

        }

        /// <summary>
        /// 当前是否正在踢除无效客户端
        /// </summary>
        static bool TickClienting = false;

        /// <summary>
        /// 轮询清除闲置客户端
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
                    #region 提取需要断开的客户端列表
                    IServerConnection currentConn = null;
                    foreach (var item in svr.TotalConnections.Keys)
                    {
                        currentConn = svr.TotalConnections[item];
                        idleSpan = System.DateTime.Now - currentConn.LastInteractive;
                        if (currentConn.KeepAliveSeconds == 0 && currentConn.KeepAlive) continue;

                        //是否有保持连接秒数设置
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
                        Log.DebugFormat("* Timer：服务器端强制断开{0}[{5}], 闲置{1} > {2}秒, 连接时间:{3}, 最近活动:{4}.",
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

            if (secondDataChecker != null)
            {
                ExtMethods.StopTimer(secondDataChecker);
                secondDataChecker.Dispose();
                secondDataChecker = null;
            }

            string crtSvrKey = "#" + _port.ToString();
            AllListenServer.RemoveSafe(crtSvrKey);

            int cTotal = allConnections.Count;
            //连接全部断开
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
                if (svrListener != null) svrListener.Stop(); //服务停止关闭
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

            Log.InfoFormat("* " + crtSvrKey + "服务已停止。");
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
        /// 配置信息的最新修改时间
        /// </summary>
        public DateTime LastChangeDateTime = DateTime.Now;
        /// <summary>
        /// 当前是否正在刷新配置信息
        /// </summary>
        bool onChangeConfig = false;

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
                    if (!hasError) InitialSessionSupport();

                    Server.Log.InfoFormat("* 应用程序配置信息已更新[{2}]，上次修改时间为：{0}，处理消息为：{1}。",
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


        // called at the end of request processing
        // to disconnect the remoting proxy for Connection object
        // and allow GC to pick it up
        public void OnRequestEnd(Connection conn) { RemotingServices.Disconnect(conn); }

        /// <summary>
        /// 当前ASP.NET服务宿主已关闭
        /// </summary>
        public void HostStopped() { _host = null; }

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

        ThreadSafeDictionary<string, IServerConnection> allConnections = new ThreadSafeDictionary<string, IServerConnection>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// 获取所有客户端连接
        /// </summary>
        public ThreadSafeDictionary<string, IServerConnection> TotalConnections { get { return allConnections; } }

        /// <summary>
        /// 所有客户端连接总数
        /// </summary>
        int currentConnectionCount = 0;

        /// <summary>
        /// 默认变更事件实现
        /// </summary>
        ConnectionEvent allConnEvents = (conn, isConn) =>
        {
            if (isConn)
                Log.DebugFormat("<< 客户端<{0}>已连接, 服务连接总数{1}。", conn.RemoteEP, conn.GetServerAPI().ConnectionCount);
            else
                Log.DebugFormat(">>> [{2}]客户端<{0}>已断开，总计连接时间{1}, 服务连接总数{3}。",
                    conn.RemoteEP, DateTime.Now.Subtract(conn.ConnectedTime), conn.Protocol, conn.GetServerAPI().ConnectionCount);
        };

        /// <summary>
        /// 连接变化时发生的事件
        /// </summary>
        public event ConnectionEvent OnConnectionChange
        {
            add { allConnEvents += value; }
            remove { allConnEvents -= value; }
        }

        /// <summary>
        /// 当前服务的所有连接总数
        /// </summary>
        public int ConnectionCount { get { return currentConnectionCount; } }

        /// <summary>
        /// 判断是否正在关闭服务器
        /// </summary>
        /// <returns></returns>
        public bool IsShuttingDown() { return _shutdownInProgress; }

        /// <summary>
        /// 建立客户端连接
        /// </summary>
        /// <param name="conn">客户端连接</param>
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
                acceptNextSocket = false; //达到上限停止
                svrListener.Stop();  //已达到连接限制
                Log.InfoFormat("* 已达到服务器许可的最多连接数[{0}]，新的接入将被禁止。", MaxClientCount);
            }
            allConnEvents.BeginInvoke(conn, true, null, null);
        }

        /// <summary>
        /// 取消建立客户端连接
        /// </summary>
        /// <param name="conn">客户端连接</param>
        public void UnRegisterClient(IServerConnection conn)
        {
            string cKey = conn.RemoteEP;
            if (!allConnections.ContainsKey(cKey) || currentConnectionCount < 1) //至少有1个连接客户端(2011-9-16)
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
        /// 在当前线程建立Socket连接，并处理当前连接的业务逻辑
        /// </summary>
        /// <param name="acceptedSocket">当前Socket连接实例</param>
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

            //未能建立连接对象
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
        /// 新的客户端到达后建立的Socket连接对象分配
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
                    acceptedSocket.Blocking = true;                             //NetworkStream使用阻塞模式
                    acceptedSocket.LingerState = new LingerOption(false, 0);    //不延迟关闭连接

                    //acceptedSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, this.socketSendBuffSize);
                    //acceptedSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, this.socketReceiveBuffSize);
                    wrap.ContextServer.ProcessSocketConnection(acceptedSocket);
                }
            }
        }

        /// <summary>
        /// FillKeepAliveStruct 得到Keep-Alive结构值
        /// </summary>
        /// <param name="onOff">是否启用Keep-Alive</param>
        /// <param name="keepAliveTimeInMSec">最大存活时间ms</param>
        /// <param name="keepAliveIntervalInMSec">探测时间间隔ms</param>
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
        /// [静态方法]关闭所有监听服务
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

        #region 辅助方法
        private int _maxClientCount = 0;
        /// <summary>
        /// 最多接受客户端连接数(为0则不限制)
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

                //Log.Debug("开始异步等待下一个连接");
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
                    Log.InfoFormat("*监听下一个客户端连接的队列存储失败！\r\n* 线程池状态(工作:IOCP)：可用{0}:{1}, 最大{2}:{3}, 最小{4}:{5}。",
                        t1, t2, ma1, ma2, mi1, mi2);

                    Thread.Sleep(500);
                }
            }

            if (acceptClientThread != null)
                acceptClientThread.Abort();
        }

        /// <summary>
        /// 当前是否正在关闭
        /// </summary>
        bool _shutdownInProgress;
        /// <summary>
        /// 判断是否接收下一个连接客户（监听中）
        /// </summary>
        bool acceptNextSocket = true;

        /// <summary>
        /// 控制连接客户端锁
        /// </summary>
        object _lock_AcceptNext = new object();

        /// <summary>
        /// 重新监听后，开始接收新的客户端。
        /// </summary>
        void startReceiveClient()
        {
            lock (_lock_AcceptNext)
            {
                if (!acceptNextSocket)             //当前位于阻止下一个客户连接状态
                {
                    acceptNextSocket = true;       //更新状态
                    if (svrListener != null)
                    {
                        svrListener.Start();       //重新监听
                        doWorkThread();        //重新监听后，接收新连接的客户端。
                        Log.InfoFormat("* 服务器开始接收新的客户端连接，当前许可连接百分比状态[{0}/{1}]。", allConnections.Count, MaxClientCount);
                    }
                    else
                    {
                        Log.ErrorFormat("* [reReceiveClient()]监听在端口{0}的服务出现异常，监听对象已不存在！", Port);
                    }
                }
            }
        }

        /// <summary>
        /// 强制断开指定客户端
        /// </summary>
        /// <param name="clientEndPointStr">客户端的ip:port标识字符串</param>
        public void DisconnectClient(string clientEndPointStr)
        {
            IServerConnection conn = null;
            if (allConnections.TryGetValue(clientEndPointStr, out conn))
            {
                ExtensionUtil.CatchAll(() => { conn.Close(); conn.Dispose(); });
                allConnections.RemoveSafe(clientEndPointStr);  //断开特定连接端点
            }
        }

        /// <summary>
        /// 使用正则匹配模式断开符合条件的客户端
        /// </summary>
        /// <param name="clientEndPattern">正则匹配模式</param>
        public void DisconnectBatchClient(string clientEndPattern)
        {
            List<string> cKeys = new List<string>();
            cKeys.AddRange(allConnections.Keys); //复制所有连接副本

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
        /// 以索引分页方式获取客户端连接集合
        /// </summary>
        /// <param name="startIdx">起始索引(0开始)</param>
        /// <param name="pageSize">每次显示多少条</param>
        /// <param name="totalClient">当前总共有多少客户端</param>
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
                listHandler("还没有客户端连接！");
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
        /// 列出监听所有客户端
        /// </summary>
        /// <param name="svr">The SVR.</param>
        public static void ListClientStatus(Server svr, ListClientWriter listHandler)
        {
            svr.ListClientStatus("*", listHandler);
        }
        #endregion

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

    /// <summary>
    /// 连接事件
    /// </summary>
    /// <param name="conn">连接对象封装</param>
    /// <param name="isConn">是否是建立连接还是断开连接</param>
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
        /// 上下文服务器
        /// </summary>
        public Server ContextServer { get; set; }

        /// <summary>
        /// 允许下一个连接回调
        /// </summary>
        public Func<bool> CallBackResult { get; set; }

        /// <summary>
        /// 接收Socket连接回调
        /// </summary>
        public Func<IAsyncResult, Socket> AcceptEndCallBack { get; set; }
    }

}
