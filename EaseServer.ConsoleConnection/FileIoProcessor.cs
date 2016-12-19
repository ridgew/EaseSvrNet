using System;
using System.IO;
using System.Text;
using CommonLib;

namespace EaseServer.ConsoleConnection
{
    public partial class FreeStyleConnection
    {
        /// <summary>
        /// 文件处理
        /// </summary>
        class FileIoProcessor
        {
            internal static FileIoProcessor Create(FreeStyleConnection connContext, string[] args)
            {
                return new FileIoProcessor(connContext, args);
            }
            /*
            Read "filePath" [L]0-[L]5 
            ReadHex "filePath"
            update "filePath" 0x03DF
            delete "filePath"
            */
            private FileIoProcessor(FreeStyleConnection connContext, string[] args)
            {
                ConnectContext = connContext;

                if (args != null)
                {
                    #region 分析参数
                    if (args.Length > 0)
                    {
                        if (args[0].IndexOf('.') != -1)
                        {
                            filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, args[0]);
                        }
                        else
                        {
                            if (Enum.IsDefined(typeof(FileAction), args[0]))
                            {
                                currentAct = (FileAction)Enum.Parse(typeof(FileAction), args[0], true);
                            }
                        }

                        #region 参数大于1个
                        if (args.Length > 1)
                        {
                            if (currentAct == FileAction.schtasks)
                            {
                                filePath = null;
                                fileData = args[1];
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(filePath))
                                {
                                    if (args[1].Equals(".", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        filePath = AppDomain.CurrentDomain.BaseDirectory;
                                    }
                                    else
                                    {
                                        filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, args[1]);
                                    }
                                }
                                else
                                {
                                    fileData = args[1];
                                }
                            }
                        }
                        #endregion

                        #region 参数大于2个
                        if (args.Length > 2)
                        {
                            if (args[2].StartsWith("en:"))
                            {
                                ExtensionUtil.CatchAll(() =>
                                {
                                    currentEncoding = Encoding.GetEncoding(args[2].Substring(3));
                                });

                                if (args.Length > 3)
                                    fileData = args[3];
                            }
                            else
                            {
                                fileData = args[2];
                            }
                        }
                        #endregion
                    }
                    #endregion
                }
            }

            FileAction currentAct = FileAction.read;
            string filePath, fileData;
            Encoding currentEncoding = Encoding.Default;

            public FreeStyleConnection ConnectContext { get; set; }

            public void ExecuteClose(Stream exchange)
            {
                if ((currentAct != FileAction.store && currentAct != FileAction.schtasks)
                    && (!File.Exists(filePath) && !Directory.Exists(filePath)))
                {
                    exchange.WriteLineWith("* Error [{0}]不存在，或操作{1}未被识别！", filePath, currentAct);
                    return;
                }

                string cmdOutPut = null;
                int exitCode = -1;
                string strRet = "* 没有任何操作";
                try
                {
                    switch (currentAct)
                    {
                        case FileAction.read:
                            strRet = FileReader.Create(filePath, fileData, currentEncoding).GetContent();
                            break;

                        case FileAction.readHex:
                            strRet = File.ReadAllBytes(filePath).GetHexViewString();
                            break;

                        case FileAction.store:
                            #region 更新文件
                            if (!string.IsNullOrEmpty(fileData))
                            {
                                byte[] fileBin = stringAsBytes(fileData);
                                //自动创建目录
                                DirectoryInfo dInfo = new DirectoryInfo(Path.GetDirectoryName(filePath));
                                if (!dInfo.Exists) dInfo.Create();

                                File.WriteAllBytes(filePath, fileBin);
                                strRet = "* 写入文件{0}共计{1}字节完成！".FormatWith(filePath, fileBin.Length);
                            }
                            #endregion
                            break;

                        case FileAction.delete:
                            #region 删除文件
                            try
                            {
                                string tar = "文件";
                                if (File.Exists(filePath))
                                {
                                    File.Delete(filePath);
                                }
                                else
                                {
                                    if (Directory.Exists(filePath))
                                    {
                                        tar = "目录";
                                        Directory.Delete(filePath, true);
                                    }
                                    else
                                    {
                                        tar = "{不存在的目录或文件}";
                                    }
                                }
                                strRet = "* 删除{1}[{0}]完成".FormatWith(filePath, tar);
                            }
                            catch (Exception fEx) { strRet = fEx.Message; }
                            #endregion
                            break;


                        case FileAction.list:
                            #region 目录列表
                            if (!Directory.Exists(filePath))
                            {
                                exchange.WriteLineWith("* 文件目录{0}不存在！", filePath);
                            }
                            else
                            {
                                DirectoryInfo dInfo = new DirectoryInfo(filePath);
                                foreach (DirectoryInfo subD in dInfo.GetDirectories())
                                {
                                    exchange.WriteLineWith("[Dir] {0}\t\t\t\t{1:yyyy-MM-dd HH:mm:ss}\t\t[{2}]",
                                       subD.Name, subD.LastWriteTime,
                                       ((subD.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) ? "R" : "");
                                }
                                FileInfo[] allFiles = dInfo.GetFiles(string.IsNullOrEmpty(fileData) ? "*.*" : fileData, SearchOption.TopDirectoryOnly);
                                int fIdx = 0; long fTotalCount = 0;
                                foreach (FileInfo f in allFiles)
                                {
                                    fTotalCount += f.Length;
                                    exchange.WriteLineWith("[{4}] {0}\t\t\t\t{1}bytes\t\t{2:yyyy-MM-dd HH:mm:ss}\t\t[{3}]",
                                        f.Name, f.Length, f.LastWriteTime, f.IsReadOnly ? "R" : "",
                                        ++fIdx);
                                }
                                strRet = string.Format("* 共计{0}个文件，总大小{1:0.00}K。", fIdx, ((double)fTotalCount / 1024.00));
                            }
                            #endregion
                            break;

                        case FileAction.exec:
                            #region 简易命令运行
                            if (filePath.IndexOf("%") != -1)
                            {
                                filePath = Environment.ExpandEnvironmentVariables(filePath);
                            }

                            if (filePath == "." || !File.Exists(filePath))
                            {
                                filePath = "cmd";
                                exchange.WriteLineWith("* 文件{0}不存在，已修改为使用cmd执行！", filePath);
                            }

                            try
                            {
                                exitCode = ConsoleUntil.RunCmd(filePath, Path.GetDirectoryName(filePath), fileData, 100, ref cmdOutPut);
                            }
                            catch (Exception execEx)
                            {
                                cmdOutPut = execEx.ToString();
                            }
                            if (!string.IsNullOrEmpty(cmdOutPut))
                            {
                                exchange.WriteLineWith(cmdOutPut);
                            }
                            strRet = (exitCode == 0) ? ">> Ok，成功执行！" : "* 退出代码为" + exitCode;
                            #endregion
                            break;

                        case FileAction.schtasks:
                            string[] schArgs = fileData.SplitString(" ", "\"", true);
                            if (schArgs.Length != 3 && schArgs.Length != 1)
                            {
                                strRet = "参数错误：任务名称 任务路径 运行时间，例：update_clrsvrHost \"d:\\Server\\ClrHostService\\update.cmd\" 10:10。";
                            }
                            else
                            {
                                #region 单次运行计划任务
                                try
                                {
                                    if (schArgs.Length == 1)
                                    {
                                        exitCode = ConsoleUntil.RunCmd("schtasks", Directory.GetCurrentDirectory(),
                                            string.Format("/delete /TN {0} /F", schArgs),
                                            100, ref cmdOutPut);
                                    }
                                    else
                                    {
                                        exitCode = ConsoleUntil.RunCmd("schtasks", Directory.GetCurrentDirectory(),
                                            string.Format("/create /Z /tn {0} /tr {1} /RU SYSTEM /RP *  /sc once /st {2}", schArgs),
                                            100, ref cmdOutPut);
                                    }
                                }
                                catch (Exception execEx)
                                {
                                    cmdOutPut = execEx.ToString();
                                }
                                if (!string.IsNullOrEmpty(cmdOutPut))
                                {
                                    exchange.WriteLineWith(cmdOutPut);
                                }
                                strRet = (exitCode == 0) ? ">> Ok，成功执行！" : "* 退出代码为" + exitCode;
                                #endregion
                            }
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception ioEx)
                {
                    strRet = ioEx.Message;
                }
                exchange.WriteLineWith(strRet);
            }

            /// <summary>
            /// 文件阅读
            /// </summary>
            class FileReader
            {

                FileReader(string filePath, string arg, Encoding enc)
                {
                    FilePath = filePath;
                    Arguments = arg;
                    TxtEncoding = enc;
                }

                public static FileReader Create(string filePath, string arguments, Encoding enc)
                {
                    return new FileReader(filePath, arguments, enc);
                }

                /// <summary>
                /// 文件路径
                /// </summary>
                public string FilePath { get; set; }

                /// <summary>
                /// 阅读参数
                /// </summary>
                public string Arguments { get; set; }

                /// <summary>
                /// 文件编码
                /// </summary>
                public Encoding TxtEncoding { get; set; }

                /// <summary>
                /// 获取当前阅读的文本内容
                /// </summary>
                /// <returns></returns>
                public string GetContent()
                {
                    if (string.IsNullOrEmpty(Arguments))
                    {
                        return TxtEncoding.GetString(File.ReadAllBytes(FilePath));
                    }
                    else
                    {
                        /*
                        >? 在?之后
                        <? 在?之前
                        读取行 [L]0-[L]5
                        读取字节区间 0 - 200
                         */
                        StringBuilder rsb = new StringBuilder();
                        int startLineIdx = 1, endLineIdx = 1;

                        if (Arguments.IndexOf('-') != -1)
                        {
                            string[] idxArr = Arguments.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                            if (idxArr.Length > 0)
                            {
                                startLineIdx = Convert.ToInt32(idxArr[0]);
                                if (startLineIdx < 1)
                                    startLineIdx = 1;
                            }

                            if (idxArr.Length > 1)
                            {
                                endLineIdx = Convert.ToInt32(idxArr[1]);
                                if (endLineIdx < startLineIdx)
                                    endLineIdx = startLineIdx;
                            }
                        }

                        using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            int padLen = Convert.ToInt32(Math.Log10(endLineIdx) + 1.00);
                            using (StreamReader sr = new StreamReader(fs, TxtEncoding))
                            {
                                int currentLine = 0;
                                while (currentLine <= endLineIdx)
                                {
                                    currentLine++;
                                    if (currentLine >= startLineIdx)
                                    {
                                        if (currentLine == endLineIdx)
                                        {
                                            rsb.AppendFormat("[{0}]{1}", currentLine.ToString().PadLeft(padLen, '0'), sr.ReadLine());
                                        }
                                        else
                                        {
                                            rsb.AppendFormat("[{0}]{1}{2}", currentLine.ToString().PadLeft(padLen, '0'), sr.ReadLine(), Environment.NewLine);
                                        }
                                    }
                                    else
                                    {
                                        sr.ReadLine();
                                    }
                                }
                            }
                        }
                        return rsb.ToString();
                    }
                }
            }

        }

        /// <summary>
        /// 相关操作
        /// </summary>
        internal enum FileAction
        {
            read,
            readHex,
            list,
            exec,
            store,
            delete,
            schtasks
        }
    }
}
