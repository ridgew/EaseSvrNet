using System;
using Gwsoft.Ease.Proxy.Service;
using Gwsoft.EaseMode;

namespace EaseServer.EaseConnection
{
    public partial class DataExchange : IDisposable
    {
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
    }
}
