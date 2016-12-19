using System;
using System.Data.Common;
using System.Web;
using Gwsoft.EaseMode;
using Gwsoft.SharpOrm;

namespace EaseServer.EaseConnection.HTTP
{
    /// <summary>
    /// HTTP方式承载EASE协议
    /// </summary>
    public class EaseModule : IHttpModule
    {
        #region IHttpModule 成员

        /// <summary>
        /// 处置由实现 <see cref="T:System.Web.IHttpModule"/> 的模块使用的资源（内存除外）。
        /// </summary>
        public void Dispose()
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.ApplicationInstance.BeginRequest -= new EventHandler(context_BeginRequest);
            }
        }

        /// <summary>
        /// 初始化模块，并使其为处理请求做好准备。
        /// </summary>
        /// <param name="context">一个 <see cref="T:System.Web.HttpApplication"/>，它提供对 ASP.NET 应用程序内所有应用程序对象的公用的方法、属性和事件的访问</param>
        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(context_BeginRequest);
        }

        #endregion

        void context_BeginRequest(object sender, EventArgs e)
        {
            HttpApplication app = (HttpApplication)sender;
            string rawURL = app.Context.Request.RawUrl;
            if (rawURL.StartsWith("/ease/servlet/", StringComparison.InvariantCultureIgnoreCase))
            {
                rawURL = rawURL.Substring("/ease/servlet/".Length);
                /*
                  <!-- http://ip:port/ease/servlet/ease?sid=%d wap注册请求-->
                  <!-- http://ip:port/ease/servlet/sms?msid=用户手机号&sms=短信内容 -->
                */

                IHttpHandler hander = null;
                if (rawURL.StartsWith("ease", StringComparison.InvariantCultureIgnoreCase))
                {
                    hander = new EaseRequestHandler();
                }
                else if (rawURL.StartsWith("sms", StringComparison.InvariantCultureIgnoreCase))
                {
                    hander = new SMSRequestHandler();
                }

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
    }

    /// <summary>
    /// HTTP请求注册或更新
    /// </summary>
    public class EaseRequestHandler : IHttpHandler
    {
        #region IHttpHandler 成员

        /// <summary>
        /// 获取一个值，该值指示其他请求是否可以使用 <see cref="T:System.Web.IHttpHandler"/> 实例。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果 <see cref="T:System.Web.IHttpHandler"/> 实例可再次使用，则为 true；否则为 false。</returns>
        public bool IsReusable
        {
            get { return false; }
        }

        /// <summary>
        /// 通过实现 <see cref="T:System.Web.IHttpHandler"/> 接口的自定义 HttpHandler 启用 HTTP Web 请求的处理。
        /// </summary>
        /// <param name="context"><see cref="T:System.Web.HttpContext"/> 对象，它提供对用于为 HTTP 请求提供服务的内部服务器对象（如 Request、Response、Session 和 Server）的引用。</param>
        public void ProcessRequest(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;
            string softwareID = request.QueryString["sid"];
            if (string.IsNullOrEmpty(softwareID))
            {
                softwareID = "请求格式为：http://ip:port/ease/servlet/ease?sid=用户编号";
            }
            else
            {
                /*
                    Host: 118.123.205.165:8095
                    X-Source-Id: 115.168.85.135
                    X-Up-Bear-Type: CDMA
                    X-Nx_remoteip: 10.145.162.39
                    Proxy-Connection: Keep-Alive
                    X-Hts_user: true
                    X-Up-Calling-Line-Id: 13398195976
                    X-Forwarded-For: 10.145.162.39
                 */
                string requestPhoneNum = null, requestRemoteIp = null;
                //System.Diagnostics.Trace.TraceInformation("{0} {1}", request.RequestType, request.RawUrl);
                #region 从HTTP头中获取手机号码及远程IP地址(并更新)
                foreach (string key in request.Headers.AllKeys)
                {
                    //System.Diagnostics.Trace.TraceInformation("{0}: {1}", key, request.Headers[key]);
                    if (string.Compare(key, "X-Up-Calling-Line-Id", true) == 0)
                    {
                        requestPhoneNum = request.Headers[key];
                    }
                    if (string.Compare(key, "X-Source-Id", true) == 0)
                    {
                        requestRemoteIp = request.Headers[key];
                    }
                }
                #endregion

                EaseUser user = new EaseUser { SOFTWARE_ID = Convert.ToInt64(softwareID) };
                #region 处理用户标识不为空的情形
                if (user.SOFTWARE_ID > 0)
                {
                    DbConnection conn = OrmHelper.GetDbConnection(user);
                    conn.Open();
                    bool doNext = true;
                    try
                    {
                        user.DataBind(new string[] { "MSID", "REMOTE_IP" }, conn);
                    }
                    catch (NotExistException)
                    {
                        doNext = false;
                    }

                    if (!doNext)
                    {
                        softwareID = "用户" + softwareID + "不存在！";
                    }
                    else
                    {
                        doNext = false;
                        //用户手机号
                        if (!string.IsNullOrEmpty(requestPhoneNum))
                        {
                            //8615332746410

                            if (requestPhoneNum.Length == 13 && requestPhoneNum.StartsWith("86"))
                                requestPhoneNum = requestPhoneNum.Substring(2);
                            if (requestPhoneNum.StartsWith("+86"))
                                requestPhoneNum = requestPhoneNum.Substring(3);

                            user.MSID = requestPhoneNum;
                            doNext = true;
                        }

                        //远程ip地址
                        if (!string.IsNullOrEmpty(requestRemoteIp))
                        {
                            user.REMOTE_IP = requestRemoteIp;
                            doNext = true;
                        }

                        if (doNext)
                        {
                            user.UserAgent = request.UserAgent;
                            softwareID = "Ok(" + user.Update(conn) + ")";
                        }
                        else
                        {
                            softwareID = "HTTP头中即没有X-Up-Calling-Line-Id也没有X-Source-Id信息，用户相关信息没有更新！";
                        }
                    }
                    conn.Close();
                    conn.Dispose();
                }
                #endregion
            }

            #region 处理为普通网关请求
            if (!(request.RequestType.Equals("POST", StringComparison.InvariantCultureIgnoreCase)
                && request.TotalBytes >= NetworkSwitchRequest.MinRequestBytesLength))
            {
                if (request.RequestType.Equals("GET", StringComparison.InvariantCultureIgnoreCase))
                {
                    response.ContentType = "text/html";
                    response.Charset = "utf-8";
                    response.Write(softwareID);
                    response.End();
                }
                else
                {
                    response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
                }
            }
            else
            {
                response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            }
            #endregion
        }

        #endregion

        /// <summary>
        /// 获得当前页面客户端的IP
        /// </summary>
        public static string GetIP(HttpRequest request)
        {
            string result = String.Empty;
            result = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (null == result || result == String.Empty)
            {
                result = request.ServerVariables["REMOTE_ADDR"];
            }

            if (null == result || result == String.Empty)
            {
                result = request.UserHostAddress;
            }

            if (null == result || result == String.Empty)
            {
                return "0.0.0.0";
            }
            return result;

        }
    }


    /// <summary>
    /// 短信注册请求或更新（2.7.3 注册短信的格式）
    /// <![CDATA[
    /// 当用户首次使用本软件时，业务服务器可以控制用户是否进行短信注册，注册短信指令格式为：
    /// sms:短信号;msg:短信内容;error:失败提示;success:成功提示
    /// 其中短信内容中会嵌入特殊关键字，客户端需将特殊关键字替换成对应的参数值。例如:
    /// sms:10669986;msg:REG#%sid%#%did%#%nid%;error:注册失败;success:您已成功注册为会员！
    /// 客户端需将%sid% %did% %nid% 分别替换为软件ID 设备ID 网络运营商ID 的参数值后再发送短信。例
    /// 如实际发送短信示例为：
    /// sms:10669986;msg:REG#99#1#2;error:注册失败;success:您已成功注册为会员！
    /// 可能存在的特殊关键字，参考 2.7.2,另外WAP 指令中也可能存在特殊关键字，也需客户端将链接
    /// 中%特殊关键字%替换成对应的参数值。
    /// 业务接入网关接收注册短信的地址为：
    /// http://ip:port/ease/servlet/sms?msid=用户手机号&sms=短信内容
    /// 参数值请用java.net.URLEncoder.encode()进行处理，编码默认为UTF-8.
    /// ]]>
    /// </summary>
    public class SMSRequestHandler : IHttpHandler
    {

        #region IHttpHandler 成员

        /// <summary>
        /// 获取一个值，该值指示其他请求是否可以使用 <see cref="T:System.Web.IHttpHandler"/> 实例。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果 <see cref="T:System.Web.IHttpHandler"/> 实例可再次使用，则为 true；否则为 false。</returns>
        public bool IsReusable
        {
            get { return false; }
        }

        /// <summary>
        /// 通过实现 <see cref="T:System.Web.IHttpHandler"/> 接口的自定义 HttpHandler 启用 HTTP Web 请求的处理。
        /// </summary>
        /// <param name="context"><see cref="T:System.Web.HttpContext"/> 对象，它提供对用于为 HTTP 请求提供服务的内部服务器对象（如 Request、Response、Session 和 Server）的引用。</param>
        public void ProcessRequest(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            string msid = request.QueryString["msid"] ?? "0";
            string message = request.QueryString["sms"];

            response.ContentType = "text/html";
            response.Charset = "utf-8";


            //msg:REG#%sid%#%did%#%nid%;
            //sms:10669986;msg:REG#99#1#2;error:注册失败;success:您已成功注册为会员！

            object result = "error";
            if (!string.IsNullOrEmpty(message) && message.IndexOf("#") != -1)
            {
                string[] allDat = message.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);


                long DID = 0L, SID = 0L;
                if (long.TryParse(allDat[1], out SID) && long.TryParse(allDat[2], out DID))
                {
                    #region sid为业务编号时
                    //EaseUser newUser = new EaseUser
                    //{
                    //    SERVICE_ID = SID, //业务编号
                    //    DEVICE_ID = DID,   //设备编号
                    //    CLIENT_VERSION = 1,
                    //    IMEI = "",
                    //    IMSI = "",
                    //    MSID = msid, //手机号码
                    //    USER_NAME = "",
                    //    USER_SEX = 0,
                    //    USER_AGE = 0,
                    //    USER_CARD_TYPE = 0,
                    //    USER_ID_CARD = "",
                    //    USER_ADDR = "",
                    //    RegionCode = "",
                    //    ProvinceId = 0,
                    //    REMOTE_IP = EaseRequestHandler.GetIP(request),
                    //    FIRST_VISIT_TIME = DateTime.Now.ToString("yyyyMMddHHmmss"), //20091208004611
                    //    RegionDateCreated = DateTime.Now
                    //};

                    //result = OrmHelper.Insert(newUser,
                    //    true, new string[] { "CLIENT_VERSION", "USER_SEX", "USER_AGE" });
                    #endregion

                    #region 更新用户手机号码
                    EaseUser user = new EaseUser { SOFTWARE_ID = SID, MSID = msid };
                    //if (!String.IsNullOrEmpty(request.UserAgent))
                    //{
                    //    user.UserAgent = request.UserAgent;
                    //}
                    result = "Ok(" + user.Update() + ")";
                    #endregion

                }
                else
                {
                    result = "sms内容[" + message + "]不符合REG#%sid%#%did%#%nid%格式。";
                }
            }

            response.ContentType = "text/plain";
            response.Charset = "utf-8";
            response.Write(result);
            response.End();

        }

        #endregion
    }

}
