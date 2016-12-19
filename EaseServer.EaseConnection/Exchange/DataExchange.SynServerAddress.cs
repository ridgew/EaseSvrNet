using System;
using System.IO;
using System.Xml;
using CommonLib;
using Gwsoft.Ease.Proxy.Service;
using Gwsoft.EaseMode;

namespace EaseServer.EaseConnection
{
    public partial class DataExchange : IDisposable
    {
        /// <summary>
        /// 根据当前同步地址请求获取最新的同步配置信息
        /// </summary>
        SynServerAddressResponse _getSynServerAddressResponse(SynServerAddressRequest synRequest, ProxyRequest reqTemplet, HandlerException exHandler)
        {
            SynServerAddressResponse resp = new SynServerAddressResponse();
            string cfgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SynServerAddress.config");
            try
            {

                SynServerAddress oldCfg = synRequest.ESP_AddressConfig;
                if (!File.Exists(cfgPath))
                {
                    resp.ESP_AddressConfig = oldCfg;
                }
                else
                {
                    //if (synRequest.ESP_Header.ESP_DailType == ClientDialType.CTWAP)
                    //{ 
                    //}
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.Load(cfgPath);
                    SynServerAddress newCfg = xDoc.GetObject<SynServerAddress>();

                    if (newCfg.ESP_ServerDomain.GetRawString().Equals(oldCfg.ESP_ServerDomain.GetRawString(), StringComparison.InvariantCultureIgnoreCase))
                        newCfg.ESP_ServerDomain = EaseString.Empty;
                    if (newCfg.ESP_ServerGateWayAddress.GetRawString().Equals(oldCfg.ESP_ServerGateWayAddress.GetRawString(), StringComparison.InvariantCultureIgnoreCase))
                        newCfg.ESP_ServerGateWayAddress = EaseString.Empty;
                    if (newCfg.ESP_ServerGateWayPath.GetRawString().Equals(oldCfg.ESP_ServerGateWayPath.GetRawString(), StringComparison.InvariantCultureIgnoreCase))
                        newCfg.ESP_ServerGateWayPath = EaseString.Empty;

                    if (newCfg.ESP_WapGateWayPort == oldCfg.ESP_WapGateWayPort)
                        newCfg.ESP_WapGateWayPort = 0;
                    if (newCfg.ESP_ServerGateWayPort == oldCfg.ESP_ServerGateWayPort)
                        newCfg.ESP_ServerGateWayPort = 0;
                    if (newCfg.ESP_ServerPort == oldCfg.ESP_ServerPort)
                        newCfg.ESP_ServerPort = 0;

                    resp.ESP_AddressConfig = newCfg;
                }
            }
            catch (Exception ex)
            {
                DataProxy.GenericExceptionHandler(exHandler, ex);
            }
            return resp;
        }
    }
}
