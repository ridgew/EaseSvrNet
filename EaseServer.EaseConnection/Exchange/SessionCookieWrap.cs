using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 会话Cookie集合，用在与其他网站交互时适用
    /// <para>Author: Ridge Wong @ 2006/8/9</para>
    /// <para>Reversion: 0.0.1 2006/8/10 16:15 by Ridge Wong</para>
    /// <para>Reversion: 0.0.2 2010/12/2 13:49 by Ridge Wong, 修改HttpOnly值Cookie的兼容。</para>
    /// </summary>
    internal class SessionCookieWrap
    {
        HttpCookieCollection _ckCollection = new HttpCookieCollection();

        /// <summary>
        /// 初始化会话Cookie集合
        /// </summary>
        private SessionCookieWrap() { }

        /// <summary>
        /// 在集合中设置Cookie
        /// </summary>
        /// <param name="CookieString">Cookie的字符串形式</param>
        public void HttpSetCookie(string CookieString)
        {
            #region Bug Fix (HttpWebResponse @ 2006-8-10)
            if (CookieString.IndexOf(",") != -1)
            {
                string[] Cookies = CookieString.Split(',');
                for (int ic = 0; ic < Cookies.Length; ic++)
                {
                    HttpSetCookie(Cookies[ic]);
                }
                return;
            }
            #endregion

            string lCookieString = CookieString.ToLower();
            string leastCookieString = CookieString;
            string _domain = null, _expires = null, _path = null;
            string _snippet = string.Empty;
            int idxBegin, idxEnd;
            int len = CookieString.Length;
            int tLen = 0;

            #region 处理Domain,Expires和Path
            // set Domain
            idxBegin = lCookieString.IndexOf("domain=");
            tLen = 7;
            if (idxBegin != -1)
            {
                idxEnd = lCookieString.IndexOf(";", idxBegin);
                if (idxEnd == -1) idxEnd = len - 1;
                _snippet = CookieString.Substring(idxBegin, idxEnd - idxBegin + 1);

                idxBegin += tLen;
                _domain = CookieString.Substring(idxBegin, idxEnd - idxBegin + 1).TrimEnd(';');
                leastCookieString = leastCookieString.Replace(_snippet, "");
                //Joyes.Web.Util.Common.Debug(_domain);
            }

            // set Expires
            idxBegin = lCookieString.IndexOf("expires=");
            tLen = 8;
            if (idxBegin != -1)
            {
                idxEnd = lCookieString.IndexOf(";", idxBegin);
                if (idxEnd == -1) idxEnd = len - 1;
                _snippet = CookieString.Substring(idxBegin, idxEnd - idxBegin + 1);

                idxBegin += tLen;
                _expires = CookieString.Substring(idxBegin, idxEnd - idxBegin + 1).TrimEnd(';');
                leastCookieString = leastCookieString.Replace(_snippet, "");
                //Joyes.Web.Util.Common.Debug(_expires);
            }

            // set Path
            idxBegin = lCookieString.IndexOf("path=");
            tLen = 5;
            if (idxBegin != -1)
            {
                idxEnd = lCookieString.IndexOf(";", idxBegin);
                if (idxEnd == -1) idxEnd = len - 1;
                _snippet = CookieString.Substring(idxBegin, idxEnd - idxBegin + 1);

                idxBegin += tLen;
                _path = CookieString.Substring(idxBegin, idxEnd - idxBegin + 1).TrimEnd(';');
                leastCookieString = leastCookieString.Replace(_snippet, "");
                //Joyes.Web.Util.Common.Debug(_path);
            }
            #endregion 处理Domain,Expires和Path

            leastCookieString = leastCookieString.TrimEnd(' ', ';');
            idxBegin = leastCookieString.IndexOf("=");
            if (idxBegin == -1) return;
            string cookieName = leastCookieString.Substring(0, idxBegin);
            string cookieValue = leastCookieString.Substring(idxBegin + 1);
            //ASP.NET_SessionId=nmnqgb45wpkfiu45pnqzwj2f;  HttpOnly
            idxBegin = cookieValue.IndexOf(';');
            if (idxBegin != -1) cookieValue = cookieValue.Substring(0, idxBegin);

            HttpCookie cookie = new HttpCookie(_internalDecode(cookieName));
            cookie.Value = _internalDecode(cookieValue);

            #region 附加属性
            if (_domain != null) cookie.Domain = _domain;
            if (_expires != null)
            {
                try
                {
                    cookie.Expires = Convert.ToDateTime(_expires);
                }
                catch (Exception) { }
            }
            if (_path != null) cookie.Path = _path;
            #endregion

            SetCookie(cookie);
        }


        /// <summary>
        /// 更新Cookie集合中的现有值
        /// </summary>
        /// <param name="cookie">HttpCookie对象</param>
        public void SetCookie(HttpCookie cookie)
        {
            _ckCollection.Set(cookie);
        }

        /// <summary>
        /// 在Cookie集合中获取指定名称的Cookie
        /// </summary>
        /// <param name="cookieName">Cookie名称</param>
        /// <returns>如果存在，则返回相关Cookie对象。</returns>
        public HttpCookie Get(string cookieName)
        {
            return _ckCollection.Get(cookieName);
        }

        /// <summary>
        /// 在Cookie集合中获取索引的Cookie
        /// </summary>
        /// <param name="idx">Cookie索引</param>
        /// <returns>如果存在，则返回相关Cookie对象。</returns>
        public HttpCookie Get(int idx)
        {
            return _ckCollection.Get(idx);
        }


        /// <summary>
        /// 在集合中移除指定名称的Cookie
        /// </summary>
        /// <param name="cookieName">Cookie名称</param>
        public void RemoveCookie(string cookieName)
        {
            _ckCollection.Remove(cookieName);
        }


        /// <summary>
        /// 在集合中移除指定名称的Cookie
        /// </summary>
        /// <param name="cookieName">Cookie名称</param>
        /// <param name="updateSession">是否更新关联的Session</param>
        public void RemoveCookie(string cookieName, bool updateSession)
        {
            _ckCollection.Remove(cookieName);
        }

        /// <summary>
        /// 内部调用编码
        /// </summary>
        static string _internalEncode(string strSource)
        {
            // 已经编码数据
            if (strSource.IndexOf("%") != -1 || strSource.IndexOf("+") != -1)
            {
                return strSource;
            }
            // %     +      ;       ,       =       &       ::      \s
            // %25  %2B     %3B     %2C     %3D     %26     %3A%3A   +
            string strEncode = HttpUtility.UrlEncode(strSource, Encoding.Default);
            return strEncode.Replace("-", "%2D");
        }

        /// <summary>
        /// 内部调用反编码
        /// </summary>
        static string _internalDecode(string strSource)
        {
            return HttpUtility.UrlDecode(strSource, Encoding.Default);
        }

        /// <summary>
        /// 获取可以发送的Cookie的字符串形式
        /// </summary>
        /// <param name="cookie">Cookie实例</param>
        /// <returns>已经编码，可发送的字符。</returns>
        static string GetRequestCookie(HttpCookie cookie)
        {
            return EncodeCookieValue(cookie.Name) + "=" + EncodeCookieValue(cookie.Value);
        }

        /// <summary>
        /// 获取请求地址应发送的Cookie的字符串形式
        /// </summary>
        /// <param name="reqUrl">请求URL地址</param>
        /// <returns>Cookie集合的字符串表达形式</returns>
        /// <remarks>主要针对Cookie的Path,Domain,Expires。</remarks>
        public string GetRequestHttpCookie(string reqUrl)
        {
            Uri reqUri = null;
            try
            {
                reqUri = new Uri(reqUrl);
            }
            catch (Exception)
            {
                return ToString();
            }

            if (this._ckCollection.Count < 1) { return string.Empty; }
            StringBuilder cookieBuilder = new StringBuilder();
            DateTime EmptyExpires = (new HttpCookie("EmptyCookie")).Expires;

            for (int i = 0; i < this._ckCollection.Count; i++)
            {
                HttpCookie cookie = this._ckCollection[i];

                #region Cookie过滤处理
                string strTemp = string.Empty;
                // 是否过期
                if (cookie.Expires != EmptyExpires && cookie.Expires < System.DateTime.Now)
                {
                    RemoveCookie(cookie.Name, true);
                    continue;
                }

                // 是否所属域
                if (cookie.Domain != null)
                {
                    strTemp = cookie.Domain.ToLower();
                    if (!reqUri.Host.ToLower().EndsWith(strTemp)) continue;
                }

                // 是否属于该Path范围
                if (cookie.Path != null && cookie.Path != "/")
                {
                    strTemp = cookie.Path.ToLower();
                    if (!reqUri.LocalPath.ToLower().StartsWith(strTemp)) continue;
                }
                #endregion
                cookieBuilder.AppendFormat("{0}={1}; ", EncodeCookieValue(cookie.Name), EncodeCookieValue(cookie.Value));
            }
            return cookieBuilder.ToString().TrimEnd(';', ' ');
        }


        /// <summary>
        /// Cookie值编码
        /// </summary>
        public static string EncodeCookieValue(string cookieValue)
        {
            //UserState=down=1&chord=24&fmt=1%2C2%2C3%2C7&mobile=N%2DGage+QD&company=%C5%B5%BB%F9%D1%C7&cid=2&mid=174&UserID=2662896&Sound=0&pix=20&UserName=IBSolution;
            //joyes=pass=qinjun+It&user=IBSolution
            if (cookieValue.IndexOf("&") != -1)                 // 多项
            {
                string[] ckItems = cookieValue.Split('&');
                for (int i = 0; i < ckItems.Length; i++)
                {
                    if (ckItems[i].IndexOf('=') != -1)
                    {
                        string[] sItems = ckItems[i].Split('=');
                        for (int j = 0; j < sItems.Length; j++)
                        {
                            sItems[j] = _internalEncode(sItems[j]);
                        }
                        ckItems[i] = String.Join("=", sItems);
                    }
                }
                return String.Join("&", ckItems);
            }
            else if (cookieValue.IndexOf("=") != -1)              // 单项
            {
                string[] singleItems = cookieValue.Split('=');
                for (int k = 0; k < singleItems.Length; k++)
                {
                    singleItems[k] = _internalEncode(singleItems[k]);
                }
                return String.Join("=", singleItems);
            }
            else
            {
                return _internalEncode(cookieValue);
            }
        }


        /// <summary>
        /// Cookie集合的字符串形式
        /// </summary>
        public override string ToString()
        {
            if (this._ckCollection.Count < 1) { return string.Empty; }

            StringBuilder cookieBuilder = new StringBuilder();
            for (int i = 0; i < this._ckCollection.Count; i++)
            {
                HttpCookie cookie = this._ckCollection[i];
                cookieBuilder.AppendFormat("{0}={1}; ", EncodeCookieValue(cookie.Name), EncodeCookieValue(cookie.Value));
            }

            return cookieBuilder.ToString().TrimEnd(';', ' ');
        }

        /// <summary>
        /// 通过旧的Cookie字符创建会话Cookie实例
        /// </summary>
        public static SessionCookieWrap Create(string oldCookieString)
        {
            SessionCookieWrap wrap = new SessionCookieWrap();
            if (!string.IsNullOrEmpty(oldCookieString))
            {
                wrap.HttpSetCookie(oldCookieString);
            }
            return wrap;
        }

        //static void Test()
        //{
        //    SessionCookieWrap wrap = new SessionCookieWrap();
        //    wrap.HttpSetCookie("ASP.NET_SessionId=nmnqgb45wpkfiu45pnqzwj2f; path=/; HttpOnly");

        //    string reqCk = wrap.GetRequestHttpCookie("http://118.123.205.185:8081/product/chuhan/do.ashx?svid=573&sid=6308&imei=460036811802537&imsi=460036811802537&nid=3&did=350&url=/chuhan/gamelogin.jsp");
        //    Console.WriteLine(reqCk);
        //    //Console.WriteLine(wrap.ToString());
        //}

    }

}
