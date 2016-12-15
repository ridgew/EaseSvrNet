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
        /// HTTPͷ�����ֽ�����
        /// </summary>
        byte[] _httpSendBytes;
        int _startHeadersOffset;
        /// <summary>
        /// HTTPͷ����λ��
        /// </summary>
        int _endHeadersOffset;
        List<ByteString> _headerByteStrings;

        // parsed request data

        bool _isClientScriptPath;
        /// <summary>
        /// ����ν��
        /// </summary>
        string _verb;
        /// <summary>
        /// �����ַ
        /// </summary>
        string _url;
        /// <summary>
        /// ����Э��
        /// </summary>
        string _prot;
        string _path, _filePath;
        /// <summary>
        /// �����ļ�������·����Ϣ
        /// </summary>
        string _pathInfo;
        /// <summary>
        /// ʵ�����������ļ���ַ
        /// </summary>
        string _pathTranslated;
        /// <summary>
        /// �����ļ��Ĳ�ѯ����
        /// </summary>
        string _queryString;
        byte[] _queryStringBytes;

        /// <summary>
        /// �����峤��
        /// </summary>
        int _contentLength;
        /// <summary>
        /// Ԥ�ȼ��ص������峤��
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
        /// ����Ӧ���ֽ����鼯��
        /// </summary>
        List<byte[]> _responseBodyBytes;

        /// <summary>
        /// ��ʼ��һ�� <see cref="Request"/> class ʵ����
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
        /// ҵ��Ӧ�ô���
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
        /// ��ȡЭ��ͷ
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
        /// URL����ת��
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
        /// ��������� URI ������·����
        /// </summary>
        /// <returns>����� URI ��·����</returns>
        public override string GetUriPath()
        {
            return _path;
        }

        /// <summary>
        /// �������� URL ��ָ���Ĳ�ѯ�ַ�����
        /// </summary>
        /// <returns>�����ѯ�ַ�����</returns>
        public override string GetQueryString()
        {
            return _queryString;
        }

        /// <summary>
        /// ���������б���дʱ�����ֽ��������ʽ������Ӧ��ѯ�ַ�����
        /// </summary>
        /// <returns>������Ӧ���ֽ����顣</returns>
        public override byte[] GetQueryStringRawBytes()
        {
            return _queryStringBytes;
        }

        /// <summary>
        /// ���ظ����˲�ѯ�ַ����������ͷ�а����� URL ·����
        /// </summary>
        /// <returns>
        /// �����ͷ��ԭʼ URL ·����˵�������ص� URL δ����������ʹ�� URL ���з��ʿ��ƻ�ȫ�����Ծ��߻ὫӦ�ó���¶�ڹ淶����ȫ©��֮�¡�
        /// </returns>
        public override string GetRawUrl() { return _url; }

        /// <summary>
        /// ���� HTTP ����ν�ʡ�
        /// </summary>
        /// <returns>������� HTTP ν�ʡ�</returns>
        public override string GetHttpVerbName() { return _verb; }

        /// <summary>
        /// ��������� HTTP �汾�ַ��������硰HTTP/1.1������
        /// </summary>
        /// <returns>�����ͷ�з��ص� HTTP �汾�ַ�����</returns>
        public override string GetHttpVersion() { return _prot; }

        /// <summary>
        /// ���ؿͻ��˵� IP ��ַ��
        /// </summary>
        /// <returns>�ͻ��˵� IP ��ַ��</returns>
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
        /// ���ؿͻ��˵Ķ˿ںš�
        /// </summary>
        /// <returns>�ͻ��˵Ķ˿ںš�</returns>
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
        /// �����յ�����Ľӿڵķ����� IP ��ַ��
        /// </summary>
        /// <returns>�յ�����Ľӿڵķ����� IP ��ַ��</returns>
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
        /// ���������б���дʱ�����ر��ط����������ơ�
        /// </summary>
        /// <returns>���ط����������ơ�</returns>
        public override string GetServerName()
        {
            string localAddress = GetLocalAddress();
            if (localAddress.Equals("127.0.0.1")) return "localhost";
            return localAddress;
        }

        /// <summary>
        /// �����յ�����Ķ˿ںš�
        /// </summary>
        /// <returns>�յ�����ķ������˿ںš�</returns>
        public override int GetLocalPort() { return _host.Port; }

        /// <summary>
        /// ��������� URI ������·����
        /// </summary>
        /// <returns>����� URI ������·����</returns>
        public override string GetFilePath() { return _filePath; }

        /// <summary>
        /// ��������� URI �������ļ�·���������������·��ת��������·�������磬�ӡ�/proj1/page.aspx��ת���ɡ�c:\dir\page.aspx����
        /// </summary>
        /// <returns>����� URI ����ת���������ļ�·����</returns>
        public override string GetFilePathTranslated() { return _pathTranslated; }

        /// <summary>
        /// ���ؾ��� URL ��չ����Դ������·����Ϣ��������·�� /virdir/page.html/tail������ֵΪ /tail��
        /// </summary>
        /// <returns>��Դ�ĸ���·����Ϣ��</returns>
        public override string GetPathInfo() { return _pathInfo; }

        /// <summary>
        /// ���ص�ǰ����ִ�еķ�����Ӧ�ó��������·����
        /// </summary>
        /// <returns>��ǰӦ�ó��������·����</returns>
        public override string GetAppPath() { return _host.VirtualPath; }

        /// <summary>
        /// ���ص�ǰ����ִ�еķ�����Ӧ�ó���� UNC ����·����
        /// </summary>
        /// <returns>��ǰӦ�ó��������·����</returns>
        public override string GetAppPathTranslated() { return _host.PhysicalPath; }

        /// <summary>
        /// ���� HTTP ���������ѱ���ȡ�Ĳ��֡�
        /// </summary>
        /// <returns>HTTP ���������ѱ���ȡ�Ĳ��֡�</returns>
        public override byte[] GetPreloadedEntityBody() { return _preloadedContent; }

        /// <summary>
        /// ����һ��ֵ����ֵָʾ�Ƿ������������ݶ����ã��Լ��Ƿ���Ҫ�Կͻ��˽��н�һ����ȡ��
        /// </summary>
        /// <returns>��������������ݶ����ã���Ϊ true������Ϊ false��</returns>
        public override bool IsEntireEntityBodyIsPreloaded()
        {
            return (_contentLength == _preloadedContentLength);
        }

        /// <summary>
        /// ��ȡ�ͻ��˵��������ݣ�����δԤ����ʱ����
        /// </summary>
        /// <param name="buffer">�����ݶ�����ֽ����顣</param>
        /// <param name="size">����ȡ���ֽ�����</param>
        /// <returns>��ȡ���ֽ�����</returns>
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
        /// ������ָ�����������Ӧ�ı�׼ HTTP �����ͷ��
        /// </summary>
        /// <param name="index">��ͷ�����������磬<see cref="F:System.Web.HttpWorkerRequest.HeaderAllow"/> �ֶΡ�</param>
        /// <returns>HTTP �����ͷ��</returns>
        public override string GetKnownRequestHeader(int index) { return _knownRequestHeaders[index]; }

        /// <summary>
        /// ���طǱ�׼�� HTTP �����ͷֵ��
        /// </summary>
        /// <param name="name">��ͷ���ơ�</param>
        /// <returns>��ͷֵ��</returns>
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
        /// ��ȡ���зǱ�׼�� HTTP ��ͷ������/ֵ�ԡ�
        /// </summary>
        /// <returns>��ͷ������/ֵ�Ե����顣</returns>
        public override string[][] GetUnknownRequestHeaders() { return _unknownRequestHeaders; }

        /// <summary>
        /// ������������ķ����������ʵ䷵�ص���������������
        /// </summary>
        /// <param name="name">����ķ��������������ơ�</param>
        /// <returns>����ķ�����������</returns>
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
        /// ������ָ������·�����Ӧ������·����
        /// </summary>
        /// <param name="path">����·����</param>
        /// <returns>
        /// �� <paramref name="path"/> ������ָ��������·�����Ӧ������·����
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
        /// ָ����Ӧ�� HTTP ״̬�����״̬˵�������� SendStatus(200, "Ok")��
        /// </summary>
        /// <param name="statusCode">Ҫ���͵�״̬����</param>
        /// <param name="statusDescription">Ҫ���͵�״̬˵����</param>
        public override void SendStatus(int statusCode, string statusDescription) { _responseStatus = statusCode; }

        /// <summary>
        /// ����׼ HTTP ��ͷ��ӵ���Ӧ��
        /// </summary>
        /// <param name="index">��ͷ���������� <see cref="F:System.Web.HttpWorkerRequest.HeaderContentLength"/>��</param>
        /// <param name="value">��ͷֵ��</param>
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
        /// ���Ǳ�׼ HTTP ��ͷ��ӵ���Ӧ��
        /// </summary>
        /// <param name="name">Ҫ���͵ı�ͷ�����ơ�</param>
        /// <param name="value">��ͷ��ֵ��</param>
        public override void SendUnknownResponseHeader(string name, string value)
        {
            if (_headersSent) return;
            _responseHeadersBuilder.Append(name);
            _responseHeadersBuilder.Append(": ");
            _responseHeadersBuilder.Append(value);
            _responseHeadersBuilder.Append("\r\n");
        }

        /// <summary>
        /// �� Content-Length HTTP ��ͷ��ӵ�С�ڻ���� 2 GB ����Ϣ���ĵ���Ӧ��
        /// </summary>
        /// <param name="contentLength">��Ӧ�ĳ��ȣ����ֽ�Ϊ��λ����</param>
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
        /// ����һ��ֵ����ֵָʾ�Ƿ���Ϊ��ǰ������ HTTP ��Ӧ��ͷ���͵��ͻ��ˡ�
        /// </summary>
        /// <returns>��� HTTP ��Ӧ��ͷ�ѷ��͵��ͻ��ˣ���Ϊ true������Ϊ false��</returns>
        public override bool HeadersSent() { return _headersSent; }

        /// <summary>
        /// ����һ��ֵ����ֵָʾ�ͻ��������Ƿ��Դ��ڻ״̬��
        /// </summary>
        /// <returns>����ͻ��������Դ��ڻ״̬����Ϊ true������Ϊ false��</returns>
        public override bool IsClientConnected()
        {
            _connectionPermission.Assert();
            return _connection.Connected;
        }

        /// <summary>
        /// ��ֹ��ͻ��˵����ӡ�
        /// </summary>
        public override void CloseConnection()
        {
            _connectionPermission.Assert();
            _connection.Close();
            _connection.Dispose();
        }

        /// <summary>
        /// ���ֽ������������ӵ���Ӧ��ָ��Ҫ���͵��ֽ�����
        /// </summary>
        /// <param name="data">Ҫ���͵��ֽ����顣</param>
        /// <param name="length">Ҫ���͵��ֽ�����</param>
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
        /// ������ָ�����Ƶ��ļ���������ӵ���Ӧ��ָ���ļ��е���ʼλ�ú�Ҫ���͵��ֽ�����
        /// </summary>
        /// <param name="filename">Ҫ���͵��ļ������ơ�</param>
        /// <param name="offset">�ļ��е���ʼλ�á�</param>
        /// <param name="length">Ҫ���͵��ֽ�����</param>
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
        /// ������ָ��������ļ���������ӵ���Ӧ��ָ���ļ��е���ʼλ�ú�Ҫ���͵��ֽ�����
        /// </summary>
        /// <param name="handle">Ҫ���͵��ļ��ľ����</param>
        /// <param name="offset">�ļ��е���ʼλ�á�</param>
        /// <param name="length">Ҫ���͵��ֽ�����</param>
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
        /// �����й������Ӧ���ݷ��͵��ͻ��ˡ�
        /// </summary>
        /// <param name="finalFlush">����⽫�����һ��ˢ����Ӧ���ݣ���Ϊ true������Ϊ false��</param>
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
        /// ֪ͨ <see cref="T:System.Web.HttpWorkerRequest"/> ��ǰ���������������ɡ�
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
