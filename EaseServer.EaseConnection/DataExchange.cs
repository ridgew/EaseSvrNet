/**********************************************
 * $Id: DataExchange.cs 1466 2011-01-10 09:13:27Z wangqj $
 * $Author: wangqj $
 * $Revision: 1466 $
 * $LastChangedRevision: 1466 $
 * $LastChangedDate: 2011-01-10 17:13:27 +0800 (Mon, 10 Jan 2011) $
 ***********************************************/

using System;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using CommonLib;
using EaseServer.Interface;
using EaseServer.Performance;
using Gwsoft.Ease.Proxy.Service;
using Gwsoft.EaseMode;
using Gwsoft.SharpOrm;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 处理缓存和HTTP数据交互
    /// </summary>
    public partial class DataExchange : IDisposable
    {
        /// <summary>
        /// 缓存代理对象入口实例
        /// </summary>
        IProxyCaching ipc = new DefaultProxyCaching();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataExchange"/> class.
        /// </summary>
        /// <param name="conn">当前连接对象引用</param>
        /// <param name="validSwitchRequest">有效的网关请求对象</param>
        public DataExchange(IServerConnection conn, NetworkSwitchRequest validSwitchRequest)
        {
            currentConn = conn;
            SvrAPI = conn.GetServerAPI();

            SwitchRequest = validSwitchRequest;
            _subRequest = validSwitchRequest.GetSubRequest();
            if (_subRequest != null)
            {
                _subRequest.Context = validSwitchRequest.Context;
            }

            #region 如果当前连接运行性能统计
            if (currentConn.EnablePerformanceCounter)
            {
                ipc.PerfCounter = (p, e, d) =>
                {
                    if (e)
                    {
                        return currentConn.EndCounter(p, d);
                    }
                    else
                    {
                        return currentConn.BeginCounter(p, d);
                    }
                };
            }
            #endregion
        }

        IServerConnection currentConn = null;
        IServerAPI SvrAPI = null;

        NetworkSwitchRequest _switchRequest = null;
        /// <summary>
        /// 网关请求对象
        /// </summary>
        public NetworkSwitchRequest SwitchRequest
        {
            get { return _switchRequest; }
            private set { _switchRequest = value; }
        }

        private RequestBase _subRequest = null;
        /// <summary>
        /// 接入网关的子级请求对象
        /// </summary>
        public RequestBase SubRequest { get { return _subRequest; } }

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
        public string ClientRequestUrl { get { return _clientRequestUrl; } }

        private EaseUser _currentUser = null;
        /// <summary>
        /// 当前获取数据的用户信息
        /// </summary>
        public EaseUser CurrentUser { get { return _currentUser; } }

        private DbConnection _currentSharedConnection = null;

        private ServiceConfig sConfig = null;
        /// <summary>
        /// 当前业务服务请求
        /// </summary>
        public ServiceConfig CurrentService { get { return sConfig; } }

        /// <summary>
        /// 默认文本内容替换函数
        /// </summary>
        /// <param name="businessId">当前业务编号</param>
        /// <param name="rawEaseCode">EASE标签代码</param>
        /// <param name="isHtmlText">是否是html代码</param>
        /// <returns></returns>
        public static string DefaultCodeFilter(short businessId, string rawEaseCode, bool isHtmlText)
        {
            string strResult = rawEaseCode;//s.Replace("\r", "").Replace("\n", "");
            return Regex.Replace(strResult, ">\\s{2,}<", "><");
        }

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
                catch (ThreadAbortException) { }
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
                resp = ExecuteWithTimeout<ProxyResponse>(() => func(request, analyzer),
                    TimeoutSetting, ref isTimeout);
            }
            catch (Exception err)
            {
                currentConn.ServerError("* #获取业务数据(GetProxyCache)失败, {0}秒内{1}\r\n{2}", TimeoutSetting,
                    isTimeout ? "超时！" : "完成。",
                    err);

                _bizStatusCode = -1;
                resp = new ProxyResponse
                {
                    EaseCode = "ProxyResponse occurred Exception!",
                    Status = new ResponseStatus
                    {
                        ErrMessage = isTimeout ? string.Concat(TimeoutSetting, "秒连接超时！") : err.GetUsefulMessage(),
                        StatusCode = (err is WebException) ? ((WebException)err).GetHttpStatusCode() : -1
                    }
                };
            }

            if (_bizStatusCode != -1)
            {
                TimeSpan tspan = DateTime.Now - startTime;
                currentConn.ServerDebug("* 获取业务数据(GetProxyCache)耗时{0}s{1}ms, {2}秒内{3}",
                    tspan.TotalSeconds.ToString("0"),
                    tspan.Milliseconds, TimeoutSetting,
                    isTimeout ? "超时!" : "完成.");
            }
            return resp;
        }

        /// <summary>
        /// 超时设置
        /// </summary>
        double TimeoutSetting = (typeof(DataExchange).FullName + ".TimeoutSecond").AppSettings<double>(20.00);

        /// <summary>
        /// 有选择的异常忽略获取缓存的业务数据
        /// </summary>
        /// <param name="func">获取数据委托</param>
        /// <param name="request">当前业务请求</param>
        /// <returns></returns>
        ExecFuncDataWrap ExecuteFun(Func<ProxyRequest, byte[]> func, ProxyRequest request)
        {
            byte[] retBytes = new byte[0];
            bool isTimeout = false;
            DateTime startTime = DateTime.Now;
            try
            {
                retBytes = ExecuteWithTimeout<byte[]>(() => func(request),
                    TimeoutSetting, ref isTimeout);
            }
            catch (WebException webEx)
            {
                currentConn.ServerError(string.Format("* #获取业务数据(GetProxyCacheItem)失败, {0}秒内{1}", TimeoutSetting,
                    isTimeout ? "超时！" : "完成。"), webEx);
                if (!request.IgnoreWebException) throw webEx;
                retBytes = ResponseBase.ResponseBizErrorBytes;
            }
            catch (Exception err)
            {
                currentConn.ServerError(string.Format("* #获取业务数据(GetProxyCacheItem)失败, {0}秒内{1}", TimeoutSetting,
                    isTimeout ? "超时！" : "完成。"), err);

                retBytes = ResponseBase.ResponseBizErrorBytes;
            }

            TimeSpan tspan = DateTime.Now - startTime;
            currentConn.ServerDebug("* 获取业务数据(GetProxyCacheItem)耗时{0}s{1}ms, {2}秒内{3}",
                tspan.TotalSeconds.ToString("0"), tspan.Milliseconds, TimeoutSetting,
                isTimeout ? "超时!" : "完成.");

            return new ExecFuncDataWrap { IsTimeout = isTimeout, RetureBytes = retBytes };
        }

        string CurrentType = typeof(DataExchange).FullName;

        /// <summary>
        /// 判断是否记录当前业务调试信息
        /// </summary>
        public bool RecordCurrentService()
        {
            if (_subRequest == null)
            {
                return true;
            }
            else
            {
                string sidsSetting = (CurrentType + ".DebugServiceID").AppSettings<string>("");
                if (string.IsNullOrEmpty(sidsSetting))
                {
                    return true;
                }
                else
                {
                    string crtId = _subRequest.GetBusinessID().ToString();
                    return ("," + sidsSetting + ",").IndexOf("," + crtId + ",") != -1;
                }
            }
        }

        /// <summary>
        /// 远程请求真实地址
        /// </summary>
        string _exchangeRemoteUrl = null;

        /// <summary>
        /// 单次业务请求缓存命中率
        /// </summary>
        CacheRate cacheRate = new CacheRate();

        /// <summary>
        /// [入口]获取网关返回的对象封装
        /// </summary>
        /// <returns></returns>
        public NetworkSwitchResponse GetSwitchResponse()
        {
            if (SubRequest == null)
            {
                if (SwitchRequest.ESP_CustomeCode == 0)
                {
                    _exchangeMessage = "心跳包";
                    _exchangeStatusCode = string.Concat(((short)SwitchRequest.ESP_SuccessFlag).ToString(), ":-1");
                    return DataProxy.GenerateSwitchResponse(SwitchRequest, new byte[0], false);
                }
                else
                {
                    currentConn.ServerError("* #无法确定网关子级请求对象！");
                    _exchangeMessage = "无法确定网关子级请求对象！";
                    _exchangeStatusCode = "-1:-1";
                    return DataProxy.GenerateSwitchResponse(SwitchRequest, new byte[0], true);
                }
            }

            ProxyRequest pReq = _translateRequest(SubRequest);      //建立业务数据请求代理对象
            if (CurrentUser == null || CurrentService == null)
            {
                currentConn.ServerError("* #无法确定用户或相关业务{0}！", SubRequest.ESP_Header.ESP_BusinessID);
                _exchangeMessage = "无法确定用户或相关业务！(" + SubRequest.ESP_Header.ESP_BusinessID + ")";
                _exchangeStatusCode = "-1:-1";
                return DataProxy.GenerateSwitchResponse(SwitchRequest, new byte[0], true);
            }
            if (pReq != null)
            {
                _exchangeRemoteUrl = pReq.GetHttpDebugData();              //更新远程请求地址
                pReq.CacheCounter = new CacheRateCounter(c =>
                {
                    cacheRate.TotalCount++;
                    if (c) cacheRate.UseCount++;
                });
            }

            if (currentConn.EnablePerformanceCounter)
            {
                currentConn.PerfCounter.BussinessID = CurrentService.SERVICE_ID.ToString();
            }

            currentConn.EndCounter(PerformancePoint.ParseProtocol);
            currentConn.EndCounter(PerformancePoint.ParseData);
            currentConn.BeginCounter(PerformancePoint.PerpareData);

            currentConn.ServerDebug("[{3}:业务编号:{2}] {0} => {1}", pReq.Method, _exchangeRemoteUrl,
                CurrentService.SERVICE_ID, SvrAPI.Port);

            //设置资源地址根
            TagAnalyzer _tagAnalyzer = new TagAnalyzer(CurrentService.SERVICE_ID, CurrentService.RES_URL_PREFIX, CurrentService.CLIENT_ROOT_URI);
            ResponseHeader header = new ResponseHeader();
            RequestHeader reqHeader = SubRequest.ESP_Header;

            HandlerException errorHanlder = (e) => { return false; };

            bool hasBizError = false;
            bool isGeneralResponse = true;

            //第2层返回数据
            byte[] retBytes = new byte[0];
            if (reqHeader.ESP_Protocol == RequestType.Application)
            {
                ApplicationRequest appRequest = _subRequest as ApplicationRequest;
                if (appRequest != null && appRequest.IsPackageReqeust())
                {
                    isGeneralResponse = false;
                    #region 单独实现的分包应用下载
                    retBytes = _getApplicationPackageResponse(appRequest, pReq, errorHanlder); //单独实现的分包应用下载
                    #endregion
                }
            }
            else if (reqHeader.ESP_Protocol == RequestType.Resource)
            {
                ResourceRequest resRequest = _subRequest as ResourceRequest;
                if (resRequest != null && resRequest.IsPackageReqeust())
                {
                    isGeneralResponse = false;
                    #region 单独实现的分包资源下载
                    retBytes = _getResourcePackageResponse(resRequest, pReq, errorHanlder); //单独实现的分包资源下载
                    #endregion
                }
            }

            ResponseBase bizResp = null; //业务员层返回对象封装
            if (isGeneralResponse)
            {
                #region 一般数据应答

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
                            bizResp = _getResourceResponse(_subRequest as ResourceRequest, pReq, errorHanlder);
                            break;
                        case RequestType.Application:
                            bizResp = _getApplicationResponse(pReq, errorHanlder);
                            break;

                        case RequestType.UpdateCenter:
                            bizResp = new GatewayUpdateResponse();
                            break;

                        //2011-3-14 by Ridge
                        case RequestType.SynServerAddress:
                            bizResp = _getSynServerAddressResponse(_subRequest as SynServerAddressRequest, pReq, errorHanlder); ;
                            break;

                        default:
                            //其他为应用下载
                            bizResp = _getApplicationResponse(pReq, errorHanlder);
                            break;
                    }
                    if (bizResp != null) bizResp.ESP_Code = StatusCode.Success;
                }
                catch (InvalidBizResponseException bizExp)
                {
                    bizResp = bizExp.ResponseDefault;
                    bizResp.ESP_Code = StatusCode.Exception;
                    hasBizError = true;

                    currentConn.ServerError("* #获取业务数据失败(InvalidBizResponseException)！{0}", bizExp);

                    _exchangeMessage = "InvalidBizResponseException：" + bizExp.Message;
                    _exchangeStatusCode = "-1:" + _bizStatusCode;
                }
                catch (Exception exp)
                {
                    if (bizResp != null) bizResp.ESP_Code = StatusCode.Exception;
                    hasBizError = true;
                    currentConn.ServerError("* #获取业务数据失败！{0}", exp);

                    _exchangeMessage = "获取业务数据失败：" + exp.Message;
                    _exchangeStatusCode = "-1:" + _bizStatusCode;
                }
                #endregion

                #region 复制回传参数
                header.ESP_Protocol = reqHeader.ESP_Protocol;
                //分配会话标识
                header.ESP_SessionID = _getSessionFromHeader(reqHeader);
                header.ESP_SoftwareID = (int)CurrentUser.SOFTWARE_ID;
                header.ESP_SuccessFlag = (hasBizError) ? EaseSuccessFlag.Error : reqHeader.ESP_SuccessFlag;
                header.ESP_LeaveLength = -1; //默认后续长度为0
                #endregion

                if (bizResp == null)
                {
                    hasBizError = true;
                    retBytes = ResponseBase.ResponseBizErrorBytes;  //错误的二级响应包
                }
                else
                {
                    #region 业务数据提取与设置
                    bizResp.ESP_Method = Gwsoft.EaseMode.CommandType.None;
                    bizResp.ESP_Command = EaseString.Empty;
                    bizResp.ESP_Message = EaseString.Empty;
                    #endregion
                    bizResp.ESP_Header = header;

                    hasBizError = false;
                    retBytes = bizResp.GetNetworkBytes();       //正常的二级响应包
                }
                #endregion
            }

            NetworkSwitchResponse swResp = DataProxy.GenerateSwitchResponse(SwitchRequest, reqHeader, retBytes, hasBizError);

            #region 设置返回状态码
            short levelOneStatus = (short)swResp.ESP_SuccessFlag;
            if (bizResp != null && swResp.ESP_SuccessFlag == EaseSuccessFlag.Success)
            {
                levelOneStatus = (short)bizResp.ESP_Header.ESP_SuccessFlag;
            }
            if (levelOneStatus >= 1010) _exchangeStatusCode = string.Concat(levelOneStatus, ":", _bizStatusCode); //设置最终返回状态码  
            #endregion

            if (SwitchRequest.Context != null)
            {
                object getRequestTime = SwitchRequest.Context.GetItem(DataProxy.PROXYPROCESS_REQUEST_FIRSTKEY);
                if (getRequestTime != null)
                {
                    DateTime lastTime = (DateTime)getRequestTime;
                    _exchangeMilliseconds = (decimal)((DateTime.Now - lastTime).TotalMilliseconds);
                }
            }
            return swResp;
        }

        #region 统计辅助
        decimal _exchangeMilliseconds = 0;
        /// <summary>
        /// 获取数据交互所发时间
        /// </summary>
        /// <returns></returns>
        public decimal GetExchangeTime() { return _exchangeMilliseconds; }

        string _exchangeStatusCode = "-1:0";
        /// <summary>
        /// 获取状态码
        /// </summary>
        /// <returns></returns>
        public string GetExchangeStatusCode() { return _exchangeStatusCode; }

        string _exchangeMessage = "OK";
        /// <summary>
        /// 获取交互消息
        /// </summary>
        /// <returns></returns>
        public string GetExchangeMessage() { return _exchangeMessage; }
        #endregion


        bool _isSuccess(int statusCode) { return (statusCode == 200); }

        /// <summary>
        /// 业务默认状态编码
        /// </summary>
        int _bizStatusCode = 200;

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
                _bizStatusCode = -1;
                throw new InvalidBizResponseException("(ProxyResponse == null)未能在预定时间(" + TimeoutSetting + "秒)内完成数据返回。", defaultResp);
            }

            if (resp.Status == null)
            {
                _bizStatusCode = -1;
                throw new InvalidBizResponseException("(ProxyResponse.Status == null)未能获取业务数据返回状态。", defaultResp);
            }

            if (!_isSuccess(resp.Status.StatusCode))
            {
                _bizStatusCode = resp.Status.StatusCode;
                throw new InvalidBizResponseException(resp.Status.ErrMessage, defaultResp);
            }
        }

        /// <summary>
        /// 转换真实远程地址
        /// </summary>
        /// <param name="reqUrl">当前请求的地址</param>
        /// <param name="sConfig">当前请求业务配置</param>
        private string _fixedRemoteUrl(string reqUrl, ServiceConfig sConfig)
        {
            if (String.IsNullOrEmpty(reqUrl))
            {
                _clientRequestUrl = sConfig.SERVICE_INDEX_URL;
                return sConfig.SERVICE_INDEX_URL;
            }

            _clientRequestUrl = reqUrl;
            if (reqUrl.Equals(SERVICE_INDEX_URL, StringComparison.InvariantCultureIgnoreCase)
                || reqUrl.Equals(SERVICE_INDEX_URL + "l", StringComparison.InvariantCultureIgnoreCase))
            {
                return sConfig.SERVICE_INDEX_URL;
            }
            else
            {
                if (reqUrl.StartsWith("http://su:"))
                {
                    return string.Concat(sConfig.LINK_URL_PREFIX.TrimEnd('/') + "/", reqUrl.Substring(10).TrimStart('/'));
                }
                else if (reqUrl.StartsWith("http://ru:"))
                {
                    return string.Concat(sConfig.RES_URL_PREFIX.TrimEnd('/') + "/", reqUrl.Substring(10).TrimStart('/'));
                }
                else
                {
                    return string.Concat(sConfig.LINK_URL_PREFIX.TrimEnd('/') + "/", reqUrl.TrimStart('/'));
                }
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
                string sid = string.Format("{0}:{1}", CurrentUser.SOFTWARE_ID, DateTime.Now.ToString("yyyyMMddHHmmss"));
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
                _bizStatusCode = -1;
                sConfig = null;
                return null;
            }

            string requestSid = string.Empty;
            //[TODO]从缓存读取服务配置
            #region 配置ServiceConfig = > CurrentService
            sConfig = new ServiceConfig { SERVICE_ID = (decimal)bizRequest.ESP_Header.ESP_BusinessID };
            _currentSharedConnection = OrmHelper.GetDbConnection(sConfig);
            _currentSharedConnection.Open();
            try
            {
                sConfig.DataBind(new string[0], _currentSharedConnection);
                sConfig.RES_URL_PREFIX = _getFixedIpAddressUrl(sConfig.RES_URL_PREFIX);
            }
            catch (NotExistException)
            {
                sConfig = null;
                _bizStatusCode = -1;
                return null;
            }
            #endregion
            requestSid = sConfig.SERVICE_ID.ToString();

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
                MSID = "0"
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
                    IMEI = bizRequest.ESP_Header.ESP_IMEI.GetRawString() ?? "",
                    IMSI = bizRequest.ESP_Header.ESP_IMEI.GetRawString() ?? "",
                    MSID = "0",
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
                    bizRequest.ESP_Header.ESP_SoftwareID = (int)newUser.SOFTWARE_ID;
                    currentConn.ServerLog("* # 已分配新用户{0}(OLD:{3}), IMEI:{1}, DID:{2}！", newUser.SOFTWARE_ID, newUser.IMEI, newUser.DEVICE_ID, tRequestUser.SOFTWARE_ID);
                    tRequestUser = newUser;
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
            #endregion
            _currentUser = tRequestUser;

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
                _clientRequestUrl = remoteURL = sConfig.SERVICE_URL;
            }
            else if (bizRequest is GatewayUpdateRequest)
            {
                isBinaryPost = true; //?
                remoteURL = sConfig.SERVICE_URL;
            }
            else if (bizRequest is SynServerAddressRequest)
            {
                isBinaryPost = true; //?
                remoteURL = sConfig.SERVICE_URL;
            }
            #endregion

            string bizRequestRawUrl = _getFixedIpAddressUrl(remoteURL);
            bizRequest.ESP_Header.CellPhoneNumber = CurrentUser.MSID;
            ClientUser proxyUser = _getProxyClientUser(bizRequest, _currentUser, bizRequestRawUrl);
            if (isBinaryPost)
            {
                byte[] sendBytes = (bizRequest is ApplicationRequest) ? ((ApplicationRequest)bizRequest).ESP_AppRequestData : bizRequest.GetNetworkBytes();
                //Server.Log.DebugFormat("* 已向远程发送业务数据！\r\n{0}", SpecUtil.ByteArrayToHexString(sendBytes));

                // 如果要发送二进制数据就使用ProxyBinaryRequest对象, 构造方法就是把Dictionary参数换成了byte[]
                return new ProxyBinaryRequest(proxyUser, requestSid, sendBytes, bizRequestRawUrl,
                    bizRequest.ESP_Header.GetContentEncoding());
            }
            else
            {
                UrlParameterProcess UrlProcess = (UrlParameterProcess)sConfig.PageParamProcess;
                if (UrlProcess == UrlParameterProcess.UnKnown
                    || UrlProcess == UrlParameterProcess.AppendGet)
                {
                    return new ProxyNormalRequest(proxyUser, requestSid,
                    null,
                    bizRequestRawUrl,
                    bizRequest.ESP_Header.GetContentEncoding(),
                    RequestMethod.GET, DateTime.Now);
                }
                else
                {
                    if (UrlProcess == UrlParameterProcess.AppendPost)
                    {
                        return new ProxyNormalRequest(proxyUser, requestSid,
                            ProxyRequest.GetAppendPostParams(requestSid, proxyUser),
                            bizRequestRawUrl,
                            bizRequest.ESP_Header.GetContentEncoding(),
                            RequestMethod.POST, DateTime.Now);
                    }
                    else
                    {
                        //PostAll
                        NameValueCollection newBodyVal = ProxyRequest.GetForcePostAllParams(ref bizRequestRawUrl, requestSid, proxyUser);
                        return new ProxyNormalRequest(proxyUser, requestSid,
                            newBodyVal,
                            bizRequestRawUrl,
                            bizRequest.ESP_Header.GetContentEncoding(),
                            RequestMethod.POST, DateTime.Now);
                    }
                }
            }
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
