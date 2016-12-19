using System;
using System.Collections.Generic;
using CommonLib;
using Gwsoft.Ease.Proxy.Service;
using Gwsoft.EaseMode;

namespace EaseServer.EaseConnection
{
    public partial class DataExchange : IDisposable
    {
        /// <summary>
        /// 根据资源名称获取资源类型
        /// </summary>
        /// <param name="resName">资源名称，不为null。</param>
        /// <returns></returns>
        ResourceCatelog getResCatelogByName(string resName)
        {
            string ext = resName.GetUrlExtension();
            if (ext != null && ext.Length > 1)
            {
                //图片默认扩展名列表
                if ("Gwsoft.EaseMode.ResourceCatelog.Picture.Extension".AppSettings<string>(".png.bmp.gif.jpg.ico.jpeg").IndexOf(ext) != -1)
                {
                    return ResourceCatelog.Picture;
                }

                //铃声默认扩展名列表
                if ("Gwsoft.EaseMode.ResourceCatelog.Ring.Extension".AppSettings<string>(".mid.midi.mp3.wav").IndexOf(ext) != -1)
                {
                    return ResourceCatelog.Ring;
                }
            }
            return ResourceCatelog.UnKnown;
        }

        /// <summary>
        /// [TODEBUG]资源下载
        /// </summary>
        /// <param name="resRequest">当前资源请求</param>
        /// <param name="reqTemplet">代理请求模板</param>
        /// <param name="exHandler">错误处理</param>
        /// <returns></returns>
        ResourceResponse _getResourceResponse(ResourceRequest resRequest, ProxyRequest reqTemplet, HandlerException exHandler)
        {
            ResourceResponse mResp = new ResourceResponse();
            try
            {
                List<EaseResource> resList = new List<EaseResource>();
                string urlTemp = string.Empty;

                reqTemplet.IgnoreWebException = true;
                for (int i = 0; i < (int)resRequest.ESP_LinksCount; i++)
                {
                    urlTemp = resRequest.ESP_LinkData[i].GetRawString();
                    reqTemplet.RawUrl = _fixedRemoteUrl(urlTemp, sConfig);

                    byte[] sngResBytes = ExecuteFun(ipc.GetProxyCacheItem, reqTemplet).RetureBytes;
                    if (sngResBytes != null && sngResBytes.Length > 0)
                    {
                        string resName = urlTemp.GetUrlFilename();
                        resList.Add(new EaseResource
                        {
                            ESP_Catelog = getResCatelogByName(resName),
                            ESP_Data = sngResBytes,
                            ESP_Length = sngResBytes.Length,
                            ESP_Name = EaseString.Get(resName),
                            ESP_URL = EaseString.Get(urlTemp)
                        });
                    }
                }

                mResp.ESP_Resources = resList.ToArray();
                mResp.ESP_PageResCount = (short)mResp.ESP_Resources.Length;
            }
            catch (Exception exp)
            {
                DataProxy.GenericExceptionHandler(exHandler, exp);
            }
            return mResp;
        }

        /// <summary>
        /// [DEBUG]单独实现的分包资源下载
        /// </summary>
        /// <param name="resRequest">当前资源请求</param>
        /// <param name="reqTemplet">代理请求模板</param>
        /// <param name="exHandler">错误处理委托</param>
        /// <returns></returns>
        byte[] _getResourcePackageResponse(ResourceRequest resRequest, ProxyRequest reqTemplet, HandlerException exHandler)
        {
            ResourcePartialResponse pkgResp = new ResourcePartialResponse();

            string pkgId = string.Concat(CurrentUser.SOFTWARE_ID, resRequest.GetBusinessID(),
                resRequest.ESP_Header.ESP_SessionID.GetRawString());

            byte[] totalBytes = null;
            if (!PackageManageFactory.Instance.Contains(pkgId, ref totalBytes))
            {
                ResourceResponse resResp = _getResourceResponse(resRequest, reqTemplet, exHandler);

                if (resResp.ESP_PageResCount > 0)
                {
                    ResourcePackageResponse resPkgResp = new ResourcePackageResponse();

                    resPkgResp.ESP_Command = resResp.ESP_Command;
                    resPkgResp.ESP_Method = resResp.ESP_Method;
                    resPkgResp.ESP_PageResCount = resResp.ESP_PageResCount;
                    resPkgResp.ESP_Resources = resResp.ESP_Resources;

                    totalBytes = resPkgResp.GetNetworkBytes();
                }

                if (totalBytes != null && totalBytes.Length > 0)
                {
                    totalBytes = pkgResp.BuildWholePackageBytes(resResp.ESP_Method, resResp.ESP_Command, totalBytes, false);
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
            header.ESP_Protocol = resRequest.ESP_Header.ESP_Protocol;
            header.ESP_SessionID = _getSessionFromHeader(resRequest.ESP_Header);
            header.ESP_SoftwareID = (int)CurrentUser.SOFTWARE_ID; //reqHeader.ESP_SoftwareID;
            header.ESP_SuccessFlag = (hasBizError) ? EaseSuccessFlag.Error : resRequest.ESP_Header.ESP_SuccessFlag;
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
                int pkgIdx = resRequest.ESP_PackageIndex;
                int pkgLen = resRequest.ESP_PackageLength;

                _buildPartialPackage(pkgResp, (int)resRequest.ESP_PackageIndex,
                    resRequest.ESP_PackageLength, totalBytes);
            }
            return pkgResp.GetNetworkBytes();
        }

    }
}
