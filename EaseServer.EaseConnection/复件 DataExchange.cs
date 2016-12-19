using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using Gwsoft.Configuration;
using Gwsoft.DataSpec;
using Gwsoft.Ease.Proxy.Service;
using Gwsoft.EaseMode;
using Gwsoft.SharpOrm;
using EaseServer.Interface;
using System.IO;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 处理缓存和HTTP数据交互
    /// </summary>
    [AppSettingsOptional("EaseServer.EaseConnection.DataExchange.DebugRequest", false, Description = "记录请求数据")]
    [AppSettingsOptional("EaseServer.EaseConnection.DataExchange.DebugResponse", false, Description = "记录应答数据")]
    [AppSettingsOptional("EaseServer.EaseConnection.DataExchange.TimeoutSecond", 20.00, Description = "业务数据获取超时秒数")]
    public class DataExchange : IDisposable
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="DataExchange"/> class.
        /// </summary>
        /// <param name="conn">当前连接对象引用</param>
        /// <param name="validSwitchRequest">有效的网关请求对象</param>
        public DataExchange(IServerConnection conn, NetworkSwitchRequest validSwitchRequest)
        {
            currentConn = conn;

            SwitchRequest = validSwitchRequest;
            _subRequest = validSwitchRequest.GetSubRequest();
            if (_subRequest != null)
            {
                _subRequest.Context = validSwitchRequest.Context;
            }
        }

        IServerConnection currentConn = null;

        NetworkSwitchRequest _switchRequest = null;
        /// <summary>
        /// 网关请求对象
        /// </summary>
        public NetworkSwitchRequest SwitchRequest
        {
            get
            {
                return _switchRequest;
            }
            private set
            {
                _switchRequest = value;
            }
        }

        private RequestBase _subRequest = null;
        /// <summary>
        /// 接入网关的子级请求对象
        /// </summary>
        public RequestBase SubRequest
        {
            get { return _subRequest; }
        }

        //private ResponseBase _subResponse = null;
        //public ResponseBase SubResponse
        //{
        //    get { return _subResponse; }
        //}

        /// <summary>
        /// 服务索引页（特殊定义）
        /// </summary>
        public const string SERVICE_INDEX_URL = "http://0.htm";

        /// <summary>
        /// 是否使用本地地址
        /// </summary>
        private const bool Use_LAN_Address = false;

        private string _clientRequestUrl = string.Empty;
        /// <summary>
        /// 客户端请求的(原始)地址
        /// </summary>
        public string ClientRequestUrl
        {
            get { return _clientRequestUrl; }
        }

        private EaseUser _currentUser = null;
        /// <summary>
        /// 当前获取数据的用户信息
        /// </summary>
        public EaseUser CurrentUser
        {
            get { return _currentUser; }
        }

        private DbConnection _currentSharedConnection = null;

        private ServiceConfig sConfig = null;
        /// <summary>
        /// 当前业务服务请求
        /// </summary>
        public ServiceConfig CurrentService
        {
            get { return sConfig; }
        }

        private EaseCodeFilter _codeFilter = (s) =>
        {
            string strResult = s;//s.Replace("\r", "").Replace("\n", "");
            return System.Text.RegularExpressions.Regex.Replace(strResult, ">\\s+<", "><");
        };

        /// <summary>
        /// 数据EASE代码过滤设置
        /// </summary>
        public EaseCodeFilter CodeFilter
        {
            get { return _codeFilter; }
            set { _codeFilter = value; }
        }

        /// <summary>
        /// 缓存代理对象入口实例
        /// </summary>
        internal static IProxyCaching ipc = new DefaultProxyCaching();


        /// <summary>
        /// 缓存块出现异常
        /// </summary>
        internal static ProxyResponse ResponseExcecption = new ProxyResponse
        {
            EaseCode = "ProxyResponse occurred Exception!",
            Status = new ResponseStatus { ErrMessage = "业务数据获取出错(ProxyResponse)", StatusCode = 500 }
        };

        /// <summary>
        /// 超时运行特定结果的函数
        /// </summary>
        /// <typeparam name="TResult">函数处理结果</typeparam>
        /// <param name="func">函数运行委托</param>
        /// <param name="timeoutSeconds">超时秒数设置</param>
        /// <param name="isTimeout">当前运行是否超时</param>
        /// <returns></returns>
        public static TResult ExecuteWithTimeout<TResult>(Func<TResult> func, double timeoutSeconds, ref bool isTimeout)
        {
            TResult result = default(TResult);
            Exception lastException = null;

            Thread fThread = new Thread(() =>
            {
                try
                {
                    result = func();
                }
                catch (Exception exp)
                {
                    lastException = exp;
                }
            });

            fThread.Start();

            bool blnSucess = fThread.Join(TimeSpan.FromSeconds(timeoutSeconds));

            if (!blnSucess)
            {
                try
                {
                    fThread.Abort();
                }
                catch { }
                isTimeout = true;
            }
            else
            {
                isTimeout = false;
            }

            if (lastException != null) throw lastException;
            return result;
        }

        /// <summary>
        /// 忽略异常获取缓存的业务数据
        /// </summary>
        /// <param name="func">The func.</param>
        /// <param name="request">The request.</param>
        /// <param name="analyzer">The analyzer.</param>
        /// <returns></returns>
        ProxyResponse SafeExecuteFun(Func<ProxyRequest, TagAnalyzer, ProxyResponse> func, ProxyRequest request, TagAnalyzer analyzer)
        {
            ProxyResponse resp = null;

            bool isTimeout = false;
            DateTime startTime = DateTime.Now;
            try
            {
                //resp = func(request, analyzer);
                resp = ExecuteWithTimeout<ProxyResponse>(() => func(request, analyzer),
                    TimeoutSetting, ref isTimeout);
            }
            catch (Exception err)
            {
                currentConn.ServerError(string.Format("* #获取业务数据(GetProxyCache)失败, {0}秒内{1}", TimeoutSetting,
                    isTimeout ? "超时！" : "完成。"), err);
                resp = ResponseExcecption;
            }

            TimeSpan tspan = DateTime.Now - startTime;
            currentConn.ServerLog("* 获取业务数据(GetProxyCache)耗时{0}s{1}ms, {2}秒内{3}",
                tspan.TotalSeconds.ToString("0"),
                tspan.Milliseconds, TimeoutSetting,
                isTimeout ? "超时!" : "完成.");

            return resp;
        }

        static double TimeoutSetting = AppSettingsOptionalAttribute.SettingValueOrDefault<DataExchange, double>("EaseServer.EaseConnection.DataExchange.TimeoutSecond");

        /// <summary>
        /// 忽略异常获取缓存的业务数据
        /// </summary>
        /// <param name="func">The func.</param>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        byte[] SafeExecuteFun(Func<ProxyRequest, byte[]> func, ProxyRequest request)
        {
            byte[] retBytes = new byte[0];

            bool isTimeout = false;
            DateTime startTime = DateTime.Now;
            try
            {
                //retBytes = func(request);
                retBytes = ExecuteWithTimeout<byte[]>(() => func(request),
                    TimeoutSetting, ref isTimeout);
            }
            catch (Exception err)
            {
                currentConn.ServerError(string.Format("* #获取业务数据(GetProxyCacheItem)失败, {0}秒内{1}", TimeoutSetting,
                    isTimeout ? "超时！" : "完成。"), err);
                retBytes = ResponseBase.ResponseBizErrorBytes;
            }

            TimeSpan tspan = DateTime.Now - startTime;
            currentConn.ServerLog("* 获取业务数据(GetProxyCacheItem)耗时{0}s{1}ms, {2}秒内{3}",
                tspan.TotalSeconds.ToString("0"), tspan.Milliseconds, TimeoutSetting,
                isTimeout ? "超时!" : "完成.");

            return retBytes;

        }

        /// <summary>
        /// 获取网关返回的对象封装
        /// </summary>
        /// <returns></returns>
        public NetworkSwitchResponse GetSwitchResponse()
        {
            if (AppSettingsOptionalAttribute.SettingValueOrDefault<DataExchange, bool>("EaseServer.EaseConnection.DataExchange.DebugRequest"))
            {
                byte[] swRequestBytes = SwitchRequest.GetNetworkBytes();
                currentConn.ServerDebug("<<< 网关请求字节序列[{0}], 长度{1}字节：\r\n{2}",
                    (SubRequest == null) ? "UnKnown SubRequest" : SubRequest.GetType().FullName,
                    swRequestBytes.Length,
                    SpecUtil.ByteArrayToHexString(swRequestBytes));
            }

            if (SubRequest == null)
            {
                currentConn.ServerError("* #无法确定网关子级请求对象！");
                return DataProxy.GenerateSwitchResponse(SwitchRequest, new byte[0], true);
            }

            ProxyRequest pReq = _translateRequest(SubRequest);
            if (CurrentUser == null || CurrentService == null)
            {
                currentConn.ServerError("* #无法确定用户或相关业务{0}！", SubRequest.ESP_Header.ESP_BusinessID);
                return DataProxy.GenerateSwitchResponse(SwitchRequest, new byte[0], true);
            }

            currentConn.ServerDebug("[业务编号:{2}] {0} => {1}", pReq.Method, pReq.GetRequestUrl(), CurrentService.SERVICE_ID);

            //设置资源地址根
            TagAnalyzer _tagAnalyzer = TagAnalyzer.GetTagAnalyzer(CurrentService.RES_URL_PREFIX);

            ResponseHeader header = new ResponseHeader();
            RequestHeader reqHeader = SubRequest.ESP_Header;

            HandlerException errorHanlder = (e) =>
            {
                return false;
            };

            bool hasBizError = false;
            ResponseBase bizResp = null;
            #region 组装对象
            try
            {
                switch (reqHeader.ESP_Protocol)
                {
                    case RequestType.PageV21:
                        bizResp = _getResponseAsPageV21(SafeExecuteFun(ipc.GetProxyCache, pReq, _tagAnalyzer), errorHanlder);
                        break;
                    case RequestType.Mixed:
                        bizResp = _getResponseAsMixed(SafeExecuteFun(ipc.GetProxyCache, pReq, _tagAnalyzer), errorHanlder);
                        break;
                    case RequestType.Page:
                        bizResp = _getResponseAsPage(SafeExecuteFun(ipc.GetProxyCache, pReq, _tagAnalyzer), errorHanlder);
                        break;
                    case RequestType.Resource:
                        bizResp = _getResponseAsResource(SafeExecuteFun(ipc.GetProxyCache, pReq, _tagAnalyzer), errorHanlder);
                        break;
                    case RequestType.Application:
                        bizResp = _getResponseAsApplication(SafeExecuteFun(ipc.GetProxyCacheItem, pReq), errorHanlder);
                        break;
                    case RequestType.UpdateCenter:
                        bizResp = new GatewayUpdateResponse();
                        break;
                    default:
                        //其他为应用下载
                        bizResp = _getResponseAsApplication(SafeExecuteFun(ipc.GetProxyCacheItem, pReq), errorHanlder);
                        break;
                }
                if (bizResp != null)
                {
                    bizResp.ESP_Code = StatusCode.Success;
                }
            }
            catch (InvalidBizResponseException bizExp)
            {
                bizResp = bizExp.ResponseDefault;
                bizResp.ESP_Code = StatusCode.Exception;
                hasBizError = true;
                currentConn.ServerError("* #获取业务数据失败(InvalidBizResponseException)！{0}", bizExp);
            }
            catch (Exception exp)
            {
                if (bizResp != null)
                {
                    bizResp.ESP_Code = StatusCode.Exception;
                }
                hasBizError = true;
                currentConn.ServerError("* #获取业务数据失败！{0}", exp);
            }
            #endregion

            #region 复制回传参数
            header.ESP_Protocol = reqHeader.ESP_Protocol;
            //分配会话标识
            header.ESP_SessionID = _getSessionFromHeader(reqHeader);
            header.ESP_SoftwareID = (int)CurrentUser.SOFTWARE_ID; //reqHeader.ESP_SoftwareID;
            header.ESP_SuccessFlag = (hasBizError) ? EaseSuccessFlag.Error : reqHeader.ESP_SuccessFlag;
            header.ESP_LeaveLength = -1; //默认后续长度为0
            #endregion

            byte[] retBytes = new byte[0];
            if (bizResp == null)
            {
                hasBizError = true;
                retBytes = ResponseBase.ResponseBizErrorBytes;
            }
            else
            {
                #region 业务数据提取与设置
                bizResp.ESP_Method = Gwsoft.EaseMode.CommandType.UnKnown;
                bizResp.ESP_Command = EaseString.Empty;
                bizResp.ESP_Message = EaseString.Empty;
                #endregion
                bizResp.ESP_Header = header;

                hasBizError = false;
                retBytes = bizResp.GetNetworkBytes();
            }

            NetworkSwitchResponse swResp = DataProxy.GenerateSwitchResponse(SwitchRequest, retBytes, hasBizError);

            decimal totalTime = 0;
            if (SwitchRequest.Context != null)
            {
                object getRequestTime = SwitchRequest.Context.GetItem(DataProxy.PROXYPROCESS_REQUEST_FIRSTKEY);
                if (getRequestTime != null)
                {
                    DateTime lastTime = (DateTime)getRequestTime;
                    totalTime = (decimal)((DateTime.Now - lastTime).TotalMilliseconds);
                }
            }

            #region PV日志记录
            CommonLib.ExtensionUtil.CatchAll(() =>
            {
                LOG_PV PV = new LOG_PV
                {
                    SOFTWARE_ID = CurrentUser.SOFTWARE_ID,
                    SERVICE_ID = CurrentService.SERVICE_ID,
                    VISIT_TIME = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    KEY_URL = ClientRequestUrl,
                    REAL_URL = pReq.GetRequestUrl(),
                    HTML_TIME = 0,
                    PARSE_TIME = totalTime
                };
                Gwsoft.SharpOrm.OrmHelper.Insert(PV, _currentSharedConnection);
            });
            #endregion

            return swResp;
        }

        /// <summary>
        /// 获取网关返回的二进制数据
        /// </summary>
        /// <returns></returns>
        public byte[] GetSwitchResponseBytes()
        {
            byte[] swRespBytes = GetSwitchResponse().GetNetworkBytes();

            //控制是否设置记录放回字节
            if (AppSettingsOptionalAttribute.SettingValueOrDefault<DataExchange, bool>("EaseServer.EaseConnection.DataExchange.DebugResponse"))
            {
                currentConn.ServerDebug(">>> 网关返回字节序列长度{1}字节：\r\n{0}", SpecUtil.ByteArrayToHexString(swRespBytes),
                    swRespBytes.Length);
            }

            return swRespBytes;
        }

        bool _isSuccess(int statusCode)
        {
            return (statusCode == 200);
        }

        /// <summary>
        /// 检测是否是有效的业务放回数据
        /// </summary>
        /// <param name="resp">数据代理返回对象</param>
        /// <param name="defaultResp">默认放回对象</param>
        /// <exception cref="InvalidBizResponseException">无效的业务数据异常</exception>
        void _checkInvalidResponse(ProxyResponse resp, ResponseBase defaultResp)
        {
            if (resp == null)
            {
                throw new InvalidBizResponseException("未能获取业务数据(ProxyResponse == null)，未能在预定时间(" + TimeoutSetting + "秒)内完成数据返回。", defaultResp);
            }

            if (resp.Status == null)
            {
                throw new InvalidBizResponseException("未能获取业务数据返回状态(ProxyResponse.Status == null)。", defaultResp);
            }

            if (!_isSuccess(resp.Status.StatusCode))
            {
                throw new InvalidBizResponseException("业务数据返回失败(" + resp.Status.StatusCode + ")，" + resp.Status.ErrMessage, defaultResp);
            }
        }

        /// <summary>
        /// 转换真实远程地址
        /// </summary>
        /// <param name="reqUrl">当前请求的地址</param>
        /// <param name="sConfig">当前请求业务配置</param>
        private string _fixedRemoteUrl(string reqUrl, ServiceConfig sConfig)
        {
            _clientRequestUrl = reqUrl;
            if (reqUrl.Equals(SERVICE_INDEX_URL))
            {
                return sConfig.SERVICE_INDEX_URL;
            }
            else
            {
                return string.Concat(sConfig.LINK_URL_PREFIX.TrimEnd('/') + "/", reqUrl.TrimStart('/'));
            }
        }

        string _getFixedIpAddressUrl(string rawUrlString)
        {
#if DEBUG
            string[] LAN_IP_HOSTS = new string[] { "192.168.10.84" };
            string[] WAN_IP_HOSTS = new string[] { "118.123.205.165" };

            if (!Use_LAN_Address)
            {
                for (int i = 0, j = LAN_IP_HOSTS.Length; i < j; i++)
                {
                    rawUrlString = rawUrlString.Replace(LAN_IP_HOSTS[i], WAN_IP_HOSTS[i]);
                }
            }
#endif
            return rawUrlString;
        }

        #region 绑定业务类型数据

        //[OK][DEBUG]
        private PageV21Response _getResponseAsPageV21(ProxyResponse resp, HandlerException exceptionHandler)
        {
            PageV21Response mResp = new PageV21Response();
            _checkInvalidResponse(resp, mResp);

            try
            {
                EmbedResourceDocument resDoc = new EmbedResourceDocument();
                resDoc.ESP_Document = _getDocumentFromResponse(resp);

                resDoc.ESP_Resources = _getResourceFromResponse(resp);
                resDoc.ESP_ResourceCount = (short)resDoc.ESP_Resources.Length;

                //属性设置
                mResp.ESP_EmbedResDocs = new EmbedResourceDocument[] { resDoc };
                mResp.ESP_PageDocCount = (short)mResp.ESP_EmbedResDocs.Length;
            }
            catch (Exception exp)
            {
                DataProxy.GenericExceptionHandler(exceptionHandler, exp);
            }
            return mResp;
        }


        private MixedResponse _getResponseAsMixed(ProxyResponse resp, HandlerException exceptionHandler)
        {
            MixedResponse mResp = new MixedResponse();
            _checkInvalidResponse(resp, mResp);

            try
            {
                mResp.ESP_Docs = new EaseDocument[] { _getDocumentFromResponse(resp) };
                mResp.ESP_PageDocCount = (short)mResp.ESP_Docs.Length;


                mResp.ESP_Resources = _getResourceFromResponse(resp);
                mResp.ESP_PageResCount = (short)mResp.ESP_Resources.Length;
            }
            catch (Exception exp)
            {
                DataProxy.GenericExceptionHandler(exceptionHandler, exp);
            }
            return mResp;
        }

        private PageResponse _getResponseAsPage(ProxyResponse resp, HandlerException exceptionHandler)
        {
            PageResponse mResp = new PageResponse();
            _checkInvalidResponse(resp, mResp);

            try
            {
                mResp.ESP_Docs = new EaseDocument[] { _getDocumentFromResponse(resp) };

                mResp.ESP_PageDocCount = (short)mResp.ESP_Docs.Length;
            }
            catch (Exception exp)
            {
                DataProxy.GenericExceptionHandler(exceptionHandler, exp);
            }
            return mResp;
        }

        ResourceResponse _getResponseAsResource(ProxyResponse resp, HandlerException exceptionHandler)
        {
            ResourceResponse mResp = new ResourceResponse();
            _checkInvalidResponse(resp, mResp);

            try
            {
                mResp.ESP_Resources = _getResourceFromResponse(resp);
                mResp.ESP_PageResCount = (short)mResp.ESP_Resources.Length;
            }
            catch (Exception exp)
            {
                DataProxy.GenericExceptionHandler(exceptionHandler, exp);
            }
            return mResp;
        }

        //[DEBUG]
        ApplicationResponse _getResponseAsApplication(byte[] respBytes, HandlerException exceptionHandler)
        {
            ApplicationResponse mResp = new ApplicationResponse();
            try
            {
                mResp.ESP_AppResponseData = respBytes;
                if (respBytes != null)
                {
                    mResp.ESP_AppResponseLength = respBytes.Length;
                }
                else
                {
                    mResp.ESP_AppResponseLength = 0;
                }
            }
            catch (Exception exp)
            {
                DataProxy.GenericExceptionHandler(exceptionHandler, exp);
            }
            return mResp;
        }

        /// <summary>
        /// 文档版本[TODO]
        /// </summary>
        EaseDocument _getDocumentFromResponse(ProxyResponse resp)
        {
            EaseString requstUrlStr = EaseString.Get(ClientRequestUrl);
            return new EaseDocument
            {
                ESP_Type = DocumentType.Render,
                ESP_Name = EaseString.Get(Path.GetFileName(ClientRequestUrl).TrimInvalidFilenameChars()),
                ESP_URL = requstUrlStr,
                ESP_Version = 0,
                ESP_Content = EaseString.Get(CodeFilter(resp.EaseCode))
            };
        }

        EaseResource[] _getResourceFromResponse(ProxyResponse resp)
        {
            List<EaseResource> resList = new List<EaseResource>();
            if (_isSuccess(resp.Status.StatusCode))
            {
                foreach (var item in resp.Resource)
                {
                    resList.Add(new EaseResource
                    {
                        ESP_Catelog = ResourceCatelog.Picture,
                        ESP_Data = item.Value,
                        ESP_Length = item.Value.Length,
                        ESP_Name = EaseString.Get(Path.GetFileName(item.Key).TrimInvalidFilenameChars()),
                        ESP_URL = EaseString.Get(item.Key)
                    });
                }
            }
            else
            {
                throw new DataExchangeException(resp.Status.ErrMessage);
            }
            return resList.ToArray();
        }
        #endregion

        /// <summary>
        /// 从当前请求中获取会话标识（没有则分配）
        /// </summary>
        /// <param name="header">The header.</param>
        /// <returns></returns>
        EaseString _getSessionFromHeader(RequestHeader header)
        {
            if (header.ESP_SessionID.ESP_Length < 1)
            {
                //3497:20100324112112

                string sid = string.Format("{0}:{1}", CurrentUser.SOFTWARE_ID,
                    DateTime.Now.ToString("yyyyMMddHHmmss"));

                if (header.Context != null)
                {
                    header.Context.SetItem("SessionID", sid);
                }
                return EaseString.Get(sid);
            }
            else
            {
                return header.ESP_SessionID;
            }
        }

        object userAssignLock = new object();

        /// <summary>
        /// 转换协议请求为业务请求（SID=0则申请新的编号）[TODO]
        /// </summary>
        [Gwsoft.Configuration.ImplementState(Gwsoft.Configuration.CompleteState.TODO, "1.0")]
        private ProxyRequest _translateRequest(RequestBase bizRequest)
        {
            if (bizRequest.ESP_Header.ESP_BusinessID == 0)
            {
                sConfig = null;
                return null;
            }

            #region 配置ServiceConfig = > CurrentService
            sConfig = new ServiceConfig { SERVICE_ID = (decimal)bizRequest.ESP_Header.ESP_BusinessID };
            _currentSharedConnection = OrmHelper.GetDbConnection(sConfig);
            _currentSharedConnection.Open();
            sConfig.DataBind(new string[0], _currentSharedConnection);

            sConfig.RES_URL_PREFIX = _getFixedIpAddressUrl(sConfig.RES_URL_PREFIX);
            #endregion


            string remoteIp = "0.0.0.0";
            if (bizRequest.Context != null)
            {
                object ctxObj = bizRequest["REMOTE_ADDR"];
                if (ctxObj != null) remoteIp = ctxObj.ToString();
            }

            #region 配置用户 CurrentUser 注册新用户
            EaseUser tRequestUser = new EaseUser
            {
                SOFTWARE_ID = bizRequest.ESP_Header.ESP_SoftwareID,
                SERVICE_ID = bizRequest.ESP_Header.ESP_BusinessID,
                DEVICE_ID = (long)bizRequest.ESP_Header.ESP_DID,
                UserAgent = bizRequest.ESP_Header.ESP_UserAgent.GetRawString(),
                CLIENT_VERSION = (long)bizRequest.ESP_Header.ESP_Version,
                IMEI = bizRequest.ESP_Header.ESP_IMEI.GetRawString(),
                IMSI = bizRequest.ESP_Header.ESP_IMEI.GetRawString(),
                MSID = ""
            };

            bizRequest["DataExchange.SharedDbConnection"] = _currentSharedConnection;
            bizRequest["DataExchange.UserAssignFormat"] = sConfig.SERVICE_UserAssignFormat;

            long currentUserId = BusinessUser.GetInstance(bizRequest.ESP_Header.ESP_BusinessID).GetCurrentUserID(bizRequest, tRequestUser);

            if (currentUserId < 1)
            {
                #region 注册新用户，分配用户ID。
                EaseUser newUser = new EaseUser
                {
                    SERVICE_ID = bizRequest.ESP_Header.ESP_BusinessID,
                    DEVICE_ID = (long)bizRequest.ESP_Header.ESP_DID,
                    CLIENT_VERSION = (long)bizRequest.ESP_Header.ESP_Version,
                    IMEI = bizRequest.ESP_Header.ESP_IMEI.GetRawString(),
                    IMSI = bizRequest.ESP_Header.ESP_IMEI.GetRawString(),
                    MSID = "",
                    USER_NAME = "",
                    USER_SEX = 0,
                    USER_AGE = 0,
                    USER_CARD_TYPE = 0,
                    USER_ID_CARD = "",
                    USER_ADDR = "",
                    RegionCode = "",
                    ProvinceId = 0,
                    REMOTE_IP = remoteIp,
                    UserAgent = bizRequest.ESP_Header.ESP_UserAgent.GetRawString(),
                    FIRST_VISIT_TIME = DateTime.Now.ToString("yyyyMMddHHmmss"), //20091208004611
                    RegionDateCreated = DateTime.Now
                };

                lock (userAssignLock)
                {
                    newUser.SOFTWARE_ID = Convert.ToInt64(Gwsoft.SharpOrm.OrmHelper.Insert(newUser,
                        _currentSharedConnection, true,
                        new string[] { "CLIENT_VERSION", "USER_SEX", "USER_AGE", "IMEI" })
                     );

                    tRequestUser = newUser;
                    bizRequest.ESP_Header.ESP_SoftwareID = (int)newUser.SOFTWARE_ID;
                    currentConn.ServerLog("* # 已分配新用户{0}, IMEI:{1}, DID:{2}！", newUser.SOFTWARE_ID, newUser.IMEI, newUser.DEVICE_ID);
                }
                #endregion
            }
            else
            {
                tRequestUser.SOFTWARE_ID = currentUserId;
                try
                {
                    tRequestUser = new EaseUser { SOFTWARE_ID = tRequestUser.SOFTWARE_ID };
                    OrmHelper.DataBind(tRequestUser, new string[0], _currentSharedConnection);
                }
                catch (NotExistException)
                {
                    currentConn.ServerError("* # 用户[{0}]在请求业务[{1}]的服务时没有找到且没有分配新的用户！",
                        tRequestUser.SOFTWARE_ID, bizRequest.ESP_Header.ESP_BusinessID);
                    _currentUser = null;
                    return null;
                }
            }

            _currentUser = tRequestUser;
            #endregion

            //sConfig.CLIENT_ROOT_URI = sConfig.LINK_URL_PREFIX 
            //    = sConfig.RES_URL_PREFIX = sConfig.SERVICE_HELP_URL 
            //    = sConfig.SERVICE_INDEX_URL = sConfig.SERVICE_REG_URL 
            //    = sConfig.SERVICE_URL;

            #region 分配会话标识 20100323
            EaseString SessionID = _getSessionFromHeader(bizRequest.ESP_Header);
            #endregion

            bool isBinaryPost = false;
            string remoteURL = string.Empty;
            string urlLink = SERVICE_INDEX_URL;

            #region 处理资源请求地址
            if (bizRequest is PageV21Request)
            {
                urlLink = (bizRequest as PageV21Request).ESP_Link.GetRawString();
                remoteURL = _fixedRemoteUrl(urlLink, sConfig);
            }
            else if (bizRequest is MixedRequest)
            {
                urlLink = (bizRequest as MixedRequest).ESP_Link.GetRawString();
                remoteURL = _fixedRemoteUrl(urlLink, sConfig);
            }
            else if (bizRequest is PageRequest)
            {
                urlLink = (bizRequest as PageRequest).ESP_Link.GetRawString();
                remoteURL = _fixedRemoteUrl(urlLink, sConfig);
            }
            else if (bizRequest is ResourceRequest)
            {
                remoteURL = sConfig.RES_URL_PREFIX;
            }
            else if (bizRequest is ApplicationRequest)
            {
                isBinaryPost = true; //应用下载
                remoteURL = sConfig.SERVICE_URL;
            }
            else if (bizRequest is GatewayUpdateRequest)
            {
                isBinaryPost = true; //?
                remoteURL = sConfig.SERVICE_URL;
            }
            #endregion

            string rawUrl = _getFixedIpAddressUrl(remoteURL);
            bizRequest.ESP_Header.CellPhoneNumber = CurrentUser.MSID;
            ClientUser proxyUser = _getProxyClientUser(bizRequest);
            if (isBinaryPost)
            {
                byte[] sendBytes = (bizRequest is ApplicationRequest) ? ((ApplicationRequest)bizRequest).ESP_AppRequestData : bizRequest.GetNetworkBytes();
                //Server.Log.DebugFormat("* 已向远程发送业务数据！\r\n{0}", SpecUtil.ByteArrayToHexString(sendBytes));

                // 如果要发送二进制数据就使用ProxyBinaryRequest对象, 构造方法就是把Dictionary参数换成了byte[]
                return new ProxyBinaryRequest(proxyUser, sConfig.SERVICE_ID.ToString(),
                rawUrl, sendBytes, rawUrl,
                bizRequest.ESP_Header.GetContentEncoding());
            }
            else
            {
                Dictionary<string, string> ps = new Dictionary<string, string>();
                return new ProxyNormalRequest(proxyUser, sConfig.SERVICE_ID.ToString(),
                   rawUrl, ps, rawUrl,
                   bizRequest.ESP_Header.GetContentEncoding(),
                   RequestMethod.GET, DateTime.Now);
            }
        }

        private ClientUser _getProxyClientUser(RequestBase bizRequest)
        {
            NetworkType targetType = NetworkType.All;
            if (bizRequest.ESP_Header.ESP_NID == NetworkID.CMCC)
            {
                targetType = NetworkType.ChinaMobile;
            }
            else if (bizRequest.ESP_Header.ESP_NID == NetworkID.CTG)
            {
                targetType = NetworkType.ChinaTelecom;
            }
            else if (bizRequest.ESP_Header.ESP_NID == NetworkID.CUC)
            {
                targetType = NetworkType.ChinaUnicom;
            }

            //? 从数据库中获取用户信息
            //EaseUser dbUser = new EaseUser() { SOFTWARE_ID = bizRequest.ESP_Header.ESP_SoftwareID };
            //Gwsoft.SharpOrm.OrmHelper.DataBind(dbUser);

            ClientUser tUser = new ClientUser(bizRequest.ESP_Header.ESP_SoftwareID.ToString(),
                                bizRequest.ESP_Header.ESP_DID.ToString(),
                                bizRequest.ESP_Header.ESP_IMEI.GetRawString(),
                                bizRequest.ESP_Header.ESP_IMEI.GetRawString(),
                                "",
                                targetType);
            tUser.UserAgent = bizRequest.ESP_Header.ESP_UserAgent.GetRawString();
            RequestHeader.WebHeaderSyn(bizRequest.ESP_Header, tUser.AllHeaders);
            return tUser;
        }

        #region IDisposable 成员

        /// <summary>
        /// 执行与释放或重置非托管资源相关的应用程序定义的任务。
        /// </summary>
        public void Dispose()
        {
            if (_currentSharedConnection != null && _currentSharedConnection.State != ConnectionState.Closed)
            {
                _currentSharedConnection.Close();
                _currentSharedConnection.Dispose();
            }
        }

        #endregion
    }
}
