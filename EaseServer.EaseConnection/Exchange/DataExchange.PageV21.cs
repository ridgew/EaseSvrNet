using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gwsoft.EaseMode;
using Gwsoft.Ease.Proxy.Service;

namespace EaseServer.EaseConnection
{
    public partial class DataExchange : IDisposable
    {
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
    }
}
