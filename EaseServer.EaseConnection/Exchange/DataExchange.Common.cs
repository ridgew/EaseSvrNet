using System;
using System.Collections.Generic;
using System.Data;
using CommonLib;
using EaseServer.EaseConnection.RefactContent;
using EaseServer.Performance;
using Gwsoft.Ease.Proxy.Service;
using Gwsoft.EaseMode;
using Gwsoft.SharpOrm;

namespace EaseServer.EaseConnection
{
    public partial class DataExchange : IDisposable
    {
        /// <summary>
        /// 获取资源
        /// </summary>
        EaseResource[] _getResourceFromResponse(ProxyResponse resp)
        {
            List<EaseResource> resList = new List<EaseResource>();
            if (_isSuccess(resp.Status.StatusCode))
            {
                foreach (var item in resp.Resource)
                {
                    string resName = item.Key;
                    //currentConn.ServerDebug("Change:{0} -> {1}", item.Key, resName);
                    if (resp.ResourceMappingDict.ContainsKey(resName))
                    {
                        resName = resp.ResourceMappingDict[resName];
                    }
                    else
                    {
                        resName = resName.GetUrlFilename();
                    }
                    resList.Add(new EaseResource
                    {
                        ESP_Catelog = getResCatelogByName(resName),
                        ESP_Data = item.Value,
                        ESP_Length = item.Value.Length,
                        ESP_Name = EaseString.Get(resName),
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


        /// <summary>
        /// 文档版本
        /// </summary>
        EaseDocument _getDocumentFromResponse(ProxyResponse resp)
        {
            EaseString requstUrlStr = EaseString.Get(ClientRequestUrl);
            //重构文档内容
            string clientCode = RefactContentFactory.CompositeHandler(_subRequest.GetBusinessID(), resp.EaseCode, true);
            EaseString docName = EaseString.Get(ClientRequestUrl.GetUrlFilename());
            if (resp.CustomData != null && resp.CustomData.ContainsKey(ProxyResponse.X_Proxy_FriendlyFileName))
            {
                docName = EaseString.Get(resp.CustomData[ProxyResponse.X_Proxy_FriendlyFileName].ToString());
            }
            return new EaseDocument
            {
                ESP_Type = DocumentType.Render,
                ESP_Name = docName,
                ESP_URL = requstUrlStr,
                ESP_Version = 0,
                ESP_Content = EaseString.Get(clientCode)
            };
        }

        /// <summary>
        /// 同步接入服务器用户为数据获取器用户
        /// </summary>
        /// <param name="bizRequest">当前业务请求</param>
        /// <param name="easeUser">易致平台的用户实例对象</param>
        /// <param name="bizRequestRawUrl">原始业务请求URL地址</param>
        /// <returns></returns>
        private ClientUser _getProxyClientUser(RequestBase bizRequest, EaseUser easeUser, string bizRequestRawUrl)
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

            #region 从会话标识中获取Cookie信息
            if (bizRequest.ESP_Header.ESP_Cookies.ESP_Length == 0)
            {
                if (!string.IsNullOrEmpty(easeUser.SessionCookie))
                {
                    SessionCookieWrap scw = SessionCookieWrap.Create(easeUser.SessionCookie);
                    //更新当前会话Cookie
                    bizRequest.ESP_Header.ESP_Cookies = EaseString.Get(scw.GetRequestHttpCookie(bizRequestRawUrl));
                }
            }
            #endregion

            ClientUser tProxyUser = new ClientUser(easeUser.SOFTWARE_ID.ToString(),
                                bizRequest.ESP_Header.ESP_DID.ToString(),
                                bizRequest.ESP_Header.ESP_IMEI.GetRawString(),
                                bizRequest.ESP_Header.ESP_IMEI.GetRawString(),
                                easeUser.MSID,
                                targetType);
            tProxyUser.UserAgent = bizRequest.ESP_Header.ESP_UserAgent.GetRawString();

            #region 扩展当前易致用户的Cookie更新动作
            tProxyUser.CookieSet = new UrlCookieSetHandler((userId, url, cookie) =>
            {
                currentConn.ServerDebug("☆需要更新用户{0}，在地址{1}上的Cookie：\r\n{2}", userId, url, cookie);

                EaseUser oldUserCookie = new EaseUser { SOFTWARE_ID = Convert.ToInt64(userId) };
                bool doNextProcessor = true;
                try
                {
                    if (_currentSharedConnection != null && _currentSharedConnection.State == ConnectionState.Open)
                    {
                        oldUserCookie.DataBind(new string[] { "SessionCookie" }, _currentSharedConnection);
                    }
                    else
                    {
                        oldUserCookie.DataBind(new string[] { "SessionCookie" });
                    }
                }
                catch (NotExistException)
                {
                    doNextProcessor = false;
                }

                if (doNextProcessor)
                {
                    SessionCookieWrap scw = SessionCookieWrap.Create(oldUserCookie.SessionCookie);
                    scw.HttpSetCookie(cookie);
                    oldUserCookie.SessionCookie = scw.ToString();
                    oldUserCookie.REMOTE_IP = currentConn.RemoteIP; //更新远程IP地址

                    if (_currentSharedConnection != null && _currentSharedConnection.State == ConnectionState.Open)
                    {
                        oldUserCookie.Update(_currentSharedConnection);
                    }
                    else
                    {
                        oldUserCookie.Update();
                    }
                }

            });
            #endregion

            RequestHeader.WebHeaderSyn(bizRequest.ESP_Header, tProxyUser.AllHeaders);
            return tProxyUser;
        }


        /// <summary>
        /// (无异常)记录数据交互访问日志
        /// </summary>
        internal void RecordPageviewLog(DateTime connectDatetime, decimal bizTime, decimal totalTime, string receiveLen, string sendLen, string status, string message)
        {
            #region 判断是否记录PV日志记录，默认为记录。
            if (!(CurrentType + ".RecordPageView").AppSettings<bool>(true))
                return;

            LOG_PV PV = null;
            try
            {
                string rateString = cacheRate.IsEmpty() ? "无缓存" : string.Concat(cacheRate.UseCount, "/", cacheRate.TotalCount);
                PV = new LOG_PV
                {
                    SOFTWARE_ID = (CurrentUser != null) ? CurrentUser.SOFTWARE_ID : ((SubRequest != null) ? SubRequest.ESP_Header.ESP_SoftwareID : -1),
                    LocalEndpoint = currentConn.LocalEP,
                    RemoteEndpoint = currentConn.RemoteEP,
                    SERVICE_ID = (CurrentService != null) ? CurrentService.SERVICE_ID : ((SubRequest != null) ? SubRequest.ESP_Header.ESP_BusinessID : -1),
                    VISIT_TIME = connectDatetime.ToString("yyyyMMddHHmmss"),
                    KEY_URL = (!string.IsNullOrEmpty(ClientRequestUrl)) ? ClientRequestUrl : ((SubRequest != null) ? SubRequest.GetType().FullName : "N/A"),
                    REAL_URL = _exchangeRemoteUrl,
                    HTML_TIME = bizTime,
                    PARSE_TIME = totalTime,
                    ReceiveByteLength = receiveLen,
                    SendByteLength = sendLen,
                    StatusCode = status,
                    CacheRate = rateString,
                    Protocol = ((SubRequest != null) ? SubRequest.ESP_Header.ESP_Protocol.ToString() : "N/A"),
                    Message = message
                };
                object logId = Gwsoft.SharpOrm.OrmHelper.Insert(PV, _currentSharedConnection, true);
                if (currentConn.EnablePerformanceCounter)
                {
                    currentConn.PerfCounter.LogID = Convert.ToInt64(logId);
                }
            }
            catch (Exception logEx)
            {
                currentConn.ServerError("* 写入日志错误:{0}", logEx);
                if (PV != null)
                    currentConn.ServerError("* 当前日志为:{0}", PV.GetXmlDocString(true));
            }
            #endregion
        }

        /// <summary>
        /// 填充分包响应数据实例
        /// </summary>
        /// <param name="currentPkg">当前分包实例</param>
        /// <param name="pkgIdx">包顺序，第1个包序号为1.</param>
        /// <param name="pkgLen">单个包分包大小</param>
        /// <param name="totalBytes">全部下发包数据</param>
        void _buildPartialPackage(PackageResponse currentPkg, int pkgIdx, int pkgLen, byte[] totalBytes)
        {
            //回传序号
            currentPkg.ESP_PackageIndex = (short)pkgIdx;

            int totalPkgLen = totalBytes.Length;
            int totalPkgCount = totalPkgLen / pkgLen;
            if (totalPkgLen % pkgLen != 0) totalPkgCount += 1;

            currentPkg.ESP_LeavePackageCount = (short)(totalPkgCount - pkgIdx);
            currentConn.ServerDebug("*>>>分包请求:总长度{0}字节, 分包大小{1}字节, 包序号{2}/{3}。", totalPkgLen, pkgLen, pkgIdx, totalPkgCount);

            #region 业务包数据
            byte[] buffer = new byte[pkgLen];
            int rStart = (pkgIdx - 1) * pkgLen;
            int rEnd = pkgIdx * pkgLen;
            if (rEnd > totalPkgLen)
            {
                rEnd = totalPkgLen;
                buffer = new byte[rEnd - rStart];
            }

            Buffer.BlockCopy(totalBytes, rStart, buffer, 0, buffer.Length);
            currentConn.ServerDebug("*>>>下发包内容:总长度{0}字节, 应用数据包大小{1}字节[{2}-{3}]。",
                totalPkgLen, buffer.Length,
                rStart, rEnd - 1);

            currentPkg.ESP_PackageBytes = buffer;
            currentPkg.ESP_PackageLength = buffer.Length;
            #endregion
        }

    }
}
