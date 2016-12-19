using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CommonLib;
using EaseServer.Configuration;
using EaseServer.Interface;

namespace EaseServer.FtpConnection
{
    /// <summary>
    /// FTP连接主要处理入口类
    /// <para>http://www.w3.org/Protocols/rfc959/</para>
    /// </summary>
    public partial class FTPConnectionProcessor : RnRProcessorBase, IConnectionProcessor, IHELOFirstRnrProcessor
    {

        #region 扩展配置支持
        static FTPConnectionProcessor()
        {
            FillBindDictionary();
        }

        /// <summary>
        /// 绑定词典
        /// </summary>
        protected static readonly Dictionary<string, Func<FTPConnectionProcessor, string, string>> PropertyBindDict
            = new Dictionary<string, Func<FTPConnectionProcessor, string, string>>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// 填充绑定词典
        /// </summary>
        private static void FillBindDictionary()
        {
            if (PropertyBindDict.Count == 0)
            {
                PropertyBindDict.Add("UserSettingPath", (r, v) =>
                {
                    string old = ApplicationSettings.UserSettingPath;
                    string newPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, v);
                    if (File.Exists(newPath))
                    {
                        ApplicationSettings.UserSettingPath = newPath;
                        ApplicationSettings.RefreshFtpSetting();
                    }
                    return old;
                });
            }
        }

        /// <summary>
        /// 通过配置文件配置实例支持
        /// </summary>
        public void ConfigInstance(SessionConfig config)
        {
            string typeName = ProtocolIdentity;
            if (config != null && config.Settings.Length > 0)
            {
                string oldVal = string.Empty;
                for (int i = 0, j = config.Settings.Length; i < j; i++)
                {
                    if (PropertyBindDict.ContainsKey(config.Settings[i].Name))
                    {
                        oldVal = PropertyBindDict[config.Settings[i].Name](this, config.Settings[i].Value);
                        if (ServerConnection != null)
                        {
                            ServerConnection.ServerLog("* [{2}] 设置 {0} = {1}, 旧值:{3}", config.Settings[i].Name, config.Settings[i].Value, typeName, oldVal);
                        }
                    }
                }
            }

        }
        #endregion

        #region IConnectionProcess 成员
        /// <summary>
        /// 协议标识
        /// </summary>
        public string ProtocolIdentity { get { return "FTP"; } }

        ConnectionMode _mode = ConnectionMode.KeepAlive;
        /// <summary>
        /// 获取或设置连接模型
        /// </summary>
        /// <value></value>
        public ConnectionMode SocketMode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        /// <summary>
        /// 判断是否接收当前连接处理
        /// </summary>
        /// <param name="firstReadBytes">首次读到的字节序列</param>
        /// <returns>如果处理则为true,否则为false。</returns>
        public bool AcceptConnection(byte[] firstReadBytes) { return false; }

        /// <summary>
        /// 获取或设置当前的服务连接对象
        /// </summary>
        /// <value></value>
        public IServerConnection ServerConnection { get; set; }

        string GetExactPath(string Path)
        {
            if (Path == null) Path = "";
            string dir = Path.Replace("/", "\\");
            if (!Path.StartsWith("/"))
                dir = ConnectedUser.CurrentWorkingDirectory.TrimEnd('\\') + dir.ForceStartWith("\\");

            ArrayList pathParts = new ArrayList();
            dir = dir.Replace("\\\\", "\\");
            string[] p = dir.Split('\\');
            pathParts.AddRange(p);

            for (int i = 0; i < pathParts.Count; i++)
            {
                if (pathParts[i].ToString() == "..")
                {
                    if (i > 0)
                    {
                        pathParts.RemoveAt(i - 1);
                        i--;
                    }
                    pathParts.RemoveAt(i);
                    i--;
                }
            }
            string result = dir.Replace("\\\\", "\\");
            return result;
        }

        FTPUser ConnectedUser = new FTPUser();

        /// <summary>
        /// 对当前连接的处理
        /// </summary>
        public void ProcessRequest()
        {
            if (!ServerConnection.IsFirstAccess) ServerConnection.MonitorDump();

            bool connected = true;
            Exception lastExp = null;
            try
            {
                string CommandText = Encoding.Default.GetString(sessionBuffer.ToArray());
                if (string.IsNullOrEmpty(CommandText))
                {
                    lastExp = new InvalidDataException("未能接收到任何指令或指令数据格式错误！");
                }
                else
                {
                    //ServerConnection.ServerLog("*[FTP]{0} {1}", CommandText.Length, CommandText);
                    string CmdArguments = null, Command = null;
                    int End = 0;
                    if ((End = CommandText.IndexOf(' ')) == -1)
                    {
                        End = (CommandText = CommandText.Trim()).Length;
                    }
                    else
                    {
                        CmdArguments = CommandText.Substring(End).TrimStart(' ');
                    }
                    Command = CommandText.Substring(0, End).ToUpper();

                    #region FTP命令运行
                    if (CmdArguments != null && CmdArguments.EndsWith("\r\n"))
                        CmdArguments = CmdArguments.Substring(0, CmdArguments.Length - 2);

                    bool CommandExecued = false;
                    #region 未认证状态
                    if (!ConnectedUser.IsAuthenticated)
                    {
                        switch (Command)
                        {
                            case "USER":
                                if (CmdArguments != null && CmdArguments.Length > 0)
                                {
                                    SendMessage("331 User name okay, need password.");
                                    ConnectedUser = new FTPUser(CmdArguments.ToUpper(), ServerConnection);
                                }
                                CommandExecued = true;
                                break;

                            case "PASS":
                                if (ConnectedUser.UserName == "")
                                {
                                    SendMessage("503 Invalid User Name");
                                    return;
                                }
                                if (ConnectedUser.Authenticate(CmdArguments))
                                {
                                    SendMessage("230 Authentication Successful");
                                }
                                else
                                {
                                    SendMessage("530 Authentication Failed!");
                                }
                                CommandExecued = true;
                                break;
                        }
                    }
                    #endregion

                    #region 已认证命令
                    if (!CommandExecued && ConnectedUser.IsAuthenticated)
                    {
                        switch (Command)
                        {
                            case "CWD":
                                string dir = GetExactPath(CmdArguments);
                                if (ConnectedUser.ChangeDirectory(dir))
                                {
                                    SendMessage("250 CWD command successful.");
                                }
                                else
                                {
                                    SendMessage("550 System can't find directory '" + dir + "'.");
                                }
                                break;

                            case "CDUP": CDUP(CmdArguments); break;
                            case "PORT": PORT(CmdArguments); break;
                            case "PASV": PASV(CmdArguments); break;
                            case "TYPE": TYPE(CmdArguments); break;
                            case "RETR": RETR(CmdArguments); break;
                            case "STOR": STOR(CmdArguments); break;
                            case "APPE": APPE(CmdArguments); break;
                            case "RNFR": RNFR(CmdArguments); break;
                            case "RNTO": RNTO(CmdArguments); break;
                            case "DELE": DELE(CmdArguments); break;
                            case "RMD": RMD(CmdArguments); break;
                            case "MKD": MKD(CmdArguments); break;
                            case "PWD":
                                SendMessage("257 \"" + ConnectedUser.CurrentWorkingDirectory.Replace('\\', '/') + "\"");
                                break;

                            case "LIST": LIST(CmdArguments); break;
                            case "NLST": NLST(CmdArguments); break;
                            case "SYST": SendMessage("215 Windows_NT"); break;
                            case "NOOP": SendMessage("200 OK"); break;
                            case "QUIT":
                                WriteErrorAndClose(221, "FTP server signing off");
                                connected = false;
                                break;

                            case "FEAT":
                                SendMessage("500 This functionality is currently Unavailable.");
                                break;

                            default:
                                SendMessage("500 Unknown Command.");
                                break;

                            //	case "STAT":
                            //		break;

                            //	case "HELP":
                            //		break;

                            //	case "REIN":
                            //		break;

                            //	case "STOU":
                            //		break;

                            //	case "REST":
                            //		break;

                            //	case "ABOR":
                            //		break;
                        }
                        CommandExecued = true;
                    }
                    #endregion

                    if (!CommandExecued)
                    {
                        if (Command.Equals("QUIT", StringComparison.InvariantCultureIgnoreCase))
                        {
                            WriteErrorAndClose(221, "FTP server signing off");
                            connected = false;
                        }
                        else
                        {
                            ServerConnection.ExchangeStream.WriteLineWith("530 Access Denied! Authenticate first");
                            ServerConnection.Close();
                        }
                    }

                    #endregion
                }
            }
            catch (Exception error)
            {
                ServerConnection.ServerLog("* Error处理FTP操作出现异常{0}", error.ToString());
                lastExp = error;
            }

            if (connected && lastExp != null)
            {
                WriteErrorAndClose(530, "server exception fired! (" + lastExp.Message + ")");
            }
        }

        void SendMessage(string msg)
        {
            if (ServerConnection.ExchangeStream != null)
            {
                ServerConnection.ExchangeStream.WriteWith(msg + "\r\n");
            }
        }

        void SendMessage(Exception exception)
        {
            SendMessage("550 " + exception.Message + ".");
        }

        /// <summary>
        /// 输出错误码，并关闭连接。
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="message">The message.</param>
        public void WriteErrorAndClose(int statusCode, string message)
        {
            SendMessage(statusCode + " " + message);
            ServerConnection.Close();
        }
        #endregion

        #region IRnRProcesor 成员
        /// <summary>
        /// 命令分隔字节
        /// </summary>
        byte[] CMD_SPLIT_BYTES = new byte[] { 0x0D, 0x0A };

        /// <summary>
        /// 判断是否已完成请求数据的发送
        /// </summary>
        /// <returns></returns>
        public override bool HasFinishedRequest()
        {
            byte[] sBytes = sessionBuffer.ToArray();
            return sBytes.BytesEndWith(CMD_SPLIT_BYTES);
        }

        /// <summary>
        /// 开始输出应答结果
        /// </summary>
        public override void ProcessResponse()
        {
            ProcessRequest();
        }

        #endregion

        #region IHelloResponse 成员

        /// <summary>
        /// 首次连接消息
        /// </summary>
        public void SayHello()
        {
            if (ServerConnection != null)
            {
                ServerConnection.KeepAlive = true;
                ServerConnection.KeepAliveSeconds = 120;
                ServerConnection.ExchangeStream.WriteLineWith("200 "
                    + ServerConnection.GetServerAPI().GetServerName() + " v" + ServerConnection.GetServerAPI().GetServerVersion()
                    + " FTP Ready");

                ClientSocket = ServerConnection.GetClientSocket();
            }
        }

        #endregion
    }

}
