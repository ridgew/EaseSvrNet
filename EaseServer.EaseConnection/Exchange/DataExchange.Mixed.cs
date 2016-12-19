using System;
using Gwsoft.Ease.Proxy.Service;
using Gwsoft.EaseMode;

namespace EaseServer.EaseConnection
{
    public partial class DataExchange : IDisposable
    {
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
    }
}
