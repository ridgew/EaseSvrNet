<%@ Application Language="C#" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="Gwsoft.Ease.Common" %>

<script RunAt="server">

    void Application_Start(object sender, EventArgs e)
    {
        //在应用程序启动时运行的代码
        log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config")));
        "* 应用程序开始".Info();
    }

    protected static string[] ShutdownReason = new string[] { 
            "None：未提供关闭原因。", // 0
            "HostingEnvironment：由于宿主环境而关闭。", // 1
            "ChangeInGlobalAsax：由于对 Global.asax 的更改而关闭。 ", // 2
            "ConfigurationChange：由于对应用程序级配置的更改而关闭。 ", // 3
            "UnloadAppDomainCalled：由于对 System.Web.HttpRuntime.UnloadAppDomain 的调用而关闭。", // 4
            "ChangeInSecurityPolicyFile：由于代码访问安全策略文件中的更改而关闭。", // 5
            "BinDirChangeOrDirectoryRename：由于对 Bin 文件夹或其中包含的文件的更改而关闭。", // 6
            "BrowsersDirChangeOrDirectoryRename：由于对 App_Browsers 文件夹或其中包含的文件的更改而关闭。", // 7
            "CodeDirChangeOrDirectoryRename：由于对 App_Code 文件夹或其中包含的文件的更改而关闭。", // 8
            "ResourcesDirChangeOrDirectoryRename：由于对 App_GlobalResources 文件夹或其中包含的文件的更改而关闭。", // 9
            "IdleTimeout：由于允许的最大空闲时间限制而关闭。", // 10
            "PhysicalApplicationPathChanged：由于对应用程序的物理路径的更改而关闭。", // 11
            "HttpRuntimeClose：由于对 System.Web.HttpRuntime.Close 的调用而关闭。 ", // 12
            "InitializationError：由于 System.AppDomain 初始化错误而关闭", // 13
            "MaxRecompilationsReached：由于资源的动态重新编译的最大次数限制而关闭。", // 14
            "BuildManagerChange", // 15
        }; 

    void Application_End(object sender, EventArgs e)
    {
        "* 应用程序结束，原因{0}".Info(ShutdownReason[System.Web.Hosting.HostingEnvironment.ShutdownReason.GetHashCode()]);
    }

    protected void Application_AuthenticateRequest(Object sender, EventArgs e)
    {
        if (HttpContext.Current.User != null)
        {
            if (HttpContext.Current.User.Identity.AuthenticationType == "Forms")
            {
                //从Cookie的票据的userData或其他方式还原用户信息
            }
        }
    }

    void Application_Error(object sender, EventArgs e)
    {
        //在出现未处理的错误时运行的代码
        log4net.ILog log = log4net.LogManager.GetLogger("ErrorLog");
        if (log != null)
        {
            HttpApplication app = sender as HttpApplication;
            if (app != null && app.Context != null)
            {
                foreach (Exception ex in app.Context.AllErrors)
                {
                    log.Error(ex);
                }
            }
        }
    }

    void Session_Start(object sender, EventArgs e)
    {
        //在新会话启动时运行的代码

    }

    void Session_End(object sender, EventArgs e)
    {
        //在会话结束时运行的代码。 
        // 注意: 只有在 Web.config 文件中的 sessionstate 模式设置为
        // InProc 时，才会引发 Session_End 事件。如果会话模式 
        //设置为 StateServer 或 SQLServer，则不会引发该事件。

    }

	void Application_BeginRequest(object sender, EventArgs e)
	{
		//修正Flash在Firefox下无法共享Cookie的Bug
		if (HttpContext.Current.Request.UserAgent == "Shockwave Flash")
		{
			var FixCookies = new string[][] { new string[] { "ASPSESSID", "ASP.NET_SessionId" }, new string[] { "AUTHID", FormsAuthentication.FormsCookieName }, new string[] { "BrowseFolder", "BrowseFolder" } };
			for (var counter = 0; counter < FixCookies.Length; counter++)
			{
				try
				{
					if (HttpContext.Current.Request.Form[FixCookies[counter][0]] != null)
					{
						UpdateCookie(FixCookies[counter][1], HttpContext.Current.Request.Form[FixCookies[counter][0]]);
					}
					else if (HttpContext.Current.Request.QueryString[FixCookies[counter][0]] != null)
					{
						UpdateCookie(FixCookies[counter][1], HttpContext.Current.Request.QueryString[FixCookies[counter][0]]);
					}
				}
				catch (Exception)
				{
					Response.StatusCode = 500;
					Response.Write("Error Initializing Session");
				}
			}
			//HttpContext.Current.Request.Cookies.Debug(HttpContext.Current.Request.UserAgent);
		}
	}

	void UpdateCookie(string cookie_name, string cookie_value)
	{
		HttpCookie cookie = HttpContext.Current.Request.Cookies.Get(cookie_name);
		if (cookie == null)
		{
			cookie = new HttpCookie(cookie_name);
			HttpContext.Current.Request.Cookies.Add(cookie);
			cookie.Value = cookie_value;
			HttpContext.Current.Request.Cookies.Set(cookie);
		}
	}
       
</script>

