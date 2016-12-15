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
using System.Globalization;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Configuration;
using System.Collections.Specialized;

namespace EaseServer
{
    /// <summary>
    /// ASP.NET������������
    /// </summary>
    class Host : MarshalByRefObject, IRegisteredObject
    {
        Server _server;

        int _port;
        volatile int _pendingCallsCount;
        string _virtualPath;

        string _lowerCasedVirtualPath, _lowerCasedVirtualPathWithTrailingSlash;
        string _physicalPath, _installPath;

        string _physicalClientScriptPath, _lowerCasedClientScriptPathWithTrailingSlash;

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
        /// ��ʼ�� <see cref="Host"/> class.
        /// </summary>
        public Host()
        {
            HostingEnvironment.RegisterObject(this);
        }

        /// <summary>
        /// Configures the specified server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="port">The port.</param>
        /// <param name="virtualPath">The virtual path.</param>
        /// <param name="physicalPath">The physical path.</param>
        public void Configure(Server server, int port, string virtualPath, string physicalPath)
        {
            _server = server;

            _port = port;
            _installPath = null;
            _virtualPath = virtualPath;

            _lowerCasedVirtualPath = CultureInfo.InvariantCulture.TextInfo.ToLower(_virtualPath);
            _lowerCasedVirtualPathWithTrailingSlash = virtualPath.EndsWith("/", StringComparison.Ordinal) ? virtualPath : virtualPath + "/";
            _lowerCasedVirtualPathWithTrailingSlash = CultureInfo.InvariantCulture.TextInfo.ToLower(_lowerCasedVirtualPathWithTrailingSlash);
            _physicalPath = physicalPath;
            _physicalClientScriptPath = HttpRuntime.AspClientScriptPhysicalPath + "\\";
            _lowerCasedClientScriptPathWithTrailingSlash = CultureInfo.InvariantCulture.TextInfo.ToLower(HttpRuntime.AspClientScriptVirtualPath + "/");
        }

        /// <summary>
        /// �Ƿ�����Ŀ¼�б�
        /// </summary>
        public bool EnableDirectoryList { get; set; }


        /// <summary>
        /// �ж��Ƿ�������ӳ��·��
        /// </summary>
        /// <param name="requestPath">The request path.</param>
        /// <param name="realPath">The real path.</param>
        /// <returns></returns>
        public bool MappedPath(string requestPath, ref string realPath)
        {
            /*
                <section name="ReadonlyDirectory" type="System.Configuration.NameValueFileSectionHandler, System, Version=1.0.5000.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" />
              </configSections>

              <ReadonlyDirectory>
                <add key="/svr/logs/" value="D:\DevRoot\Remote\release\ClrServerHost\Logs\" />
              </ReadonlyDirectory>
            */
            bool hasMapped = false;
            object objTemp = ConfigurationManager.GetSection("ReadonlyDirectory");
            NameValueCollection ReadonlyDirectory = objTemp != null ? (NameValueCollection)objTemp : null;
            if (ReadonlyDirectory != null)
            {
                foreach (var item in ReadonlyDirectory.AllKeys)
                {
                    if (requestPath.StartsWith(item, StringComparison.InvariantCultureIgnoreCase))
                    {
                        realPath = ReadonlyDirectory[item] + requestPath.Substring(item.Length);
                        hasMapped = true;
                        break;
                    }
                }
            }
            return hasMapped;
        }

        /// <summary>
        /// ����Ӧ����������
        /// </summary>
        public void ProcessRequest(Connection conn)
        {
            // Add a pending call to make sure our thread doesn't get killed
            AddPendingCall();
            try
            {
                Request request = new Request(_server, this, conn);
                request.Process();
            }
            finally
            {
                RemovePendingCall();
            }
        }

        void WaitForPendingCallsToFinish()
        {
            for (; ; )
            {
                if (_pendingCallsCount <= 0)
                    break;
                Thread.Sleep(250);
            }
        }

        void AddPendingCall()
        {
#pragma warning disable 0420
            Interlocked.Increment(ref _pendingCallsCount);
#pragma warning restore 0420
        }

        void RemovePendingCall()
        {
#pragma warning disable 0420
            Interlocked.Decrement(ref _pendingCallsCount);
#pragma warning restore 0420
        }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public void Shutdown()
        {
            HostingEnvironment.InitiateShutdown();
        }

        /// <summary>
        /// ����ע��һ����ע�����
        /// </summary>
        /// <param name="immediate">���ע�����Ӧ�ڷ���ǰ������������ע������Ϊ true������Ϊ false��</param>
        void IRegisteredObject.Stop(bool immediate)
        {
            // Unhook the Host so Server will process the requests in the new appdomain.
            if (_server != null)
            {
                _server.HostStopped();
            }

            // Make sure all the pending calls complete before this Object is unregistered.
            WaitForPendingCallsToFinish();
            HostingEnvironment.UnregisterObject(this);
        }

        public string InstallPath { get { return _installPath; } }
        public string NormalizedClientScriptPath { get { return _lowerCasedClientScriptPathWithTrailingSlash; } }
        public string NormalizedVirtualPath { get { return _lowerCasedVirtualPathWithTrailingSlash; } }
        public string PhysicalClientScriptPath { get { return _physicalClientScriptPath; } }
        public string PhysicalPath { get { return _physicalPath; } }
        public int Port { get { return _port; } }
        public string VirtualPath { get { return _virtualPath; } }

        public bool IsVirtualPathInApp(String path)
        {
            bool isClientScriptPath;
            return IsVirtualPathInApp(path, out isClientScriptPath);
        }

        /// <summary>
        /// Check if the path is not well formed or is not for the current app
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isClientScriptPath">if set to <c>true</c> [is client script path].</param>
        /// <returns>
        /// 	<c>true</c> if [is virtual path in app] [the specified path]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsVirtualPathInApp(string path, out bool isClientScriptPath)
        {
            isClientScriptPath = false;

            if (path == null)
            {
                return false;
            }

            if (_virtualPath == "/" && path.StartsWith("/", StringComparison.Ordinal))
            {
                if (path.StartsWith(_lowerCasedClientScriptPathWithTrailingSlash, StringComparison.Ordinal))
                    isClientScriptPath = true;
                return true;
            }

            path = CultureInfo.InvariantCulture.TextInfo.ToLower(path);

            if (path.StartsWith(_lowerCasedVirtualPathWithTrailingSlash, StringComparison.Ordinal))
                return true;

            if (path == _lowerCasedVirtualPath)
                return true;

            if (path.StartsWith(_lowerCasedClientScriptPathWithTrailingSlash, StringComparison.Ordinal))
            {
                isClientScriptPath = true;
                return true;
            }

            return false;
        }

        public bool IsVirtualPathAppPath(string path)
        {
            if (path == null) return false;
            path = CultureInfo.InvariantCulture.TextInfo.ToLower(path);
            return (path == _lowerCasedVirtualPath || path == _lowerCasedVirtualPathWithTrailingSlash);
        }
    }
}
