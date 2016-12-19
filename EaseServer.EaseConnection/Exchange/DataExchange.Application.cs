using System;
using CommonLib;
using Gwsoft.Ease.Proxy.Service;
using Gwsoft.EaseMode;
using System.Text;

namespace EaseServer.EaseConnection
{
    public partial class DataExchange : IDisposable
    {
        //[DEBUG]
        ApplicationResponse _getApplicationResponse(ProxyRequest request, HandlerException exHandler)
        {
            ApplicationRequest appRequest = _subRequest as ApplicationRequest;
            if (appRequest == null) return null;

            byte[] respBytes = null;
            if (appRequest.IsPackageReqeust())
            {
                return null;
            }
            else
            {
                #region 非分包数据下载
                ApplicationResponse mResp = new ApplicationResponse();
                try
                {
                    ExecFuncDataWrap wrap = ExecuteFun(ipc.GetProxyCacheItem, request);
                    respBytes = wrap.RetureBytes;
                    if (wrap.IsTimeout)
                    {
                        _exchangeMessage = "获取业务数据超时！";
                        _bizStatusCode = -1;
                    }
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
                    DataProxy.GenericExceptionHandler(exHandler, exp);
                }
                return mResp;
                #endregion
            }
        }

        static object SynRoot = new object();
        static HashProvider HP = new HashProvider(HashProvider.ServiceProviderEnum.MD5, Encoding.UTF8);

        /// <summary>
        /// [DEBUG]单独实现的分包应用下载
        /// </summary>
        /// <param name="appRequest">当前应用请求</param>
        /// <param name="request">当前请求</param>
        /// <param name="exHandler">错误处理委托</param>
        /// <returns></returns>
        byte[] _getApplicationPackageResponse(ApplicationRequest appRequest, ProxyRequest request, HandlerException exHandler)
        {
            ApplicationPartialResponse pkgResp = new ApplicationPartialResponse();

            string hashStr = string.Empty;
            if (appRequest.ESP_AppRequestData != null && appRequest.ESP_AppRequestData.Length > 0)
            {
                lock (SynRoot)
                {
                    hashStr = HP.ComputeHash(appRequest.ESP_AppRequestData);
                }
            }

            string pkgId = string.Concat(CurrentUser.SOFTWARE_ID, appRequest.ESP_AppServerID,
                appRequest.ESP_Header.ESP_SessionID.GetRawString(),
                hashStr);

            byte[] totalBytes = null;
            if (!PackageManageFactory.Instance.Contains(pkgId, ref totalBytes))
            {
                try
                {
                    ExecFuncDataWrap wrap = ExecuteFun(ipc.GetProxyCacheItem, request);
                    totalBytes = wrap.RetureBytes;
                    if (wrap.IsTimeout)
                    {
                        _exchangeMessage = "获取业务数据超时！";
                        _bizStatusCode = -1;
                    }
                }
                catch (Exception exp)
                {
                    DataProxy.GenericExceptionHandler(exHandler, exp);
                }
                if (totalBytes != null && totalBytes.Length > 0)
                {
                    totalBytes = pkgResp.BuildWholePackageBytes(Gwsoft.EaseMode.CommandType.None, EaseString.Empty, totalBytes, true);
                    PackageManageFactory.Instance.Insert(pkgId, totalBytes);

                    currentConn.ServerDebug("*>>包数据来自(业务), Key={0}, 包数据长度:{1}.", pkgId, totalBytes.Length);
                }
            }
            else
            {
                if (totalBytes != null && totalBytes.Length > 0)
                {
                    currentConn.ServerDebug("*>>包数据来自(缓存), Key={0}, 包数据长度:{1}.", pkgId, totalBytes.Length);
                }
            }

            bool hasBizError = !(totalBytes != null && totalBytes.Length > 0);
            #region 复制回传参数
            ResponseHeader header = new ResponseHeader();
            header.ESP_Protocol = appRequest.ESP_Header.ESP_Protocol;
            header.ESP_SessionID = _getSessionFromHeader(appRequest.ESP_Header);
            header.ESP_SoftwareID = (int)CurrentUser.SOFTWARE_ID; //reqHeader.ESP_SoftwareID;
            header.ESP_SuccessFlag = (hasBizError) ? EaseSuccessFlag.Error : appRequest.ESP_Header.ESP_SuccessFlag;
            header.ESP_LeaveLength = -1; //默认后续长度为0
            pkgResp.ESP_Header = header;
            #endregion
            pkgResp.ESP_Code = (hasBizError) ? StatusCode.Exception : StatusCode.Success;
            pkgResp.ESP_Message = (hasBizError) ? EaseString.Get("Fetch Data Erorr!") : EaseString.Empty;

            if (hasBizError)
            {
                pkgResp.ESP_PackageLength = 0;
            }
            else
            {
                int pkgIdx = appRequest.ESP_PackageIndex;
                int pkgLen = appRequest.ESP_PackageLength;

                _buildPartialPackage(pkgResp, (int)appRequest.ESP_PackageIndex,
                    appRequest.ESP_PackageLength, totalBytes);
            }
            return pkgResp.GetNetworkBytes();
        }

    }
}
