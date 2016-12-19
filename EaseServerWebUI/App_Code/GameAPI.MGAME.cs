/**********************************************
 * $Id: GameAPI.MGAME.cs 23 2009-06-23 08:06:06Z wangqj $
 * $URL: https://wangjj/svn/game/trunk/App_Code/GameAPI.MGAME.cs $
 * $Author: wangqj $
 * $Revision: 23 $
 * $LastChangedRevision: 23 $
 * $LastChangedDate: 2009-06-23 16:06:06 +0800 (星期二, 2009-06-23) $
 ***********************************************/
using System;
using System.IO;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Data.OleDb;
using System.Data;

namespace GameAPI
{
    /// <summary>
    /// 手机游戏数据输出接口
    /// </summary>
    public class MGAME : IHttpHandler
    {
        #region 根据会话保存值
        /// <summary>
        /// 输出模式(默认为true)
        /// </summary>
        public static bool isReleaseModel
        {
            get 
            {
                return HttpContext.Current.Request.QueryString["debug"] != "1";
            }
        }

        /// <summary>
        /// 输出总长度(默认为true)
        /// </summary>
        public static bool writeTotalLength
        {
            get
            {
                return HttpContext.Current.Request.QueryString["nt"] != "1";
            }
        }

        /// <summary>
        /// 输出二进制Hex数据(默认为false)
        /// </summary>
        public static bool outputBinnary 
        {
            get
            {
                return HttpContext.Current.Request.QueryString["bin"] == "1";
            }
        }
        #endregion

        /// <summary>
        /// Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see cref="T:System.Web.IHttpHandler"/> interface.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"/> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
        public void ProcessRequest(HttpContext context)
        {

            #region 错误调试与屏蔽
            context.ApplicationInstance.Error += new EventHandler(ApplicationInstance_Error);
            #endregion

            string strTemp, strScreenDir; 

            #region 上传界面输出
            if (context.Request.PathInfo == "/upload")
            {
                #region 处理上传
                if (context.Request.RequestType.ToUpper() == "POST")
                {
                    string selPath = context.Request.Form["selPath"];
                    string ftype = context.Request.Form["ftype"];
                    Match m = Regex.Match(selPath, @"^(\d)(\d{2})$", RegexOptions.IgnoreCase);
                    string saveDir = context.Server.MapPath("/App_Data/pass" + m.Groups[1].Value
                        + (m.Groups[2].Value.Equals("00") ? "" : "/stage") //存储到根目录 
                        + m.Groups[2].Value.TrimStart('0'));

                    //相关屏幕数据，默认为空则为240*320
                    if (!string.IsNullOrEmpty(context.Request.Form["selScreen"]))
                    {
                        saveDir += "\\" + context.Request.Form["selScreen"];
                    }

                    //创建相关存储目录
                    DirectoryInfo diSave = new DirectoryInfo(saveDir);
                    if (!diSave.Exists) diSave.Create();

                    string fileName = "";
                    int fileCount = 0;
                    if (context.Request.Files.Count > 0)
                    {
                        if (ftype == "xls")
                        {
                            fileName = "Object.xls";
                        }
                        else if (ftype == "map")
                        {
                            fileName = "Pass.GAP";
                        }
                        else if (ftype == "bg")
                        {
                            fileName = "background.png";
                        }
                        else
                        {
                            fileName = "part1.dat";
                        }

                        bool isClearOld = context.Request.Form["clearOld"] != null;

                        if (ftype == "act" && context.Request.Files.Count > 0)
                        {
                            #region 动作文件
                            DirectoryInfo di = new DirectoryInfo(saveDir + "\\Action");
                            if (!di.Exists) di.Create();

                            if (isClearOld)
                            {
                                foreach (FileInfo fi in di.GetFiles("*.*", SearchOption.TopDirectoryOnly))
                                {
                                    fi.Delete();
                                }
                            }

                            for (int i = 1; i <= context.Request.Files.Count; i++)
                            {
                                strTemp = "file" + i;
                                if (context.Request.Files[strTemp].ContentLength > 0)
                                {
                                    context.Request.Files[strTemp].SaveAs(saveDir + "\\Action\\"
                                        + Path.GetFileName(context.Request.Files[strTemp].FileName));
                                    fileCount++;
                                }
                            } 
                            #endregion
                        }
                        else
                        {
                            strTemp = "file1";

                            #region 清除旧的相关文件
                            if (isClearOld)
                            {
                                DirectoryInfo diOld = new DirectoryInfo(saveDir);
                                if (ftype == "res")
                                {
                                    foreach (FileInfo fi in diOld.GetFiles("part*.dat", SearchOption.TopDirectoryOnly))
                                    {
                                        fi.Delete();
                                    }
                                }
                                else
                                {
                                    FileInfo f2Del = new FileInfo(saveDir + "\\" + fileName);
                                    if (f2Del.Exists) f2Del.Delete();
                                }
                            }
                            #endregion

                            if (context.Request.Files[strTemp].ContentLength > 0)
                            {
                                context.Request.Files[strTemp].SaveAs(saveDir + "\\" + fileName);
                                fileCount++;
                            }

                            if (context.Request.Files.Count > 1 && ftype == "res")
                            {
                                #region 图片资源分块
                                for (int i = 2; i <= context.Request.Files.Count; i++)
                                {
                                    strTemp = "file" + i;
                                    if (context.Request.Files[strTemp].ContentLength > 0)
                                    {
                                        context.Request.Files[strTemp].SaveAs(saveDir + "\\part" + i + ".dat");
                                        fileCount++;
                                    }
                                }
                                #endregion
                            }
                        }
                        //context.Response.Write(saveDir + "\\" + fileName);
                        context.Response.Write("OK,相关老文件(" +fileCount + "个)已被你干掉！");
                        context.Response.End();
                    }
                    return;
                }
                #endregion

                #region 上传界面输出
                context.Response.ContentType = "text/html";
                context.Response.TransmitFile(context.Server.MapPath("/App_Data/tpt/DataUpdate.html"));
                context.Response.End(); 
                #endregion
                return;
            } 
            #endregion
            
            int currentpass = 0;
            int currentStage = 1;

            strTemp = context.Request.QueryString["pass"];
            strScreenDir = context.Request.QueryString["s"];
            if (strTemp != null
                && Regex.IsMatch(strTemp, @"^\d+$"))
            {
                if (strTemp.Length > 2)
                {
                    currentpass = Convert.ToInt32(strTemp.Substring(0, strTemp.Length - 2));
                    currentStage = Convert.ToInt32(strTemp.Substring(strTemp.Length - 2, 2));
                    //context.Response.Write(currentStage);
                    //context.Response.End();
                    //return;
                }
            }
            else
            {
                context.Response.Write("Error Request.(请求错误)");
                context.Response.Write("\n<pre>");
                context.Response.Write(@"/upload		上传游戏资源文件
pass = 103 	第1关第3个场景
s = 176		176像素宽的数据
debug = 1 	开启输出调试
nt = 1		不输出文件总长度
t = 1-7 	仅输出Excel文件的第2个sheet
bin = 1 	查看二进制文件的16进制数据
get = 1-5	输出分块数据1:图片数据 2:地图数据 3:Excel数据 4:动作数据  5:背景数据");
                context.Response.Write("</pre>");
                context.Response.End();
                return;
            }

            if (isReleaseModel == false)
            {
                context.Response.ContentType = "text/html";
                context.Response.Charset = "utf-8";
                context.Response.Buffer = false;
                context.Response.Write("<style>* {font-size:10.5pt; font-family:'宋体' }</style>" + Environment.NewLine);
            }

            //确保数据文件和地图存在
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                GamePass pass = new GamePass(currentpass, currentStage, strScreenDir); //通过参数获取第N关数据

                //if (pass.IsExistsPassFile())
                //{
                //    context.Response.TransmitFile(pass.GetTransmitPassFile());
                //    context.Response.End();
                //}
                //else
                //{

                    PassWriterBase[] totalDatArray = new PassWriterBase[] {  new ImageWriter(pass),
                        new MapDataWriter(pass),  new ItemWriter(pass),
                        new ActionWriter(pass), new BackgroundWriter(pass) };

                    int getIdx = -1;
                    strTemp = context.Request.QueryString["get"];
                    if (strTemp != null && Regex.IsMatch(strTemp, "^[1-5]$"))
                    {
                        getIdx = int.Parse(strTemp);
                    }

                    IBinWriter[] binWriters = (getIdx == -1) ? totalDatArray : new PassWriterBase[] { totalDatArray[getIdx-1] };
                    if (writeTotalLength)
                    {
                        bw.Write((int)0);//下载数据总长度占位
                    }

                    foreach (IBinWriter w in binWriters)
                        w.Write(bw);

                    if (writeTotalLength)
                    {
                        //下载数据总长度更新
                        bw.Seek(0, SeekOrigin.Begin);
                        bw.Write(PassWriterBase.ReverseBytes(BitConverter.GetBytes((int)ms.Length)));
                    }

                    ms.Position = 0;
                    byte[] passDat = ms.ToArray();
                    bw.Close();
                    //return;
          

                    //using (FileStream pfs = new FileStream(
                    //    context.Server.MapPath(pass.GetTransmitPassFile()),
                    //    FileMode.Create, FileAccess.Write, FileShare.Read))
                    //{
                    //    pfs.Write(passDat, 0, passDat.Length);
                    //    pfs.Flush();
                    //    pfs.Close();
                    //}

                    if (isReleaseModel == true)
                    {
                        context.Response.ContentType = "application/octet-stream";
                        strTemp = context.Request.QueryString["pass"];
                        if (!string.IsNullOrEmpty(strTemp))
                        {
                            context.Response.AddHeader("Content-Disposition", "attachment; filename=" + strTemp + ".dat");
                        }
                        else
                        {
                            context.Response.AddHeader("Content-Disposition", "attachment; filename=" + Guid.NewGuid().ToString("N") + ".dat");
                        }
                        context.Response.AppendHeader("Content-Length", passDat.Length.ToString());
                        context.Response.BinaryWrite(passDat);
                        context.Response.Flush();
                        context.Response.End();
                    }
                //}
            }


        }

        void ApplicationInstance_Error(object sender, EventArgs e)
        {
            HttpApplication app = (HttpApplication)sender;
            if (HttpContext.Current.Request["err"] == null)
            {
                HttpContext.Current.Response.Write(string.Format("出现错误：{0}", HttpContext.Current.Error.Message));
                HttpContext.Current.Response.End();
                HttpContext.Current.ClearError();
                app.CompleteRequest();
            }
        }

        /// <summary>
        /// Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler"/> instance.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Web.IHttpHandler"/> instance is reusable; otherwise, false.
        /// </returns>
        public bool IsReusable
        {
            get
            {
                return true;
            }
        }

    }

    /// <summary>
    /// 游戏关卡数据设置(TODO)
    /// </summary>
    public class GamePass
    {
        /// <summary>
        /// 游戏关卡
        /// </summary>
        /// <param name="pn">第n关</param>
        public GamePass(int pn)
        {
            passIdx = pn;
        }

        /// <summary>
        /// 游戏关卡
        /// </summary>
        /// <param name="pn">第n关</param>
        /// <param name="sn">第n个场景</param>
        public GamePass(int pn, int sn)
        {
            passIdx = pn;
            stageIdx = sn;
        }

        /// <summary>
        /// 游戏关卡
        /// </summary>
        /// <param name="pn">第n关</param>
        /// <param name="sn">第n个场景</param>
        /// <param name="screen">适配屏幕</param>
        public GamePass(int pn, int sn, string screen)
        {
            passIdx = pn;
            stageIdx = sn;

            if (!string.IsNullOrEmpty(screen))
            {
                screenDir = screen;
            }
        }

        private int passIdx = 1;
        private int stageIdx = 0;
        private string screenDir = "";

        private const string baseDataPath = "/App_Data/";
        private string _imgDatPath = "pass{0}.bin";
        private string _otherDatPath = "pass{0}.txt";
        private string _passDatPath = "pass{0}.pas.bin";

        private string _txtContent;
        private byte[] _binDat;

        /// <summary>
        /// 获取是游戏的第几关
        /// </summary>
        /// <returns></returns>
        public int GetPassNumber()
        {
            return passIdx;
        }

        /// <summary>
        /// 获取是游戏的第n个场景
        /// </summary>
        /// <returns></returns>
        public int GetPassStage()
        {
            return stageIdx;
        }

        /// <summary>
        /// 获取二进制图片路径
        /// </summary>
        /// <returns></returns>
        public string GetImagePath()
        {
            return HttpContext.Current.Server.MapPath(baseDataPath + string.Format(_imgDatPath, passIdx));
        }

        /// <summary>
        /// 获取二进制图片数据
        /// </summary>
        /// <returns></returns>
        public byte[] GetImageDat()
        {
            if (_binDat == null)
            {
                using (FileStream fs = new FileStream(GetImagePath(), FileMode.Open, FileAccess.Read))
                {
                    int totalLen = (int)fs.Length;
                    byte[] fDat = new byte[totalLen];
                    fs.Read(fDat, 0, totalLen);
                    _binDat = fDat;
                    fs.Close();
                }
            }
            return _binDat;
        }

        public bool IsExistsPassFile()
        {
            return File.Exists(HttpContext.Current.Server.MapPath(baseDataPath 
                + string.Format(_passDatPath, passIdx)));
        }

        public string GetTransmitPassFile()
        {
            return baseDataPath + string.Format(_passDatPath, passIdx);
        }

        /// <summary>
        /// 获取格式规范文件路径
        /// </summary>
        /// <returns></returns>
        public string GetSPECPath()
        {
            return HttpContext.Current.Server.MapPath(baseDataPath + string.Format(_otherDatPath, passIdx));
        }

        /// <summary>
        /// 获取格式规范文本内容
        /// </summary>
        /// <returns></returns>
        public string GetSPECContent()
        {
            if (_txtContent == null)
            {
                using (FileStream fs = new FileStream(GetSPECPath(), FileMode.Open,
                    FileAccess.Read, FileShare.ReadWrite))
                {
                    int totalLen = (int)fs.Length;
                    byte[] fDat = new byte[totalLen];
                    fs.Read(fDat, 0, totalLen);
                    fs.Close();
                    _txtContent = Encoding.Default.GetString(fDat);
                }
            }
            return _txtContent;
        }

        /// <summary>
        /// 获取关及场景的数据目录
        /// </summary>
        /// <returns></returns>
        public string GetPassDataDir()
        {
            string passDir = Path.GetDirectoryName(GetSPECPath()) + "\\pass" + GetPassNumber().ToString();
            if (GetPassStage() > 0) passDir += "\\stage" + GetPassStage().ToString();
            if (screenDir.Trim() != string.Empty)
            {
                passDir += "\\" + screenDir;
            }
            return passDir;
        }

        public static void Debug(object obj)
        {
            HttpContext.Current.Response.Write(obj);
            HttpContext.Current.Response.Write("<br/>");
        }
    }

    /// <summary>
    /// 游戏关卡数据输出基类
    /// </summary>
    public class PassWriterBase : IBinWriter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pass"></param>
        public PassWriterBase(GamePass pass)
        {
            _pass = pass;
        }

        private GamePass _pass;

        #region IBinWriter 成员
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bw"></param>
        public virtual void Write(BinaryWriter bw)
        {
            
        }

        #endregion
        /// <summary>
        /// 当前游戏关，包含场景位置。
        /// </summary>
        public GamePass Pass
        {
            get { return _pass; }
            set { _pass = value; }
        }

        #region 辅助函数
        /// <summary>
        /// 反转二进制字节序列
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] ReverseBytes(byte[] bytes)
        {
            int num = bytes.Length / 2;
            byte by;
            int idx;
            for (int i = 0; i < num; i++)
            {
                by = bytes[i];
                idx = bytes.Length - i - 1;
                bytes[i] = bytes[idx];
                bytes[idx] = by;
            }
            return bytes;
        }

        /// <summary>
        /// 获取满足条件的替换数据
        /// </summary>
        public static object GetReplaceData(object old, object rep)
        {
            return (old == DBNull.Value || old.ToString() == "") ? rep : old;
        }

        /// <summary>
        /// 获取二进制的十六进制查看方式数据
        /// </summary>
        /// <param name="binDat">二进制数据</param>
        /// <returns>二进制的16进制字符形式</returns>
        public static string GetHexViewString(byte[] binDat)
        {
            //tbxBinView.Text = "总长度：" + binDat.Length.ToString() + "字节"
            //    + Environment.NewLine + Environment.NewLine;

            byte[] ascByte = new byte[16];
            int lastRead = 0;

            StringBuilder sb = new StringBuilder();
            for (int i = 0, j = binDat.Length; i < j; i++)
            {
                if (i == 0)
                {
                    sb.Append("00000000  ");
                }

                sb.Append(binDat[i].ToString("X2") + " ");
                lastRead = i % 16;
                ascByte[lastRead] = binDat[i];

                if (i > 0 && (i + 1) % 8 == 0 && (i + 1) % 16 != 0)
                {
                    sb.Append(" ");
                }

                if (i > 0 && (i + 1) % 16 == 0)
                {
                    sb.Append(" ");
                    foreach (byte chrB in ascByte)
                    {
                        if (chrB >= 0x20 && chrB <= 0x7E) //[32,126]
                        {
                            sb.Append((char)chrB);
                        }
                        else
                        {
                            sb.Append('.');
                        }
                    }

                    if (i + 1 != j)
                    {
                        sb.Append(Environment.NewLine);
                        sb.Append((i + 1).ToString("X2").PadLeft(8, '0') + "  ");
                    }
                }
            }

            if (lastRead < 15)
            {
                sb.Append(new string(' ', (15 - lastRead) * 3));
                if (lastRead < 8) sb.Append(" ");
                sb.Append(" ");
                for (int m = 0; m <= lastRead; m++)
                {
                    byte charL = ascByte[m];
                    if (charL >= 0x20 && charL <= 0x7E) //[32,126]
                    {
                        sb.Append((char)charL);
                    }
                    else
                    {
                        sb.Append('.');
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取特定字符片断字符块
        /// </summary>
        /// <param name="Source">原始字符</param>
        /// <param name="snippet">中间代码片断，以“REGEX:”开头则后续字符视为不区分大小写正则匹配模式。</param>
        /// <param name="strBefore">片断前置字符</param>
        /// <param name="strAfter">片断后置字符</param>
        public static string GetSnippetBlock(ref string Source, string snippet, string strBefore, string strAfter)
        {
            int idx = -1, len = snippet.Length;
            string strReturn = string.Empty;
            int regexIndex = 0, regexCount = 1;
            MatchCollection mc = null;

            if (snippet.StartsWith("REGEX:"))
            {
                string pattern = snippet.Substring(6);
                mc = Regex.Matches(Source, pattern, RegexOptions.IgnoreCase);
                if (mc.Count > 0)
                {
                    regexCount = mc.Count;
                    idx = mc[regexIndex].Index;
                    len = mc[regexIndex].Length;
                }
            }
            else
            {
                idx = Source.IndexOf(snippet);
            }


        FetchNext:

            if (idx != -1)
            {
                int idxBegin = Source.LastIndexOf(strBefore, idx);
                int idxEnd = Source.IndexOf(strAfter, idx + len);
                if (idxBegin != -1 && idxEnd != -1)
                {
                    len = idxEnd + strAfter.Length - idxBegin;
                    strReturn = Source.Substring(idxBegin, len);
                }
                else
                {
                    if (mc != null)
                    {
                        #region 如果有下一组匹配
                        if (regexIndex + 1 < regexCount)
                        {
                            regexIndex++;
                            idx = mc[regexIndex].Index;
                            len = mc[regexIndex].Length;
                            goto FetchNext;
                        }
                        #endregion
                    }
                    else
                    {
                        #region 字符下一个查找
                        idx = Source.IndexOf(snippet, idx + 1);
                        if (idx != -1)
                            goto FetchNext;
                        #endregion
                    }
                }
            }

            return strReturn;
        }

        /// <summary>
        /// 剔除前后的字符片段
        /// </summary>
        /// <param name="source">字符采集源</param>
        /// <param name="prefStr">前置字符片段</param>
        /// <param name="subStr">后台字符片段</param>
        /// <returns>文本中间所剩内容块</returns>
        public static string StripSnippet(ref string source, string prefStr, string subStr)
        {
            string strTemp;
            int idxBegin = source.IndexOf(prefStr);
            int idxEnd = source.LastIndexOf(subStr);
            if (idxBegin != -1 && idxEnd != -1)
            {
                strTemp = source.Substring(idxBegin + prefStr.Length);
                return strTemp.Substring(0, idxEnd - idxBegin - prefStr.Length);
            }
            else
                return source;
        }


        /// <summary>
        /// 获取单一匹配项组的值 ，如果有多项则选最后一项。
        /// </summary>
        /// <param name="pattern">匹配模式，支持直接量语法和选项‘/regex/gim’。</param>
        /// <param name="strInput">匹配查找源</param>
        /// <param name="objMatch">捕获匹配项集合</param>
        /// <returns>是否找到匹配</returns>
        public static bool GetSingleMatchValue(string pattern, string strInput, out string[] objMatch)
        {
            RegexOptions ExOption = RegexOptions.IgnoreCase | RegexOptions.Multiline;
            Match m = Regex.Match(pattern, "^/(.+)/(g?i?m?)$", ExOption);
            if (m.Success)
            {
                pattern = m.Groups[1].Value;
                string strOption = m.Groups[2].Value;
                if (strOption != string.Empty)
                {
                    ExOption = (strOption.IndexOf("g") != -1) ? RegexOptions.Multiline : RegexOptions.Singleline;
                    if (strOption.IndexOf("i") != -1)
                    {
                        ExOption |= RegexOptions.IgnoreCase;
                    }
                }
            }

            m = Regex.Match(strInput, pattern, ExOption);
            if (m.Success)
            {
                objMatch = new string[m.Groups.Count];
                for (int i = 0; i < objMatch.Length; i++)
                {
                    objMatch[i] = m.Groups[i].Value;
                }
                return true;
            }
            else
            {
                objMatch = null;
                return false;
            }
        }

        /// <summary>
        /// 提取文件中的前置数据ID
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <returns></returns>
        public static string ExtractID(string filename)
        {
            string pattern = "^(\\d+)(.*)\\.(\\w{1,4})";
            Match m = Regex.Match(filename, pattern, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                return m.Groups[1].Value;
            }
            else
            {
                return "0";
            }
        }

        /// <summary>
        /// 获取文件的二进制字节
        /// </summary>
        /// <param name="fileNamePath">文件路径</param>
        /// <returns></returns>
        public static byte[] GetFileBytes(string fileNamePath)
        {
            FileInfo fi = new FileInfo(fileNamePath);
            byte[] buffer = new byte[(int)fi.Length];
            using (FileStream fsR = fi.OpenRead())
            {
                fsR.Read(buffer, 0, (int)fi.Length);
                fsR.Close();
            }
            return buffer;
        }
        #endregion

    }

    public interface IBinWriter
    {
        void Write(BinaryWriter bw);
    }

    /// <summary>
    /// 地图数据
    /// </summary>
    public class MapDataWriter : PassWriterBase
    {
        /// <summary>
        /// 游戏地图数据输出
        /// </summary>
        /// <param name="pass">游戏关卡</param>
        public MapDataWriter(GamePass pass)
            : base(pass)
        {
            
        }

        public override void Write(BinaryWriter bw)
        {
            //string Dat = Pass.GetSPECContent();
            //string txtMapDat = PassWriterBase.GetSnippetBlock(ref Dat, @"REGEX:=\s*\{",
            //    "map", "}");

            //txtMapDat = PassWriterBase.StripSnippet(ref txtMapDat, "{", "}");
            //string[] datArray = txtMapDat.Split(',');
            ////地图数组总长度
            //bw.Write(PassWriterBase.ReverseBytes(BitConverter.GetBytes((short)datArray.Length)));
            ////地图数组元素
            //foreach (string str in datArray)
            //{
            //    bw.Write(PassWriterBase.ReverseBytes(BitConverter.GetBytes(Convert.ToInt16(str.Trim()))));
            //}
            #region 地图编辑文件
            string passDir = Pass.GetPassDataDir();
            FileInfo fiMap = new FileInfo(passDir + "\\pass.gap");
            if (fiMap.Exists)
            {
                bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(fiMap.Length))));
                bw.Write(GetFileBytes(fiMap.FullName));
            }
            else
            {
                bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(0))));
            }
            #endregion
        }
    }

    /// <summary>
    /// 碰撞数据
    /// </summary>
    [Obsolete]
    public class CollideWriter : PassWriterBase
    {
        public CollideWriter(GamePass pass)
            : base(pass)
        {
        }

        public override void Write(BinaryWriter bw)
        {
            string Dat = Pass.GetSPECContent();
            string txtMapDat = PassWriterBase.GetSnippetBlock(ref Dat, @"REGEX:=\s*\{",
                "collide", "}");

            txtMapDat = PassWriterBase.StripSnippet(ref txtMapDat, "{", "}");
            string[] datArray = txtMapDat.Split(',');
            //碰撞数组总长度
            bw.Write(PassWriterBase.ReverseBytes(BitConverter.GetBytes((short)datArray.Length)));
            //碰撞数组元素
            foreach (string str in datArray)
            {
                bw.Write(PassWriterBase.ReverseBytes(BitConverter.GetBytes(Convert.ToInt16(str.Trim()))));
            }
        }
    }

    /// <summary>
    /// NPC数据
    /// </summary>
    [Obsolete]
    public class NPCWriter : PassWriterBase
    {
        public NPCWriter(GamePass pass)
            : base(pass)
        {
        }

        public override void Write(BinaryWriter bw)
        {
            StringReader npw = new StringReader(Pass.GetSPECContent());
            string lineStr;
            bool gotted = false;
            string[] numArray = null;
            while ((lineStr = npw.ReadLine()) != null)
            {
                if (lineStr.StartsWith("NPC"))
                {
                    gotted = true;
                    if (PassWriterBase.GetSingleMatchValue(@"(:|：)\s*(\d+)", lineStr, out numArray))
                    {
                        bw.Write(PassWriterBase.ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(numArray[2]))));
                    }
                }
                else
                {
                    if (gotted == true) break;
                }
            }
        }
    }

    /// <summary>
    /// 物体数据
    /// </summary>
    public class ItemWriter : PassWriterBase
    {
        /// <summary>
        /// 物件数据(Excel文件数据)
        /// </summary>
        /// <param name="pass">游戏关卡</param>
        public ItemWriter(GamePass pass)
            : base(pass)
        {
        }

        private void WriteBytes(BinaryWriter bw, object dat, Type dType)
        {
            if (MGAME.isReleaseModel == false)
            {
                object oOut = GetReplaceData(dat, (dType == typeof(string)) ?  "" : "0"); 
                HttpContext.Current.Response.Write("<td>" + oOut.ToString() + "</td>");
                return;
            }
            
            bool hasSplitWrite = false;
            byte[] bts = new byte[0];
            if (dType == typeof(string))
            {
                bts = Encoding.Unicode.GetBytes(dat != null ? dat.ToString() : "");
                bw.Write(ReverseBytes(BitConverter.GetBytes((int)bts.Length)));
            }
            else if (dType == typeof(byte))
            {
                bts = ReverseBytes(BitConverter.GetBytes(Convert.ToByte(GetReplaceData(dat, 0))));
            }
            else if (dType == typeof(short))
            {
                bts = ReverseBytes(BitConverter.GetBytes(Convert.ToInt16(GetReplaceData(dat, 0))));
            }
            else if (dType == typeof(int))
            {
                bts = ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(GetReplaceData(dat, 0))));
            }
            else if (dType == typeof(byte[]))
            {
                string raw = GetReplaceData(dat, "").ToString();
                if (raw == "")
                {
                    bts = ReverseBytes(BitConverter.GetBytes(Convert.ToByte(0)));
                }
                else
                {
                    string[] objRaw = raw.Trim('[', ']').Split(',');
                    bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToByte(objRaw.Length))));
                    foreach (string s in objRaw)
                    {
                        bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToByte(s))));
                    }
                    hasSplitWrite = true;
                }
            }
            else if (dType == typeof(short[]))
            {
                string raw = GetReplaceData(dat, "").ToString();
                if (raw == "")
                {
                    bts = ReverseBytes(BitConverter.GetBytes(Convert.ToInt16(0)));
                }
                else
                {
                    string[] objRaw = raw.Trim('[', ']').Split(',');
                    bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt16(objRaw.Length))));
                    foreach (string s in objRaw)
                    {
                        bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt16(s))));
                    }
                    hasSplitWrite = true;
                }
            }
            else if (dType == typeof(int[]))
            {
                string raw = GetReplaceData(dat, "").ToString();
                if (raw == "")
                {
                    bts = ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(0)));
                }
                else
                {
                    string[] objRaw = raw.Trim('[', ']').Split(',');
                    bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(objRaw.Length))));
                    foreach (string s in objRaw)
                    {
                        bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(s))));
                    }
                    hasSplitWrite = true;
                }
            }
            if (!hasSplitWrite) bw.Write(bts);
        }

        private void WriteSingleTable(BinaryWriter bw, DataTable dTab, Type[] cTypes)
        {
            HttpResponse resp = HttpContext.Current.Response;
            if (MGAME.isReleaseModel == false)
            {
                #region 表头字符输出
                resp.Write("<table border=\"1\" style=\"border-collapse:collapse;\" cellpadding=\"5\" cellspacing=\"0\">");
                resp.Write("<tr><td colspan=\"" + (dTab.Columns.Count + 1) + "\">");
                resp.Write("<span style=\"font-weight:bold;color:#990000;\"><u>#" + dTab.TableName + "</u></span>");
                resp.Write("</td></tr>" + Environment.NewLine);
                resp.Write("<tr><th>序 号</th>");

                int idx = 0;
                foreach (DataColumn col in dTab.Columns)
                {
                    //输出列名
                    resp.Write(string.Format("<th><span title=\"{1}\">{0}</span></th>", col.ColumnName, cTypes[idx]));
                    idx++;
                }
                resp.Write("</tr>" + Environment.NewLine);
                #endregion
            }
            
            if (dTab != null && dTab.Rows.Count > 0)
            {
                int realRows = 0;

                #region 统计有效数据的行数
                for (int i = 0, j = dTab.Rows.Count; i < j; i++)
                {
                    DataRow dRow = dTab.Rows[i];
                    if (dRow[0] == DBNull.Value || dRow[0].ToString().Trim() == string.Empty)
                    {
                        continue;
                    }
                    else
                    {
                        realRows++;
                    }
                }
                #endregion
                if (MGAME.isReleaseModel == false && realRows != dTab.Rows.Count)
                {
                    resp.Write("<tr><td bgcolor=\"#f3f3f3\" colspan=\"" + (dTab.Columns.Count + 1) + "\">");
                    resp.Write(string.Format("获取数据数量不一致：<span style=\"font-weight:bold;color:#FF0000;\">实际记录数：{0}，有效数据数：{1}</span>",
                        dTab.Rows.Count,
                        realRows));
                    resp.Write("</td></tr>");
                }

                //bw.Write(ReverseBytes(BitConverter.GetBytes((System.Int16)dTab.Rows.Count)));
                bw.Write(ReverseBytes(BitConverter.GetBytes((System.Int16)realRows)));

                realRows = 0;
                for (int i = 0, j = dTab.Rows.Count; i < j; i++)
                {
                    DataRow dRow = dTab.Rows[i];
                    if (dRow[0] == DBNull.Value || dRow[0].ToString().Trim() == string.Empty)
                    {
                        continue;
                    }
                    realRows++;

                    if (MGAME.isReleaseModel == false)
                    {
                        HttpContext.Current.Response.Write("<tr><td>"  + realRows +"</td>");
                    }

                    for (int k = 0; k < dTab.Columns.Count; k++)
                    {
                        WriteBytes(bw, dRow[k], cTypes[k]);
                    }

                    if (MGAME.isReleaseModel == false)
                    {
                        HttpContext.Current.Response.Write("</tr>" + Environment.NewLine);
                    }
                }

            }
            else
            {
                bw.Write(ReverseBytes(BitConverter.GetBytes((System.Int16)0)));
            }

            if (MGAME.isReleaseModel == false)
            {
                HttpContext.Current.Response.Write("</table><br/>" + Environment.NewLine);
            }
        }

        public override void Write(BinaryWriter bw)
        {
            string[] sheets = "道具 人物 事件 任务 动画 技能 对话".Split(' ');

            #region 表格数据类型
            Type[][] stTypes = new Type[][] {
                //道具
                new Type[] { typeof(string), typeof(string), typeof(int), typeof(short), typeof(short),
                    typeof(short), typeof(short), typeof(short), typeof(short), typeof(short),
                    typeof(int), typeof(short), typeof(short), typeof(short), typeof(int),
                    typeof(short)},

                //人物
                new Type[] { typeof(string), typeof(int), typeof(short), typeof(short), typeof(short),
                    /*最大生命*/typeof(short), typeof(short), typeof(short), typeof(int), typeof(short),
                    /*初始坐标Y*/ typeof(short), typeof(short), typeof(short), typeof(int), typeof(int),
                    /*怒气值*/ typeof(short), typeof(short), typeof(short), typeof(short), typeof(short),
                    /*相关事件ID列表*/ typeof(int[]), typeof(int[]), typeof(int[])},

                //事件
                new Type[] { typeof(int), typeof(short), typeof(short), typeof(short),
                 /*触发矩形宽*/typeof(short), typeof(short), typeof(short[]), typeof(int[]), typeof(short[]),
                 /*地图ID*/typeof(short), typeof(short), typeof(int), typeof(int)},

                //任务
                new Type[] { typeof(string), typeof(string), typeof(int), typeof(short), typeof(short),
                 /*是否接受任务*/typeof(short), typeof(short), typeof(int), typeof(short), typeof(short),
                 /*相关怪物ID*/ typeof(int), typeof(short), typeof(short), typeof(short), typeof(int[]),
                 /*相关NPC_ID*/ typeof(int[])},

                //动画
                new Type[] { typeof(int), typeof(short), typeof(short), typeof(short),
                   /*结束坐标X*/typeof(short), typeof(short), typeof(int), typeof(short), typeof(short),
                   /*速度*/typeof(short), typeof(int[])},

               //技能
               new Type[] { typeof(string), typeof(string), typeof(int), typeof(short), typeof(short),
                   /*消耗自身技能值*/typeof(short), typeof(short)},

                //对话
                new Type[] { typeof(int), typeof(string)}
            };
            #endregion

            string objFileDir = Pass.GetPassDataDir();
            if (!File.Exists(objFileDir + "\\Object.xls")) throw new FileNotFoundException("物件数据文件Excel在当前数据目录不存在！");
            DataSet dSet = new DataSet();
            using (OleDbConnection conn = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source="
                + objFileDir + "\\Object.xls;Extended Properties=Excel 8.0;Persist Security Info=False"))
            {
                conn.Open();
                string sql = string.Format("select * from [{0}$]", sheets[0]);
                OleDbDataAdapter adp = new OleDbDataAdapter(sql, conn);
                if (HttpContext.Current.Request.QueryString["t"] != null)
                {
                    if (Regex.IsMatch(HttpContext.Current.Request.QueryString["t"], @"^[1-7]$"))
                    {
                        int idx = Convert.ToInt32(HttpContext.Current.Request.QueryString["t"])-1;
                        adp.SelectCommand.CommandText = string.Format("select * from [{0}$]",
                            sheets[idx]);

                        adp.Fill(dSet, sheets[idx]);

                        WriteSingleTable(bw, dSet.Tables[0], stTypes[idx]);

                        //return;
                    }
                }
                else
                {
                    adp.Fill(dSet, sheets[0]);
                    for (int v = 1; v < sheets.Length; v++)
                    {
                        adp.SelectCommand.CommandText = string.Format("select * from [{0}$]", sheets[v]);
                        adp.Fill(dSet, sheets[v]);
                    }
                }
                adp.Dispose();
                conn.Close();
            }

            if (dSet.Tables.Count > 1)
            {
                for (int t = 0; t < sheets.Length; t++)
                {
                    WriteSingleTable(bw, dSet.Tables[t], stTypes[t]);
                }
            }
        }
    }

    /// <summary>
    /// 图片数据
    /// </summary>
    public class ImageWriter : PassWriterBase
    {
        /// <summary>
        /// 图片数据输出
        /// </summary>
        /// <param name="pass">游戏关卡</param>
        public ImageWriter(GamePass pass)
            : base(pass)
        {
            //GameImageBinGen binGen = new GameImageBinGen(pass);
            //if (!binGen.ExistBinFile())
            //{
            //    binGen.AppendTo(pass.GetSPECPath(), binGen.Generator());
            //}
        }

        public override void Write(BinaryWriter bw)
        {
            // + 判断图片二进制文件是否存在，不存在则生成。
            //bool useMergin = false;
            //bool tempTest = true;

            //if (useMergin == true)
            //{
            //    #region 整合进数据文件
            //    StringReader npw = new StringReader(Pass.GetSPECContent());
            //    string lineStr;
            //    bool gotted = false;
            //    string[] numArray = null;
            //    while ((lineStr = npw.ReadLine()) != null)
            //    {
            //        if (lineStr.StartsWith("img"))
            //        {
            //            gotted = true;

            //            if (PassWriterBase.GetSingleMatchValue(@"(:|：)\s*(\d+)", lineStr, out numArray))
            //            {
            //                bw.Write(PassWriterBase.ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(numArray[2]))));
            //            }

            //            //图片大小下直接附加二进制文件流
            //            if (lineStr.StartsWith("imgSize"))
            //            {
            //                bw.Write(Pass.GetImageDat());
            //            }
            //        }
            //        else
            //        {
            //            if (gotted == true) break;
            //        }
            //    }
            //    #endregion
            //}
            //else if (tempTest == true)
            //{

                string passDir = Pass.GetPassDataDir();
                DirectoryInfo dif = new DirectoryInfo(passDir);
                FileInfo[] files = dif.GetFiles("part*.dat", SearchOption.TopDirectoryOnly);
                
                //文件块数量
                bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt16(files.Length))));
                //if (MGAME.isReleaseModel == false)
                //{
                    //HttpContext.Current.Response.Write(files.Length);
                    //HttpContext.Current.Response.End();
                //}
                #region 压缩图片块数据
                foreach (FileInfo fi in files)
                {
                    bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(fi.Length))));
                    bw.Write(GetFileBytes(fi.FullName)); 
                }

                #endregion


            //}
            //else
            //{
            //    #region 分别输出索引和图片图片数据
            //    FileInfo binFile = new FileInfo(Pass.GetImagePath());
            //    //数据总大小
            //    bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(binFile.Length))));
            //    //输出图片二进制数据
            //    bw.Write(Pass.GetImageDat());

            //    using (OleDbConnection conn = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source="
            //     + HttpContext.Current.Server.MapPath("/App_Data/idx/")
            //     + ";Extended Properties=\"text;HDR=Yes;FMT=Delimited\";"))
            //    {
            //        string sql = "select * from [" + Pass.GetPassNumber().ToString() + "#txt]";
            //        OleDbDataAdapter adp = new OleDbDataAdapter(sql, conn);
            //        DataSet dSet = new DataSet();
            //        adp.Fill(dSet);

            //        if (dSet.Tables.Count > 0)
            //        {
            //            DataTable dTab = dSet.Tables[0];
            //            DataRow dRow;
            //            //图片总数
            //            bw.Write(ReverseBytes(BitConverter.GetBytes(dTab.Rows.Count)));

            //            for (int i = 0, j = dTab.Rows.Count; i < j; i++)
            //            {
            //                dRow = dTab.Rows[i];
            //                //img_n_start
            //                bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(dRow["Pos"]))));
            //                //img_n_size
            //                bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(dRow["Size"]))));
            //                //img_n_id
            //                bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(Regex.Replace(dRow["FName"].ToString(), @"[^\d+]", "").TrimStart('0')))));
            //            }
            //        }
            //        adp.Dispose();
            //    }
            //    #endregion
            //}


        }
    }


    /// <summary>
    /// 动作数据输出
    /// LastMod:2009-2-25 Ridge
    /// </summary>
    public class ActionWriter : PassWriterBase
    {
        /// <summary>
        /// 动作数据输出
        /// </summary>
        /// <param name="pass">游戏关卡</param>
        public ActionWriter(GamePass pass)
            : base(pass)
        { 
        
        }

        public override void Write(BinaryWriter bw)
        {
            /*
            动作总个数	int
            第1个人物动作ID	int
            第1个动作的图片数据长度	int
            图片数据	byte

            第1个动作的数据长度	int
            动作数据	byte
            ...
            */

            string passDir = Pass.GetPassDataDir();
            DirectoryInfo dif = new DirectoryInfo(passDir + "\\Action");
            if (!dif.Exists) { dif.Create(); };
            FileInfo[] files = dif.GetFiles("*.png", SearchOption.TopDirectoryOnly);

            //动作总个数	int(文件块数量)
            if (MGAME.isReleaseModel == false)
            {
                GamePass.Debug("Action:文件总数 - " + files.Length);
            }
            else
            {
                bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(files.Length))));
            }
            FileInfo actDat = null;

            byte[] fileData = null;
            foreach (FileInfo fi in files)
            {
                if (MGAME.isReleaseModel == false)
                {
                    GamePass.Debug("<br>");
                    GamePass.Debug("Action:文件ID - " + ExtractID(fi.Name));
                }
                else
                {
                    bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(ExtractID(fi.Name)))));
                }

                if (MGAME.isReleaseModel == false)
                {
                    GamePass.Debug("Action:文件大小 - " + fi.Length);
                    GamePass.Debug("Action:文件名称 - " + fi.Name);
                }
                else
                {
                    bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(fi.Length))));
                }

                fileData = GetFileBytes(fi.FullName);
                if (MGAME.isReleaseModel == false && MGAME.outputBinnary == true)
                {
                    GamePass.Debug("Action:文件数据 -> <hr><span style=\"background-color:F3f3f3\">" 
                        + GetHexViewString(fileData).Replace(Environment.NewLine, "<br>")
                        + "</span>");
                }
                else
                {
                    bw.Write(fileData);
                }

                actDat = new FileInfo(Path.ChangeExtension(fi.FullName, ".dat"));
                if (actDat.Exists)
                {
                    if (MGAME.isReleaseModel == false)
                    {
                        GamePass.Debug("<br>Action:文件数据长度 - " + actDat.Length);
                        GamePass.Debug("Action:文件名称 - " + actDat.Name);
                    }
                    else
                    {
                        bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(actDat.Length))));
                    }

                    fileData = GetFileBytes(actDat.FullName);
                    if (MGAME.isReleaseModel == false && MGAME.outputBinnary == true)
                    {
                        GamePass.Debug("Action:文件数据 -> <hr><span style=\"background-color:F3f3f3\">" 
                            + GetHexViewString(fileData).Replace(Environment.NewLine, "<br>")
                            + "</span>");
                    }
                    else
                    {
                        bw.Write(fileData);
                    }
                    
                }
            }

        }
    }

    /// <summary>
    /// 背景图片数据输出
    /// </summary>
    public class BackgroundWriter : PassWriterBase
    {
        /// <summary>
        /// 背景图片数据输出
        /// </summary>
        /// <param name="pass">当前游戏关卡</param>
        public BackgroundWriter(GamePass pass)
            : base(pass)
        { }

        public override void Write(BinaryWriter bw)
        {
            string passDir = Pass.GetPassDataDir();
            FileInfo bgFi = new FileInfo(passDir + "\\background.png");
            if (bgFi.Exists)
            {
                if (MGAME.isReleaseModel == false)
                {
                    GamePass.Debug("背景:文件大小 - " + bgFi.Length);
                }
                else
                {
                    bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(bgFi.Length))));
                }

                byte[] fileData = GetFileBytes(bgFi.FullName);
                if (MGAME.isReleaseModel == false && MGAME.outputBinnary == true)
                {
                    GamePass.Debug("背景:文件数据 -> <hr><span style=\"background-color:F3f3f3\">"
                        + GetHexViewString(fileData).Replace(Environment.NewLine, "<br>")
                        + "</span>");
                }
                else
                {
                    bw.Write(fileData);
                }
            }
            else
            {
                bw.Write(ReverseBytes(BitConverter.GetBytes(Convert.ToInt32(0))));
            }
        }
    }

    /// <summary>
    /// 游戏关卡二进制图片文件生成与更新协议文件
    /// </summary>
    public class GameImageBinGen
    { 
        //图片张数	int
        //img_1（起始位置）	int
        //img_1（图片大小）	int
        //……	
        //img_n（起始位置）	int
        //img_n（图片大小）	int

        public GameImageBinGen(GamePass pass)
        {
            _pass = pass;
        }

        private GamePass _pass;

        public bool ExistBinFile()
        {
            return File.Exists(_pass.GetImagePath());
        }

        internal void AppendTo(string specFilePath, string imgBinDesc)
        {
            StreamWriter sw = new StreamWriter(specFilePath, true);
            sw.Write(imgBinDesc);
            sw.Close();
            sw.Dispose();
        }

        internal string Generator()
        {
            DirectoryInfo dif = new DirectoryInfo(Path.GetDirectoryName(_pass.GetSPECPath()) + "\\pic"
                + _pass.GetPassNumber().ToString());
            FileInfo[] files = dif.GetFiles("*.png", SearchOption.TopDirectoryOnly);

            FileStream fsBin = new FileStream(_pass.GetImagePath(),
                FileMode.Create, FileAccess.Write);
            FileStream fs;
            byte[] buffer;
            string[] arrStart = new string[files.Length];
            string[] arrSize = new string[files.Length];
            int idx = 1;
            long offset = 0;

            StringBuilder sb = new StringBuilder();
            foreach (FileInfo fi in files)
            {
                sb.AppendLine(string.Format("img_{0}_start:{1} //int", idx, offset));
                sb.AppendLine(string.Format("img_{0}_size:{1} //int", idx, fi.Length));
                sb.AppendLine(string.Format("img_{0}_id:{1} //int", idx, Regex.Replace(fi.Name, @"[^\d+]", "").TrimStart('0')));

                using (fs = fi.OpenRead())
                {
                    buffer = new byte[(int)fi.Length];
                    fs.Read(buffer, 0, (int)fi.Length);
                    fs.Close();

                    #region 执行相应压缩算法

                    #endregion

                    fsBin.Write(buffer, 0, buffer.Length);
                    fsBin.Flush();
                }

                offset += fi.Length;
                idx++;
            }

            sb.Insert(0, string.Format("imgNum:{0}{1}", files.Length, Environment.NewLine));
            sb.Insert(0, string.Format("imgSize:{0}{1}", offset, Environment.NewLine));

            fsBin.Close();
            fsBin.Dispose();
            return sb.ToString();
        }
    }
}