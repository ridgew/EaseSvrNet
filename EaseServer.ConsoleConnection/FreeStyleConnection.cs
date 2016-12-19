using System;
using System.IO;
using System.Text;
using CommonLib;
using EaseServer.Configuration;
using EaseServer.Interface;
using System.Collections.Generic;

namespace EaseServer.ConsoleConnection
{
    /// <summary>
    /// 管理控制台连接，会话配置密钥sessionKey。
    /// </summary>
    public partial class FreeStyleConnection : IConnectionProcessor
    {
        #region 扩展配置支持
        static FreeStyleConnection()
        {
            FillBindDictionary();
        }

        /// <summary>
        /// 绑定词典
        /// </summary>
        protected static readonly Dictionary<string, Func<FreeStyleConnection, string, string>> PropertyBindDict = new Dictionary<string, Func<FreeStyleConnection, string, string>>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// 填充绑定词典
        /// </summary>
        static void FillBindDictionary()
        {
            if (PropertyBindDict.Count == 0)
            {
                PropertyBindDict.Add("sessionKey", (r, v) =>
                {
                    string old = Encoding.Default.GetString(r.sessionBytes);
                    if (v.StartsWith("0x"))
                    {
                        r.sessionBytes = (byte[])(new byte[] { 0x30, 0x78 }).Combine(stringAsBytes(v));
                    }
                    else
                    {
                        r.sessionBytes = Encoding.Default.GetBytes("0x" + v);
                    }
                    return old.StartsWith("0x") ? old.Substring(2) : old;
                });
            }
        }
        #endregion

        /// <summary>
        /// 最多错误命令允许的次数
        /// </summary>
        int maxErrorCommandNum = 5;
        int currentErrorCommmandCount = 0;

        #region IConnectionProcess 成员

        /// <summary>
        /// 协议标识
        /// </summary>
        /// <value></value>
        public string ProtocolIdentity
        {
            get { return "Console"; }
        }

        /// <summary>
        /// 获取或设置连接模型
        /// </summary>
        /// <value></value>
        public ConnectionMode SocketMode { get; set; }

        MemoryStream sessionBuffer = new MemoryStream();

        /// <summary>
        /// 判断是否接收当前连接处理
        /// </summary>
        /// <param name="firstReadBytes">首次读到的字节序列</param>
        /// <returns>如果处理则为true,否则为false。</returns>
        public bool AcceptConnection(byte[] firstReadBytes)
        {
            //0x开始
            return (firstReadBytes.Length <= 20 && firstReadBytes[0] == 0x30);
        }

        /// <summary>
        /// 通过配置文件配置实例支持
        /// </summary>
        /// <param name="config"></param>
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
                        ServerConnection.ServerDebug("* [{2}] 设置 {0} = {1}, 旧值:{3}", config.Settings[i].Name, config.Settings[i].Value, typeName, oldVal);
                    }
                }
            }
        }

        /// <summary>
        /// 获取或设置当前的服务连接对象
        /// </summary>
        /// <value></value>
        public IServerConnection ServerConnection { get; set; }

        byte[] sessionBytes = new byte[] { 0x30, 0x78, 0x65, 0x61, 0x73, 0x65 }; //0xease
        byte[] commandEndBytes = new byte[] { 0x0D, 0x0A };
        bool hasBeginSession = false;

        void clearSessionBuffer()
        {
            sessionBuffer.SetLength(0);
        }

        internal static byte[] stringAsBytes(string cmdArgs)
        {
            if (cmdArgs.StartsWith("0x"))
            {
                cmdArgs = cmdArgs.Substring(2);
                return cmdArgs.HexPatternStringToByteArray();
            }
            else
            {
                return Encoding.Default.GetBytes(cmdArgs);
            }
        }

        /// <summary>
        /// 设置当前命令结束标识
        /// </summary>
        public bool SetBreakBytes(string byteHexStr)
        {
            bool result = false;
            if (string.IsNullOrEmpty(byteHexStr))
            {
                commandEndBytes = new byte[] { 0x0D, 0x0A };
            }
            else
            {
                byte[] oldBytes = commandEndBytes;
                try
                {
                    commandEndBytes = stringAsBytes(byteHexStr);
                    result = true;
                }
                catch (Exception)
                {
                    commandEndBytes = oldBytes;
                }
            }
            return result;
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        public void Log(string format, params object[] args)
        {
            ServerConnection.ServerLog(format, args);
        }

        SesssionCommandParse pInstance;

        /// <summary>
        /// 对当前连接的处理
        /// </summary>
        public void ProcessRequest()
        {
            if (!ServerConnection.IsFirstAccess) ServerConnection.MonitorDump();

            byte[] buffer = ServerConnection.SocketBufferData;
            if (buffer.Length == 1 && (
                buffer[0] == 0x1B || buffer[0] == 0x08
                ))
            {
                if (buffer[0] == 0x1B)
                {
                    WriteErrorAndClose(200, ">>Bye!");
                }
                else if (buffer[0] == 0x08)
                {
                    long csLen = sessionBuffer.Length;
                    if (csLen > 0)
                    {
                        sessionBuffer.SetLength(csLen - 1);
                        ServerConnection.ExchangeStream.WriteWith("{0}{1}{2}",
                            Environment.NewLine, ">", Encoding.Default.GetString(sessionBuffer.ToArray()));

                        return;
                    }
                }
            }
            else
            {
                sessionBuffer.Write(buffer, 0, buffer.Length);

                long bufferLen = sessionBuffer.Length;
                byte[] total = sessionBuffer.ToArray();

                #region 验证控制台会话
                if (!hasBeginSession)
                {
                    //0xease
                    if (total.Length <= sessionBytes.Length)
                    {
                        if (!sessionBytes.BytesStartWith(total))
                        {
                            WriteErrorAndClose(500, "Invalid console credential!");
                            return;
                        }
                        else
                        {
                            if (total.Length == sessionBytes.Length)
                            {
                                hasBeginSession = true;
                                ServerConnection.ExchangeStream.WriteWith(":::欢迎进入服务状态控制台:::\r\n>");
                                clearSessionBuffer();
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (total.Length > sessionBytes.Length && total.BytesStartWith(sessionBytes))
                        {
                            hasBeginSession = true;
                            ServerConnection.ExchangeStream.WriteWith(":::欢迎进入服务状态控制台:::\r\n>");
                            clearSessionBuffer();

                        }
                        else
                        {
                            WriteErrorAndClose(500, "Invalid console credential!");
                        }
                        return;
                    }
                }
                #endregion

                //WriteLine(string.Format("\r\n输入{2}, 缓冲长度:{0},已进入会话:{1}. \r\n", bufferLen, hasBeginSession, buffer[0].ToString("X2")));

                #region 数据接收与处理
                if (hasBeginSession)
                {
                    if (total.BytesEndWith(commandEndBytes))
                    {
                        byte[] cmdBytes = new byte[total.Length - commandEndBytes.Length];
                        Buffer.BlockCopy(total, 0, cmdBytes, 0, cmdBytes.Length);
                        string cmdStr = Encoding.Default.GetString(cmdBytes);

                        if (pInstance == null)
                        {
                            pInstance = new SesssionCommandParse(ServerConnection.ExchangeStream, cmdStr);
                            pInstance.ConnectContext = this;
                        }
                        else
                        {
                            pInstance.SendCommand(cmdStr);
                        }

                        if (!pInstance.RememberCommand() || !pInstance.IsValidCommand())
                        {
                            currentErrorCommmandCount++;
                            pInstance.ClearArgument();
                            ServerConnection.ExchangeStream.WriteLineWith("* 无效操作命令，连续失误操作机会({0}/{1})！",
                                currentErrorCommmandCount,
                                maxErrorCommandNum);

                            //强制断开
                            if (currentErrorCommmandCount >= maxErrorCommandNum)
                            {
                                WriteErrorAndClose(501, "* 连续尝试失败，已被服务器强制断开。");
                                return;
                            }
                        }
                        else
                        {
                            currentErrorCommmandCount = 0;
                            if (pInstance.ExecuteClose())
                            {
                                WriteErrorAndClose(200, ">>bye!");
                            }
                        }
                        clearSessionBuffer();
                    }
                }
                #endregion
            }
        }

        void ListClientToConsole(string format, params object[] args)
        {
            ServerConnection.ExchangeStream.WriteLineWith(format, args);
        }

        /// <summary>
        /// 输出错误码，并关闭连接。
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="message">The message.</param>
        public void WriteErrorAndClose(int statusCode, string message)
        {
            ServerConnection.ExchangeStream.WriteLineWith(message);
            ServerConnection.Close();
            ServerConnection.Dispose();
        }

        #endregion

    }

}
