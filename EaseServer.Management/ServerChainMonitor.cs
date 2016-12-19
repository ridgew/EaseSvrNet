using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using CommonLib;
using System.IO;
using System.Web.Extensions;
using EaseServer.Management.Package;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace EaseServer.Management
{
    /// <summary>
    /// 易致接入服务器系列监控
    /// </summary>
    public class ServerChainMonitor : IHttpHandler
    {

        #region IHttpHandler 成员
        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {
            HttpRequest req = context.Request;
            HttpResponse resp = context.Response;

            //同步ping
            string svrChainFile = context.Server.MapPath("/App_Data/ServerChain.config");
            if (!File.Exists(svrChainFile))
            {
                resp.Write("0");
            }
            else
            {
                XmlDocument xdoc = new XmlDocument();
            doProcess:
                try
                {
                    xdoc.Load(svrChainFile);
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(500);
                    goto doProcess;
                }

                ServerChain chain = xdoc.GetObject<ServerChain>();
                if (req.HttpMethod == "GET")
                {
                    //输出上次同步时间
                    if (req.RawUrl.IndexOf('?') == -1)
                    {
                        resp.Write(chain.LastSynDatetime.GetTimeMilliseconds());
                    }
                    else
                    {
                        if (req.RawUrl.IndexOf("?telnet", StringComparison.InvariantCultureIgnoreCase) != -1)
                        {
                            int tIdx = -1;
                            //?telnet/Proxy/1
                            if (IsProxySyn(req.RawUrl, ref tIdx) && (tIdx >= 0 && tIdx < chain.Servers.Length))
                            {
                                Uri svrUri = new Uri(chain.Servers[tIdx].PingUrl);
                                resp.Write(string.Format("Telnet State -> {0}:{1}<br/>", svrUri.Host, svrUri.Port));
                                resp.Write(getSeverItemState(chain.Servers[tIdx]));
                            }
                            else
                            {
                                telnetServerChain(ref chain);
                                reportServerStateHtml(chain, resp.Output);
                            }
                        }
                        else
                        {
                            resp.ContentType = "application/json; charset=utf-8";
                            resp.Write(chain.ToJSON());
                        }
                    }
                }
                else
                {
                    //application/json; charset=UTF-8
                    if (req.ContentType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        resp.Write("error data");
                        return;
                    }

                    #region 保存同步数据
                    Exception lastExp = null;
                    string synJson = "";
                    try
                    {
                        using (StreamReader sr = new StreamReader(req.InputStream))
                        {
                            synJson = sr.ReadToEnd();
                            sr.Close();
                        }
                    }
                    catch (Exception) { }

                    if (synJson.Length > 2 && (!synJson.StartsWith("{") || !synJson.EndsWith("}")))
                    {
                        resp.Write("-1, JSON DateError.");
                    }
                    else
                    {
                        ServerChain newChain = null;
                        try
                        {
                            newChain = synJson.LoadFromJson<ServerChain>();
                        }
                        catch (Exception jsonEx)
                        {
                            lastExp = jsonEx;
                            resp.Write("-2," + jsonEx.Message);
                        }

                        if (lastExp == null && newChain != null)
                        {
                            int synIdx = -1;
                            if (!string.IsNullOrEmpty(req.PathInfo) && IsProxySyn(req.PathInfo, ref synIdx))
                            {
                                #region 远程同步
                                if (synIdx >= 0 && synIdx < newChain.Servers.Length)
                                {
                                    resp.Write(postRemoteJson(newChain.Servers[synIdx].PingUrl, synJson));
                                }
                                else
                                {
                                    resp.Write("-4,代理同步失败！");
                                }
                                #endregion
                            }
                            else
                            {
                                #region 本地同步
                                newChain.LastSynDatetime = DateTime.Now;
                                try
                                {
                                    newChain.GetXmlDoc(true).Save(svrChainFile);
                                }
                                catch (Exception synEx)
                                {
                                    lastExp = synEx;
                                    resp.Write("-3," + synEx.Message);
                                }
                                if (lastExp == null)
                                    resp.Write(newChain.LastSynDatetime.GetTimeMilliseconds());
                                #endregion
                            }
                        }

                    }
                    #endregion
                }
            }
        }

        bool IsProxySyn(string pathInfo, ref int synIdx)
        {
            Match m = Regex.Match(pathInfo, "proxy/(\\d+)", RegexOptions.IgnoreCase);
            if (m.Success)
                synIdx = Convert.ToInt32(m.Groups[1].Value);
            return m.Success;
        }

        string postRemoteJson(string url, string json)
        {
            string retStr = "N/A";
            try
            {
                HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                request.UserAgent = "Mozilla/5.0 (Windows NT 5.2; rv:2.0.1) Gecko/20100101 Firefox/4.0.1";
                request.Timeout = 5000;

                byte[] sendBytes = Encoding.UTF8.GetBytes(json);
                request.ContentLength = sendBytes.LongLength;
                using (Stream rs = request.GetRequestStream())
                {
                    rs.Write(sendBytes, 0, sendBytes.Length);
                    rs.Close();
                }

                using (Stream s = request.GetResponse().GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
                    {
                        retStr = sr.ReadToEnd();
                    }
                    s.Close();
                }
            }
            catch (Exception ex)
            {
                retStr = "N/A," + ex.Message;
            }
            return retStr;
        }

        #endregion

        /// <summary>
        /// 利用telnet接口获取运行时间
        /// </summary>
        /// <param name="chain"></param>
        void telnetServerChain(ref ServerChain chain)
        {
            foreach (ServerItem item in chain.Servers)
            {
                item.State = getSeverItemState(item);
            }
        }

        string getSeverItemState(ServerItem item)
        {
            string svrState = "N/A";
            if (item == null || string.IsNullOrEmpty(item.PingUrl))
                return svrState;

            try
            {
                Uri pingUrl = new Uri(item.PingUrl);
                using (TcpClient client = new TcpClient(pingUrl.Host, pingUrl.Port))
                {
                    client.NoDelay = true;
                    using (NetworkStream ns = client.GetStream())
                    {
                        ns.ReadTimeout = 2000;
                        ns.WriteTimeout = 5000;

                        byte[] keyBytes = Encoding.ASCII.GetBytes("0xease");
                        #region 进入
                        ns.Write(keyBytes, 0, keyBytes.Length);
                        #endregion

                        StreamReader sr = new StreamReader(ns, Encoding.Default);
                        string responseStr = string.Empty;
                        char[] buffer = new char[1024 * 4];

                        int offSet = 0, readCount = 0;
                        #region 读取欢迎消息
                        while ((readCount = sr.Read(buffer, offSet, buffer.Length - offSet)) > 0)
                        {
                            offSet += readCount;
                            responseStr = new string(buffer, 0, offSet);
                            if (responseStr.EndsWith("\r\n", StringComparison.InvariantCultureIgnoreCase) || responseStr.EndsWith(">"))
                                break;
                        }
                        #endregion

                        #region 获取服务器端状态
                        keyBytes = Encoding.ASCII.GetBytes("now\r\n");
                        ns.Write(keyBytes, 0, keyBytes.Length);

                        offSet = 0;
                        while ((readCount = sr.Read(buffer, offSet, buffer.Length - offSet)) > 0)
                        {
                            offSet += readCount;
                            responseStr = new string(buffer, 0, offSet);
                            if (responseStr.EndsWith("\r\n", StringComparison.InvariantCultureIgnoreCase) || responseStr.EndsWith(">"))
                                break;
                        }
                        svrState = responseStr.Trim();
                        #endregion

                        #region 友好退出
                        keyBytes = Encoding.ASCII.GetBytes("exit\r\n");
                        ns.Write(keyBytes, 0, keyBytes.Length);

                        offSet = 0;
                        while ((readCount = sr.Read(buffer, offSet, buffer.Length - offSet)) > 0)
                        {
                            offSet += readCount;
                            responseStr = new string(buffer, 0, offSet);
                            if (responseStr.EndsWith("\r\n", StringComparison.InvariantCultureIgnoreCase) || responseStr.EndsWith(">"))
                                break;
                        }
                        #endregion

                        sr.Close();

                        ns.Close();
                    }
                    client.Close();
                }
            }
            catch (Exception) { }
            return svrState;
        }

        void reportServerStateHtml(ServerChain chain, TextWriter writer)
        {
            writer.WriteLine("<style type=\"text/css\">#telnetTab td {font-size:12px;} #telnetTab th {font-size:12px;}</style>");
            writer.WriteLine("<table border=\"1\" cellpadding=\"5\" style=\"border-collapse:collapse;\" id=\"telnetTab\">");
            writer.WriteLine("<tr align=\"center\" bgcolor=\"#f3f3f3\"><th>名称</th><th>监听IP</th><th>端口</th><th>服务状态</th></tr>");
            foreach (ServerItem item in chain.Servers)
            {
                writer.Write("<tr>");
                Uri svrUri = new Uri(item.PingUrl);
                writer.WriteLine("<td>{0}</td>", item.ServerID);
                writer.WriteLine("<td>{0}</td><td>{1}</td>", svrUri.Host, svrUri.Port);
                if (item.State == "N/A")
                {
                    writer.WriteLine("<td><font color=red>{0}</font></td>", item.State);
                }
                else
                {
                    writer.WriteLine("<td>{0}</td>", item.State);
                }
                writer.WriteLine("</tr>");
            }
            writer.WriteLine("</table>");
        }
    }

    /// <summary>
    /// 服务队列
    /// </summary>
    [Serializable]
    public class ServerChain
    {
        [XmlAttribute]
        public DateTime LastSynDatetime { get; set; }

        /// <summary>
        /// 相关服务器列表
        /// </summary>
        public ServerItem[] Servers { get; set; }
    }

    /// <summary>
    /// 服务器项
    /// </summary>
    [Serializable]
    public class ServerItem
    {
        [XmlAttribute]
        public string ServerID { get; set; }

        [XmlAttribute]
        public string PingUrl { get; set; }

        [XmlAttribute]
        public DateTime LastPingTime { get; set; }

        /// <summary>
        /// 是否是ping主机，不需更新
        /// </summary>
        [XmlAttribute]
        public bool IsPingServer { get; set; }

        [XmlIgnore]
        public string State { get; set; }
    }
}
