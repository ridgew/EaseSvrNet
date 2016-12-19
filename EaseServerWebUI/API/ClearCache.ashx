<%@ WebHandler Language="C#" Class="ClearCache" %>

using System;
using System.Web;
using ClrServiceHost.Management;
using ClrServiceHost.DataNode;
using CommonLib;

public class ClearCache : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {
        HttpRequest request = context.Request;
        context.Response.Charset = "utf-8";
        context.Response.ContentType = "text/plain";

        string processID = request.QueryString["pid"];
        string action = "清除全部缓存数据";
        if (string.IsNullOrEmpty(processID))
        {
            //context.Response.Write("请输入进程标识参数pid、业务编号参数sid（可选）以清除指定的缓存数据。");
            processID = string.Format("#{0}-EaseServer-{1}", System.Diagnostics.Process.GetCurrentProcess().Id,
                context.Server.MapPath("/EaseServer.exe").GetHashCode().ToString("X"));
        }

        Exception lasException = null;
        APIResponse resp = null;
        try
        {
            string strTemp = null;
            AppHostMethodRun mRun = new AppHostMethodRun
            {
                ProcessID = processID,
                TypeFullName = "Gwsoft.Ease.Proxy.Service.Caching.CachController, Gwsoft.Ease.Proxy.Service",
                MethodName = "FlushData",
                MethodArgument = new object[0]
            };

            strTemp = request.QueryString["sid"];
            if (!string.IsNullOrEmpty(strTemp))
            {
                mRun.MethodName = "FlushServiceData";
                mRun.MethodArgument = new object[] { strTemp };
                action = string.Format("清除业务编号为{0}的缓存数据", strTemp);
            }

            strTemp = request.QueryString["ckey"];
            if (!string.IsNullOrEmpty(strTemp))
            {
                mRun.MethodName = "RemoveCache";
                mRun.MethodArgument = new object[] { strTemp };
                action = string.Format("清除缓存键值为{0}的缓存数据", strTemp);
            }

            SingleRequestArgument arg = SingleRequestArgument.CreateRequest<BLV, BLV>("ClrServiceHost.Management.Communication::CallStaticMethod, ClrServiceHost",
                mRun.GetBytes().ToBLV());
            resp = ManageChannel.GetHostResponse(APIReqeust.CreateGeneralRequest(arg));
        }
        catch (Exception err)
        {
            lasException = err;
        }

        if (lasException != null)
        {
            context.Response.Write(lasException.ToString());
        }
        else
        {
            if (resp.Status == BizCode.Exception)
            {
                context.Response.Write(System.Text.Encoding.UTF8.GetString(resp.ResponseBody.DataContent));
            }
            else
            {
                //结果内容格式为BLV
                BLV result = Gwsoft.DataSpec.ESPDataBase.BindFromNetworkBytes<BLV>(resp.ResponseBody.DataContent);

                context.Response.Write(action + ": ");
                if (result.DataContent != null && result.DataContent.Length > 0)
                {
                    //内置数据格式为布尔值
                    context.Response.Write(result.DataContent.GetObject<bool>() ? "OK" : "没找到相关缓存！");
                }
                else
                {
                    context.Response.Write("没找到相关缓存！");
                }
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