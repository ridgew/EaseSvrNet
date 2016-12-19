using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Collections;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    /// 静态文件的压缩格式输出
    /// </summary>
    /// <example>
    /// 
    /// 映射节点配置
    /// 
    ///  &lt;HandlerItem MappingUrl="/GZip/js.ashx" IsPattern="false" TypeFullName="GWSoft.PMS.BLL.GZipHandler, GWSoft.PMS.BLL" /&gt;
    /// 
    /// URL: /Gzip/js.ashx/Ext/ext-all-debug.js
    /// 
    /// 表示输出/Ext/ext-all-debug.js文件的GZip格式，如浏览器客户端支持。
    /// </example>
    public class GZipHandler : IHttpHandler
    {
        //http://pesta.googlecode.com/svn-history/r140/trunk/pesta/pesta/Utilities/ASPNETMVC/StaticFileHandler.cs
        private readonly static TimeSpan DEFAULT_CACHE_DURATION = TimeSpan.FromDays(30);
        private readonly static string[] FILE_TYPES =
            new string[] { ".css", ".js", ".html", ".htm", ".png", ".jpeg", ".jpg", ".gif", ".bmp" };

        private readonly static string[] COMPRESS_FILE_TYPES =
            new string[] { ".css", ".js", ".html", ".htm" };

        static GZipHandler()
        {
            Array.Sort(FILE_TYPES);
            Array.Sort(COMPRESS_FILE_TYPES);
        }

        static Encoding GetBufferEncoding(byte[] fileBytes)
        {
            Encoding enc = Encoding.UTF8;
            byte[] buffer = new byte[4];
            if (fileBytes.Length >= 4)
            {
                Buffer.BlockCopy(fileBytes, 0, buffer, 0, buffer.Length);

                if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
                    enc = Encoding.UTF8;
                else if (buffer[0] == 0xfe && buffer[1] == 0xff)
                    enc = Encoding.Unicode;
                else if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
                    enc = Encoding.UTF32;
                else if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
                    enc = Encoding.UTF7;
            }
            return enc;
        }

        private enum ResponseCompressionType { None, GZip, Deflate }

        private class CachedContent
        {
            public byte[] ResponseBytes;
            public DateTime LastModified;
            public Encoding ContentEncoding = Encoding.Default;

            public CachedContent(byte[] bytes, DateTime lastModified, Encoding contentEncoding)
            {
                if (contentEncoding != null)
                    ContentEncoding = contentEncoding;

                this.ResponseBytes = bytes;
                // milliseconds in If-Modified-Since header is always 0 
                this.LastModified = new DateTime(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second);
            }
        }

        private ResponseCompressionType GetCompressionMode(HttpRequest request)
        {
            string acceptEncoding = request.Headers["Accept-Encoding"];
            if (string.IsNullOrEmpty(acceptEncoding)) return ResponseCompressionType.None;
            acceptEncoding = acceptEncoding.ToUpperInvariant();
            if (acceptEncoding.Contains("DEFLATE"))
                return ResponseCompressionType.Deflate;
            else if (acceptEncoding.Contains("GZIP"))
                return ResponseCompressionType.GZip;
            else
                return ResponseCompressionType.None;
        }

        private void ProduceResponseHeader(HttpResponse response, int count, ResponseCompressionType mode, string physicalFilePath,
           DateTime lastModified, string charset)
        {
            response.Buffer = false;
            response.BufferOutput = false;

            // Emit content type and encoding based on the file extension and 
            // whether the response is compressed
            response.ContentType = MimeMapping.GetMimeMapping(physicalFilePath);
            if (mode != ResponseCompressionType.None)
                response.AppendHeader("Content-Encoding", mode.ToString().ToLower());

            if (!string.IsNullOrEmpty(charset) &&
                (response.ContentType.StartsWith("text", StringComparison.InvariantCultureIgnoreCase)
                || response.ContentType.IndexOf("javascript", StringComparison.InvariantCultureIgnoreCase) != -1))
            {
                response.Charset = charset;
            }

            response.AppendHeader("Content-Length", count.ToString());
            // Emit proper cache headers that will cache the response in browser's 
            // cache for the default cache duration
            response.Cache.SetCacheability(HttpCacheability.Public);
            response.Cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
            response.Cache.SetMaxAge(DEFAULT_CACHE_DURATION);
            response.Cache.SetExpires(DateTime.Now.Add(DEFAULT_CACHE_DURATION));
            try
            {
                response.Cache.SetLastModified(lastModified);
            }
            catch (Exception) { }
        }

        private void TransmitFileUsingHttpResponse(HttpRequest request, HttpResponse response,
            string physicalFilePath, ResponseCompressionType compressionType, FileInfo file)
        {
            if (file.Exists)
            {
                // We don't cache/compress such file types. Must be some binary file that's better
                // to let IIS handle
                this.ProduceResponseHeader(response, Convert.ToInt32(file.Length), compressionType,
                    physicalFilePath, file.LastWriteTimeUtc, "utf-8");
                response.TransmitFile(physicalFilePath);

                Debug.WriteLine("TransmitFile: " + request.FilePath);
            }
        }

        private bool DeliverFromCache(HttpContext context,
            HttpRequest request, HttpResponse response,
            string cacheKey,
            string physicalFilePath, ResponseCompressionType compressionType)
        {
            CachedContent cachedContent = context.Cache[cacheKey] as CachedContent;
            if (null != cachedContent)
            {
                if (request.Headers["If-Modified-Since"] != null)
                {
                    string modSince = request.Headers["If-Modified-Since"];
                    if (modSince.IndexOf(";") > 0)
                    {
                        modSince = modSince.Split(';')[0];
                    }
                    DateTime modSinced = Convert.ToDateTime(modSince).ToUniversalTime();
                    //
                    if (DateTime.Compare(modSinced, cachedContent.LastModified.ToUniversalTime()) >= 0)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotModified;
                        return true;
                    }
                    // unreachable as cache would be invalidated if file was modified but added just in case
                    return false;
                }

                byte[] cachedBytes = cachedContent.ResponseBytes;

                // We have it cached
                this.ProduceResponseHeader(response, cachedBytes.Length, compressionType,
                    physicalFilePath, cachedContent.LastModified, cachedContent.ContentEncoding.WebName);
                this.WriteResponse(response, cachedBytes, compressionType, physicalFilePath);

                Debug.WriteLine("StaticFileHandler: Cached: " + request.FilePath);
                return true;
            }
            return false;
        }

        private void CacheAndDeliver(HttpContext context,
            HttpRequest request, HttpResponse response,
            string physicalFilePath, ResponseCompressionType compressionType,
            string cacheKey, MemoryStream memoryStream, FileInfo file)
        {
            // Cache the content in ASP.NET Cache
            byte[] responseBytes = memoryStream.ToArray();
            CachedContent cache = new CachedContent(responseBytes, file.LastWriteTimeUtc, GetBufferEncoding(responseBytes));
            context.Cache.Insert(cacheKey, cache, new CacheDependency(physicalFilePath), DateTime.Now.Add(DEFAULT_CACHE_DURATION), Cache.NoSlidingExpiration);
            this.ProduceResponseHeader(response, responseBytes.Length, compressionType,
                physicalFilePath, file.LastWriteTimeUtc, cache.ContentEncoding.WebName);

            this.WriteResponse(response, responseBytes, compressionType, physicalFilePath);

            Debug.WriteLine("StaticFileHandler: NonCached: " + request.FilePath);
        }

        private void WriteResponse(HttpResponse response, byte[] bytes, ResponseCompressionType mode, string physicalFilePath)
        {
            if (bytes.Length > 0)
            {
                try
                {
                    response.OutputStream.Write(bytes, 0, bytes.Length);
                    if (response.IsClientConnected) response.OutputStream.Flush();
                }
                catch { }
            }
        }

        private static void ReadFileData(ResponseCompressionType compressionType, FileInfo file, MemoryStream memoryStream)
        {
            using (Stream outputStream =
                (compressionType == ResponseCompressionType.None ? memoryStream :
                (compressionType == ResponseCompressionType.GZip ?
                    (Stream)new GZipStream(memoryStream, CompressionMode.Compress, true) :
                    (Stream)new DeflateStream(memoryStream, CompressionMode.Compress))))
            {
                // We can compress and cache this file
                using (FileStream fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {

                    int bufSize = Convert.ToInt32(Math.Min(file.Length, 8 * 1024));
                    byte[] buffer = new byte[bufSize];

                    int bytesRead;
                    while ((bytesRead = fs.Read(buffer, 0, bufSize)) > 0)
                    {
                        outputStream.Write(buffer, 0, bytesRead);
                    }
                }

                outputStream.Flush();
            }
        }

        /// <summary>
        /// 通过实现 <see cref="T:System.Web.IHttpHandler"/> 接口的自定义 HttpHandler 启用 HTTP Web 请求的处理。
        /// </summary>
        /// <param name="context"><see cref="T:System.Web.HttpContext"/> 对象，它提供对用于为 HTTP 请求提供服务的内部服务器对象（如 Request、Response、Session 和 Server）的引用。</param>
        public void ProcessRequest(HttpContext context)
        {
            HttpContext Context = HttpContext.Current;
            HttpRequest Request = Context.Request;
            HttpResponse Response = Context.Response;
            string scriptFilePath = context.Server.MapPath(Request.Path);
            if (Request.PathInfo.Length > 2)
            {
                scriptFilePath = context.Server.MapPath(Request.PathInfo);
            }

            if (!File.Exists(scriptFilePath))
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
                Response.Write(Request.FilePath + " Not Found");
                Response.End();
                return;
            }

            ResponseCompressionType physicalFilePath = GetCompressionMode(Request);

            FileInfo file = new FileInfo(scriptFilePath);
            string fileExtension = file.Extension.ToLower();
            if (!(Array.BinarySearch(FILE_TYPES, fileExtension) >= 0))
            {
                TransmitFileUsingHttpResponse(Request, Response, scriptFilePath, physicalFilePath, file);
            }
            else
            {
                #region process
                // If this is a binary file like image, then we won't compress it.
                if (Array.BinarySearch(COMPRESS_FILE_TYPES, fileExtension) < 0)
                    physicalFilePath = ResponseCompressionType.None;

                // If the response bytes are already cached, then deliver the bytes directly from cache
                string cacheKey = this.GetType() + ":" + physicalFilePath + ":" + scriptFilePath;

                if (!DeliverFromCache(context, Request, Response,
                    cacheKey, scriptFilePath, physicalFilePath))
                {
                    // When not compressed, buffer is the size of the file but when compressed, 
                    // initial buffer size is one third of the file size. Assuming, compression 
                    // will give us less than 1/3rd of the size
                    using (MemoryStream memoryStream = new MemoryStream(
                        physicalFilePath == ResponseCompressionType.None ?
                            Convert.ToInt32(file.Length) :
                            Convert.ToInt32((double)file.Length / 3)))
                    {
                        ReadFileData(physicalFilePath, file, memoryStream);
                        CacheAndDeliver(context, Request, Response, scriptFilePath, physicalFilePath, cacheKey, memoryStream, file);
                    }
                }
                #endregion
            }
        }

        /// <summary>
        /// 获取一个值，该值指示其他请求是否可以使用 <see cref="T:System.Web.IHttpHandler"/> 实例。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果 <see cref="T:System.Web.IHttpHandler"/> 实例可再次使用，则为 true；否则为 false。
        /// </returns>
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

    }

    #region MimeMapping
    internal class MimeMapping
    {
        // Fields
        private static Hashtable _extensionToMimeMappingTable = new Hashtable(190, StringComparer.CurrentCultureIgnoreCase);

        // Methods
        static MimeMapping()
        {
            AddMimeMapping(".323", "text/h323");
            AddMimeMapping(".asx", "video/x-ms-asf");
            AddMimeMapping(".acx", "application/internet-property-stream");
            AddMimeMapping(".ai", "application/postscript");
            AddMimeMapping(".aif", "audio/x-aiff");
            AddMimeMapping(".aiff", "audio/aiff");
            AddMimeMapping(".axs", "application/olescript");
            AddMimeMapping(".aifc", "audio/aiff");
            AddMimeMapping(".asr", "video/x-ms-asf");
            AddMimeMapping(".avi", "video/x-msvideo");
            AddMimeMapping(".asf", "video/x-ms-asf");
            AddMimeMapping(".au", "audio/basic");
            AddMimeMapping(".application", "application/x-ms-application");
            AddMimeMapping(".bin", "application/octet-stream");
            AddMimeMapping(".bas", "text/plain");
            AddMimeMapping(".bcpio", "application/x-bcpio");
            AddMimeMapping(".bmp", "image/bmp");
            AddMimeMapping(".cdf", "application/x-cdf");
            AddMimeMapping(".cat", "application/vndms-pkiseccat");
            AddMimeMapping(".crt", "application/x-x509-ca-cert");
            AddMimeMapping(".c", "text/plain");
            AddMimeMapping(".css", "text/css");
            AddMimeMapping(".cer", "application/x-x509-ca-cert");
            AddMimeMapping(".crl", "application/pkix-crl");
            AddMimeMapping(".cmx", "image/x-cmx");
            AddMimeMapping(".csh", "application/x-csh");
            AddMimeMapping(".cod", "image/cis-cod");
            AddMimeMapping(".cpio", "application/x-cpio");
            AddMimeMapping(".clp", "application/x-msclip");
            AddMimeMapping(".crd", "application/x-mscardfile");
            AddMimeMapping(".deploy", "application/octet-stream");
            AddMimeMapping(".dll", "application/x-msdownload");
            AddMimeMapping(".dot", "application/msword");
            AddMimeMapping(".doc", "application/msword");
            AddMimeMapping(".dvi", "application/x-dvi");
            AddMimeMapping(".dir", "application/x-director");
            AddMimeMapping(".dxr", "application/x-director");
            AddMimeMapping(".der", "application/x-x509-ca-cert");
            AddMimeMapping(".dib", "image/bmp");
            AddMimeMapping(".dcr", "application/x-director");
            AddMimeMapping(".disco", "text/xml");
            AddMimeMapping(".exe", "application/octet-stream");
            AddMimeMapping(".etx", "text/x-setext");
            AddMimeMapping(".evy", "application/envoy");
            AddMimeMapping(".eml", "message/rfc822");
            AddMimeMapping(".eps", "application/postscript");
            AddMimeMapping(".flr", "x-world/x-vrml");
            AddMimeMapping(".fif", "application/fractals");
            AddMimeMapping(".gtar", "application/x-gtar");
            AddMimeMapping(".gif", "image/gif");
            AddMimeMapping(".gz", "application/x-gzip");
            AddMimeMapping(".hta", "application/hta");
            AddMimeMapping(".htc", "text/x-component");
            AddMimeMapping(".htt", "text/webviewhtml");
            AddMimeMapping(".h", "text/plain");
            AddMimeMapping(".hdf", "application/x-hdf");
            AddMimeMapping(".hlp", "application/winhlp");
            AddMimeMapping(".html", "text/html");
            AddMimeMapping(".htm", "text/html");
            AddMimeMapping(".hqx", "application/mac-binhex40");
            AddMimeMapping(".isp", "application/x-internet-signup");
            AddMimeMapping(".iii", "application/x-iphone");
            AddMimeMapping(".ief", "image/ief");
            AddMimeMapping(".ivf", "video/x-ivf");
            AddMimeMapping(".ins", "application/x-internet-signup");
            AddMimeMapping(".ico", "image/x-icon");
            AddMimeMapping(".jpg", "image/jpeg");
            AddMimeMapping(".jfif", "image/pjpeg");
            AddMimeMapping(".jpe", "image/jpeg");
            AddMimeMapping(".jpeg", "image/jpeg");
            AddMimeMapping(".js", "application/x-javascript");
            AddMimeMapping(".lsx", "video/x-la-asf");
            AddMimeMapping(".latex", "application/x-latex");
            AddMimeMapping(".lsf", "video/x-la-asf");
            AddMimeMapping(".manifest", "application/x-ms-manifest");
            AddMimeMapping(".mhtml", "message/rfc822");
            AddMimeMapping(".mny", "application/x-msmoney");
            AddMimeMapping(".mht", "message/rfc822");
            AddMimeMapping(".mid", "audio/mid");
            AddMimeMapping(".mpv2", "video/mpeg");
            AddMimeMapping(".man", "application/x-troff-man");
            AddMimeMapping(".mvb", "application/x-msmediaview");
            AddMimeMapping(".mpeg", "video/mpeg");
            AddMimeMapping(".m3u", "audio/x-mpegurl");
            AddMimeMapping(".mdb", "application/x-msaccess");
            AddMimeMapping(".mpp", "application/vnd.ms-project");
            AddMimeMapping(".m1v", "video/mpeg");
            AddMimeMapping(".mpa", "video/mpeg");
            AddMimeMapping(".me", "application/x-troff-me");
            AddMimeMapping(".m13", "application/x-msmediaview");
            AddMimeMapping(".movie", "video/x-sgi-movie");
            AddMimeMapping(".m14", "application/x-msmediaview");
            AddMimeMapping(".mpe", "video/mpeg");
            AddMimeMapping(".mp2", "video/mpeg");
            AddMimeMapping(".mov", "video/quicktime");
            AddMimeMapping(".mp3", "audio/mpeg");
            AddMimeMapping(".mpg", "video/mpeg");
            AddMimeMapping(".ms", "application/x-troff-ms");
            AddMimeMapping(".nc", "application/x-netcdf");
            AddMimeMapping(".nws", "message/rfc822");
            AddMimeMapping(".oda", "application/oda");
            AddMimeMapping(".ods", "application/oleobject");
            AddMimeMapping(".pmc", "application/x-perfmon");
            AddMimeMapping(".p7r", "application/x-pkcs7-certreqresp");
            AddMimeMapping(".p7b", "application/x-pkcs7-certificates");
            AddMimeMapping(".p7s", "application/pkcs7-signature");
            AddMimeMapping(".pmw", "application/x-perfmon");
            AddMimeMapping(".ps", "application/postscript");
            AddMimeMapping(".p7c", "application/pkcs7-mime");
            AddMimeMapping(".pbm", "image/x-portable-bitmap");
            AddMimeMapping(".ppm", "image/x-portable-pixmap");
            AddMimeMapping(".pub", "application/x-mspublisher");
            AddMimeMapping(".pnm", "image/x-portable-anymap");
            AddMimeMapping(".png", "image/png");
            AddMimeMapping(".pml", "application/x-perfmon");
            AddMimeMapping(".p10", "application/pkcs10");
            AddMimeMapping(".pfx", "application/x-pkcs12");
            AddMimeMapping(".p12", "application/x-pkcs12");
            AddMimeMapping(".pdf", "application/pdf");
            AddMimeMapping(".pps", "application/vnd.ms-powerpoint");
            AddMimeMapping(".p7m", "application/pkcs7-mime");
            AddMimeMapping(".pko", "application/vndms-pkipko");
            AddMimeMapping(".ppt", "application/vnd.ms-powerpoint");
            AddMimeMapping(".pmr", "application/x-perfmon");
            AddMimeMapping(".pma", "application/x-perfmon");
            AddMimeMapping(".pot", "application/vnd.ms-powerpoint");
            AddMimeMapping(".prf", "application/pics-rules");
            AddMimeMapping(".pgm", "image/x-portable-graymap");
            AddMimeMapping(".qt", "video/quicktime");
            AddMimeMapping(".ra", "audio/x-pn-realaudio");
            AddMimeMapping(".rgb", "image/x-rgb");
            AddMimeMapping(".ram", "audio/x-pn-realaudio");
            AddMimeMapping(".rmi", "audio/mid");
            AddMimeMapping(".ras", "image/x-cmu-raster");
            AddMimeMapping(".roff", "application/x-troff");
            AddMimeMapping(".rtf", "application/rtf");
            AddMimeMapping(".rtx", "text/richtext");
            AddMimeMapping(".sv4crc", "application/x-sv4crc");
            AddMimeMapping(".spc", "application/x-pkcs7-certificates");
            AddMimeMapping(".setreg", "application/set-registration-initiation");
            AddMimeMapping(".snd", "audio/basic");
            AddMimeMapping(".stl", "application/vndms-pkistl");
            AddMimeMapping(".setpay", "application/set-payment-initiation");
            AddMimeMapping(".stm", "text/html");
            AddMimeMapping(".shar", "application/x-shar");
            AddMimeMapping(".sh", "application/x-sh");
            AddMimeMapping(".sit", "application/x-stuffit");
            AddMimeMapping(".spl", "application/futuresplash");
            AddMimeMapping(".sct", "text/scriptlet");
            AddMimeMapping(".scd", "application/x-msschedule");
            AddMimeMapping(".sst", "application/vndms-pkicertstore");
            AddMimeMapping(".src", "application/x-wais-source");
            AddMimeMapping(".sv4cpio", "application/x-sv4cpio");
            AddMimeMapping(".tex", "application/x-tex");
            AddMimeMapping(".tgz", "application/x-compressed");
            AddMimeMapping(".t", "application/x-troff");
            AddMimeMapping(".tar", "application/x-tar");
            AddMimeMapping(".tr", "application/x-troff");
            AddMimeMapping(".tif", "image/tiff");
            AddMimeMapping(".txt", "text/plain");
            AddMimeMapping(".texinfo", "application/x-texinfo");
            AddMimeMapping(".trm", "application/x-msterminal");
            AddMimeMapping(".tiff", "image/tiff");
            AddMimeMapping(".tcl", "application/x-tcl");
            AddMimeMapping(".texi", "application/x-texinfo");
            AddMimeMapping(".tsv", "text/tab-separated-values");
            AddMimeMapping(".ustar", "application/x-ustar");
            AddMimeMapping(".uls", "text/iuls");
            AddMimeMapping(".vcf", "text/x-vcard");
            AddMimeMapping(".wps", "application/vnd.ms-works");
            AddMimeMapping(".wav", "audio/wav");
            AddMimeMapping(".wrz", "x-world/x-vrml");
            AddMimeMapping(".wri", "application/x-mswrite");
            AddMimeMapping(".wks", "application/vnd.ms-works");
            AddMimeMapping(".wmf", "application/x-msmetafile");
            AddMimeMapping(".wcm", "application/vnd.ms-works");
            AddMimeMapping(".wrl", "x-world/x-vrml");
            AddMimeMapping(".wdb", "application/vnd.ms-works");
            AddMimeMapping(".wsdl", "text/xml");
            AddMimeMapping(".xml", "text/xml");
            AddMimeMapping(".xlm", "application/vnd.ms-excel");
            AddMimeMapping(".xaf", "x-world/x-vrml");
            AddMimeMapping(".xla", "application/vnd.ms-excel");
            AddMimeMapping(".xls", "application/vnd.ms-excel");
            AddMimeMapping(".xof", "x-world/x-vrml");
            AddMimeMapping(".xlt", "application/vnd.ms-excel");
            AddMimeMapping(".xlc", "application/vnd.ms-excel");
            AddMimeMapping(".xsl", "text/xml");
            AddMimeMapping(".xbm", "image/x-xbitmap");
            AddMimeMapping(".xlw", "application/vnd.ms-excel");
            AddMimeMapping(".xpm", "image/x-xpixmap");
            AddMimeMapping(".xwd", "image/x-xwindowdump");
            AddMimeMapping(".xsd", "text/xml");
            AddMimeMapping(".z", "application/x-compress");
            AddMimeMapping(".zip", "application/x-zip-compressed");
            AddMimeMapping(".*", "application/octet-stream");
        }

        private MimeMapping()
        {
        }

        private static void AddMimeMapping(string extension, string MimeType)
        {
            _extensionToMimeMappingTable.Add(extension, MimeType);
        }

        internal static string GetMimeMapping(string FileName)
        {
            string str = null;
            int startIndex = FileName.LastIndexOf('.');
            if ((0 < startIndex) && (startIndex > FileName.LastIndexOf('\\')))
            {
                str = (string)_extensionToMimeMappingTable[FileName.Substring(startIndex)];
            }
            if (str == null)
            {
                str = (string)_extensionToMimeMappingTable[".*"];
            }
            return str;
        }
    }
    #endregion

}
