<%@ WebHandler Language="C#" Class="Tail" %>

using System;
using System.Web;
using System.IO;
using System.Text;
using System.Threading;
using Gwsoft.Ease.Common;

public class Tail : IHttpHandler
{
    public class FileInfo
    {
        public long Offset { get; set; }
        public StringBuilder Text { get; set; }
    }

    public class ClientInfo
    {
        public string Guid { get; set; }
        public string RawUrl { get; set; }
        public string UserHostAddress { get; set; }
        public string UserAgent { get; set; }
    }
    
    private FileSystemWatcher _watcher;
    private FileInfo _fi = new FileInfo();
    private string _path;
    private Encoding _encoding;
    private string _guid = Guid.NewGuid().ToString("N");

    public void ProcessRequest(HttpContext context)
    {
        context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
        context.Response.ContentType = "text/html";

        _path = "path".Request<string>();
        if (string.IsNullOrEmpty(_path))
        {
            context.Response.Write("缺少请求参数 path，如 path=/test.log (只支持相对路径，后缀名为txt或log)。");
            return;
        }
        else
        {
            _path = context.Server.MapPath(_path);
            if (File.Exists(_path))
            {
                if (!(_path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || _path.EndsWith(".log", StringComparison.OrdinalIgnoreCase)))
                {
                    context.Response.Write("参数错误 path，只支持后缀名为txt或log的文本文件。");
                    return;
                }
            }
            else
            {
                context.Response.Write("参数错误 path，文件不存在。");
                return;
            }
        }
        string code = "code".Request<string>();
        if (string.IsNullOrEmpty(code))
            _encoding = Encoding.Default;
        else
        {
            try
            {
                _encoding = Encoding.GetEncoding(code);
            }
            catch
            {
                context.Response.Write("参数错误 code，文件编码格式必须是web名称，如gb2312、utf-8等。");
                return;
            }
        }

        context.Response.Write(@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml""><head><title>tail -f " + "path".Request<string>() + @"</title></head><body style=""margin: 0 0 0 0; padding: 0 0 0 0; background-color: Black; color: White""><pre style=""margin: 0 0 0 0; padding: 0 0 0 0;"">Tail文本跟踪器已启动 ID=" + _guid + "\r\n\r\n");
        context.Response.Flush();

        System.IO.FileInfo ff = new System.IO.FileInfo(_path);
        if ("all".Request<bool>())
            FlushCurrent(context);
        else
            _fi.Offset = ff.Length;

        _watcher = new FileSystemWatcher(ff.DirectoryName, ff.Name);
        _watcher.NotifyFilter = NotifyFilters.Size | NotifyFilters.CreationTime;
        _watcher.EnableRaisingEvents = true;
        _watcher.Changed += new FileSystemEventHandler(_watcher_Changed);

        ClientInfo ci = new ClientInfo { Guid = _guid, RawUrl = context.Request.RawUrl, UserHostAddress = context.Request.UserHostAddress, UserAgent = context.Request.UserAgent };
        ci.Debug("Tail文本跟踪器已启动 ID=" + _guid);

        while (true)
        {
            lock (_fi)
            {
                while (Monitor.Wait(_fi))
                {
                    if (!context.Response.IsClientConnected)
                    {
                        _watcher.EnableRaisingEvents = false;
                        _watcher.Dispose();
                        ci.Debug("Tail文本跟踪器已结束 ID=" + _guid);
                        return;
                    }

                    context.Response.Write(_fi.Text);
                    context.Response.Write("<script>document.documentElement.scrollTop=document.body.scrollHeight;</script>");
                    context.Response.Flush();
                }
            }
        }
    }

    void FlushCurrent(HttpContext context)
    {
        using (FileStream fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            TextReader tr = new StreamReader(fs, _encoding);
            fs.Position = _fi.Offset > fs.Length ? 0 : _fi.Offset;
            string str;
            while (null != (str = tr.ReadLine()))
            {
                context.Response.Write(str + "\r\n" + "<script>document.documentElement.scrollTop=document.body.scrollHeight;</script>");
                context.Response.Flush();
            }
            _fi.Offset = fs.Length;
        }
    }

    void _watcher_Changed(object sender, FileSystemEventArgs e)
    {
        int count = 0;
        while (File.GetAttributes(e.FullPath) == FileAttributes.Offline)
        {
            System.Threading.Thread.Sleep(200);
            count++;
            if (count > 50)
                break;
        }
        using (FileStream fs = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            lock (_fi)
            {
                _fi.Text = new StringBuilder();
                TextReader tr = new StreamReader(fs, _encoding);
                fs.Position = _fi.Offset > fs.Length ? 0 : _fi.Offset;
                string str;
                while (null != (str = tr.ReadLine()))
                {
                    _fi.Text.Append(str);
                    _fi.Text.Append("\r\n");
                }
                _fi.Offset = fs.Length;                

                Monitor.PulseAll(_fi);
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