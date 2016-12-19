using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace EaseServer.Management
{
    /// <summary>
    /// 为服务器在线更新做处理
    /// </summary>
    public class ServerUpdateHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/html";
            context.Response.Charset = "utf-8";
            HttpRequest request = context.Request;
            string updateResultFilePath = null;
            context.Response.Buffer = false;//不缓冲输出数据

            ProcessModule mainModule = Process.GetCurrentProcess().MainModule;
            string rootWorkDir = Path.GetDirectoryName(mainModule.FileName);

            if (context.Request.HttpMethod == "GET")
            {
                if (string.IsNullOrEmpty(request.QueryString["uid"]))
                {
                    if (request.QueryString["uid"] != null)
                    {
                        context.Response.Write("请准确提交需要查询的任务标识！");
                    }
                    else
                    {
                        WriterHeader(context, "服务器升级在线升级");
                        context.Response.Write(string.Format("* 更新{0} v{1}, 位于系统目录<input type='text' size='55' readonly value='{2}' class='readonly' /><br/>",
                            Path.GetFileName(mainModule.FileVersionInfo.FileName), mainModule.FileVersionInfo.FileVersion, rootWorkDir));
                        context.Response.Write(string.Format("* 应用工作目录<input type='text' size='55' readonly value='{0}' class='readonly' />",
                            AppDomain.CurrentDomain.BaseDirectory));
                        string localFilePath = context.Server.MapPath("/App_Data/ServerUpdate.html");
                        if (File.Exists(localFilePath))
                        {
                            context.Response.TransmitFile(localFilePath);
                        }
                        else
                        {
                            context.Response.Write(ManagementUtil.GetManifestString(this.GetType(), "utf-8", "App_Data/ServerUpdate.html"));
                        }
                        WriterFooter(context);
                    }
                    context.Response.End();
                }
                else
                {
                    updateResultFilePath = request.MapPath("/" + request.QueryString["uid"] + ".html");

                    int currentTryTimes = 0, totalTimes = 4;
                tryOutPutResult:
                    if (!File.Exists(updateResultFilePath))
                    {
                        //context.Response.StatusCode = 404;
                        context.Response.Write("查看更新结果已过期，标识文件已不存在！");
                    }
                    else
                    {
                        try
                        {
                            context.Response.TransmitFile(updateResultFilePath);
                        }
                        catch (IOException)
                        {
                            if (currentTryTimes < totalTimes)
                            {
                                currentTryTimes++;
                                System.Threading.Thread.Sleep(500);
                                goto tryOutPutResult;
                            }
                        }

                        context.Response.Flush();
                        ThreadPool.QueueUserWorkItem(new WaitCallback(o =>
                        {
                            Thread.Sleep(1000);
                            File.Delete(updateResultFilePath);
                        }));
                        context.Response.End();
                    }
                }
            }
            else
            {
                #region 提交数据验证
                System.Text.StringBuilder errorBuilder = new System.Text.StringBuilder();
                if (String.IsNullOrEmpty(request.Form["updateGuid"]))
                {
                    errorBuilder.Append("* 需要指定更新标识(updateGuid)！<br/>");
                }
                else
                {
                    updateResultFilePath = request.MapPath("/" + request.Form["updateGuid"] + ".html");
                }

                if (String.IsNullOrEmpty(request.Form["pkgType"])
                    || ",pzip,pdir,fzip,fdir,none,".IndexOf("," + request.Form["pkgType"] + ",") == -1
                    )
                {
                    errorBuilder.Append("* 备份类型设置错误(pkgType)！<br/>");
                }

                string baseDir = request.Form["updateBaseDir"];  //更新目标目录
                if (String.IsNullOrEmpty(baseDir))
                {
                    errorBuilder.Append("* 需要指定已存在的更新基础目录(updateBaseDir)！<br/>");
                }
                else
                {
                    if (baseDir == "/")
                    {
                        baseDir = rootWorkDir;
                    }
                    else
                    {
                        if (baseDir.IndexOf(Path.VolumeSeparatorChar) == -1)
                        {
                            baseDir = Path.Combine(rootWorkDir, baseDir.TrimStart('/', '\\'));
                        }
                    }
                    if (!Directory.Exists(baseDir))
                        errorBuilder.Append("* 需要指定已存在的更新基础目录(updateBaseDir)！<br/>");
                }

                if (String.IsNullOrEmpty(request.Form["backDirPath"]) || !Directory.Exists(request.Form["backDirPath"]))
                {
                    errorBuilder.Append("* 需要指定已存在的备份目录路径(backDirPath)！<br/>");
                }

                bool useSvrBakPkg = false;
                string zipBakPkgPath = request.Form["bakPkgPath"];
                if (!String.IsNullOrEmpty(zipBakPkgPath))
                {
                    if (zipBakPkgPath.IndexOf(Path.VolumeSeparatorChar) == -1)
                        zipBakPkgPath = request.MapPath(zipBakPkgPath);

                    if (File.Exists(zipBakPkgPath))
                        useSvrBakPkg = true;
                }

                if (!useSvrBakPkg && request.Form["pkgType"] != "none")
                {
                    if (request.Files["updatePkg"] == null
                        || request.Files["updatePkg"].ContentLength < 3
                        || !request.Files["updatePkg"].FileName.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase))
                    {
                        errorBuilder.Append("* 需要提交更新名为updatePkg的zip更新包，或从已备份的zip包还原！<br/>");
                    }
                }
                #endregion

                string errorResult = errorBuilder.ToString();

                CreateUpdatePanel(context);
                if (errorResult.Length > 0)
                {
                    ShowUpdateMessage(context, "上传数据错误：<br/><hr size=\"1\" noshade /><font color=\"red\">" + errorResult + "</font>");
                    WriteScript(context, @"var btn=parent.document.getElementById('doUpdate'); btn.value='开始更新';btn.disabled=false;");
                }
                else
                {
                    DateTime taskStartTime = DateTime.Now;
                    DateTime lastTime = DateTime.Now;
                    #region 生成操作日志
                    using (FileStream fs = new FileStream(updateResultFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
                        {
                            sw.WriteLine(lastTime.ToString("<<< yyyy-MMd-d HH:mm:ss,fff") + " 接收到更新请求.<br/>");

                            string backDir = request.Form["backDirPath"];
                            string backType = request.Form["pkgType"].ToLower();
                            lastTime = DateTime.Now;
                            int rootDirLen = rootWorkDir.Length;
                            bool hasBackup = false;
                            #region 全部备份
                            if (backType.StartsWith("f"))
                            {
                                if (backType == "fzip")
                                {
                                    using (ZipStorer zbk = ZipStorer.Create(backDir + "\\fbak-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".zip", request.Form["updateComment"]))
                                    {
                                        doDirFileAction(rootWorkDir, request.Form["ignoreDirNames"],
                                            fpath =>
                                            {
                                                string zipFileName = fpath.Substring(rootDirLen);
                                                ShowUpdateMessage(context, "* 备份文件 -> " + zipFileName);
                                                try
                                                {
                                                    zbk.AddFile(ZipStorer.Compression.Deflate, fpath, zipFileName, "");
                                                }
                                                catch (Exception zBkEx)
                                                {
                                                    sw.WriteLine(" #备份文件{0}出现错误:{1}.<br/>", fpath, zBkEx.Message);
                                                }
                                            });
                                    }
                                }
                                else
                                {
                                    string pkgBackRoot = Path.Combine(backDir, "fbak-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                                    Directory.CreateDirectory(pkgBackRoot);
                                    doDirFileAction(rootWorkDir, request.Form["ignoreDirNames"],
                                        dfPath =>
                                        {
                                            string subFileName = dfPath.Substring(rootDirLen);
                                            string backupFullPath = Path.Combine(pkgBackRoot, subFileName.TrimStart('/', '\\'));
                                            string dirName = Path.GetDirectoryName(backupFullPath);
                                            if (!Directory.Exists(dirName))
                                                Directory.CreateDirectory(dirName);
                                            try
                                            {
                                                ShowUpdateMessage(context, "* 备份文件 -> " + subFileName);
                                                File.Copy(dfPath, backupFullPath, true);
                                            }
                                            catch (Exception zBkEx)
                                            {
                                                sw.WriteLine(" #备份文件{0}出现错误:{1}.<br/>", dfPath, zBkEx.Message);
                                            }
                                        });
                                }
                                hasBackup = true;
                            }
                            #endregion

                            //update2010-1228
                            string dirUpdateRoot = Path.Combine(backDir, "update-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                            Directory.CreateDirectory(dirUpdateRoot);
                            HttpPostedFile sqlFile = request.Files["updateSqlPkg"];
                            string tempFileFullPath = null;
                            if (sqlFile != null && sqlFile.ContentLength > 0)
                            {
                                tempFileFullPath = Path.Combine(dirUpdateRoot, "server.update.sql");
                                sqlFile.SaveAs(tempFileFullPath);
                                sw.WriteLine("* 数据库更新文件已保持在 {0}.<br/>", tempFileFullPath);
                            }

                            int totalUpdateFileCount = 0; //计算总共需要更新的文件个数
                            #region 部分备份
                            if (backType != "none")
                            {
                                ZipStorer zip = null;
                                if (!useSvrBakPkg)
                                {
                                    zip = ZipStorer.Open(request.Files["updatePkg"].InputStream, FileAccess.Read);
                                }
                                else
                                {
                                    zip = ZipStorer.Open(zipBakPkgPath, FileAccess.Read);
                                }
                                #region 解压更新包，并部分备份(可选)
                                if (zip != null)
                                {
                                    ZipStorer zbkf = null;
                                    if (!hasBackup && backType == "pzip")
                                        zbkf = ZipStorer.Create(backDir + "\\pbak-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".zip", request.Form["updateComment"]);
                                    string pBackRoot = Path.Combine(backDir, "pbak-" + DateTime.Now.ToString("yyyyMMddHHmmss"));

                                    zip.ReadCentralDirAction(zfe =>
                                    {
                                        totalUpdateFileCount++;

                                        tempFileFullPath = Path.Combine(dirUpdateRoot, zfe.FilenameInZip.TrimStart('/', '\\'));
                                        zip.ExtractFile(zfe, tempFileFullPath);
                                        ShowUpdateMessage(context, "已解压文件" + zfe.FilenameInZip);

                                        #region 执行备份
                                        if (!hasBackup)
                                        {
                                            string dfPath = Path.Combine(baseDir, zfe.FilenameInZip.TrimStart('/', '\\'));
                                            if (File.Exists(dfPath))
                                            {
                                                if (zbkf != null)
                                                {
                                                    string zipFileName = dfPath.Substring(rootDirLen);
                                                    ShowUpdateMessage(context, "* 备份文件 -> " + zipFileName);
                                                    try
                                                    {
                                                        zbkf.AddFile(ZipStorer.Compression.Deflate, dfPath, zipFileName, "");
                                                    }
                                                    catch (Exception zBkEx)
                                                    {
                                                        sw.WriteLine(" #备份文件{0}出现错误:{1}.<br/>", dfPath, zBkEx.Message);
                                                    }
                                                }
                                                else
                                                {
                                                    string subFileName = dfPath.Substring(rootDirLen);
                                                    string backupFullPath = Path.Combine(pBackRoot, subFileName.TrimStart('/', '\\'));
                                                    string dirName = Path.GetDirectoryName(backupFullPath);
                                                    if (!Directory.Exists(dirName))
                                                        Directory.CreateDirectory(dirName);
                                                    try
                                                    {
                                                        ShowUpdateMessage(context, "* 备份文件 -> " + subFileName);
                                                        File.Copy(dfPath, backupFullPath, true);
                                                    }
                                                    catch (Exception zBkEx)
                                                    {
                                                        sw.WriteLine(" #备份文件{0}出现错误:{1}.<br/>", dfPath, zBkEx.Message);
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                    });

                                    if (zbkf != null)
                                    {
                                        zbkf.Close();
                                        zbkf.Dispose();
                                    }

                                    zip.Close();
                                    zip.Dispose();
                                }
                                #endregion
                                sw.WriteLine("* 执行备份及解压共耗时{0}ms.<br/>", (DateTime.Now - lastTime).TotalMilliseconds);
                            }
                            #endregion

                            if (totalUpdateFileCount < 1 && backType != "none")
                            {
                                ShowUpdateMessage(context, "没有任务文件需要更新！");
                                WriteScript(context, "parent.callUpdateResultAfter(1000);");
                            }
                            else
                            {
                                if (backType != "none")
                                    ShowUpdateMessage(context, "* 需更新" + totalUpdateFileCount + "个文件，开始创建更新计划任务！");

                                lastTime = DateTime.Now;
                                #region 建立更新计划任务
                                DateTime baseTime = DateTime.Now;
                                DateTime updateTime = baseTime.AddMinutes(baseTime.Second > 30 ? 2.00 : 1.00);

                                string cmdOutPut = "";
                                string updateCmdFilePath = Path.Combine(dirUpdateRoot, "SvrUpdateTask.cmd");
                                using (StreamWriter cmdWriter = new StreamWriter(updateCmdFilePath, false))
                                {
                                    cmdWriter.WriteLine("@rem 文件由 [" + this.GetType().FullName + "] 创建于 " + baseTime.ToString());
                                    string stopFlag = "", monitorFlag = "";
                                    if (String.IsNullOrEmpty(request.Form["svrNeedStop"]))
                                        stopFlag = "-n ";     //默认不需要停止服务
                                    if (!String.IsNullOrEmpty(request.Form["svrMonitorUID"]))
                                        monitorFlag = "-m "; //默认需指定更新标识文件

                                    string svrSwitchSetting = ConfigurationManager.AppSettings["ServerUpdateHandler.SwitcherPath"] ?? @"d:\EaseSvrSwitcher\EaseSvrSwitcher.exe";
                                    string svrSwitchName = ConfigurationManager.AppSettings["ServerUpdateHandler.ServiceName"] ?? "CLRSvrHost";

                                    string updateLine = string.Format("\"{0}\" {4}{5}-s \"{1}\" -b \"{2}\" +l \"{3}\" \"%CD%\"",
                                        svrSwitchSetting,
                                        svrSwitchName,
                                        baseDir.TrimEnd('\\', '/'),
                                        updateResultFilePath,
                                        stopFlag, monitorFlag);

                                    if (backType == "none")
                                        updateLine = string.Format("\"{0}\" -r {2}-s \"{1}\" +l \"{3}\"",
                                        svrSwitchSetting,
                                        svrSwitchName,
                                        monitorFlag, updateResultFilePath);

                                    cmdWriter.WriteLine(updateLine);
                                    cmdWriter.Close();
                                }
                                File.SetAttributes(updateCmdFilePath, FileAttributes.Hidden | FileAttributes.System);

                                int exitCode = ManagementUtil.RunCmd("schtasks", Directory.GetCurrentDirectory(),
                                                string.Format("/create /Z /tn SvrUpdateTask /tr {0} /RU SYSTEM /RP *  /sc once /st {1}",
                                                updateCmdFilePath, updateTime.ToString("HH:mm")),
                                                100, ref cmdOutPut);

                                if (exitCode != 0)
                                {
                                    ShowUpdateMessage(context, "创建计划任务失败，退出码为:" + exitCode);
                                    WriteScript(context, "parent.callUpdateResultAfter(1000);");
                                }
                                else
                                {
                                    int offSet = 5;
                                    int tms = Convert.ToInt32((Convert.ToDateTime(updateTime.ToString("yyyy-MM-dd HH:mm:00")) - DateTime.Now).TotalSeconds);
                                    ShowUpdateMessage(context, "<font color=red><strong>与服务器的连接交互已完成，服务器将短暂停止服务完成更新过程...("
                                        + tms + "秒后应用更新)</strong></font>");
                                    WriteScript(context, "parent.callUpdateResultAfter(" + ((tms + offSet) * 1000) + ");");
                                }
                                #endregion

                                string jobResult = string.Format("* [status:{3}]已提交[{1}运行,目前{2}]更新计划，共耗时{0}.<br/>", DateTime.Now - lastTime,
                                    updateTime.ToString("HH:mm"),
                                    DateTime.Now.ToString("HH:mm:ss"),
                                    exitCode);

                                ShowUpdateMessage(context, jobResult);
                                sw.WriteLine(jobResult);
                            }
                            //完成在线日志写入
                            sw.Close();
                        }
                    }
                    #endregion
                }
            }
        }

        void doDirFileAction(string dirPath, string ignoreCfg, Action<string> filePathfn)
        {
            string[] filterArr = new string[0];
            if (ignoreCfg != null)
                filterArr = ignoreCfg.Split(new char[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);

            DirectoryInfo di = new DirectoryInfo(dirPath);
            if (di.Exists)
            {
                FileInfo[] subFiles = di.GetFiles("*.*", SearchOption.TopDirectoryOnly);
                foreach (FileInfo cfi in subFiles)
                {
                    if ((cfi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        continue;

                    bool doCurrentFile = true;
                    foreach (string filter in filterArr)
                    {
                        if (filter.IndexOf('.') == -1)
                            continue;

                        if (Regex.IsMatch(cfi.Name, filter.Replace("*", ".+").Replace("?", ".").Replace(".", "\\."), RegexOptions.IgnoreCase))
                        {
                            doCurrentFile = false;
                            break;
                        }
                    }

                    if (doCurrentFile)
                        filePathfn(cfi.FullName);
                }

                DirectoryInfo[] subDirs = di.GetDirectories("*.*", SearchOption.TopDirectoryOnly);
                foreach (DirectoryInfo cdi in subDirs)
                {
                    if ((cdi.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        continue;

                    bool doCurrentDir = true;
                    foreach (string filter in filterArr)
                    {
                        if (filter.IndexOf('.') != -1)
                            continue;

                        if (Regex.IsMatch(cdi.Name, filter.Replace("*", ".+").Replace("?", "."), RegexOptions.IgnoreCase))
                        {
                            doCurrentDir = false;
                            break;
                        }
                    }

                    if (doCurrentDir)
                    {
                        doDirFileAction(cdi.FullName, ignoreCfg, filePathfn);
                    }
                }
            }
        }

        public bool IsReusable { get { return false; } }

        void WriterHeader(HttpContext context, string title)
        {
            string tpt = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />
<title>{TITLE}</title>
<style type=""text/css"" media=""all"">
* {font-size:10.5pt;line-height:120%;}
.readonly { background-color:#f0f3f3;}
</style>
<script type=""text/javascript"" src=""js/jquery.js""></script>
</head>
<body bgcolor=""#f3f3f3"">
";
            context.Response.Write(tpt.Replace("{TITLE}", title));
        }

        void WriterFooter(HttpContext context)
        {
            context.Response.Write(@"
</body></html>");
        }

        void WriteScript(HttpContext context, string scriptCotent)
        {
            context.Response.Write(@"<script type=""text/javascript"">");
            context.Response.Write(scriptCotent);
            context.Response.Write("</script>");
        }

        void CreateUpdatePanel(HttpContext context)
        {
            context.Response.Write(@"<script type=""text/javascript"">");
            context.Response.Write("var pln = parent.document.getElementById('reportPanel');");
            context.Response.Write("</script>");
        }

        void ShowUpdateMessage(HttpContext context, string msg)
        {
            context.Response.Write(@"<script type=""text/javascript"">");
            context.Response.Write("pln.innerHTML='" + msg.Replace("'", "\\'")
                .Replace("\\", "\\\\")
                .Replace("\n", "<br/>")
                .Replace(" ", "&nbsp;")
                + "';");
            context.Response.Write("</script>");
        }

    }

    // ZipStorer, by Jaime Olivares
    // Website: zipstorer.codeplex.com
    // Version: 2.35 (March 14, 2010)

    /*
     Ridge Wong,  vbyte@163.com

     Ridge, 添加zip文件读取时，如果存在注释则读取注释, 2010-4-10。
     Ridge, 添加CreateZipFromDir静态方法, 从目录创建zip文件;
            添加ExtractZipToDir静态方法, 解压zip文件到目录。2010-12-28

     */

    /// <summary>
    /// Unique class for compression/decompression file. Represents a Zip file.
    /// </summary>
    public class ZipStorer : IDisposable
    {
        /// <summary>
        /// Compression method enumeration
        /// <remarks>
        /// http://en.wikipedia.org/wiki/ZIP_(file_format)
        /// http://zh.wikipedia.org/zh-cn/ZIP_(%E6%96%87%E4%BB%B6%E6%A0%BC%E5%BC%8F)
        /// </remarks>
        /// </summary>
        public enum Compression : ushort
        {
            /// <summary>Uncompressed storage</summary> 
            Store = 0,
            /// <summary>Deflate compression method, 32K windows zie.</summary>
            Deflate = 8,

            ///// <summary>
            ///// Enhanced Deflate compression method, 64K windows zie.
            ///// </summary>
            //EnhanceDeflate = 9,
            ///// <summary>
            ///// Reserved by PKWARE
            ///// </summary>
            //Reserved = 11,
            ///// <summary>
            ///// http://en.wikipedia.org/wiki/Bzip2
            ///// </summary>
            //Bzip2 = 12

        }

        /// <summary>
        /// Represents an entry in Zip file directory
        /// </summary>
        public struct ZipFileEntry
        {
            /// <summary>Compression method</summary>
            public Compression Method;
            /// <summary>Full path and filename as stored in Zip</summary>
            public string FilenameInZip;
            /// <summary>Original file size</summary>
            public uint FileSize;
            /// <summary>Compressed file size</summary>
            public uint CompressedSize;
            /// <summary>Offset of header information inside Zip storage</summary>
            public uint HeaderOffset;
            /// <summary>Offset of file inside Zip storage</summary>
            public uint FileOffset;
            /// <summary>Size of header information</summary>
            public uint HeaderSize;
            /// <summary>32-bit checksum of entire file</summary>
            public uint Crc32;
            /// <summary>Last modification time of file</summary>
            public DateTime ModifyTime;
            /// <summary>User comment for file</summary>
            public string Comment;
            /// <summary>True if UTF8 encoding for filename and comments, false if default (CP 437)</summary>
            public bool EncodeUTF8;

            /// <summary>Overriden method</summary>
            /// <returns>Filename in Zip</returns>
            public override string ToString()
            {
                return this.FilenameInZip;
            }
        }

        #region Public fields
        /// <summary>True if UTF8 encoding for filename and comments, false if default (CP 437)</summary>
        public bool EncodeUTF8 = false;
        /// <summary>Force deflate algotithm even if it inflates the stored file. Off by default.</summary>
        public bool ForceDeflating = false;
        /// <summary>
        /// 文件的描述
        /// </summary>
        public string Comment = "";
        #endregion

        #region Private fields
        // List of files to store
        private List<ZipFileEntry> Files = new List<ZipFileEntry>();
        // Filename of storage file
        private string FileName;
        // Stream object of storage file
        private Stream ZipFileStream;
        // Central dir image
        private byte[] FileDataImage = null;
        // Existing files in zip
        private ushort ExistingFileNumber = 0;
        // File access for Open method
        private FileAccess Access;
        // Static CRC32 Table
        private static UInt32[] CrcTable = null;

        // United States (DOS) 437
        // Default filename encoder
        private static Encoding DefaultEncoding = Encoding.Default; //Encoding.GetEncoding(437);
        #endregion

        #region Public methods
        // Static constructor. Just invoked once in order to create the CRC32 lookup table.
        static ZipStorer()
        {
            // Generate CRC32 table
            CrcTable = new UInt32[256];
            for (int i = 0; i < CrcTable.Length; i++)
            {
                UInt32 c = (UInt32)i;
                for (int j = 0; j < 8; j++)
                {
                    if ((c & 1) != 0)
                        c = 3988292384 ^ (c >> 1);
                    else
                        c >>= 1;
                }
                CrcTable[i] = c;
            }
        }

        #region 实用静态方法
        /// <summary>
        /// Method to create a new storage file
        /// </summary>
        /// <param name="_filename">Full path of Zip file to create</param>
        /// <param name="_comment">General comment for Zip file</param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Create(string _filename, string _comment)
        {
            Stream stream = new FileStream(_filename, FileMode.Create, FileAccess.ReadWrite);

            ZipStorer zip = Create(stream, _comment);
            zip.Comment = _comment;
            zip.FileName = _filename;

            return zip;
        }

        /// <summary>
        /// Method to create a new zip storage in a stream
        /// </summary>
        /// <param name="_stream"></param>
        /// <param name="_comment"></param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Create(Stream _stream, string _comment)
        {
            ZipStorer zip = new ZipStorer();
            zip.Comment = _comment;
            zip.ZipFileStream = _stream;
            zip.Access = FileAccess.Write;

            return zip;
        }

        /// <summary>
        /// Method to open an existing storage file
        /// </summary>
        /// <param name="_filename">Full path of Zip file to open</param>
        /// <param name="_access">File access mode as used in FileStream constructor</param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Open(string _filename, FileAccess _access)
        {
            Stream stream = (Stream)new FileStream(_filename, FileMode.OpenOrCreate, _access == FileAccess.Read ? FileAccess.Read : FileAccess.ReadWrite);

            ZipStorer zip = Open(stream, _access);
            zip.FileName = _filename;

            return zip;
        }


        /// <summary>
        /// Method to open an existing storage from stream
        /// </summary>
        /// <param name="_stream">Already opened stream with zip contents</param>
        /// <param name="_access">File access mode for stream operations</param>
        /// <returns>A valid ZipStorer object</returns>
        public static ZipStorer Open(Stream _stream, FileAccess _access)
        {
            if (!_stream.CanSeek && _access != FileAccess.Read)
                throw new InvalidOperationException("Stream cannot seek");

            ZipStorer zip = new ZipStorer();
            //zip.FileName = _filename;
            zip.ZipFileStream = _stream;
            zip.Access = _access;

            if (!zip.ReadFileInfo() && _stream.Length > 22)
                throw new System.IO.InvalidDataException();
            else
                return zip;
        }


        /// <summary>
        /// 从目录创建zip文件
        /// </summary>
        /// <param name="zipFilePath">zip文件保存路径</param>
        /// <param name="zipDir">压缩的文件目录</param>
        /// <param name="ignoreItemHandler">忽略文件路径的判断委托，可以为null忽略判断。</param>
        public static void CreateZipFromDir(string zipFilePath, string zipDir, Predicate<string> ignoreItemHandler)
        {
            using (ZipStorer zip = ZipStorer.Open(zipFilePath, FileAccess.ReadWrite))
            {
                string[] allFiles = Directory.GetFiles(zipDir, "*.*", SearchOption.AllDirectories);
                int trimLen = zipDir.Length;
                foreach (string fileItem in allFiles)
                {
                    if (ignoreItemHandler != null && ignoreItemHandler(fileItem))
                        continue;
                    zip.AddFile(Compression.Deflate, fileItem, fileItem.Substring(trimLen), "");
                }
            }
        }

        /// <summary>
        /// 解压zip文件到目录
        /// </summary>
        /// <param name="zipFilePath">zip文件路径</param>
        /// <param name="extraDir">解压压缩的文件目录</param>
        public static void ExtractZipToDir(string zipFilePath, string extraDir)
        {
            using (ZipStorer zip = ZipStorer.Open(zipFilePath, FileAccess.Read))
            {
                if (!Directory.Exists(extraDir)) Directory.CreateDirectory(extraDir);
                zip.ReadCentralDirAction(f => { zip.ExtractFile(f, Path.Combine(extraDir, f.FilenameInZip)); });
            }
        }
        #endregion

        /// <summary>
        /// Add full contents of a file into the Zip storage
        /// </summary>
        /// <param name="_method">Compression method</param>
        /// <param name="_pathname">Full path of file to add to Zip storage</param>
        /// <param name="_filenameInZip">Filename and path as desired in Zip directory</param>
        /// <param name="_comment">Comment for stored file</param>        
        public void AddFile(Compression _method, string _pathname, string _filenameInZip, string _comment)
        {
            if (Access == FileAccess.Read)
                throw new InvalidOperationException("Writing is not allowed");

            FileStream stream = new FileStream(_pathname, FileMode.Open, FileAccess.Read);
            AddStream(_method, _filenameInZip, stream, File.GetLastWriteTime(_pathname), _comment);
            stream.Close();
        }

        /// <summary>
        /// Add full contents of a stream into the Zip storage
        /// </summary>
        /// <param name="_method">Compression method</param>
        /// <param name="_filenameInZip">Filename and path as desired in Zip directory</param>
        /// <param name="_source">Stream object containing the data to store in Zip</param>
        /// <param name="_modTime">Modification time of the data to store</param>
        /// <param name="_comment">Comment for stored file</param>
        public void AddStream(Compression _method, string _filenameInZip, Stream _source, DateTime _modTime, string _comment)
        {
            if (Access == FileAccess.Read)
                throw new InvalidOperationException("Writing is not allowed");

            long offset;
            if (this.Files.Count == 0)
                offset = 0;
            else
            {
                ZipFileEntry last = this.Files[this.Files.Count - 1];
                offset = last.HeaderOffset + last.HeaderSize;
            }

            // Prepare the fileinfo
            ZipFileEntry zfe = new ZipFileEntry();
            zfe.Method = _method;
            zfe.EncodeUTF8 = this.EncodeUTF8;
            zfe.FilenameInZip = NormalizedFilename(_filenameInZip);
            zfe.Comment = (_comment == null ? "" : _comment);

            // Even though we write the header now, it will have to be rewritten, since we don't know compressed size or crc.
            zfe.Crc32 = 0;  // to be updated later
            zfe.HeaderOffset = (uint)this.ZipFileStream.Position;  // offset within file of the start of this local record
            zfe.ModifyTime = _modTime;

            // Write local header
            WriteLocalHeader(ref zfe);
            zfe.FileOffset = (uint)this.ZipFileStream.Position;

            // Write file to zip (store)
            Store(ref zfe, _source);
            _source.Close();

            this.UpdateCrcAndSizes(ref zfe);

            Files.Add(zfe);
        }

        /// <summary>
        /// Updates central directory (if pertinent) and close the Zip storage
        /// </summary>
        /// <remarks>This is a required step, unless automatic dispose is used</remarks>
        public void Close()
        {
            if (this.Access != FileAccess.Read && ZipFileStream != null)
            {
                uint centralOffset = (uint)this.ZipFileStream.Position;
                uint centralSize = 0;

                if (this.FileDataImage != null)
                    this.ZipFileStream.Write(FileDataImage, 0, FileDataImage.Length);

                for (int i = 0; i < Files.Count; i++)
                {
                    long pos = this.ZipFileStream.Position;
                    this.WriteCentralDirRecord(Files[i]);
                    centralSize += (uint)(this.ZipFileStream.Position - pos);
                }

                if (this.FileDataImage != null)
                    this.WriteEndRecord(centralSize + (uint)FileDataImage.Length, centralOffset);
                else
                    this.WriteEndRecord(centralSize, centralOffset);
            }

            if (this.ZipFileStream != null)
            {
                this.ZipFileStream.Flush();
                this.ZipFileStream.Dispose();
                this.ZipFileStream = null;
            }
        }

        /// <summary>
        /// 读取文件结构时执行的操作
        /// </summary>
        /// <param name="zfeAct">文件项操作</param>
        public void ReadCentralDirAction(Action<ZipFileEntry> zfeAct)
        {
            if (this.FileDataImage == null)
                throw new InvalidOperationException("Central directory currently does not exist");

            for (int pointer = 0; pointer < this.FileDataImage.Length; )
            {
                uint signature = BitConverter.ToUInt32(FileDataImage, pointer);
                if (signature != 0x02014b50)
                    break;

                bool encodeUTF8 = (BitConverter.ToUInt16(FileDataImage, pointer + 8) & 0x0800) != 0;
                ushort method = BitConverter.ToUInt16(FileDataImage, pointer + 10);
                uint modifyTime = BitConverter.ToUInt32(FileDataImage, pointer + 12);
                uint crc32 = BitConverter.ToUInt32(FileDataImage, pointer + 16);
                uint comprSize = BitConverter.ToUInt32(FileDataImage, pointer + 20);
                uint fileSize = BitConverter.ToUInt32(FileDataImage, pointer + 24);
                ushort filenameSize = BitConverter.ToUInt16(FileDataImage, pointer + 28);
                ushort extraSize = BitConverter.ToUInt16(FileDataImage, pointer + 30);
                ushort commentSize = BitConverter.ToUInt16(FileDataImage, pointer + 32);
                uint headerOffset = BitConverter.ToUInt32(FileDataImage, pointer + 42);
                uint headerSize = (uint)(46 + filenameSize + extraSize + commentSize);

                Encoding encoder = encodeUTF8 ? Encoding.UTF8 : DefaultEncoding;

                ZipFileEntry zfe = new ZipFileEntry();
                zfe.Method = (Compression)method;
                zfe.FilenameInZip = encoder.GetString(FileDataImage, pointer + 46, filenameSize);
                zfe.FileOffset = GetFileOffset(headerOffset);
                zfe.FileSize = fileSize;
                zfe.CompressedSize = comprSize;
                zfe.HeaderOffset = headerOffset;
                zfe.HeaderSize = headerSize;
                zfe.Crc32 = crc32;
                zfe.ModifyTime = DosTimeToDateTime(modifyTime);
                if (commentSize > 0)
                    zfe.Comment = encoder.GetString(FileDataImage, pointer + 46 + filenameSize + extraSize, commentSize);

                zfeAct(zfe);

                pointer += (46 + filenameSize + extraSize + commentSize);
            }
        }

        /// <summary>
        /// Read all the file records in the central directory 
        /// <para>读取文件列表</para>
        /// </summary>
        /// <returns>List of all entries in directory</returns>
        public List<ZipFileEntry> ReadCentralDir()
        {
            List<ZipFileEntry> result = new List<ZipFileEntry>();
            ReadCentralDirAction(f => result.Add(f));
            return result;
        }

        /// <summary>
        /// Copy the contents of a stored file into a physical file
        /// </summary>
        /// <param name="_zfe">Entry information of file to extract</param>
        /// <param name="_filename">Name of file to store uncompressed data</param>
        /// <returns>True if success, false if not.</returns>
        /// <remarks>Unique compression methods are Store and Deflate</remarks>
        public bool ExtractFile(ZipFileEntry _zfe, string _filename)
        {
            // Make sure the parent directory exist
            string path = System.IO.Path.GetDirectoryName(_filename);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            // Check it is directory. If so, do nothing
            if (Directory.Exists(_filename))
                return true;

            //去掉只读属性，设置为正常属性
            if (File.Exists(_filename)) File.SetAttributes(_filename, FileAttributes.Normal);

            using (Stream output = new FileStream(_filename, FileMode.Create, FileAccess.Write))
            {
                bool result = ExtractFile(_zfe, output);
                if (result)
                    output.Close();

                File.SetCreationTime(_filename, _zfe.ModifyTime);
                File.SetLastWriteTime(_filename, _zfe.ModifyTime);

                return result;
            }
        }

        /// <summary>
        /// Copy the contents of a stored file into an opened stream
        /// </summary>
        /// <param name="_zfe">Entry information of file to extract</param>
        /// <param name="_stream">Stream to store the uncompressed data</param>
        /// <returns>True if success, false if not.</returns>
        /// <remarks>Unique compression methods are Store and Deflate</remarks>
        public bool ExtractFile(ZipFileEntry _zfe, Stream _stream)
        {
            if (!_stream.CanWrite)
                throw new InvalidOperationException("Stream cannot be written");

            // check signature
            byte[] signature = new byte[4];
            this.ZipFileStream.Seek(_zfe.HeaderOffset, SeekOrigin.Begin);
            this.ZipFileStream.Read(signature, 0, 4);
            if (BitConverter.ToUInt32(signature, 0) != 0x04034b50)
                return false;

            // Select input stream for inflating or just reading
            Stream inStream;
            if (_zfe.Method == Compression.Store)
                inStream = this.ZipFileStream;
            else if (_zfe.Method == Compression.Deflate)
                inStream = new DeflateStream(this.ZipFileStream, CompressionMode.Decompress, true);
            else
                return false;

            // Buffered copy
            byte[] buffer = new byte[16384];
            this.ZipFileStream.Seek(_zfe.FileOffset, SeekOrigin.Begin);
            uint bytesPending = _zfe.FileSize;
            while (bytesPending > 0)
            {
                int bytesRead = inStream.Read(buffer, 0, (int)Math.Min(bytesPending, buffer.Length));
                _stream.Write(buffer, 0, bytesRead);
                bytesPending -= (uint)bytesRead;
            }
            _stream.Flush();

            if (_zfe.Method == Compression.Deflate)
                inStream.Dispose();
            return true;
        }

        /// <summary>
        /// Removes one of many files in storage. It creates a new Zip file.
        /// </summary>
        /// <param name="_zip">Reference to the current Zip object</param>
        /// <param name="_zfes">List of Entries to remove from storage</param>
        /// <returns>True if success, false if not</returns>
        /// <remarks>This method only works for storage of type FileStream</remarks>
        public static bool RemoveEntries(ref ZipStorer _zip, List<ZipFileEntry> _zfes)
        {
            if (!(_zip.ZipFileStream is FileStream))
                throw new InvalidOperationException("RemoveEntries is allowed just over streams of type FileStream");

            //Get full list of entries
            List<ZipFileEntry> fullList = _zip.ReadCentralDir();
            //In order to delete we need to create a copy of the zip file excluding the selected items
            string tempZipName = Path.GetTempFileName();
            string tempEntryFile = Path.GetTempFileName();

            try
            {
                ZipStorer tempTargetZip = ZipStorer.Create(tempZipName, _zip.Comment);
                foreach (ZipFileEntry zfe in fullList)
                {
                    if (!_zfes.Contains(zfe))
                    {
                        if (_zip.ExtractFile(zfe, tempEntryFile))
                        {
                            tempTargetZip.AddFile(zfe.Method, tempEntryFile, zfe.FilenameInZip, zfe.Comment);
                        }
                    }
                }
                _zip.Close();
                tempTargetZip.Close();

                File.Delete(_zip.FileName);
                File.Move(tempZipName, _zip.FileName);
                _zip = ZipStorer.Open(_zip.FileName, _zip.Access);
            }
            catch
            {
                return false;
            }
            finally
            {
                if (File.Exists(tempZipName)) File.Delete(tempZipName);
                if (File.Exists(tempEntryFile)) File.Delete(tempEntryFile);
            }
            return true;
        }
        #endregion

        #region Private methods
        // Calculate the file offset by reading the corresponding local header
        private uint GetFileOffset(uint _headerOffset)
        {
            byte[] buffer = new byte[2];
            this.ZipFileStream.Seek(_headerOffset + 26, SeekOrigin.Begin);
            this.ZipFileStream.Read(buffer, 0, 2);
            ushort filenameSize = BitConverter.ToUInt16(buffer, 0);
            this.ZipFileStream.Read(buffer, 0, 2);
            ushort extraSize = BitConverter.ToUInt16(buffer, 0);
            return (uint)(30 + filenameSize + extraSize + _headerOffset);
        }

        /* Local file header: 文件头开始标志
            local file header signature     4 bytes  (0x04034b50)
            version needed to extract       2 bytes
            general purpose bit flag        2 bytes
            compression method              2 bytes
            last mod file time              2 bytes
            last mod file date              2 bytes
            crc-32                          4 bytes
            compressed size                 4 bytes
            uncompressed size               4 bytes
            filename length                 2 bytes
            extra field length              2 bytes

            filename (variable size)
            extra field (variable size)
        */
        private void WriteLocalHeader(ref ZipFileEntry _zfe)
        {
            long pos = this.ZipFileStream.Position;
            Encoding encoder = _zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedFilename = encoder.GetBytes(_zfe.FilenameInZip);

            this.ZipFileStream.Write(new byte[] { 80, 75, 3, 4, 20, 0 }, 0, 6); // No extra header
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)(_zfe.EncodeUTF8 ? 0x0800 : 0)), 0, 2); // filename and comment encoding 
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);  // zipping method
            this.ZipFileStream.Write(BitConverter.GetBytes(DateTimeToDosTime(_zfe.ModifyTime)), 0, 4); // zipping date and time
            this.ZipFileStream.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 12); // unused CRC, un/compressed size, updated later
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedFilename.Length), 0, 2); // filename length
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // extra length

            this.ZipFileStream.Write(encodedFilename, 0, encodedFilename.Length);
            _zfe.HeaderSize = (uint)(this.ZipFileStream.Position - pos);
        }

        /* Central directory's File header: //目录开始标志
            central file header signature   4 bytes  (0x02014b50)
            version made by                 2 bytes
            version needed to extract       2 bytes
            general purpose bit flag        2 bytes
            compression method              2 bytes
            last mod file time              2 bytes
            last mod file date              2 bytes
            crc-32                          4 bytes
            compressed size                 4 bytes
            uncompressed size               4 bytes
            filename length                 2 bytes
            extra field length              2 bytes
            file comment length             2 bytes
            disk number start               2 bytes
            internal file attributes        2 bytes
            external file attributes        4 bytes
            relative offset of local header 4 bytes

            filename (variable size)
            extra field (variable size)
            file comment (variable size)
        */
        private void WriteCentralDirRecord(ZipFileEntry _zfe)
        {
            Encoding encoder = _zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedFilename = encoder.GetBytes(_zfe.FilenameInZip);
            byte[] encodedComment = encoder.GetBytes(_zfe.Comment);

            this.ZipFileStream.Write(new byte[] { 80, 75, 1, 2, 23, 0xB, 20, 0 }, 0, 8);
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)(_zfe.EncodeUTF8 ? 0x0800 : 0)), 0, 2); // filename and comment encoding 
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);  // zipping method
            this.ZipFileStream.Write(BitConverter.GetBytes(DateTimeToDosTime(_zfe.ModifyTime)), 0, 4);  // zipping date and time
            this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.Crc32), 0, 4); // file CRC
            this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.CompressedSize), 0, 4); // compressed file size
            this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.FileSize), 0, 4); // uncompressed file size
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedFilename.Length), 0, 2); // Filename in zip
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // extra length
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedComment.Length), 0, 2);

            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // disk=0
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // file type: binary
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // Internal file attributes
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)0x8100), 0, 2); // External file attributes (normal/readable)
            this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.HeaderOffset), 0, 4);  // Offset of header

            this.ZipFileStream.Write(encodedFilename, 0, encodedFilename.Length);
            this.ZipFileStream.Write(encodedComment, 0, encodedComment.Length);
        }

        /* End of central dir record: //目录结束标识
            end of central dir signature                                                            4 bytes  (0x06054b50)
            number of this disk                                                                     2 bytes
            number of the disk with the start of the central directory                              2 bytes
            total number of entries in the central dir on this disk                                 2 bytes


            total number of entries in the central dir                                              2 bytes
            size of the central directory                                                           4 bytes
            offset of start of central directory with respect to the starting disk number           4 bytes
            zipfile comment length                                                                  2 bytes
            zipfile comment (variable size)
        */
        private void WriteEndRecord(uint _size, uint _offset)
        {
            Encoding encoder = this.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedComment = encoder.GetBytes(this.Comment);

            this.ZipFileStream.Write(new byte[] { 80, 75, 5, 6, 0, 0, 0, 0 }, 0, 8);
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)Files.Count + ExistingFileNumber), 0, 2);
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)Files.Count + ExistingFileNumber), 0, 2);

            //
            this.ZipFileStream.Write(BitConverter.GetBytes(_size), 0, 4);
            this.ZipFileStream.Write(BitConverter.GetBytes(_offset), 0, 4);

            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedComment.Length), 0, 2);
            this.ZipFileStream.Write(encodedComment, 0, encodedComment.Length);
        }

        // Reads the end-of-central-directory record
        private bool ReadFileInfo()
        {
            if (this.ZipFileStream.Length < 22)
                return false;

            try
            {
                this.ZipFileStream.Seek(-17, SeekOrigin.End);
                BinaryReader br = new BinaryReader(this.ZipFileStream);
                do
                {
                    this.ZipFileStream.Seek(-5, SeekOrigin.Current);
                    UInt32 sig = br.ReadUInt32();
                    if (sig == 0x06054b50)
                    {
                        this.ZipFileStream.Seek(6, SeekOrigin.Current);

                        UInt16 entries = br.ReadUInt16();           //2
                        Int32 fileTotalSize = br.ReadInt32();       //4
                        UInt32 fileDataOffset = br.ReadUInt32();    //4
                        UInt16 commentSize = br.ReadUInt16();       //2

                        // check if comment field is the very last data in file
                        if (this.ZipFileStream.Position + commentSize != this.ZipFileStream.Length)
                            return false;

                        // [TODO]读取Zip文件的注释
                        if (commentSize > 0)
                        {
                            byte[] fileCmtBytes = new byte[commentSize];
                            ZipFileStream.Read(fileCmtBytes, 0, fileCmtBytes.Length);
                            this.Comment = DefaultEncoding.GetString(fileCmtBytes);
                        }

                        // Copy entire central directory to a memory buffer
                        this.ExistingFileNumber = entries;
                        this.FileDataImage = new byte[fileTotalSize];
                        this.ZipFileStream.Seek(fileDataOffset, SeekOrigin.Begin);
                        this.ZipFileStream.Read(this.FileDataImage, 0, fileTotalSize);

                        // Leave the pointer at the begining of central dir, to append new files
                        this.ZipFileStream.Seek(fileDataOffset, SeekOrigin.Begin);
                        return true;
                    }
                } while (this.ZipFileStream.Position > 0);
            }
            catch { }

            return false;
        }

        // Copies all source file into storage file
        private void Store(ref ZipFileEntry _zfe, Stream _source)
        {
            byte[] buffer = new byte[16384];
            int bytesRead;
            uint totalRead = 0;
            Stream outStream;

            long posStart = this.ZipFileStream.Position;
            long sourceStart = _source.Position;

            if (_zfe.Method == Compression.Store)
                outStream = this.ZipFileStream;
            else
                outStream = new DeflateStream(this.ZipFileStream, CompressionMode.Compress, true);

            _zfe.Crc32 = 0 ^ 0xffffffff;

            do
            {
                bytesRead = _source.Read(buffer, 0, buffer.Length);
                totalRead += (uint)bytesRead;
                if (bytesRead > 0)
                {
                    outStream.Write(buffer, 0, bytesRead);

                    for (uint i = 0; i < bytesRead; i++)
                    {
                        _zfe.Crc32 = ZipStorer.CrcTable[(_zfe.Crc32 ^ buffer[i]) & 0xFF] ^ (_zfe.Crc32 >> 8);
                    }
                }
            }
            while (bytesRead == buffer.Length);
            outStream.Flush();

            if (_zfe.Method == Compression.Deflate)
                outStream.Dispose();

            _zfe.Crc32 ^= 0xffffffff;
            _zfe.FileSize = totalRead;
            _zfe.CompressedSize = (uint)(this.ZipFileStream.Position - posStart);

            // Verify for real compression 确保压缩后数据长度减小
            if (_zfe.Method == Compression.Deflate && !this.ForceDeflating
                && _source.CanSeek && _zfe.CompressedSize > _zfe.FileSize)
            {
                // Start operation again with Store algorithm
                _zfe.Method = Compression.Store;
                this.ZipFileStream.Position = posStart;
                this.ZipFileStream.SetLength(posStart);
                _source.Position = sourceStart;

                this.Store(ref _zfe, _source);
            }

        }

        /* DOS Date and time:
            MS-DOS date. The date is a packed value with the following format. Bits Description 
                0-4 Day of the month (1?1) 
                5-8 Month (1 = January, 2 = February, and so on) 
                9-15 Year offset from 1980 (add 1980 to get actual year) 
            MS-DOS time. The time is a packed value with the following format. Bits Description 
                0-4 Second divided by 2 
                5-10 Minute (0?9) 
                11-15 Hour (0?3 on a 24-hour clock) 
        */
        public static uint DateTimeToDosTime(DateTime _dt)
        {
            return (uint)(
                (_dt.Second / 2) | (_dt.Minute << 5) | (_dt.Hour << 11) |
                (_dt.Day << 16) | (_dt.Month << 21) | ((_dt.Year - 1980) << 25));
        }

        public static DateTime DosTimeToDateTime(uint _dt)
        {
            return new DateTime(
                (int)(_dt >> 25) + 1980,
                (int)(_dt >> 21) & 15,
                (int)(_dt >> 16) & 31,

                (int)(_dt >> 11) & 31,
                (int)(_dt >> 5) & 63,
                (int)(_dt & 31) * 2);
        }

        /* CRC32 algorithm
          The 'magic number' for the CRC is 0xdebb20e3.  
          The proper CRC pre and post conditioning
          is used, meaning that the CRC register is
          pre-conditioned with all ones (a starting value
          of 0xffffffff) and the value is post-conditioned by
          taking the one's complement of the CRC residual.
          If bit 3 of the general purpose flag is set, this
          field is set to zero in the local header and the correct
          value is put in the data descriptor and in the central
          directory.
        */
        private void UpdateCrcAndSizes(ref ZipFileEntry _zfe)
        {
            long lastPos = this.ZipFileStream.Position;                                     // remember position

            this.ZipFileStream.Position = _zfe.HeaderOffset + 8;
            this.ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);    // zipping method

            this.ZipFileStream.Position = _zfe.HeaderOffset + 14;
            this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.Crc32), 0, 4);              // Update CRC
            this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.CompressedSize), 0, 4);     // Compressed size
            this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.FileSize), 0, 4);           // Uncompressed size

            this.ZipFileStream.Position = lastPos;  // restore position
        }

        // Replaces backslashes with slashes to store in zip header
        private string NormalizedFilename(string _filename)
        {
            string filename = _filename.Replace('\\', '/');
            int pos = filename.IndexOf(':');
            if (pos >= 0)
                filename = filename.Remove(0, pos) + "_" + filename.Substring(pos + 1);
            return filename.Trim('/');
        }

        #endregion

        #region IDisposable Members
        /// <summary>
        /// Closes the Zip file stream
        /// </summary>
        public void Dispose()
        {
            this.Close();
        }
        #endregion
    }
}