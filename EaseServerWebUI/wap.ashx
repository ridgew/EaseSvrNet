<%@ WebHandler Language="C#" Class="WAP" %>

using System;
using System.Web;

public class WAP : IHttpHandler
{

    string wapPattern = @"<?xml version=""1.0"" encoding=""UTF-8""?>
        <!DOCTYPE wml PUBLIC ""-//WAPFORUM//DTD WML 1.1//EN"" ""http://www.wapforum.org/DTD/wml_1.1.xml"">
        <wml>
        <head>
        <meta forua=""true"" http-equiv=""Cache-Control"" content=""max-age=0""/> 
        <meta forua=""true"" http-equiv=""Cache-Control"" content=""no-store""/>
        </head>
        <card title=""Ease接入服务器"">
        <p>{0}</p>
        <p><do type=""prev"" label=""返回""><prev/></do></p>
        </card>
        </wml>";

    public void ProcessRequest(HttpContext context)
    {
        HttpRequest Request = context.Request;
        HttpResponse Response = context.Response;

        Response.Clear();
        Response.Charset = "UTF-8";
        Response.ContentType = "text/vnd.wap.wml";
        string filePath = Request.ServerVariables["PATH_INFO"];

        string result = string.Format(wapPattern, "&#9786;");
        Response.Write(result);
    }

    public bool IsReusable { get { return false; } }

}