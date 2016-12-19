using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CommonLib;
using EaseServer.Configuration;
using EaseServer.Interface;

namespace EaseServer.SmtpConnection
{
    /// <summary>
    /// SMTP连接主要处理入口类
    /// </summary>
    public class SmtpConnectionProcess : RnRProcessorBase, IConnectionProcessor, IHELOFirstRnrProcessor
    {
        public event EventHandler<SmtpMessageReceivedEventArgs> MessageReceived;

        private const string replyOK = "250 OK";
        private const string replySyntax = "500 Syntax error, command unrecognized";
        private const string replyBadArgs = "501 Syntax error in parameters or arguments";

        private string domain;

        /// <summary>
        /// 初始化 <see cref="SmtpConnectionProcess"/> class.
        /// </summary>
        public SmtpConnectionProcess()
        {
            this.domain = Environment.MachineName;
        }

        #region IConnectionProcessor 成员

        /// <summary>
        /// 协议标识
        /// </summary>
        /// <value></value>
        public string ProtocolIdentity
        {
            get { return "SMTP"; }
        }

        ConnectionMode _mode = ConnectionMode.SelfHosting;
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
        /// 通过配置文件配置实例支持
        /// </summary>
        /// <param name="config"></param>
        public void ConfigInstance(SessionConfig config)
        {

        }

        IServerConnection _conn = null;
        /// <summary>
        /// 获取或设置当前的服务连接对象
        /// </summary>
        public IServerConnection ServerConnection
        {
            get { return _conn; }
            set
            {
                _conn = value;
                ExchangeStream = _conn.ExchangeStream;
            }
        }

        /// <summary>
        /// 对当前连接的处理
        /// </summary>
        public void ProcessRequest()
        {
            StreamReader reader = new StreamReader(ServerConnection.ExchangeStream, Encoding.Default, false);
            bool quit = false, notAccept = true;
            string command = Encoding.Default.GetString(sessionBuffer.ToArray());
            SmtpMailMessage message = new SmtpMailMessage();
            try
            {
                while (ClientSocket.Connected && !quit
                    && (notAccept || (command = reader.ReadLine()) != null))
                {
                    command = command.Trim();
                    notAccept = false;
                    //ServerConnection.ServerDebug("*[SMTP] {0}", command);
                    if (command.Length >= 4)
                    {
                        #region SMTP命令处理
                        switch (command.Substring(0, 4).ToUpper())
                        {
                            case "HELO":
                            case "EHLO":
                                SendMessage("250 {0}", IPAddress.Parse(((IPEndPoint)ClientSocket.RemoteEndPoint).Address.ToString()));
                                break;
                            //case "AUTH":
                            //    SendMessage("334 {0}", "");
                            //    break;
                            case "MAIL":
                                message = new SmtpMailMessage();
                                if (command.Length > 10 && command.Substring(4, 6).ToUpper() == " FROM:")
                                {
                                    message.From = command.Substring(10);
                                    SendMessage(replyOK);
                                }
                                else
                                {
                                    SendMessage(replyBadArgs);
                                }
                                break;
                            case "RCPT":
                                if (command.Length > 8 && command.Substring(4, 4).ToUpper() == " TO:")
                                {
                                    message.AddRecipient(command.Substring(8));
                                    SendMessage(replyOK);
                                }
                                else
                                {
                                    SendMessage(replyBadArgs);
                                }
                                break;
                            case "DATA":
                                SendMessage("354 Start mail input; end with <CRLF>.<CRLF>");
                                string line;
                                while ((line = reader.ReadLine()) != null && line != ".")
                                {
                                    if (line.Length > 0 && line[0] == '.')
                                    {
                                        line = line.Substring(1);
                                    }
                                    message.AddMessageLine(line);
                                }
                                OnMessageReceived(message);
                                SendMessage(replyOK);
                                break;
                            case "RSET":
                                message = new SmtpMailMessage();
                                SendMessage(replyOK);
                                break;
                            case "VRFY":
                            // To support RFC 5321 and since this is a mock, all addresses are valid 
                            case "NOOP":
                                SendMessage(replyOK);
                                break;
                            case "QUIT":
                                SendMessage("221 {0} Service closing transmission channel", domain);
                                quit = true;
                                break;
                            default:
                                // We only implement the minimum command set
                                SendMessage(replySyntax);
                                break;
                        }

                        #endregion
                    }
                    else
                    {
                        // Reply to garbage with
                        SendMessage(replySyntax);
                    }
                }
            }
            catch (Exception error)
            {
                ServerConnection.ServerError("* Error处理SMTP操作出现异常{0}", error.ToString());
            }
            ServerConnection.Close();
        }

        public void OnMessageReceived(SmtpMailMessage message)
        {
            //DEBUG
            //message.GetXmlDoc(true).Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.xml"));
            EventHandler<SmtpMessageReceivedEventArgs> handler = MessageReceived;
            if (handler != null)
            {
                handler(this, new SmtpMessageReceivedEventArgs(message));
            }
        }

        void SendMessage(string format, params object[] args)
        {
            SendMessage(string.Format(format, args));
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


        #region IHELOFirstRnrProcessor 成员
        Socket ClientSocket;
        /// <summary>
        /// 首次连接消息
        /// </summary>
        public void SayHello()
        {
            if (ServerConnection != null && ServerConnection.ExchangeStream != null)
            {
                ServerConnection.KeepAlive = true;
                ServerConnection.KeepAliveSeconds = 120;
                ServerConnection.ExchangeStream.WriteLineWith("220 "
                    + ServerConnection.GetServerAPI().GetServerName() + " v" + ServerConnection.GetServerAPI().GetServerVersion()
                    + " Smtp Service Ready");

                ClientSocket = ServerConnection.GetClientSocket();
            }
        }

        #endregion

        #region IRnRProcessor 成员
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
    }

    /// <summary>
    /// 
    /// </summary>
    public class SmtpMessageReceivedEventArgs : EventArgs
    {
        public SmtpMailMessage Message { get; set; }

        public SmtpMessageReceivedEventArgs(SmtpMailMessage message)
        {
            this.Message = message;
        }
    }

}
