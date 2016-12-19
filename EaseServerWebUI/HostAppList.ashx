<%@ WebHandler Language="C#" Class="HostAppList" %>

using CommonLib;
using System;
using System.Text;
using System.Web;
using Gwsoft.DataSpec;
using ClrServiceHost.Management;
using ClrServiceHost.DataNode;

public class HostAppList : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {
        HttpRequest request = context.Request;
        context.Response.Charset = "utf-8";
        context.Response.ContentType = "text/plain";

        APIResponse resp = null;
        if (request.Form["action"] != null && request.Form["pid"] != null)
        {
            string currentAct = request.Form["action"];
            string validActions = ",ReStartSpecialApp,StartSpecialApp,StopSpecialApp,";
            if (validActions.IndexOf("," + currentAct + ",") == -1)
            {
                context.Response.Write("无效操作！");
            }
            else
            {
                string pid = request.Form["pid"];
                //context.Response.Write(pid + "\n" + currentAct);
                //context.Response.Write("\n" + System.Diagnostics.Process.GetCurrentProcess().Id);
                SingleRequestArgument arg = SingleRequestArgument.CreateRequest<CLAV, CLTV>(currentAct, CLAV.GetCLAV(pid));
                resp = ManageChannel.GetHostResponse(APIReqeust.CreateGeneralRequest(arg));
                if (resp.Status != BizCode.GeneralResponse)
                {
                    context.Response.Write(resp.Status.ToString());
                    if (resp.ResponseBody.Length > 0)
                    {
                        context.Response.Write("\n");
                        context.Response.Write(Encoding.UTF8.GetString(resp.ResponseBody.DataContent));
                    }
                }
                else
                {
                    CLTV respBody = ESPDataBase.BindFromNetworkBytes<CLTV>(resp.ResponseBody.DataContent);
                    context.Response.Write(respBody.GetContent());
                }
            }
        }
        else
        {
            context.Response.ContentType = "text/html";
            resp = ManageChannel.GetHostResponse(APIReqeust.CreateNoArgumentsRequest("HostAppList"));
            if (resp.Status != BizCode.GeneralResponse)
            {
                context.Response.Write(resp.Status.ToString());
                if (resp.ResponseBody.Length > 0)
                {
                    context.Response.Write("<br/>");
                    context.Response.Write(System.Text.Encoding.UTF8.GetString(resp.ResponseBody.DataContent));
                }
            }
            else
            {
                string currentProcessID = string.Format("#{0}-EaseServer-{1}", System.Diagnostics.Process.GetCurrentProcess().Id,
                        context.Server.MapPath("/EaseServer.exe").GetHashCode().ToString("X"));

                context.Response.Write("<form method=\"post\">\n");
                DataList<HostApplication> appList = ESPDataBase.BindFromNetworkBytes<DataList<HostApplication>>(resp.ResponseBody.DataContent);
                string appListResult = appList.Data.Join("</ul><ul>\r\n",
                    o =>
                    {
                        HostApplication app = (HostApplication)o;
                        string pidCurrent = app.ProcessId.GetContent();
                        return string.Format("<li>ID:<input type=\"radio\" name=\"pid\" value=\"{0}\"{3} />{0}, FilePath:{1}, Arguments:{2}</li>",
                            pidCurrent,
                            app.FilePath.GetContent(),
                            app.Arguments.GetContent(),
                            pidCurrent.Equals(currentProcessID) ? " disabled" : "");
                    });
                context.Response.Write("<ul>" + appListResult + "</ul>");
                context.Response.Write(@"
                    <input type=""submit"" value=""StartSpecialApp"" name=""action""/> 
                    <input type=""submit"" value=""StopSpecialApp"" name=""action""/> 
                    <input type=""submit"" value=""ReStartSpecialApp"" name=""action""/> ");
                context.Response.Write("\n</form>");
            }
        }

    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}