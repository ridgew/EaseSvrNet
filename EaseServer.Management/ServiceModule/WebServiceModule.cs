using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services.Protocols;
using CommonLib;
using log4net;
using EaseServer.Management.ServiceModule;

namespace EaseServer.Management
{
    /// <summary>
    /// 接入服务器控制台管理的WebService模块
    /// </summary>
    [Serializable]
    public sealed class WebServiceModule : IHttpModule, IHttpHandlerFactory
    {
        /// <summary>
        /// 初始化 <see cref="WebServiceModule"/> class.
        /// </summary>
        public WebServiceModule()
        {
            if (Initialized) return;
            InitializeProtocolDict();
        }

        //消息记录日志
        ILog log = LogManager.GetLogger("InfoLog");

        static object objInitialLock = new object();

        void InitializeProtocolDict()
        {
            if (Initialized) return;

            lock (objInitialLock)
            {
                foreach (Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    #region 跳过全局缓存程序集和微软公司的程序集
                    if (loadedAssembly.GlobalAssemblyCache) continue;

                    object[] asmCom = loadedAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), true);
                    if (asmCom != null && asmCom.Length > 0)
                    {
                        AssemblyCompanyAttribute asmCompany = (AssemblyCompanyAttribute)asmCom[0];
                        if (asmCompany.Company.Equals("Microsoft Corporation", StringComparison.InvariantCultureIgnoreCase))
                        {
                            continue;
                        }
                    }
                    #endregion

                    if (loadedAssembly.ReflectionOnly) continue;
                    try
                    {
                        //System.Diagnostics.Trace.TraceInformation("* 查找Assembly[{0}]的全部类型", loadedAssembly.FullName);
                        #region 循环查找注册类型及方法
                        foreach (Type innerType in loadedAssembly.GetTypes())
                        {
                            if (innerType.IsAbstract || innerType.IsInterface || innerType.IsGenericType || innerType.IsNotPublic
                                || !innerType.IsSubclassOf(typeof(System.Web.Services.WebService)))
                            {
                                //System.Diagnostics.Debugger.Log(0, "Info", "Skipped:" + innerType.FullName + Environment.NewLine);
                                continue;
                            }

                            //System.Diagnostics.Debugger.Log(0, "Info", "Found:" + innerType.FullName + Environment.NewLine);
                            foreach (MethodInfo eachMethod in innerType.GetMethods())
                            {
                                object[] pAttr = eachMethod.GetCustomAttributes(typeof(ProtocolAttribute), true);
                                if (pAttr == null || pAttr.Length < 1)
                                {
                                    continue;
                                }
                                else
                                {
                                    ProtocolAttribute p = (ProtocolAttribute)pAttr[0];
                                    string asmFileName = Path.GetFileNameWithoutExtension(loadedAssembly.Location);
                                    if (p.RegexPattern)
                                    {
                                        PatternProtoDict[p.Identity] = string.Format("{0}::{1}, {2}", innerType.FullName, eachMethod.Name, asmFileName);
                                    }
                                    else
                                    {
                                        NoPatternProtoDict[p.Identity] = string.Format("{0}::{1}, {2}", innerType.FullName, eachMethod.Name, asmFileName);
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                    catch (Exception typeLoadEx)
                    {
                        if (typeLoadEx.InnerException != null) typeLoadEx = typeLoadEx.InnerException;
                        Trace.TraceError("* 查找Assembly[{0}]时获取全部类型出现异常:{1}", loadedAssembly.FullName, typeLoadEx.Message);
                    }
                }
            }
            Initialized = true;
        }

        internal static bool Initialized = false;

        //存储协议号和方法完整方法名称
        internal static Dictionary<string, string> NoPatternProtoDict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        internal static Dictionary<string, string> PatternProtoDict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        #region IHttpModule 成员

        /// <summary>
        /// 处置由实现 <see cref="T:System.Web.IHttpModule"/> 的模块使用的资源（内存除外）。
        /// </summary>
        public void Dispose()
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.ApplicationInstance.PostAuthorizeRequest -= new EventHandler(processTargetHandler);
            }
        }

        /// <summary>
        /// 初始化模块，并使其为处理请求做好准备。
        /// </summary>
        /// <param name="context">一个 <see cref="T:System.Web.HttpApplication"/>，它提供对 ASP.NET 应用程序内所有应用程序对象的公用的方法、属性和事件的访问</param>
        public void Init(HttpApplication context)
        {
            context.PostAuthorizeRequest += new EventHandler(processTargetHandler);
        }

        #endregion

        void processTargetHandler(object sender, EventArgs e)
        {
            HttpApplication app = (HttpApplication)sender;
            string rawURL = app.Context.Request.RawUrl;
            string prefixPath = "WebServiceModule.PrefixePath".AppSettings<string>("/cpl/service/");

            if (app.Context.Request.UserAgent.IndexOf("HttpPing") == -1)
            {
                log.InfoFormat("Authorization:{0}\r\n{1} {2} {3} {4}", app.Context.Request.Headers["Authorization"],
                    app.Context.Request.HttpMethod, rawURL, app.Context.Request.UserHostAddress, app.Context.Request.UserAgent);
            }

            if (rawURL.StartsWith(prefixPath, StringComparison.InvariantCultureIgnoreCase))
            {
                //rawURL = rawURL.Substring(prefixPath.Length);
                IHttpHandler hander = GetHandler(app.Context, app.Context.Request.RequestType, rawURL, app.Request.PhysicalApplicationPath);
                if (hander != null)
                {
                    hander.ProcessRequest(app.Context);
                }
                else
                {
                    app.Context.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                }
                app.CompleteRequest();
            }
        }


        /// <summary>
        /// Use one of the visible attributes in the assembly to get a reference to the "System.Web.Extentions" Library.
        /// </summary>
        Assembly _ajaxAssembly = null;
        Assembly AjaxAssembly
        {
            get
            {
                if (_ajaxAssembly == null) _ajaxAssembly = typeof(GenerateScriptTypeAttribute).Assembly;
                return _ajaxAssembly;
            }
        }

        // Used to remember which Factory has been used
        private IHttpHandlerFactory UsedHandlerFactory;

        /*
         <httpModules>
            <add name="ServiceModule" type="EaseServer.ConsoleConnection.WebServiceModule, EaseServer.ConsoleConnection"/>
         </httpModules>
          <httpHandlers>
           <add verb="*" path="/public/*.asmx" validate="false" type="EaseServer.ConsoleConnection.WebServiceModule, EaseServer.ConsoleConnection"/>
          </httpHandlers>
         */
        // (?<proto>(([a-z$_])[\w_\-]*)(\.(([a-z$_])[\w_\-]*))*)    => Type.FullName Pattern
        // (?<proto>(([a-z$_])[\\w_\\-]*)(\\.(([a-z$_])[\\w_\\-]*))*)

        /// <summary>
        /// 全数字加点分隔的协议匹配模式
        /// </summary>
        const string FullNumSperatePattern = "\\/(?<proto>(\\d+\\.)+\\d+)";
        /// <summary>
        /// 入口协议匹配模式, 必须包含匹配组(proto) /cpl/service/1.3.5.1.1/
        /// </summary>
        static string ProtocolPattern = "WebServiceModule.ProtocolPattern".AppSettings<string>(FullNumSperatePattern);

        /// <summary>
        /// 获取特定路径的webservice类型及方法名称
        /// </summary>
        /// <param name="requestURL">请求的URL路径文件</param>
        /// <param name="webServiceMethodName">请求的方法名称，如果有</param>
        /// <returns></returns>
        private Type GetServiceType(string requestURL, ref string webServiceMethodName)
        {
            if (!Initialized) InitializeProtocolDict();

            Match m = Regex.Match(requestURL, ProtocolPattern, RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                return null;
            }
            else
            {
                string protocol = m.Groups["proto"].Value;
                if (!Regex.IsMatch(m.Value, FullNumSperatePattern, RegexOptions.IgnoreCase))
                {
                    //直接使用webservice类型访问
                    if (protocol.EndsWith(".asmx", StringComparison.InvariantCultureIgnoreCase))
                        protocol = protocol.Substring(0, protocol.Length - 5);
                    return Type.GetType(protocol, false);
                }
                else
                {
                    string targetMethodString = null;
                    #region 使用内置协议访问特定方法处理
                    if (NoPatternProtoDict.ContainsKey(protocol))
                    {
                        targetMethodString = NoPatternProtoDict[protocol];
                    }
                    else
                    {
                        foreach (var tPattern in PatternProtoDict.Keys)
                        {
                            //Trace.TraceInformation("Pattern:{0}, Method:{1}, Url:{2}", tPattern, requestURL, PatternProtoDict[tPattern]);
                            if (Regex.IsMatch(requestURL, tPattern, RegexOptions.IgnoreCase))
                            {
                                targetMethodString = PatternProtoDict[tPattern];
                                break;
                            }
                        }
                    }

                    if (targetMethodString == null) return null;

                    Match mMethod = Regex.Match(targetMethodString, "::([^,]+)", RegexOptions.IgnoreCase);
                    if (!mMethod.Success)
                    {
                        throw new System.Configuration.ConfigurationErrorsException("方法名称[" + targetMethodString
                            + "]配置错误, 请使用形如'命名空间.对象类型名称::函数名称, 程序集名称'的格式配置！");

                    }
                    else
                    {
                        webServiceMethodName = mMethod.Groups[1].Value;
                        targetMethodString = targetMethodString.Replace(mMethod.Value, string.Empty);
                    }
                    #endregion
                    return Type.GetType(targetMethodString, false);
                }
            }
        }

        #region IHttpHandlerFactory

        /// <summary>
        /// 返回实现 <see cref="T:System.Web.IHttpHandler"/> 接口的类的实例。
        /// </summary>
        /// <param name="context"><see cref="T:System.Web.HttpContext"/> 类的实例，它提供对用于为 HTTP 请求提供服务的内部服务器对象（如 Request、Response、Session 和 Server）的引用。</param>
        /// <param name="requestType">客户端使用的 HTTP 数据传输方法（GET 或 POST）。</param>
        /// <param name="url">所请求资源的 <see cref="P:System.Web.HttpRequest.RawUrl"/>。</param>
        /// <param name="pathTranslated">所请求资源的 <see cref="P:System.Web.HttpRequest.PhysicalApplicationPath"/>。</param>
        /// <returns>
        /// 处理请求的新的 <see cref="T:System.Web.IHttpHandler"/> 对象。
        /// </returns>
        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            IHttpHandler HttpHandler = null;

            try
            {
                // Request Hosting permissions
                new AspNetHostingPermission(AspNetHostingPermissionLevel.Minimal).Demand();


                // 尝试获取请求webservice的对象类型和方法名称
                string targetWebServiceMethodName = null;
                Type targetWebServiceType = GetServiceType(url, ref targetWebServiceMethodName);

                // if we did not find any send it on to the original ajax script service handler.
                if (targetWebServiceType == null)
                {
                    // [REFLECTION] Get the internal class System.Web.Script.Services.ScriptHandlerFactory create it.
                    IHttpHandlerFactory ScriptHandlerFactory = (IHttpHandlerFactory)Activator.CreateInstance(AjaxAssembly.GetType("System.Web.Script.Services.ScriptHandlerFactory"));
                    UsedHandlerFactory = ScriptHandlerFactory;
                    return ScriptHandlerFactory.GetHandler(context, requestType, url, pathTranslated);
                }

                // [REFLECTION] get the Handlerfactory : RestHandlerFactory (Handles Javascript proxy Generation and actions)
                IHttpHandlerFactory JavascriptHandlerFactory = (IHttpHandlerFactory)Activator.CreateInstance(AjaxAssembly.GetType("System.Web.Script.Services.RestHandlerFactory"));

                // [REFLECTION] Check if the current request is a Javasacript method
                // JavascriptHandlerfactory.IsRestRequest(context);
                MethodInfo IsScriptRequestMethod = JavascriptHandlerFactory.GetType().GetMethod("IsRestRequest", BindingFlags.Static | BindingFlags.NonPublic);
                if (targetWebServiceMethodName == null && !(bool)IsScriptRequestMethod.Invoke(null, new object[] { context }))
                {
                    // Remember the used factory for later in ReleaseHandler
                    IHttpHandlerFactory WebServiceHandlerFactory = new WebServiceHandlerFactory();
                    UsedHandlerFactory = WebServiceHandlerFactory;

                    // [REFLECTION] Get the method CoreGetHandler
                    MethodInfo CoreGetHandlerMethod = UsedHandlerFactory.GetType()
                        .GetMethod("CoreGetHandler", BindingFlags.NonPublic | BindingFlags.Instance);

                    // [REFLECTION] Invoke the method CoreGetHandler :
                    // WebServiceHandlerFactory.CoreGetHandler(WebServiceType,context,context.Request, context.Response);
                    HttpHandler = (IHttpHandler)CoreGetHandlerMethod.Invoke(UsedHandlerFactory,
                        new object[] { targetWebServiceType, context, context.Request, context.Response }
                    );
                    return HttpHandler;
                }
                else
                {
                    #region AJAX WebService
                    // Remember the used factory for later in ReleaseHandler
                    UsedHandlerFactory = JavascriptHandlerFactory;

                    // Check and see if it is a Javascript Request or a request for a Javascript Proxy.
                    bool IsJavascriptDebug = string.Equals(context.Request.PathInfo, "/jsdebug", StringComparison.OrdinalIgnoreCase);
                    bool IsJavascript = string.Equals(context.Request.PathInfo, "/js", StringComparison.OrdinalIgnoreCase);
                    if (IsJavascript || IsJavascriptDebug)
                    {
                        #region 获取自动生成Javascript语句
                        // [REFLECTION] fetch the constructor for the WebServiceData Object
                        ConstructorInfo WebServiceDataConstructor = AjaxAssembly.GetType("System.Web.Script.Services.WebServiceData")
                            .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(Type), typeof(bool) }, null);

                        // [REFLECTION] fetch the constructor for the WebServiceClientProxyGenerator
                        ConstructorInfo WebServiceClientProxyGeneratorConstructor = AjaxAssembly.GetType("System.Web.Script.Services.WebServiceClientProxyGenerator")
                            .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string), typeof(bool) }, null);

                        // [REFLECTION] get the method from WebServiceClientProxy to create the javascript : GetClientProxyScript
                        MethodInfo GetClientProxyScriptMethod = AjaxAssembly.GetType("System.Web.Script.Services.ClientProxyGenerator")
                            .GetMethod("GetClientProxyScript", BindingFlags.NonPublic | BindingFlags.Instance, null,
                                new Type[] { AjaxAssembly.GetType("System.Web.Script.Services.WebServiceData") }, null);

                        // [REFLECTION] We invoke : 
                        // new WebServiceClientProxyGenerator(url,false).WebServiceClientProxyGenerator.GetClientProxyScript(new WebServiceData(WebServiceType));
                        string Javascript = (string)GetClientProxyScriptMethod.Invoke(
                          WebServiceClientProxyGeneratorConstructor.Invoke(new Object[] { url, IsJavascriptDebug })
                        , new Object[] {
                            WebServiceDataConstructor.Invoke(new object[] { targetWebServiceType, false }) 
                            }
                        );

                        // The following caching code was copied from the original assembly, read with Reflector, comments were added manualy.
                        #region Caching
                        // Check the assembly modified time and use it as caching http header
                        DateTime AssemblyModifiedDate = GetAssemblyModifiedTime(targetWebServiceType.Assembly);

                        // See "if Modified since" was requested in the http headers, and check it with the assembly modified time
                        string s = context.Request.Headers["If-Modified-Since"];

                        DateTime TempDate;
                        if (((s != null) && DateTime.TryParse(s, out TempDate)) && (TempDate >= AssemblyModifiedDate))
                        {
                            context.Response.StatusCode = 0x130;
                            return null;
                        }

                        // Add HttpCaching data to the http headers
                        if (!IsJavascriptDebug && (AssemblyModifiedDate.ToUniversalTime() < DateTime.UtcNow))
                        {
                            HttpCachePolicy cache = context.Response.Cache;
                            cache.SetCacheability(HttpCacheability.Public);
                            cache.SetLastModified(AssemblyModifiedDate);
                        }
                        #endregion

                        // Set Add the javascript to a new custom handler and set it in HttpHandler.
                        HttpHandler = new JavascriptProxyHandler(Javascript);
                        return HttpHandler;
                        #endregion
                    }
                    else
                    {
                        IHttpHandler JavascriptHandler = (IHttpHandler)Activator.CreateInstance(AjaxAssembly.GetType("System.Web.Script.Services.RestHandler"));

                        // [REFLECTION] fetch the constructor for the WebServiceData Object
                        ConstructorInfo WebServiceDataConstructor = AjaxAssembly.GetType("System.Web.Script.Services.WebServiceData")
                            .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(Type), typeof(bool) }, null);

                        // [REFLECTION] get method : JavaScriptHandler.CreateHandler
                        MethodInfo CreateHandlerMethod = JavascriptHandler.GetType()
                            .GetMethod("CreateHandler", BindingFlags.NonPublic | BindingFlags.Static, null,
                                new Type[] { AjaxAssembly.GetType("System.Web.Script.Services.WebServiceData"), typeof(string) }, null);

                        // [REFLECTION] Invoke CreateHandlerMethod :
                        // HttpHandler = JavaScriptHandler.CreateHandler(WebServiceType,false);
                        HttpHandler = (IHttpHandler)CreateHandlerMethod.Invoke(JavascriptHandler, new Object[]
                                {
                                    WebServiceDataConstructor.Invoke(new object[] { targetWebServiceType, false }),
                                    targetWebServiceMethodName //Ajax方式的WebService方法名称
                                }
                        );
                    }
                    return HttpHandler;
                    #endregion
                }
            }
            // Because we are using Reflection, errors generated in reflection will be an InnerException, 
            // to get the real Exception we throw the InnerException it.
            catch (TargetInvocationException e)
            {
                throw e.InnerException ?? e;
            }
        }

        /// <summary>
        /// 使工厂可以重用现有的处理程序实例。
        /// </summary>
        /// <param name="handler">要重用的 <see cref="T:System.Web.IHttpHandler"/> 对象。</param>
        public void ReleaseHandler(IHttpHandler handler)
        {
            if (UsedHandlerFactory != null)
                UsedHandlerFactory.ReleaseHandler(handler);

        }
        #endregion

        static DateTime GetAssemblyModifiedTime(Assembly assembly)
        {
            DateTime lastWriteTime = File.GetLastWriteTime(new Uri(assembly.GetName().CodeBase).LocalPath);
            return new DateTime(lastWriteTime.Year, lastWriteTime.Month, lastWriteTime.Day, lastWriteTime.Hour, lastWriteTime.Minute, 0);
        }

    }

    /// <summary>
    /// A custom handler to deliver the generated Javascript.
    /// </summary>
    internal class JavascriptProxyHandler : IHttpHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JavascriptProxyHandler"/> class.
        /// </summary>
        /// <param name="_javascript">The _javascript.</param>
        public JavascriptProxyHandler(string _javascript)
        {
            Javascript = _javascript;
        }

        string Javascript = "";

        #region IHttpHandler Members

        /// <summary>
        /// 获取一个值，该值指示其他请求是否可以使用 <see cref="T:System.Web.IHttpHandler"/> 实例。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果 <see cref="T:System.Web.IHttpHandler"/> 实例可再次使用，则为 true；否则为 false。</returns>
        bool IHttpHandler.IsReusable { get { return false; } }

        /// <summary>
        /// 通过实现 <see cref="T:System.Web.IHttpHandler"/> 接口的自定义 HttpHandler 启用 HTTP Web 请求的处理。
        /// </summary>
        /// <param name="context"><see cref="T:System.Web.HttpContext"/> 对象，它提供对用于为 HTTP 请求提供服务的内部服务器对象（如 Request、Response、Session 和 Server）的引用。</param>
        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/x-javascript";
            context.Response.Write(this.Javascript);
        }

        #endregion
    }

}
