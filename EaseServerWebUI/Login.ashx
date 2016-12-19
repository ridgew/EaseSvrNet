<%@ WebHandler Language="C#" Class="Login" %>

using System;
using System.Web;
using System.Text;
using System.Configuration;
using System.Web.Configuration;
using System.Security;
using System.Web.Security;
using System.Security.Principal;
using log4net;
using EaseServerAPI.Authentication;

public class Login : IHttpHandler
{
    ILog log = LogManager.GetLogger("DebugLog");
    
    AuthenticationModule auth = null;
    string authUserName = "";
    
    public Login()
    {
        //auth = new BasicAuthentication(OnAuthenticate, r => { return true; });
        auth = new DigestAuthentication(OnAuthenticate, r => { return true; });
    }

    public bool IsReusable { get { return false; } }

    public void ProcessRequest(HttpContext context)
    {
        HttpRequest Request = context.Request;
        HttpResponse Response = context.Response;

        if (Request.PathInfo != null
            && Request.PathInfo.Equals("/logout", StringComparison.InvariantCultureIgnoreCase))
        {
            FormsAuthentication.SignOut();
            Response.Write("Success，<a href=\"/\">点击这里回首页</a>！");
            Response.End();
            return;
        }

        HttpCookie UserCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
        if (UserCookie != null)
        {
            Response.Write("您已通过服务认证，<a href=\"/Login.ashx/logout\">点击这里退出</a>！");
            Response.End();
        }
        else
        {
            object authTag = null;
            if (Request.Headers["authorization"] == null)
            {
                ToAuthenticate(Request, Response);
            }
            else
            {
                string authHeader = Request.Headers["authorization"];
                int pos = authHeader.IndexOf(' ');
                if (pos == -1)
                    throw new InvalidCastException("Invalid authorization header");

                authTag = auth.Authenticate(authHeader, GetRealm(Request), Request.HttpMethod);
                if (authTag == null)
                {
                    ToAuthenticate(Request, Response);
                }
                else
                {
                    if (authTag is GenericPrincipal)
                    {
                        HttpContext.Current.User = (GenericPrincipal)authTag;
                    }
                    //FormsAuthentication.SetAuthCookie(authUserName, false);
                    FormsAuthentication.RedirectFromLoginPage(authUserName, false);
                    Response.Write(authUserName + "已通过服务认证，<a href=\"/Login.ashx/logout\">点击这里退出</a>！");
                    Response.End();
                }
            }
        }
    }

    private void OnAuthenticate(string realm, string userName, ref string password, out object login)
    {
        login = null;
        password = string.Empty;
        
        Configuration configuration = WebConfigurationManager.OpenWebConfiguration(HttpContext.Current.Request.ApplicationPath);
        AuthenticationSection authenticationSection = (AuthenticationSection)configuration.GetSection("system.web/authentication");
        FormsAuthenticationCredentials allCredentials = authenticationSection.Forms.Credentials;
        if (allCredentials.PasswordFormat == FormsAuthPasswordFormat.Clear)
        {
            for (int i = 0, j = allCredentials.Users.Count; i < j; i++)
            {
                if (userName.Equals(allCredentials.Users[i].Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    password = allCredentials.Users[i].Password;
                    authUserName = userName;
                    login = new GenericPrincipal(new GenericIdentity(userName), new string[] { "administrators" });
                    break;
                }
            }
        }
    }

    string GetRealm(HttpRequest request) { return "EaseServer"; }

    private void ToAuthenticate(HttpRequest Request, HttpResponse Response)
    {
        Response.Clear();
        Response.Charset = "utf-8";
        Response.AddHeader("www-authenticate", auth.CreateResponse(GetRealm(Request)));
        Response.Status = "401 Authorization Required";
        Response.Write("401 Authorization Required or invalid NetworkCredential");
        Response.End();
    }
    
}