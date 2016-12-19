using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Services;
using CommonLib;
using System.Reflection;

namespace EaseServerAPI.Management
{
    /// <summary>
    /// 接入服务器管理API服务
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/", Description = "接入服务器管理API服务")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    //若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。 
    [System.Web.Script.Services.ScriptService]
    public class SvrMgr : System.Web.Services.WebService
    {
        public SvrMgr()
        {

            //如果使用设计的组件，请取消注释以下行 
            //InitializeComponent(); 
        }

        [WebMethod(Description = "保护配置文件的节点数据, sectionName:节点名称，如appSettings; provider:加密提供者，简称：Rsa及Data。")]
        public string ProtectSection(string sectionName, string provider)
        {
            /*
    <configProtectedData defaultProvider="RsaProtectedConfigurationProvider">
       <providers>
           <clear />
           <add description="Uses RsaCryptoServiceProvider to encrypt and decrypt" keyContainerName="NetFrameworkConfigurationKey" cspProviderName="" useMachineContainer="true" useOAEP="false" 
            name="RsaProtectedConfigurationProvider" type="System.Configuration.RsaProtectedConfigurationProvider,System.Configuration, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" />
           <add description="Uses CryptProtectData and CryptUnProtectData Windows APIs to encrypt and decrypt" useMachineProtection="true" keyEntropy="" 
            name="DataProtectionConfigurationProvider" type="System.Configuration.DpapiProtectedConfigurationProvider,System.Configuration, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" />
       </providers>
   </configProtectedData>
            */
            //ProtectSection("appSettings", "DataProtectionConfigurationProvider");

            string result = "Ok";
            try
            {
                if (string.IsNullOrEmpty(provider) ||
                    (!provider.Equals("Rsa", StringComparison.InvariantCultureIgnoreCase) && !provider.Equals("Data", StringComparison.InvariantCultureIgnoreCase)))
                {
                    provider = "DataProtectionConfigurationProvider";
                }
                else
                {
                    provider = (!provider.Equals("Data", StringComparison.InvariantCultureIgnoreCase)) ? "RsaProtectedConfigurationProvider" : "DataProtectionConfigurationProvider";
                }

                if (!ProtectSection(HttpContext.Current.Request, sectionName, provider))
                {
                    result = "未应用保护，可能没有相关节点！-> " + sectionName;
                }
            }
            catch (Exception errEx)
            {
                result = errEx.Message;
            }
            return result;
        }

        [WebMethod(Description = "取消保护配置文件的节点数据, sectionName:节点名称。")]
        public string UnProtectSection(string sectionName)
        {
            string result = "Ok";
            try
            {
                if (!UnProtectSection(HttpContext.Current.Request, sectionName))
                {
                    result = "未移除保护，可能没有相关节点！-> " + sectionName;
                }
            }
            catch (Exception errEx)
            {
                result = errEx.Message;
            }
            return result;
        }

        [WebMethod(Description = "重启ASP.NET应用程序")]
        public void ReloadAspnetHosting()
        {
            HttpRuntime.UnloadAppDomain();
        }

        [WebMethod(Description = "保存管理凭据数据, username:登录用户名, password:登录密码。")]
        public string StoreCredential(string username, string password)
        {
            HttpRequest request = HttpContext.Current.Request;
            Configuration config = WebConfigurationManager.OpenWebConfiguration(request.ApplicationPath);
            string result = "Ok";
            try
            {
                AuthenticationSection authenticationSection = (AuthenticationSection)config.GetSection("system.web/authentication");
                FormsAuthenticationCredentials allCredentials = authenticationSection.Forms.Credentials;

                bool foundUser = false;
                for (int i = 0, j = allCredentials.Users.Count; i < j; i++)
                {
                    if (username.Equals(allCredentials.Users[i].Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        allCredentials.Users[i].Password = password;
                        foundUser = true;
                        break;
                    }
                }
                if (!foundUser)
                {
                    allCredentials.Users.Add(new FormsAuthenticationUser(username, password));
                }
                config.Save();
            }
            catch (Exception storeEx)
            {
                result = storeEx.Message;
            }
            return result;
        }

        [WebMethod(Description = "移除特定用户名的管理凭据数据, username:登录用户名。")]
        public string RemoveCredential(string username)
        {
            HttpRequest request = HttpContext.Current.Request;
            Configuration config = WebConfigurationManager.OpenWebConfiguration(request.ApplicationPath);
            string result = "Ok";
            try
            {
                AuthenticationSection authenticationSection = (AuthenticationSection)config.GetSection("system.web/authentication");
                FormsAuthenticationCredentials allCredentials = authenticationSection.Forms.Credentials;
                allCredentials.Users.Remove(username);
                config.Save();
            }
            catch (Exception storeEx)
            {
                result = storeEx.Message;
            }
            return result;
        }

        private bool ProtectSection(HttpRequest request, string sectionName, string provider)
        {
            Configuration config = WebConfigurationManager.OpenWebConfiguration(request.ApplicationPath);
            ConfigurationSection section = config.GetSection(sectionName);
            if (section != null && !section.SectionInformation.IsProtected)
            {
                section.SectionInformation.ProtectSection(provider);
                config.Save();
                return true;
            }
            return false;
        }

        private bool UnProtectSection(HttpRequest request, string sectionName)
        {
            Configuration config = WebConfigurationManager.OpenWebConfiguration(request.ApplicationPath);
            ConfigurationSection section = config.GetSection(sectionName);
            if (section != null && section.SectionInformation.IsProtected)
            {
                section.SectionInformation.UnprotectSection();
                config.Save();
                return true;
            }
            return false;
        }

        [WebMethod(Description = "清空数据的日志并收缩数据库，connKey:数据连接字符串键值; dbName:数据库名称。")]
        public string ShrinkDatabase(string connKey, string dbName)
        {
            string sqlPattern = @"--清空日志
DUMP TRANSACTION {0} WITH NO_LOG 
BACKUP LOG {0} WITH NO_LOG

--收缩数据库
DBCC SHRINKDATABASE({0})";

            StringBuilder sb = new StringBuilder();
            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings[connKey].ConnectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(string.Format(sqlPattern, dbName), conn);

                    SqlDataReader r = cmd.ExecuteReader();
                    bool rowCaption = false;
                    while (r.Read())
                    {
                        for (int i = 0; i < r.FieldCount; i++)
                        {
                            if (!rowCaption && i == 0)
                            {
                                #region 列名
                                for (int m = 0, n = r.FieldCount; m < n; m++)
                                {
                                    if (m > 0)
                                    {
                                        sb.AppendFormat("    {0}", r.GetName(m));
                                    }
                                    else
                                    {
                                        sb.Append(r.GetName(m));
                                    }
                                }
                                sb.AppendLine();
                                #endregion
                                rowCaption = true;
                            }

                            if (i > 0)
                            {
                                sb.AppendFormat("    {0}", r[i] == DBNull.Value ? "<null>" : r[i].ToString());
                            }
                            else
                            {
                                sb.Append(r[i] == DBNull.Value ? "<null>" : r[i].ToString());
                            }
                        }
                        sb.AppendLine();
                    }
                    r.Close();
                    r.Dispose();

                    cmd.Dispose();

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine(ex.ToString());
            }
            return sb.ToString();
        }

        [WebMethod(Description = "备份数据库到特定路径，connKey:数据连接字符串键值; dbName:数据库名称; backupPath:数据库服务器磁盘路径; memo:备份描述。")]
        public string BackupDatabase(string connKey, string dbName, string backupPath, string memo)
        {
            string sqlPattern = @"backup database {0} To Disk = '{1}' with FORMAT,Name='{2}'";
            StringBuilder sb = new StringBuilder();
            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings[connKey].ConnectionString))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(string.Format(sqlPattern, dbName, backupPath,
                        memo ?? "web backup @" + DateTime.Now.ToString()), conn);

                    cmd.ExecuteNonQuery();

                    cmd.Dispose();

                    conn.Close();

                    sb.AppendLine("ok");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine(ex.ToString());
            }
            return sb.ToString();
        }

        [WebMethod(Description = "调试数据传输，requestDebug:调试请求; responseDebug:调试应答。")]
        public string DataExchangeDebug(bool requestDebug, bool responseDebug)
        {
            string svrConfig = HttpContext.Current.Request.MapPath("/EaseServer.exe.config");
            return svrConfig.SetAppSettings("EaseServer.EaseConnection.DataExchange.DebugRequest|EaseServer.EaseConnection.DataExchange.DebugResponse",
                requestDebug.ToString() + "|" + responseDebug.ToString());
        }

        void ShowMessageRedirect(int seconds, string message, string url)
        {
            string responseText = string.Format("<meta http-equiv=\"refresh\" content=\"{0};url={1}\" /><body><pre>{2}</pre></body>",
                seconds,
                url, message);

            HttpContext.Current.Response.Write(responseText);
            HttpContext.Current.Response.End();
        }

        [WebMethod(Description = "设置特定业务编号的缓存分钟数，sid:业务编号, m1CacheMinutes:一级缓存分钟数; m2CacheMinutes:二级缓存分钟数。")]
        public void SetServiceCacheTime(long sid, int m1CacheMinutes, int m2CacheMinutes)
        {
            string sqlPattern = @"insert into [GW_Proxy_CacheContorl]([ServiceID], [MemoryCacheTime], [IsolatedCacheTime], [Created], [Modified]) values({0}, {1}, {2}, getdate(), getdate())";
            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ProxyServer"].ConnectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(string.Format("select count(*) from [GW_Proxy_CacheContorl] where ServiceID={0}", sid), conn);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                    {
                        cmd.CommandText = string.Format("update [GW_Proxy_CacheContorl] set MemoryCacheTime={1}, IsolatedCacheTime={2}, Modified=getdate() where ServiceID={0}",
                            sid, m1CacheMinutes, m2CacheMinutes);
                    }
                    else
                    {
                        cmd.CommandText = string.Format(sqlPattern, sid, m1CacheMinutes, m2CacheMinutes);
                    }
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                HttpContext.Current.Response.Write(ex.ToString());
                return;
            }
            ShowMessageRedirect(2, "缓存时间已配置，2秒后开始转向清除时间设置缓存数据...", "/API/ClearCache.ashx?ckey=CacheTimeSetting");
        }


        /// <summary>
        /// 复制Java版的接入服务器配置
        /// </summary>
        /// <param name="sid">The sid.</param>
        /// <param name="sourceDb">原始数据库</param>
        /// <param name="desDb">目标库</param>
        /// <returns></returns>
        [WebMethod(Description = "复制Java版的接入服务器配置，sid:业务编号，sourceDb:源数据库serviceconfig，desDb:目标数据库GatewayServer|ProxyServer。")]
        public string CopyServiceConfig(long sid, string sourceDb, string desDb)
        {
            if (string.IsNullOrEmpty(sourceDb)) sourceDb = "serviceconfig";
            if (string.IsNullOrEmpty(desDb)) desDb = "GatewayServer";

            string sqlPattern = @"insert into [{2}].[dbo].[GW_Proxy_SERVICE_CONFIG]([SERVICE_ID] ,[SERVICE_NAME] ,[SERVICE_VERSION] ,[SERVICE_ENCODE]
      ,[TXT_IN_ENCODE] ,[TXT_OUT_ENCODE] ,[FIRST_MODE] ,[FIRST_ACTION_TYPE]
      ,[FIRST_ACTION] ,[CLIENT_ROOT_URI] ,[SERVICE_URL] ,[CONNECT_TYPE]
      ,[LINK_URL_PREFIX] ,[RES_URL_PREFIX] ,[SERVICE_INDEX_URL] ,[SERVICE_REG_URL] 
      ,[SERVICE_HELP_URL] ,[SERVICE_DATABASE], [SERVICE_UserAssignFormat])
select top 1 [SERVICE_ID] ,[SERVICE_NAME] ,[SERVICE_VERSION] ,[SERVICE_ENCODE]
      ,[TXT_IN_ENCODE] ,[TXT_OUT_ENCODE] ,[FIRST_MODE] ,[FIRST_ACTION_TYPE]
      ,[FIRST_ACTION] ,[CLIENT_ROOT_URI] ,[SERVICE_URL] ,[CONNECT_TYPE]
      ,[LINK_URL_PREFIX] ,[RES_URL_PREFIX] ,[SERVICE_INDEX_URL] ,[SERVICE_REG_URL] 
      ,[SERVICE_HELP_URL] ,[SERVICE_DATABASE]
      ,N'select isnull((SELECT top 1 SOFTWARE_ID from GW_Proxy_SOFTWARE where IMEI=''?''),0) as CurrentUserID' 
   from [{1}].[dbo].[SERVICE_CONFIG] where Service_ID={0}";

            StringBuilder sb = new StringBuilder();
            try
            {
                using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultDB"].ConnectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(string.Format(sqlPattern, sid, sourceDb, desDb).Replace("?", "{IMEI}"), conn);
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                    conn.Close();
                    sb.AppendLine("ok");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine(ex.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// 设置特定类型的静态值为null
        /// </summary>
        /// <param name="typeFullName">类型全称</param>
        /// <param name="fieldName">字段名称</param>
        /// <returns></returns>
        [WebMethod(Description = "设置特定类型的静态值为null，typeFullName:类型全称，fieldName:字段名称。")]
        public string SetStaticInstanceNull(string typeFullName, string fieldName)
        {
            string strRet = "ok";
            try
            {
                Type targetType = Type.GetType(typeFullName, true);
                targetType.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(null, null);
            }
            catch (Exception exError)
            {
                strRet = exError.ToString();
            }
            return strRet;
        }

    }
}