using System.Web.Script.Services;
using System.Web.Services;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    ///WEB 服务基类
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ScriptService]
    public class WebServiceBase : WebService
    {

    }

}
