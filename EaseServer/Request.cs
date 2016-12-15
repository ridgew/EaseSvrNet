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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Web;
using System.Web.Hosting;
using Microsoft.Win32.SafeHandles;

namespace EaseServer
{

    class Request : SimpleWorkerRequest
    {

        static char[] badPathChars = new char[] { '%', '>', '<', ':', '\\' };
        static string[] defaultFileNames = new string[] { "default.aspx", "default.htm", "default.html" };

        static string[] restrictedDirs = new string[] { 
                "/bin",
                "/app_browsers", 
                "/app_code", 
                "/app_data", 
                "/app_localresources", 
                "/app_globalresources", 
                "/app_webreferences" };

        const int MaxChunkLength = 64 * 1024;

        Server _server;
        Host _host;
        Connection _connection;

        // security permission to Assert remoting calls to _connection
        IStackWalk _connectionPermission = new PermissionSet(PermissionState.Unrestricted);

        /// <summary>
        /// HTTP头所有字节数据
        /// </summary>
        byte[] _httpSendBytes;
        int _startHeadersOffset;
        /// <summary>
        /// HTTP头结束位置
        /// </summary>
        int _endHeadersOffset;
        List<ByteString> _headerByteStrings;

        // parsed request data

        bool _isClientScriptPath;
        /// <summary>
        /// 请求谓词
        /// </summary>
        string _verb;
        /// <summary>
        /// 请求地址
        /// </summary>
        string _url;
        /// <summary>
        /// 请求协议
        /// </summary>
        string _prot;
        string _path, _filePath;
        /// <summary>
        /// 请求文件附带的路径信息
        /// </summary>
        string _pathInfo;
        /// <summary>
        /// 实际请求物理文件地址
        /// </summary>
        string _pathTranslated;
        /// <summary>
        /// 请求文件的查询参数
        /// </summary>
        string _queryString;
        byte[] _queryStringBytes;

        /// <summary>
        /// 内容体长度
        /// </summary>
        int _contentLength;
        /// <summary>
        /// 预先加载的内容体长度
        /// </summary>
        int _preloadedContentLength;
        byte[] _preloadedContent;

        string _allRawHeaders;
        string[][] _unknownRequestHeaders;
        string[] _knownRequestHeaders;
        bool _specialCaseStaticFileHeaders;

        // cached response
        bool _headersSent;
        int _responseStatus;

        StringBuilder _responseHeadersBuilder;

        /// <summary>
        /// 所有应答字节数组集合
        /// </summary>
        List<byte[]> _responseBodyBytes;

        /// <summary>
        /// 初始化一个 <see cref="Request"/> class 实例。
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="host">The host.</param>
        /// <param name="connection">The connection.</param>
        public Request(Server server, Host host, Connection connection)
            : base(string.Empty, string.Empty, null)
        {
            _server = server;
            _host = host;
            _connection = connection;
        }

        /// <summary>
        /// 业务应用处理
        /// </summary>
        public void Process()
        {
            // read the request
            if (!TryParseRequest())
            {
                return;
            }

            // 100 response to POST
            if (_verb == "POST" && _contentLength > 0 && _preloadedContentLength < _contentLength)
            {
                _connection.Write100Continue();
            }

            // special case for client script
            if (_isClientScriptPath)
            {
                _connection.WriteEntireResponseFromFile(_host.PhysicalClientScriptPath + _path.Substring(_host.NormalizedClientScriptPath.Length), false);
                return;
            }

            // deny access to code, bin, etc.
            if (IsRequestForRestrictedDirectory())
            {
                _connection.WriteErrorAndClose(403);
                return;
            }

            // special case for a request to a directory (ensure / at the end and process default documents)
            if (ProcessDirectoryRequest())
            {
                return;
            }

            PrepareResponse();
            // Hand the processing over to HttpRuntime
            HttpRuntime.ProcessRequest(this);
        }

        void Reset()
        {
            _httpSendBytes = null;
            _startHeadersOffset = 0;
            _endHeadersOffset = 0;
            _headerByteStrings = null;

            _isClientScriptPath = false;

            _verb = null;
            _url = null;
            _prot = null;

            _path = null;
            _filePath = null;
            _pathInfo = null;
            _pathTranslated = null;
            _queryString = null;
            _queryStringBytes = null;

            _contentLength = 0;
            _preloadedContentLength = 0;
            _preloadedContent = null;

            _allRawHeaders = null;
            _unknownRequestHeaders = null;
            _knownRequestHeaders = null;
            _specialCaseStaticFileHeaders = false;
        }

        /// <summary>
        /// Tries the parse request.
        /// </summary>
        /// <returns></returns>
        bool TryParseRequest()
        {
            Reset();
            ReadAllHeaders();

            if (_httpSendBytes == null || _endHeadersOffset < 0 ||
                _headerByteStrings == null || _headerByteStrings.Count == 0)
            {
                _connection.WriteErrorAndClose(400);
                return false;
            }

            ParseRequestLine();
            // Check for bad path
            if (IsBadPath())
            {
                _connection.WriteErrorAndClose(400);
                return false;
            }

            // Check if the path is not well formed or is not for the current app
            if (!_host.IsVirtualPathInApp(_path, out _isClientScriptPath))
            {
                _connection.WriteErrorAndClose(404);
                return false;
            }
            ParseHeaders();
            ParsePostedContent();

            /*
            Keep-Alive	300
            Connection	keep-alive
            */
            string connectionVal = _knownRequestHeaders[HttpWorkerRequest.HeaderConnection];
            if (connectionVal != null && connectionVal.Equals("keep-alive", StringComparison.InvariantCultureIgnoreCase))
            {
                string knv = _knownRequestHeaders[HttpWorkerRequest.HeaderKeepAlive];
                int staySeconds = 120;
                _connection.KeepAlive = true;
                if (knv != null && Int32.TryParse(knv, out staySeconds))
                {

                }
                _connection.KeepAliveSeconds = staySeconds;
            }

            return true;
        }

        /// <summary>
        /// 读取协议头
        /// </summary>
        /// <returns></returns>
        bool TryReadAllHeaders()
        {
            byte[] headerBytes = _connection.SocketBufferData;
            if (headerBytes == null || headerBytes.Length == 0)
                return false;

            if (_httpSendBytes != null)
            {
                // previous partial read
                int len = headerBytes.Length + _httpSendBytes.Length;
                if (len > Connection.MaxHeaderBytes)
                    return false;

                byte[] bytes = new byte[len];
                Buffer.BlockCopy(_httpSendBytes, 0, bytes, 0, _httpSendBytes.Length);
                Buffer.BlockCopy(headerBytes, 0, bytes, _httpSendBytes.Length, headerBytes.Length);
                _httpSendBytes = bytes;
            }
            else
            {
                _httpSendBytes = headerBytes;
            }

            // start parsing
            _startHeadersOffset = -1;
            _endHeadersOffset = -1;
            _headerByteStrings = new List<ByteString>();

            // find the end of headers
            ByteParser parser = new ByteParser(_httpSendBytes);

            for (; ; )
            {
                ByteString line = parser.ReadLine();
                if (line == null) break;

                if (_startHeadersOffset < 0)
                {
                    _startHeadersOffset = parser.CurrentOffset;
                }

                if (line.IsEmpty)
                {
                    _endHeadersOffset = parser.CurrentOffset;
                    break;
                }
                _headerByteStrings.Add(line);
            }
            return true;
        }

        void ReadAllHeaders()
        {
            _httpSendBytes = null;
            do
            {
                if (!TryReadAllHeaders())
                {
                    // something bad happened
                    break;
                }
            }
            while (_endHeadersOffset < 0); // found \r\n\r\n
        }

        void ParseRequestLine()
        {
            ByteString requestLine = _headerByteStrings[0];
            ByteString[] elems = requestLine.Split(' ');

            if (elems == null || elems.Length < 2 || elems.Length > 3)
            {
                _connection.WriteErrorAndClose(400);
                return;
            }

            _verb = elems[0].GetString();

            ByteString urlBytes = elems[1];
            _url = urlBytes.GetString();

            if (elems.Length == 3)
            {
                _prot = elems[2].GetString();
            }
            else
            {
                _prot = "HTTP/1.0";
            }

            // query string
            int iqs = urlBytes.IndexOf('?');
            if (iqs > 0)
            {
                _queryStringBytes = urlBytes.Substring(iqs + 1).GetBytes();
            }
            else
            {
                _queryStringBytes = new byte[0];
            }

            iqs = _url.IndexOf('?');
            if (iqs > 0)
            {
                _path = _url.Substring(0, iqs);
                _queryString = _url.Substring(iqs + 1);
            }
            else
            {
                _path = _url;
                _queryStringBytes = new byte[0];
            }

            // url-decode path
            if (_path.IndexOf('%') >= 0)
            {
                _path = HttpUtility.UrlDecode(_path, Encoding.UTF8);
                iqs = _url.IndexOf('?');
                if (iqs >= 0)
                {
                    _url = _path + _url.Substring(iqs);
                }
                else
                {
                    _url = _path;
                }
            }

            // path info
            int lastDot = _path.LastIndexOf('.');
            int lastSlh = _path.LastIndexOf('/');

            if (lastDot >= 0 && lastSlh >= 0 && lastDot < lastSlh)
            {
                int ipi = _path.IndexOf('/', lastDot);
                _filePath = _path.Substring(0, ipi);
                _pathInfo = _path.Substring(ipi);
            }
            else
            {
                _filePath = _path;
                _pathInfo = String.Empty;
            }
            _pathTranslated = MapPath(_filePath);
        }

        bool IsBadPath()
        {
            if (_path.IndexOfAny(badPathChars) >= 0)
            {
                return true;
            }

            if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(_path, "..", CompareOptions.Ordinal) >= 0)
            {
                return true;
            }

            if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(_path, "//", CompareOptions.Ordinal) >= 0)
            {
                return true;
            }

            return false;
        }

        void ParseHeaders()
        {
            _knownRequestHeaders = new string[RequestHeaderMaximum];

            // construct unknown headers as array list of name1,value1,...
            var headers = new List<string>();

            for (int i = 1; i < _headerByteStrings.Count; i++)
            {
                string s = _headerByteStrings[i].GetString();

                int c = s.IndexOf(':');

                if (c >= 0)
                {
                    string name = s.Substring(0, c).Trim();
                    string value = s.Substring(c + 1).Trim();

                    // remember
                    int knownIndex = GetKnownRequestHeaderIndex(name);
                    if (knownIndex >= 0)
                    {
                        _knownRequestHeaders[knownIndex] = value;
                    }
                    else
                    {
                        headers.Add(name);
                        headers.Add(value);
                    }
                }
            }

            // copy to array unknown headers

            int n = headers.Count / 2;
            _unknownRequestHeaders = new string[n][];
            int j = 0;

            for (int i = 0; i < n; i++)
            {
                _unknownRequestHeaders[i] = new string[2];
                _unknownRequestHeaders[i][0] = headers[j++];
                _unknownRequestHeaders[i][1] = headers[j++];
            }

            // remember all raw headers as one string

            if (_headerByteStrings.Count > 1)
            {
                _allRawHeaders = Encoding.UTF8.GetString(_httpSendBytes, _startHeadersOffset, _endHeadersOffset - _startHeadersOffset);
            }
            else
            {
                _allRawHeaders = String.Empty;
            }
        }

        void ParsePostedContent()
        {
            _contentLength = 0;
            _preloadedContentLength = 0;

            string contentLengthValue = _knownRequestHeaders[HttpWorkerRequest.HeaderContentLength];
            if (contentLengthValue != null)
            {
                try
                {
                    _contentLength = Int32.Parse(contentLengthValue, CultureInfo.InvariantCulture);
                }
                catch { }
            }

            if (_httpSendBytes.Length > _endHeadersOffset)
            {
                _preloadedContentLength = _httpSendBytes.Length - _endHeadersOffset;
                if (_preloadedContentLength > _contentLength)
                {
                    _preloadedContentLength = _contentLength; // don't read more than the content-length
                }

                if (_preloadedContentLength > 0)
                {
                    _preloadedContent = new byte[_preloadedContentLength];
                    Buffer.BlockCopy(_httpSendBytes, _endHeadersOffset, _preloadedContent, 0, _preloadedContentLength);
                }
            }
        }

        //void SkipAllPostedContent()
        //{
        //    if (_contentLength > 0 && _preloadedContentLength < _contentLength)
        //    {
        //        int bytesRemaining = (_contentLength - _preloadedContentLength);

        //        while (bytesRemaining > 0)
        //        {
        //            byte[] bytes = _connection.ReadRequestBytes(bytesRemaining);
        //            if (bytes == null || bytes.Length == 0)
        //            {
        //                return;
        //            }
        //            bytesRemaining -= bytes.Length;
        //        }
        //    }
        //}

        bool IsRequestForRestrictedDirectory()
        {
            String p = CultureInfo.InvariantCulture.TextInfo.ToLower(_path);

            if (_host.VirtualPath != "/")
            {
                p = p.Substring(_host.VirtualPath.Length);
            }

            foreach (String dir in restrictedDirs)
            {
                if (p.StartsWith(dir, StringComparison.Ordinal))
                {
                    if (p.Length == dir.Length || p[dir.Length] == '/')
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        bool ProcessDirectoryRequest()
        {
            String dirPathTranslated = _pathTranslated;

            if (_pathInfo.Length > 0)
            {
                // directory path can never have pathInfo
                dirPathTranslated = MapPath(_path);
            }

            if (!Directory.Exists(dirPathTranslated))
            {
                return false;
            }

            // have to redirect /foo to /foo/ to allow relative links to work
            if (!_path.EndsWith("/", StringComparison.Ordinal))
            {
                string newPath = _path + "/";
                string location = "Location: " + UrlEncodeRedirect(newPath) + "\r\n";
                string body = "<html><head><title>Object moved</title></head><body>\r\n" +
                              "<h2>Object moved to <a href='" + newPath + "'>here</a>.</h2>\r\n" +
                              "</body></html>\r\n";

                _connection.WriteEntireResponseFromString(302, location, body, _connection.KeepAlive);
                return true;
            }

            // check for the default file
            foreach (string filename in defaultFileNames)
            {
                string defaultFilePath = dirPathTranslated + "\\" + filename;

                if (File.Exists(defaultFilePath))
                {
                    // pretend the request is for the default file path
                    _path += filename;
                    _filePath = _path;
                    _url = (_queryString != null) ? (_path + "?" + _queryString) : _path;
                    _pathTranslated = defaultFilePath;
                    return false; // go through normal processing
                }
            }

            return false; // go through normal processing
        }

        bool ProcessDirectoryListingRequest()
        {
            if (!_host.EnableDirectoryList || _verb != "GET")
            {
                return false;
            }

            String dirPathTranslated = _pathTranslated;

            if (_pathInfo.Length > 0)
            {
                // directory path can never have pathInfo
                dirPathTranslated = MapPath(_path);
            }

            if (!Directory.Exists(dirPathTranslated))
            {
                return false;
            }

            // get all files and subdirs
            FileSystemInfo[] infos = null;
            try
            {
                infos = (new DirectoryInfo(dirPathTranslated)).GetFileSystemInfos();
            }
            catch
            {
            }

            // determine if parent is appropriate
            string parentPath = null;

            if (_path.Length > 1)
            {
                int i = _path.LastIndexOf('/', _path.Length - 2);

                parentPath = (i > 0) ? _path.Substring(0, i) : "/";
                if (!_host.IsVirtualPathInApp(parentPath))
                {
                    parentPath = null;
                }
            }

            _connection.WriteEntireResponseFromString(200, "Content-type: text/html; charset=utf-8\r\n",
                                                      Messages.FormatDirectoryListing(_path, parentPath, infos),
                                                      _connection.KeepAlive);
            return true;
        }

        static char[] IntToHex = new char[16] {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'
        };

        /// <summary>
        /// URL编码转发
        /// </summary>
        static string UrlEncodeRedirect(string path)
        {
            // this method mimics the logic in HttpResponse.Redirect (which relies on internal methods)

            // count non-ascii characters
            byte[] bytes = Encoding.UTF8.GetBytes(path);
            int count = bytes.Length;
            int countNonAscii = 0;
            for (int i = 0; i < count; i++)
            {
                if ((bytes[i] & 0x80) != 0)
                {
                    countNonAscii++;
                }
            }

            // encode all non-ascii characters using UTF-8 %XX
            if (countNonAscii > 0)
            {
                // expand not 'safe' characters into %XX, spaces to +s
                byte[] expandedBytes = new byte[count + countNonAscii * 2];
                int pos = 0;
                for (int i = 0; i < count; i++)
                {
                    byte b = bytes[i];

                    if ((b & 0x80) == 0)
                    {
                        expandedBytes[pos++] = b;
                    }
                    else
                    {
                        expandedBytes[pos++] = (byte)'%';
                        expandedBytes[pos++] = (byte)IntToHex[(b >> 4) & 0xf];
                        expandedBytes[pos++] = (byte)IntToHex[b & 0xf];
                    }
                }

                path = Encoding.ASCII.GetString(expandedBytes);
            }

            // encode spaces into %20
            if (path.IndexOf(' ') >= 0)
            {
                path = path.Replace(" ", "%20");
            }

            return path;
        }

        void PrepareResponse()
        {
            _headersSent = false; _responseStatus = 200;
            _responseHeadersBuilder = new StringBuilder();
            _responseBodyBytes = new List<byte[]>();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region Implementation of HttpWorkerRequest

        /// <summary>
        /// 返回请求的 URI 的虚拟路径。
        /// </summary>
        /// <returns>请求的 URI 的路径。</returns>
        public override string GetUriPath()
        {
            return _path;
        }

        /// <summary>
        /// 返回请求 URL 中指定的查询字符串。
        /// </summary>
        /// <returns>请求查询字符串。</returns>
        public override string GetQueryString()
        {
            return _queryString;
        }

        /// <summary>
        /// 在派生类中被重写时，以字节数组的形式返回响应查询字符串。
        /// </summary>
        /// <returns>包含响应的字节数组。</returns>
        public override byte[] GetQueryStringRawBytes()
        {
            return _queryStringBytes;
        }

        /// <summary>
        /// 返回附加了查询字符串的请求标头中包含的 URL 路径。
        /// </summary>
        /// <returns>
        /// 请求标头的原始 URL 路径。说明：返回的 URL 未经正常化。使用 URL 进行访问控制或安全敏感性决策会将应用程序暴露在规范化安全漏洞之下。
        /// </returns>
        public override string GetRawUrl() { return _url; }

        /// <summary>
        /// 返回 HTTP 请求谓词。
        /// </summary>
        /// <returns>此请求的 HTTP 谓词。</returns>
        public override string GetHttpVerbName() { return _verb; }

        /// <summary>
        /// 返回请求的 HTTP 版本字符串（例如“HTTP/1.1”）。
        /// </summary>
        /// <returns>请求标头中返回的 HTTP 版本字符串。</returns>
        public override string GetHttpVersion() { return _prot; }

        /// <summary>
        /// 返回客户端的 IP 地址。
        /// </summary>
        /// <returns>客户端的 IP 地址。</returns>
        public override string GetRemoteAddress()
        {
            _connectionPermission.Assert();
            string remoteAddr = "0.0.0.0";
            try
            {
                remoteAddr = _connection.RemoteIP;
            }
            catch (Exception) { }
            return remoteAddr;
        }

        /// <summary>
        /// 返回客户端的端口号。
        /// </summary>
        /// <returns>客户端的端口号。</returns>
        public override int GetRemotePort()
        {
            _connectionPermission.Assert();
            string remoteEp = _connection.RemoteEP;
            int idx = remoteEp.IndexOf(':');
            if (idx != -1)
            {
                return Convert.ToInt32(remoteEp.Substring(idx + 1));
            }
            return 0;
        }

        /// <summary>
        /// 返回收到请求的接口的服务器 IP 地址。
        /// </summary>
        /// <returns>收到请求的接口的服务器 IP 地址。</returns>
        public override string GetLocalAddress()
        {
            _connectionPermission.Assert();
            string localAddr = "127.0.0.1";
            try
            {
                localAddr = _connection.LocalIP;
            }
            catch (Exception) { }
            return localAddr;
        }

        /// <summary>
        /// 在派生类中被重写时，返回本地服务器的名称。
        /// </summary>
        /// <returns>本地服务器的名称。</returns>
        public override string GetServerName()
        {
            string localAddress = GetLocalAddress();
            if (localAddress.Equals("127.0.0.1")) return "localhost";
            return localAddress;
        }

        /// <summary>
        /// 返回收到请求的端口号。
        /// </summary>
        /// <returns>收到请求的服务器端口号。</returns>
        public override int GetLocalPort() { return _host.Port; }

        /// <summary>
        /// 返回请求的 URI 的物理路径。
        /// </summary>
        /// <returns>请求的 URI 的物理路径。</returns>
        public override string GetFilePath() { return _filePath; }

        /// <summary>
        /// 返回请求的 URI 的物理文件路径（并将其从虚拟路径转换成物理路径：例如，从“/proj1/page.aspx”转换成“c:\dir\page.aspx”）
        /// </summary>
        /// <returns>请求的 URI 的已转换的物理文件路径。</returns>
        public override string GetFilePathTranslated() { return _pathTranslated; }

        /// <summary>
        /// 返回具有 URL 扩展的资源的其他路径信息。即对于路径 /virdir/page.html/tail，返回值为 /tail。
        /// </summary>
        /// <returns>资源的附加路径信息。</returns>
        public override string GetPathInfo() { return _pathInfo; }

        /// <summary>
        /// 返回当前正在执行的服务器应用程序的虚拟路径。
        /// </summary>
        /// <returns>当前应用程序的虚拟路径。</returns>
        public override string GetAppPath() { return _host.VirtualPath; }

        /// <summary>
        /// 返回当前正在执行的服务器应用程序的 UNC 翻译路径。
        /// </summary>
        /// <returns>当前应用程序的物理路径。</returns>
        public override string GetAppPathTranslated() { return _host.PhysicalPath; }

        /// <summary>
        /// 返回 HTTP 请求正文已被读取的部分。
        /// </summary>
        /// <returns>HTTP 请求正文已被读取的部分。</returns>
        public override byte[] GetPreloadedEntityBody() { return _preloadedContent; }

        /// <summary>
        /// 返回一个值，该值指示是否所有请求数据都可用，以及是否不需要对客户端进行进一步读取。
        /// </summary>
        /// <returns>如果所有请求数据都可用，则为 true；否则，为 false。</returns>
        public override bool IsEntireEntityBodyIsPreloaded()
        {
            return (_contentLength == _preloadedContentLength);
        }

        /// <summary>
        /// 读取客户端的请求数据（在尚未预加载时）。
        /// </summary>
        /// <param name="buffer">将数据读入的字节数组。</param>
        /// <param name="size">最多读取的字节数。</param>
        /// <returns>读取的字节数。</returns>
        public override int ReadEntityBody(byte[] buffer, int size)
        {
            int bytesRead = 0;
            _connectionPermission.Assert();
            byte[] bytes = _connection.ReadRequestBytes(size);
            if (bytes != null && bytes.Length > 0)
            {
                bytesRead = bytes.Length;
                Buffer.BlockCopy(bytes, 0, buffer, 0, bytesRead);
            }
            return bytesRead;
        }

        /// <summary>
        /// 返回与指定的索引相对应的标准 HTTP 请求标头。
        /// </summary>
        /// <param name="index">标头的索引。例如，<see cref="F:System.Web.HttpWorkerRequest.HeaderAllow"/> 字段。</param>
        /// <returns>HTTP 请求标头。</returns>
        public override string GetKnownRequestHeader(int index) { return _knownRequestHeaders[index]; }

        /// <summary>
        /// 返回非标准的 HTTP 请求标头值。
        /// </summary>
        /// <param name="name">标头名称。</param>
        /// <returns>标头值。</returns>
        public override string GetUnknownRequestHeader(string name)
        {
            int n = _unknownRequestHeaders.Length;
            for (int i = 0; i < n; i++)
            {
                if (string.Compare(name, _unknownRequestHeaders[i][0], StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return _unknownRequestHeaders[i][1];
                }
            }
            return null;
        }

        /// <summary>
        /// 获取所有非标准的 HTTP 标头的名称/值对。
        /// </summary>
        /// <returns>标头的名称/值对的数组。</returns>
        public override string[][] GetUnknownRequestHeaders() { return _unknownRequestHeaders; }

        /// <summary>
        /// 从与请求关联的服务器变量词典返回单个服务器变量。
        /// </summary>
        /// <param name="name">请求的服务器变量的名称。</param>
        /// <returns>请求的服务器变量。</returns>
        public override string GetServerVariable(string name)
        {
            string s = String.Empty;

            switch (name)
            {
                case "ALL_RAW":
                    s = _allRawHeaders;
                    break;

                case "SERVER_PROTOCOL":
                    s = _prot;
                    break;

                case "SERVER_SOFTWARE":
                    s = Server.ServerName + "/" + Messages.VersionString;
                    break;
            }

            return s;
        }

        /// <summary>
        /// 返回与指定虚拟路径相对应的物理路径。
        /// </summary>
        /// <param name="path">虚拟路径。</param>
        /// <returns>
        /// 与 <paramref name="path"/> 参数中指定的虚拟路径相对应的物理路径。
        /// </returns>
        public override string MapPath(string path)
        {
            string mappedPath = String.Empty;
            bool isClientScriptPath = false;

            if (path == null || path.Length == 0 || path.Equals("/"))
            {
                // asking for the site root
                if (_host.VirtualPath == "/")
                {
                    // app at the site root
                    mappedPath = _host.PhysicalPath;
                }
                else
                {
                    // unknown site root - don't point to app root to avoid double config inclusion
                    mappedPath = Environment.SystemDirectory;
                }
            }
            else if (_host.IsVirtualPathAppPath(path))
            {
                // application path
                mappedPath = _host.PhysicalPath;
            }
            else if (_host.IsVirtualPathInApp(path, out isClientScriptPath))
            {
                if (isClientScriptPath)
                {
                    mappedPath = _host.PhysicalClientScriptPath + path.Substring(_host.NormalizedClientScriptPath.Length);
                }
                else
                {
                    string tempMappingPath = null;
                    if (_host.MappedPath(path, ref tempMappingPath))
                    {
                        mappedPath = tempMappingPath;
                    }
                    else
                    {
                        // inside app but not the app path itself
                        mappedPath = _host.PhysicalPath + path.Substring(_host.NormalizedVirtualPath.Length);
                    }
                }
            }
            else
            {
                // outside of app -- make relative to app path
                if (path.StartsWith("/", StringComparison.Ordinal))
                {
                    mappedPath = _host.PhysicalPath + path.Substring(1);
                }
                else
                {
                    mappedPath = _host.PhysicalPath + path;
                }
            }

            mappedPath = mappedPath.Replace('/', '\\');

            if (mappedPath.EndsWith("\\", StringComparison.Ordinal) && !mappedPath.EndsWith(":\\", StringComparison.Ordinal))
            {
                mappedPath = mappedPath.Substring(0, mappedPath.Length - 1);
            }

            return mappedPath;
        }

        /// <summary>
        /// 指定响应的 HTTP 状态代码和状态说明；例如 SendStatus(200, "Ok")。
        /// </summary>
        /// <param name="statusCode">要发送的状态代码</param>
        /// <param name="statusDescription">要发送的状态说明。</param>
        public override void SendStatus(int statusCode, string statusDescription) { _responseStatus = statusCode; }

        /// <summary>
        /// 将标准 HTTP 标头添加到响应。
        /// </summary>
        /// <param name="index">标头索引。例如 <see cref="F:System.Web.HttpWorkerRequest.HeaderContentLength"/>。</param>
        /// <param name="value">标头值。</param>
        public override void SendKnownResponseHeader(int index, string value)
        {
            if (_headersSent) return;
            switch (index)
            {
                case HttpWorkerRequest.HeaderServer:
                case HttpWorkerRequest.HeaderDate:
                case HttpWorkerRequest.HeaderConnection:
                    // ignore these
                    return;
                case HttpWorkerRequest.HeaderAcceptRanges:
                    if (value == "bytes")
                    {
                        // use this header to detect when we're processing a static file
                        _specialCaseStaticFileHeaders = true;
                        return;
                    }
                    break;
                case HttpWorkerRequest.HeaderExpires:
                case HttpWorkerRequest.HeaderLastModified:
                    if (_specialCaseStaticFileHeaders)
                    {
                        // NOTE: Ignore these for static files. These are generated
                        //       by the StaticFileHandler, but they shouldn't be.
                        return;
                    }
                    break;
            }
            _responseHeadersBuilder.Append(GetKnownResponseHeaderName(index));
            _responseHeadersBuilder.Append(": ");
            _responseHeadersBuilder.Append(value);
            _responseHeadersBuilder.Append("\r\n");
        }

        /// <summary>
        /// 将非标准 HTTP 标头添加到响应。
        /// </summary>
        /// <param name="name">要发送的标头的名称。</param>
        /// <param name="value">标头的值。</param>
        public override void SendUnknownResponseHeader(string name, string value)
        {
            if (_headersSent) return;
            _responseHeadersBuilder.Append(name);
            _responseHeadersBuilder.Append(": ");
            _responseHeadersBuilder.Append(value);
            _responseHeadersBuilder.Append("\r\n");
        }

        /// <summary>
        /// 将 Content-Length HTTP 标头添加到小于或等于 2 GB 的消息正文的响应。
        /// </summary>
        /// <param name="contentLength">响应的长度（以字节为单位）。</param>
        public override void SendCalculatedContentLength(int contentLength)
        {
            if (!_headersSent)
            {
                _responseHeadersBuilder.Append("Content-Length: ");
                _responseHeadersBuilder.Append(contentLength.ToString(CultureInfo.InvariantCulture));
                _responseHeadersBuilder.Append("\r\n");
            }
        }

        /// <summary>
        /// 返回一个值，该值指示是否已为当前的请求将 HTTP 响应标头发送到客户端。
        /// </summary>
        /// <returns>如果 HTTP 响应标头已发送到客户端，则为 true；否则，为 false。</returns>
        public override bool HeadersSent() { return _headersSent; }

        /// <summary>
        /// 返回一个值，该值指示客户端连接是否仍处于活动状态。
        /// </summary>
        /// <returns>如果客户端连接仍处于活动状态，则为 true；否则，为 false。</returns>
        public override bool IsClientConnected()
        {
            _connectionPermission.Assert();
            return _connection.Connected;
        }

        /// <summary>
        /// 终止与客户端的连接。
        /// </summary>
        public override void CloseConnection()
        {
            _connectionPermission.Assert();
            _connection.Close();
            _connection.Dispose();
        }

        /// <summary>
        /// 将字节数组的内容添加到响应并指定要发送的字节数。
        /// </summary>
        /// <param name="data">要发送的字节数组。</param>
        /// <param name="length">要发送的字节数。</param>
        public override void SendResponseFromMemory(byte[] data, int length)
        {
            if (length > 0)
            {
                byte[] bytes = new byte[length];
                Buffer.BlockCopy(data, 0, bytes, 0, length);
                _responseBodyBytes.Add(bytes);
            }
        }

        /// <summary>
        /// 将具有指定名称的文件的内容添加到响应并指定文件中的起始位置和要发送的字节数。
        /// </summary>
        /// <param name="filename">要发送的文件的名称。</param>
        /// <param name="offset">文件中的起始位置。</param>
        /// <param name="length">要发送的字节数。</param>
        public override void SendResponseFromFile(string filename, long offset, long length)
        {
            if (length == 0)
            {
                return;
            }

            FileStream f = null;
            try
            {
                f = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                SendResponseFromFileStream(f, offset, length);
            }
            finally
            {
                if (f != null)
                {
                    f.Close();
                }
            }
        }

        /// <summary>
        /// 将具有指定句柄的文件的内容添加到响应并指定文件中的起始位置和要发送的字节数。
        /// </summary>
        /// <param name="handle">要发送的文件的句柄。</param>
        /// <param name="offset">文件中的起始位置。</param>
        /// <param name="length">要发送的字节数。</param>
        public override void SendResponseFromFile(IntPtr handle, long offset, long length)
        {
            if (length == 0)
            {
                return;
            }

            FileStream f = null;
            try
            {
                SafeFileHandle sfh = new SafeFileHandle(handle, false);
                f = new FileStream(sfh, FileAccess.Read);
                SendResponseFromFileStream(f, offset, length);
            }
            finally
            {
                if (f != null)
                {
                    f.Close();
                    f = null;
                }
            }
        }

        void SendResponseFromFileStream(FileStream f, long offset, long length)
        {
            long fileSize = f.Length;
            if (length == -1) length = fileSize - offset;

            if (length == 0 || offset < 0 || length > fileSize - offset)
            {
                return;
            }

            if (offset > 0) f.Seek(offset, SeekOrigin.Begin);

            if (length <= MaxChunkLength)
            {
                byte[] fileBytes = new byte[(int)length];
                int bytesRead = f.Read(fileBytes, 0, (int)length);
                SendResponseFromMemory(fileBytes, bytesRead);
            }
            else
            {
                byte[] chunk = new byte[MaxChunkLength];
                int bytesRemaining = (int)length;

                while (bytesRemaining > 0)
                {
                    int bytesToRead = (bytesRemaining < MaxChunkLength) ? bytesRemaining : MaxChunkLength;
                    int bytesRead = f.Read(chunk, 0, bytesToRead);

                    SendResponseFromMemory(chunk, bytesRead);
                    bytesRemaining -= bytesRead;

                    // flush to release keep memory
                    if ((bytesRemaining > 0) && (bytesRead > 0))
                    {
                        FlushResponse(false);
                    }
                }
            }
        }

        /// <summary>
        /// 将所有挂起的响应数据发送到客户端。
        /// </summary>
        /// <param name="finalFlush">如果这将是最后一次刷新响应数据，则为 true；否则为 false。</param>
        public override void FlushResponse(bool finalFlush)
        {
            if (_responseStatus == 404 && !_headersSent && finalFlush && _verb == "GET")
            {
                // attempt directory listing
                if (ProcessDirectoryListingRequest())
                    return;
            }

            _connectionPermission.Assert();
            if (!_headersSent)
            {
                _connection.WriteHeaders(_responseStatus, _responseHeadersBuilder.ToString(), _connection.KeepAlive);
                _headersSent = true;
            }

            for (int i = 0; i < _responseBodyBytes.Count; i++)
            {
                byte[] bytes = _responseBodyBytes[i];
                _connection.WriteBody(bytes, 0, bytes.Length);
            }

            _responseBodyBytes = new List<byte[]>();
            if (finalFlush)
            {
                _connection.MaxAccessCount--;
                if (!_connection.KeepAlive) { _connection.CloseAndDispose(); }
            }
        }

        /// <summary>
        /// 通知 <see cref="T:System.Web.HttpWorkerRequest"/> 当前请求的请求处理已完成。
        /// </summary>
        public override void EndOfRequest()
        {
            if (!_connection.KeepAlive)
            {
                Connection conn = _connection;
                if (conn != null)
                {
                    _server.OnRequestEnd(conn);
                    _connection = null;
                }
            }
        }

        #endregion

    }
}
