using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CommonLib;
using System.Text.RegularExpressions;
using System.Reflection;
using EaseServer.Interface;

namespace EaseServer.ConsoleConnection
{
    public partial class FreeStyleConnection
    {

        internal class SesssionCommandParse
        {
            public SesssionCommandParse(Stream networkStream, string cmdStr)
            {
                exchangeStream = networkStream;
                commandSendRaw = cmdStr;
                RefreshCommand(cmdStr);
            }

            Stream exchangeStream = null;
            string commandSendRaw = "";
            string[] arguments = new string[0];

            public const string supportCommand = ",help,exit,list,session,calc,sqlserver,fileio,keepalive,setbreak,invoke,now,";

            /// <summary>
            /// 命令前置符号
            /// </summary>
            string cmdprefixString = "";

            string _mainCmd = null;
            /// <summary>
            /// 获取或设置主命令
            /// </summary>
            public string MainCommand
            {
                get
                {
                    return _mainCmd;
                }
                set
                {
                    if (value != null && value.StartsWith("*"))
                    {
                        cmdprefixString = "*";
                        _mainCmd = value.Substring(1);
                    }
                    else
                    {
                        _mainCmd = value;
                    }
                }
            }

            /// <summary>
            /// 判断是否是特殊命令
            /// </summary>
            /// <returns></returns>
            public bool IsSpecailCommand()
            {
                return cmdprefixString == "*";
            }

            /// <summary>
            /// 输出命令帮助信息
            /// </summary>
            internal static void ShowHelp(Stream targetStream)
            {
                string helpText = @" list: 列出所有连接客户端信息。格式：[开始索引|remove] [分页大小|终端匹配模式];
 exit: 退出管理
 now: 显示服务器时刻
 calc: 网络序列转换，格式：[数据，如：short:1010] [附加参数:utf-8|目标数据类型|false(不反转字节序列)]
 invoke: 调用CLR当前应用程序域内静态函数。格式：""[M:]CLR类型全称::静态方法名称, 程序集文件名称"" [""初始化参数列表""] ""调用参数""
 sqlserver: 执行SQL数据库操作, bin:[内置键值]|""* -> 所有连接配置, $ -> 当前连接配置或其他Sql连接字符串"" ""sql语句""
 fileio: 文件系统操作，下列从属命令：""文件路径"" [附加参数]
          read -> 读取文件，附加参数编码en:编码名称;
          readHex -> 读取16进制字符串
          list -> 列出目录下文件
          exec -> 执行文件内命令，字符形式的参数传递，以空格分隔多个参数;
          store -> 存储更新文件内容，附加参数文件数据，其中0x开始表示为二进制写入;
          schtasks -> 单次运行的系统计划任务，例：fileio schtasks ""update_clrsvrHost """"d:\Server\ClrHostService\restart.cmd"""" 14:45""
          delete -> 删除文件/目录
 setbreak: 设置命令终止符号，默认为CRLF，0x0D0A。
 keepalive: [off] 保持[关闭]长连接
";
                foreach (string lineStr in helpText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    targetStream.WriteLineWith(lineStr);
                }
                targetStream.WriteWith(">>");
            }


            public bool RememberCommand()
            {
                return true;
            }

            public FreeStyleConnection ConnectContext { get; set; }

            public void ClearArgument()
            {
                arguments = new string[0];
            }

            void RefreshCommand(string cmdStr)
            {
                if (cmdStr.StartsWith("keepalive", StringComparison.InvariantCultureIgnoreCase))
                {
                    MainCommand = "KeepAlive";
                    if (ConnectContext != null)
                    {
                        if (cmdStr.EndsWith("off", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ConnectContext.ServerConnection.KeepAlive = false;
                        }
                        else
                        {
                            ConnectContext.ServerConnection.KeepAlive = true;
                        }
                    }
                    return;
                }

                int idx = cmdStr.IndexOf(' ');
                if (idx != -1 && idx < cmdStr.Length)
                {
                    MainCommand = cmdStr.Substring(0, idx);
                    //arguments = commandSendRaw.Substring(idx).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    arguments = cmdStr.Substring(idx + 1).SplitString(" ", "\"", true);
                }
                else
                {
                    if (cmdStr.StartsWith(">"))
                    {
                        arguments = cmdStr.Substring(1).SplitString(" ", "\"", true);
                    }
                    else
                    {
                        arguments = new string[0];
                        if (string.IsNullOrEmpty(MainCommand)
                            || !MainCommand.Equals(cmdStr.Trim(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            MainCommand = cmdStr.Trim();
                        }
                    }
                }
            }

            public void SendCommand(string newCmdStr)
            {
                RefreshCommand(newCmdStr);
            }

            /// <summary>
            /// 判断是否是有效的主命令
            /// </summary>
            public bool IsValidCommand()
            {
                return supportCommand.IndexOf("," + MainCommand + ",", StringComparison.InvariantCultureIgnoreCase) != -1;
            }

            /// <summary>
            /// 操作入口,如果断开则返回为true。
            /// </summary>
            public bool ExecuteClose()
            {
                bool closeResult = false;
                IServerAPI svrAPI = ConnectContext.ServerConnection.GetServerAPI();

                switch (MainCommand.ToLower())
                {
                    case "help":
                        ShowHelp(exchangeStream);
                        break;

                    case "list":
                        bool doGeneralList = !(arguments != null && arguments.Length > 0);
                        int totalCount = 0, startIndex = 0, pageSize = 20;
                        bool doRemoveClient = false, pagingList = true;
                        string listPattern = "*";
                        if (!doGeneralList)
                        {
                            if (arguments[0].IsNumbers())
                            {
                                startIndex = Convert.ToInt32(arguments[0]);
                                if (arguments.Length > 1 && arguments[1].IsNumbers())
                                {
                                    pageSize = Convert.ToInt32(arguments[1]);
                                }
                            }
                            else
                            {
                                pagingList = false;
                                if (arguments[0].Equals("remove", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (arguments.Length > 1) listPattern = arguments[1];
                                    doRemoveClient = true;
                                }
                                else
                                {
                                    listPattern = arguments[0];
                                }
                            }
                        }

                        #region 列出已连接服务器的所有客户端
                        if (svrAPI != null)
                        {
                            int t1, t2, ma1, ma2, mi1, mi2;
                            System.Threading.ThreadPool.GetAvailableThreads(out t1, out t2);
                            System.Threading.ThreadPool.GetMaxThreads(out ma1, out ma2);
                            System.Threading.ThreadPool.GetMinThreads(out mi1, out mi2);
                            WriteToNetworkStream("* 监听服务器：端口{0}, 您的终端信息：{1}。\r\n* 线程池状态(工作:IOCP)：可用{2}:{3}, 最大{4}:{5}, 最小{6}:{7}。",
                                svrAPI.Port,
                                ConnectContext.ServerConnection.RemoteEP,
                                t1, t2, ma1, ma2, mi1, mi2);

                            if (pagingList)
                            {
                                WriteToNetworkStream("* 当前分页参数：StartIndex = {0}, PageSize = {1}", startIndex, pageSize);
                                IServerConnection[] allConnections = svrAPI.GetConnectionList(startIndex, pageSize, out totalCount);
                                ListAllConnections(allConnections, this.WriteToNetworkStream);
                                WriteToNetworkStream("* 总共连接数：{0}", totalCount);
                            }
                            else
                            {
                                WriteToNetworkStream("* 当前列表匹配模式：{0}", listPattern);
                                if (!doRemoveClient)
                                {
                                    svrAPI.ListClientStatus(listPattern, this.WriteToNetworkStream);
                                }
                                else
                                {
                                    svrAPI.DisconnectBatchClient(listPattern);
                                }
                            }
                        }
                        #endregion
                        break;

                    case "setbreak":
                        if (ConnectContext != null)
                        {
                            string strResult = ConnectContext.SetBreakBytes((arguments != null && arguments.Length > 0) ? arguments[0] : "") ? "成功" : "失败";
                            WriteToNetworkStream("命令终止符设置为：{0} [{1}]",
                                (arguments != null && arguments.Length > 0) ? arguments[0] : "0x0D0A",
                                strResult);
                        }
                        else
                        {
                            WriteToNetworkStream("* Error 服务器内部错误！");
                        }
                        break;

                    case "now":
                        WriteToNetworkStream("服务器时刻为：{0}, 当前服务已运行{1}。({2} v{3})",
                                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff"),
                                DateTime.Now.Subtract(svrAPI.StartDateTime),
                                svrAPI.GetServerName(), svrAPI.GetServerVersion());
                        break;

                    case "exit":
                        closeResult = true;
                        break;

                    case "calc":
                        //数据转换计算器
                        try
                        {
                            CalcWriter(arguments, exchangeStream);
                        }
                        catch (Exception calEx)
                        {
                            WriteToNetworkStream("* 数据转换错误:{0}", calEx.Message);
                        }
                        break;

                    case "sqlserver":
                        //执行SQL语句
                        SqlServerQuery.Create(arguments).ExecuteClose(exchangeStream);
                        break;

                    case "fileio":
                        //服务端文件处理
                        FileIoProcessor.Create(ConnectContext, arguments).ExecuteClose(exchangeStream);
                        break;

                    case "invoke":
                        ConsoleInvoke(arguments);
                        //exchangeStream.WriteLineWith("* 暂未实现");
                        break;

                    case "keepalive":
                        //连接保持
                        WriteToNetworkStream("* 已设置保持连接：{0}", ConnectContext.ServerConnection.KeepAlive);
                        break;

                    case "session":
                        closeResult = SupportSessionDebug.Create(ConnectContext.ServerConnection, arguments).ExecuteClose(exchangeStream);
                        //会话调试
                        break;

                    default:
                        break;
                }

                return closeResult;
            }

            void WriteToNetworkStream(string format, params object[] args)
            {
                exchangeStream.WriteLineWith(format, args);
            }

            void ListAllConnections(IServerConnection[] allConnections, ListClientWriter listHandler)
            {
                int i = 0;
                foreach (var item in allConnections)
                {
                    i++;
                    listHandler("{0}:{1} <{2}> Connect:{3:M-d HH:mm:ss,fff} Active:{4:M-d HH:mm:ss,fff} Mode:{5}",
                             i, item.Protocol,
                             item.RemoteEP,
                             item.ConnectedTime,
                             item.LastInteractive,
                             item.SocketMode);

                }
            }

            /*
            invoke "System.String::Format, mscorlib" "{0} 100"
            invoke "System.String::Format, mscorlib" "{0} 'Hello Word!'"
            invoke "M:EaseServer.FtpConnection.ApplicationSettings::EditUser, EaseServer.FtpConnection" "wangqj wangqj test 'd:\' 111111111 bool:true"
             */
            /// <summary>
            /// 控制台静态函数调用
            /// </summary>
            public void ConsoleInvoke(string[] args)
            {
                if (args == null || args.Length < 1)
                {
                    WriteToNetworkStream("Error:函数调用提交参数不正确！");
                }
                else
                {
                    string pattern = "::([^,]+)";
                    Match m = Regex.Match(args[0], pattern, RegexOptions.IgnoreCase);
                    if (!m.Success)
                    {
                        WriteToNetworkStream("Error:函数调用提交参数不正确,第一个参数格式为：CLR类型全称::静态方法名称, 程序集文件名称！");
                    }
                    else
                    {
                        string targetTypeFullName = args[0].Replace(m.Value, string.Empty);

                        bool memberInvoke = false;
                        if (targetTypeFullName.StartsWith("M:"))
                        {
                            targetTypeFullName = targetTypeFullName.Substring(2);
                            memberInvoke = true;
                        }

                        Type methodType = Type.GetType(targetTypeFullName, false);
                        if (methodType == null)
                        {
                            WriteToNetworkStream("Error:类型" + targetTypeFullName + "在当前应用程序域中不可见!");
                            return;
                        }

                        string initialArg = (args.Length > 2) ? args[2] : "";
                        string methodArg = (args.Length > 2) ? args[2] : ((args.Length > 1) ? args[1] : "");
                        object objRet = null;
                        try
                        {
                            if (memberInvoke)
                            {
                                objRet = ConsoleUntil.InvokeMethodOrGetProperty(methodType, m.Groups[1].Value,
                                    BuildArgumentObject(initialArg), BuildArgumentObject(methodArg));
                            }
                            else
                            {
                                MethodInfo targetMethod = methodType.GetMethod(m.Groups[1].Value,
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod);

                                if (targetMethod == null)
                                {
                                    WriteToNetworkStream("Error:类型" + targetTypeFullName + "中没有找到静态方法" + m.Groups[1].Value + "!");
                                    return;
                                }
                                objRet = targetMethod.Invoke(null, BuildArgumentObject(methodArg));
                            }
                        }
                        catch (Exception ivkExp)
                        {
                            if (ivkExp.InnerException != null) ivkExp = ivkExp.InnerException;
                            WriteToNetworkStream("调用异常：" + ivkExp.Message);
                            return;
                        }

                        #region 输出结果
                        if (objRet != null)
                        {
                            Type DataType = objRet.GetType();
                            if (!DataType.IsArray)
                            {
                                WriteToNetworkStream("调用结果为:[{0}].", objRet);
                            }
                            else
                            {
                                Type ElementType = DataType.GetElementType();
                                if (ElementType == typeof(byte))
                                {
                                    WriteToNetworkStream(((byte[])objRet).GetHexViewString());
                                }
                                else
                                {
                                    Array arrObj = (Array)objRet;
                                    int eLen = arrObj.Length;
                                    WriteToNetworkStream("* {1}[], 共计元素:{0}.", eLen, ElementType.FullName);
                                    for (int p = 0, n = eLen; p < n; p++)
                                    {
                                        exchangeStream.WriteLineWith("[{0}]", p, arrObj.GetValue(p));
                                    }
                                }
                            }
                        }
                        else
                        {
                            WriteToNetworkStream("调用结果 == null");
                        }
                        #endregion
                    }
                }
            }

            object[] BuildArgumentObject(string argsString)
            {
                if (string.IsNullOrEmpty(argsString))
                    return new object[0];
                else
                {
                    string[] args = argsString.SplitString(" ", "'", true);
                    object[] objTarget = new object[args.Length];
                    for (int i = 0, j = args.Length; i < j; i++)
                    {
                        int idx = args[i].IndexOf(':');
                        if (idx == -1)
                        {
                            objTarget[i] = args[i];
                        }
                        else
                        {
                            string typeStr = args[i].Substring(0, idx);
                            string typeDat = args[i].Substring(idx + 1);
                            switch (typeStr.ToLower())
                            {
                                case "byte":
                                    objTarget[i] = Convert.ToByte(typeDat);
                                    break;
                                case "char":
                                    objTarget[i] = Convert.ToChar(typeDat);
                                    break;
                                case "short":
                                    objTarget[i] = Convert.ToInt16(typeDat);
                                    break;
                                case "ushort":
                                    objTarget[i] = Convert.ToUInt16(typeDat);
                                    break;
                                case "int":
                                    objTarget[i] = Convert.ToInt32(typeDat);
                                    break;
                                case "uint":
                                    objTarget[i] = Convert.ToUInt32(typeDat);
                                    break;
                                case "long":
                                    objTarget[i] = Convert.ToInt32(typeDat);
                                    break;
                                case "ulong":
                                    objTarget[i] = Convert.ToUInt64(typeDat);
                                    break;
                                case "float":
                                    objTarget[i] = Convert.ToSingle(typeDat);
                                    break;
                                case "bool":
                                    objTarget[i] = Convert.ToBoolean(typeDat);
                                    break;
                                case "double":
                                    objTarget[i] = Convert.ToDouble(typeDat);
                                    break;
                                case "date":
                                    objTarget[i] = Convert.ToDateTime(typeDat);
                                    break;
                                case "string":
                                    objTarget[i] = typeDat;
                                    break;
                                case "byte[]":
                                    objTarget[i] = FreeStyleConnection.stringAsBytes(typeDat);
                                    break;
                                default:
                                    break;
                            }
                        }


                    }
                    return objTarget;
                }
            }

            void AppendIfNotHave(int targetLen, Stream writer, ref byte[] refBytes)
            {
                if (refBytes != null && refBytes.Length < targetLen)
                {
                    int padLen = targetLen - refBytes.Length;
                    byte[] targetBytes = new byte[targetLen];
                    Buffer.BlockCopy(refBytes, 0, targetBytes, 0, refBytes.Length);
                    writer.WriteLineWith("*> 添加了{0}字节数据..., HEX:{1}", padLen, targetBytes.ByteArrayToHexString());
                    refBytes = targetBytes;
                }
            }


            void CalcWriter(string[] args, Stream writer)
            {
                if (args.Length < 1)
                {
                    writer.WriteLineWith("请输入需要转换的参数值：格式示例ushort:1010。");
                    return;
                }

                object[] dats = BuildArgumentObject(args[0]);
                if (dats.Length < 1)
                {
                    writer.WriteLineWith("转换参数错误！");
                    return;
                }
                else
                {
                    Type dataType = dats[0].GetType();
                    string strResult = string.Empty;
                    byte[] rawBin = new byte[0];
                    if (dataType.Equals(typeof(byte[])))
                    {
                        #region 还原数据
                        if (args.Length < 2)
                        {
                            writer.WriteLineWith("请输入目标数据格式，支持格式byte,char,(u)short,(u)int,(u)long,float,bool,double,date,byte[]。");
                            return;
                        }
                        else
                        {
                            rawBin = (byte[])dats[0];
                            if (!(args.Length > 2)) rawBin = rawBin.ReverseBytes();
                            switch (args[1].ToLower())
                            {
                                case "byte":
                                    strResult = BitConverter.ToChar(rawBin, 0).ToString();
                                    break;
                                case "short":
                                    AppendIfNotHave(2, writer, ref rawBin);
                                    strResult = BitConverter.ToInt16(rawBin, 0).ToString();
                                    break;
                                case "ushort":
                                    AppendIfNotHave(2, writer, ref rawBin);
                                    strResult = BitConverter.ToUInt16(rawBin, 0).ToString();
                                    break;
                                case "int":
                                    AppendIfNotHave(4, writer, ref rawBin);
                                    strResult = BitConverter.ToInt32(rawBin, 0).ToString();
                                    break;
                                case "uint":
                                    AppendIfNotHave(4, writer, ref rawBin);
                                    strResult = BitConverter.ToUInt32(rawBin, 0).ToString();
                                    break;
                                case "long":
                                    AppendIfNotHave(8, writer, ref rawBin);
                                    strResult = BitConverter.ToInt64(rawBin, 0).ToString();
                                    break;
                                case "ulong":
                                    AppendIfNotHave(8, writer, ref rawBin);
                                    strResult = BitConverter.ToUInt64(rawBin, 0).ToString();
                                    break;
                                default:
                                    break;
                            }
                            if (strResult == string.Empty)
                            {
                                writer.WriteLineWith("* 数据格式暂不支持！");
                            }
                            else
                            {
                                writer.WriteLineWith(strResult);
                            }
                            writer.WriteLineWith("* 转换完成.");
                        }
                        #endregion
                    }
                    else
                    {
                        #region 转换为字节序列
                        TypeCode typeCode = Type.GetTypeCode(dataType);
                        switch (typeCode)
                        {
                            case TypeCode.Byte:
                                rawBin = BitConverter.GetBytes(Convert.ToByte(dats[0]));
                                break;
                            case TypeCode.SByte:
                                rawBin = BitConverter.GetBytes(Convert.ToSByte(dats[0]));
                                break;
                            case TypeCode.Char:
                                rawBin = BitConverter.GetBytes(Convert.ToChar(dats[0]));
                                break;
                            case TypeCode.Int16:
                                rawBin = BitConverter.GetBytes(Convert.ToInt16(dats[0]));
                                break;
                            case TypeCode.UInt16:
                                rawBin = BitConverter.GetBytes(Convert.ToUInt16(dats[0]));
                                break;
                            case TypeCode.Int32:
                                rawBin = BitConverter.GetBytes(Convert.ToInt32(dats[0]));
                                break;
                            case TypeCode.UInt32:
                                rawBin = BitConverter.GetBytes(Convert.ToUInt32(dats[0]));
                                break;
                            case TypeCode.Int64:
                                rawBin = BitConverter.GetBytes(Convert.ToInt64(dats[0]));
                                break;
                            case TypeCode.UInt64:
                                rawBin = BitConverter.GetBytes(Convert.ToUInt64(dats[0]));
                                break;
                            case TypeCode.Single:
                                rawBin = BitConverter.GetBytes(Convert.ToSingle(dats[0]));
                                break;
                            case TypeCode.Boolean:
                                rawBin = BitConverter.GetBytes(Convert.ToBoolean(dats[0]));
                                break;
                            case TypeCode.Double:
                                rawBin = BitConverter.GetBytes(Convert.ToDouble(dats[0]));
                                break;
                            case TypeCode.String:
                                string charset = "utf-8";
                                if (args.Length > 1 && !string.IsNullOrEmpty(args[1])) charset = args[1];
                                rawBin = Encoding.GetEncoding(charset).GetBytes((string)dats[0]);
                                break;
                            default:
                                break;
                        }
                        #endregion

                        if (rawBin != null && rawBin.Length > 0)
                        {
                            if (typeCode != TypeCode.String)
                            {
                                if (!(args.Length > 1)) rawBin = rawBin.ReverseBytes();
                            }
                            writer.WriteLineWith(rawBin.GetHexViewString());
                            writer.WriteLineWith("* 转换完成.");
                        }
                        else
                        {
                            writer.WriteLineWith("* 数据转换失败，数据格式暂不支持！");
                        }
                    }
                }
            }

        }

    }
}
