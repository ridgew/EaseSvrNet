using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using CommonLib;
using EaseServer.Configuration;
using EaseServer.Interface;
using Gwsoft.DataSpec;
using Gwsoft.EaseMode;
using EaseServer.Performance;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 易致协议连接处理实现
    /// </summary>
    public class ESPConnectionProcess : RnRProcessorBase, IConnectionProcessor
    {

        #region 扩展配置支持
        static ESPConnectionProcess()
        {
            FillBindDictionary();
        }

        /// <summary>
        /// 绑定词典
        /// </summary>
        protected static readonly Dictionary<string, Func<ESPConnectionProcess, string, string>> PropertyBindDict
            = new Dictionary<string, Func<ESPConnectionProcess, string, string>>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// 填充绑定词典
        /// </summary>
        private static void FillBindDictionary()
        {
            if (PropertyBindDict.Count == 0)
            {
                PropertyBindDict.Add("bufferSize", (r, v) =>
                {
                    string old = r.BufferSize.ToString();
                    r.BufferSize = Convert.ToInt32(v);
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
                        ServerConnection.ServerDebug("* [{2}] 设置 {0} = {1}, 旧值:{3}", config.Settings[i].Name, config.Settings[i].Value, typeName, oldVal);
                    }
                }
            }

        }
        #endregion



        #region IConnectionProcess 成员
        /// <summary>
        /// 协议标识
        /// </summary>
        public string ProtocolIdentity { get { return "EASE"; } }

        /// <summary>
        /// 获取或设置连接模型
        /// </summary>
        /// <value></value>
        public ConnectionMode SocketMode { get; set; }

        /// <summary>
        /// 判断是否接收当前连接处理
        /// </summary>
        /// <param name="firstReadBytes">首次读到的字节序列</param>
        /// <returns>如果处理则为true,否则为false。</returns>
        public bool AcceptConnection(byte[] firstReadBytes)
        {
            bool blRet = false;
            if (firstReadBytes != null && firstReadBytes.Length >= 10)
            {
                return ((firstReadBytes[0] == 0x03 && firstReadBytes[1] == 0xF2)  //1010
                    || (firstReadBytes[0] == 0x03 && firstReadBytes[1] == 0xFC)   //1020
                    || (firstReadBytes[0] == 0x04 && firstReadBytes[1] == 0x60)   //1120
                    || (firstReadBytes[0] == 0xFF && firstReadBytes[1] == 0xFF)); // -1
            }
            return blRet;
        }

        /// <summary>
        /// 获取或设置当前的服务连接对象
        /// </summary>
        /// <value></value>
        public IServerConnection ServerConnection { get; set; }

        /// <summary>
        /// 从包含后续字节长度计算总长度
        /// </summary>
        internal static Func<byte[], long> CalculateTotalSize = bt =>
        {
            byte[] _4B = new byte[4];
            if (bt.Length >= 10)
            {
                Buffer.BlockCopy(bt, 6, _4B, 0, _4B.Length);
                _4B = SpecUtil.ReverseBytes(_4B);
            }
            return BitConverter.ToInt32(_4B, 0) + 10L;
        };

        /// <summary>
        /// 对当前连接的处理
        /// </summary>
        public void ProcessRequest()
        {
            ServerConnection.BeginCounter(PerformancePoint.ParseData);
            #region 解析请求数据阶段
            Exception lastExp = null;
            bool hasSendClientBytes = false;
            //进入时间
            DateTime enterDateTime = DateTime.Now;
            string defaultStatusCode = "1010:200";
            string defaultMessage = "OK";
            long receivedByteLen = 0;
            bool isSms = false;

            IFormatter espFormat = new ESPDataFormatter(typeof(NetworkSwitchRequest));
            if (ServerConnection.ExchangeStream is SocketMonitorStream)
            {
                SocketMonitorStream sms = (SocketMonitorStream)ServerConnection.ExchangeStream;
                sms.ConnectionBufferStream = sessionBuffer;
                sms.Position = 0L;
                isSms = true;
            }

            NetworkSwitchRequest request = null;
            try
            {
                if (isSms) receivedByteLen = requestSession.Position;
                request = espFormat.Deserialize(ServerConnection.ExchangeStream) as NetworkSwitchRequest;
                if (isSms && ServerConnection.ExchangeStream != null)
                {
                    receivedByteLen = ServerConnection.ExchangeStream.Position;
                    ServerConnection.ExchangeStream.Position = 0L;
                }
            }
            catch (Exception reqEx)
            {
                lastExp = reqEx;
                defaultStatusCode = "-1:-1";
                defaultMessage = "无效协议数据请求：" + reqEx.Message;
                if (isSms && ServerConnection.ExchangeStream != null)
                    receivedByteLen = ServerConnection.ExchangeStream.Position;
                //if (!(reqEx is BadSpecDataException))
                //{
                //    ServerConnection.ServerError("{0}", reqEx);
                //}
            }
            #endregion

            if (request == null)
            {
                ServerConnection.EndCounter(PerformancePoint.ParseData);
                ServerConnection.BeginCounter(PerformancePoint.RecordLog);
                CommonLib.ExtensionUtil.CatchAll(() =>
                {
                    LOG_PV PV = new LOG_PV
                    {
                        SOFTWARE_ID = -1,
                        SERVICE_ID = -1,
                        LocalEndpoint = ServerConnection.LocalEP,
                        RemoteEndpoint = ServerConnection.RemoteEP,
                        CacheRate = "无缓存",
                        Protocol = "N/A",
                        VISIT_TIME = enterDateTime.ToString("yyyyMMddHHmmss"),
                        KEY_URL = "",
                        REAL_URL = "",
                        HTML_TIME = -1,
                        PARSE_TIME = (decimal)(DateTime.Now - enterDateTime).TotalMilliseconds,
                        ReceiveByteLength = receivedByteLen.ToString(),
                        SendByteLength = "0",
                        StatusCode = defaultStatusCode,
                        Message = defaultMessage
                    };
                    Gwsoft.SharpOrm.OrmHelper.Insert(PV);
                });
                ServerConnection.EndCounter(PerformancePoint.RecordLog);
            }
            else
            {
                ServerConnection.BeginCounter(PerformancePoint.ParseProtocol);
                if (request.Context == null)
                {
                    ESPContext espContext = new ESPContext();
                    request.Context = espContext;
                }
                request.Context.SetItem("REMOTE_ADDR", ServerConnection.RemoteIP);
                request.Context.SetItem(DataProxy.PROXYPROCESS_REQUEST_FIRSTKEY, DateTime.Now);

                NetworkSwitchResponse result = null;
                byte[] swRespBytes = new byte[0];

                using (DataExchange ex = new DataExchange(ServerConnection, request))
                {
                    try
                    {
                        #region 处理数据交互
                        if (ex.RecordCurrentService())
                        {
                            if ((ex.GetType().FullName + ".DebugRequest").AppSettings<bool>(false))
                            {
                                swRespBytes = request.GetNetworkBytes();
                                ServerConnection.ServerLog("<<< 网关请求字节序列[{0}], 长度{1}字节：\r\n{2}",
                                    (ex.SubRequest == null) ? "UnKnown SubRequest" : ex.SubRequest.GetType().FullName,
                                    swRespBytes.Length,
                                    SpecUtil.ByteArrayToHexString(swRespBytes));
                            }
                        }
                        result = ex.GetSwitchResponse();
                        defaultStatusCode = ex.GetExchangeStatusCode();
                        defaultMessage = ex.GetExchangeMessage();

                        ServerConnection.EndCounter(PerformancePoint.PerpareData);
                        ServerConnection.BeginCounter(PerformancePoint.PackageData);

                        swRespBytes = result.GetNetworkBytes();

                        ServerConnection.EndCounter(PerformancePoint.PackageData);

                        if (ex.RecordCurrentService())
                        {
                            if ((ex.GetType().FullName + ".DebugResponse").AppSettings<bool>(false))
                            {
                                //控制是否设置记录返回字节
                                ServerConnection.ServerLog(">>> 网关返回字节序列长度{1}字节：\r\n{0}", SpecUtil.ByteArrayToHexString(swRespBytes),
                                       swRespBytes.Length);
                            }
                        }

                        ServerConnection.BeginCounter(PerformancePoint.SendData);

                        #region 下发数据处理
                        try
                        {
                            using (MemoryStream ms = new MemoryStream(swRespBytes))
                            {
                                RnRStream resp = SizeableStream.CreateNew(ms, false, 10, CalculateTotalSize);
                                ServerConnection.SendResponseStream(resp, BufferSize);
                                resp.Close();
                                resp.Dispose();
                                ms.Close();
                            }
                            hasSendClientBytes = true;
                        }
                        catch (Exception respEx)
                        {
                            lastExp = respEx;
                            //ServerConnection.ServerError("{0}", respEx);
                            defaultStatusCode = string.Concat("*", defaultStatusCode);
                            defaultMessage = "下发数据出错：" + respEx.Message;
                            //Server.Log.ErrorFormat("* #协议应答处理出错，返回二进制数据为{0}", BitConverter.ToString(result.GetNetworkBytes()));
                        }
                        #endregion

                        ServerConnection.EndCounter(PerformancePoint.SendData);
                        ServerConnection.BeginCounter(PerformancePoint.RecordLog);

                        ex.RecordPageviewLog(enterDateTime, ex.GetExchangeTime(),
                            (decimal)(DateTime.Now - enterDateTime).TotalMilliseconds,
                            receivedByteLen.ToString(), swRespBytes.Length.ToString(),
                            defaultStatusCode, defaultMessage);

                        ServerConnection.EndCounter(PerformancePoint.RecordLog);

                        #endregion
                    }
                    catch (Exception exError)
                    {
                        lastExp = exError;
                        ex.RecordPageviewLog(enterDateTime, 0, (decimal)(DateTime.Now - enterDateTime).TotalMilliseconds,
                            receivedByteLen.ToString(), "0", "-1:-1", exError.Message);
                    }
                    finally
                    {
                        ex.Dispose();
                    }
                }
            }

            ServerConnection.EndCounter(PerformancePoint.WholeTime);
            if (ServerConnection.EnablePerformanceCounter)
            {
                ServerConnection.ServerDebug("{0}", ServerConnection.PerfCounter.GetXmlDocString(true));
            }
            ServerConnection.ResetCounter();

            if (lastExp != null)
                ServerConnection.ServerError("* 处理ESP出现异常:{0}", lastExp);

            if (request == null || lastExp != null || SocketMode == ConnectionMode.SingleCall)
            {
                #region 向客户端补偿发送错误处理结果(2011-4-1, Ridge)
                if (!hasSendClientBytes)
                {
                    NetworkSwitchResponse errorResp = null;
                    if (request != null)
                        errorResp = DataProxy.GenerateSwitchResponse(request, ResponseBase.ResponseBizErrorBytes, true);
                    else
                    {
                        errorResp = new NetworkSwitchResponse { ESP_SuccessFlag = EaseSuccessFlag.Error };
                    }

                    lastExp = null;
                    byte[] errorBytes = errorResp.GetNetworkBytes();
                    try
                    {
                        ServerConnection.ExchangeStream.Write(errorBytes, 0, errorBytes.Length);
                    }
                    catch (Exception finalEx)
                    {
                        lastExp = finalEx;
                    }
                    ServerConnection.ServerLog("*补偿发送处理处理结果{0}字节, 执行:{1}。", errorBytes.Length, (lastExp != null) ? lastExp.Message : "成功");
                }
                #endregion

                ServerConnection.Close();
                ServerConnection.Dispose();
            }
        }

        /// <summary>
        /// 输出错误码，并关闭连接。
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="message">The message.</param>
        public void WriteErrorAndClose(int statusCode, string message)
        {
            string errMsg = string.Format("{0}\r\n{1}", statusCode, message);
            byte[] errorBytes = Encoding.UTF8.GetBytes(errMsg);

            ServerConnection.ExchangeStream.Write(errorBytes, 0, errorBytes.Length);
            ServerConnection.Close();
            ServerConnection.Dispose();
        }

        #endregion

        #region IRnRProcesor 成员
        SizeableStream requestSession = null;
        /// <summary>
        /// 判断是否已完成请求数据的发送
        /// </summary>
        /// <returns></returns>
        public override bool HasFinishedRequest()
        {
            if (requestSession == null)
            {
                return false;
            }
            else
            {
                return requestSession.HasFinished();
            }
        }

        /// <summary>
        /// 心跳包数据长度
        /// </summary>
        const long HeartBeatByteLength = 10L;

        /// <summary>
        /// 继续写入请求片段数据
        /// </summary>
        /// <param name="requestSnippet">片段数据字节序列</param>
        public override void WriteReqeustBytes(byte[] requestSnippet)
        {
            //System.Diagnostics.Debug.WriteLine(string.Format("#{1} 写入{0}字节 @<{2}>",
            //    requestSnippet.Length,
            //    System.Threading.Thread.CurrentThread.ManagedThreadId,
            //    requestAccessCount));

            requestAccessCount++;
            if (requestAccessCount == 1)
            {
                if (requestSession == null) requestSession = new SizeableStream(sessionBuffer, true);
                if (requestSnippet.Length <= HeartBeatByteLength)
                {
                    //心跳包或自定义数据包
                    requestSession.SetLength(HeartBeatByteLength);
                }
                else
                {
                    requestSession.SetLength(CalculateTotalSize(requestSnippet));
                }
            }
            requestSession.Write(requestSnippet, 0, requestSnippet.Length);
            //System.Diagnostics.Debug.WriteLine(BitConverter.ToString(requestSnippet));
        }

        /// <summary>
        /// 当前会话处理已完成
        /// </summary>
        public override void ResetRequest()
        {
            base.ResetRequest();
            if (requestSession != null)
            {
                requestSession.Dispose();
                requestSession = null;
            }
        }

        /// <summary>
        /// 开始输出应答结果
        /// </summary>
        public override void ProcessResponse()
        {
            requestSession.SetPosition(0L);
            ProcessRequest();
        }

        #endregion
    }
}
