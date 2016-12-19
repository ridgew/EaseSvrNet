using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Services;
using System.Web.Script.Services;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    ///接入服务器数据管理
    /// </summary>
    [WebService(Name = "接入服务器数据管理", Description = "接入服务器业务配置管理")]
    public class GatewayServer : WebServiceBase
    {
        [Protocol("1.3.99.1.4"), WebMethod(Description = "获取业务配置信息列表")]
        [GenerateScriptType(typeof(SvcMultiRecord<EntServerTreeItem>), ScriptTypeId = "0")]
        public SvcMultiRecord<EntServerTreeItem> GetList()
        {
            string[] arrDB = "SYS_EnterSrvDB".GetSysParam<string>().Split('|');

            using (SqlConnection conn = new SqlConnection("Server=" + arrDB[0] + ";User ID=" + arrDB[2] + ";Password=" + arrDB[3] + ";Database=" + arrDB[1] + ""))
            {
                DataTable dt = new DataTable();
                SqlDataAdapter addpter = new SqlDataAdapter(@"select SERVICE_ID,SERVICE_NAME from GW_Proxy_SERVICE_CONFIG order by Service_ID asc", conn);
                addpter.Fill(dt);

                return new SvcMultiRecord<EntServerTreeItem>
                {
                    Protocol = "1.3.99.1.4",
                    Status = 1,
                    Message = "获取接入服务器配置列表",
                    Data = (from p in dt.AsEnumerable()
                            select new EntServerTreeItem
                            {
                                SERVICE_ID = p.Field<decimal>("SERVICE_ID"),
                                SERVICE_NAME = p.Field<string>("SERVICE_NAME")
                            }
                     ).ToArray()
                };
            }
        }

        [Protocol("1.3.99.1.3"), WebMethod(Description = "获取业务配置信息")]
        [GenerateScriptType(typeof(SvcSingleRecord<EntServer>), ScriptTypeId = "0")]
        public SvcSingleRecord<EntServer> Get(int serviceid)
        {
            string[] arrDB = "SYS_EnterSrvDB".GetSysParam<string>().Split('|');

            using (SqlConnection conn = new SqlConnection("Server=" + arrDB[0] + ";User ID=" + arrDB[2] + ";Password=" + arrDB[3] + ";Database=" + arrDB[1] + ""))
            {
                DataTable dt = new DataTable();
                SqlDataAdapter addpter = new SqlDataAdapter(@"select top 1 S.*, isnull(C.MemoryCacheTime,0) as MemoryCacheTime, isNull(C.IsolatedCacheTime,0) as IsolatedCacheTime, C.SplitRate, C.CacheMode from GW_Proxy_SERVICE_CONFIG S 
  left join GW_Proxy_CacheContorl C on S.SERVICE_ID = C.ServiceID where S.SERVICE_ID=" + serviceid, conn);
                addpter.Fill(dt);

                int fNum = dt.Rows.Count;
                var _tmpData = from p in dt.AsEnumerable()
                               select new EntServer
                               {
                                   SERVICE_ID = p.Field<decimal>("SERVICE_ID"),
                                   SERVICE_NAME = p.Field<string>("SERVICE_NAME"),
                                   SERVICE_VERSION = p.Field<decimal>("SERVICE_VERSION"),
                                   SERVICE_ENCODE = p.Field<decimal>("SERVICE_ENCODE"),
                                   TXT_IN_ENCODE = p.Field<decimal>("TXT_IN_ENCODE"),
                                   TXT_OUT_ENCODE = p.Field<decimal>("TXT_OUT_ENCODE"),
                                   FIRST_MODE = p.Field<decimal>("FIRST_MODE"),
                                   FIRST_ACTION_TYPE = p.Field<decimal>("FIRST_ACTION_TYPE"),
                                   FIRST_ACTION = p.Field<string>("FIRST_ACTION"),
                                   CLIENT_ROOT_URI = p.Field<string>("CLIENT_ROOT_URI"),
                                   SERVICE_URL = p.Field<string>("SERVICE_URL"),
                                   CONNECT_TYPE = p.Field<decimal>("CONNECT_TYPE"),
                                   LINK_URL_PREFIX = p.Field<string>("LINK_URL_PREFIX"),
                                   RES_URL_PREFIX = p.Field<string>("RES_URL_PREFIX"),
                                   SERVICE_INDEX_URL = p.Field<string>("SERVICE_INDEX_URL"),
                                   SERVICE_REG_URL = p.Field<string>("SERVICE_REG_URL"),
                                   SERVICE_HELP_URL = p.Field<string>("SERVICE_HELP_URL"),
                                   SERVICE_DATABASE = p.Field<string>("SERVICE_DATABASE"),
                                   SERVICE_UserAssignFormat = p.Field<string>("SERVICE_UserAssignFormat"),
                                   MemoryCacheTime = p.Field<int>("MemoryCacheTime"),
                                   IsolatedCacheTime = p.Field<int>("IsolatedCacheTime"),
                                   PageParamProcess = p["PageParamProcess"] == DBNull.Value ? (byte)1 : Convert.ToByte(p["PageParamProcess"]),
                                   CacheMode = p["CacheMode"] == DBNull.Value ? (byte)0 : Convert.ToByte(p["CacheMode"]),
                                   SplitRate = p["SplitRate"] == DBNull.Value ? 0.50f : Convert.ToSingle(p["SplitRate"])
                               };

                return new SvcSingleRecord<EntServer>
                {
                    Message = fNum == 0 ? "没有对应的数据，请检查！" : "获取数据成功",
                    Status = fNum == 0 ? -1 : 1,
                    Protocol = "1.3.99.1.3",
                    Data = fNum == 0 ? null : _tmpData.Single<EntServer>()
                };
            }
        }

        [Protocol("1.3.99.1.1"), WebMethod(Description = "修改业务配置信息")]
        [GenerateScriptType(typeof(SvcSingleRecord<long>), ScriptTypeId = "0")]
        public SvcSingleRecord<long> Edit(int SERVICE_ID, string SERVICE_NAME, int SERVICE_VERSION, int SERVICE_ENCODE,
            int TXT_IN_ENCODE, int TXT_OUT_ENCODE, int FIRST_MODE, int FIRST_ACTION_TYPE, string FIRST_ACTION, string CLIENT_ROOT_URI,
            string SERVICE_URL, int CONNECT_TYPE, string LINK_URL_PREFIX, string RES_URL_PREFIX, string SERVICE_INDEX_URL, string SERVICE_REG_URL, string SERVICE_HELP_URL,
            string SERVICE_DATABASE, int oldid, string SERVICE_UserAssignFormat, int MemoryCacheTime, int IsolatedCacheTime, byte cacheMode, float splitRate, byte pageParamProcess)
        {
            object[] datas = new object[] { SERVICE_ID, SERVICE_NAME, SERVICE_VERSION, SERVICE_ENCODE, TXT_IN_ENCODE, TXT_OUT_ENCODE, FIRST_MODE,
                FIRST_ACTION_TYPE, FIRST_ACTION, CLIENT_ROOT_URI, SERVICE_URL, CONNECT_TYPE, LINK_URL_PREFIX, RES_URL_PREFIX,
                SERVICE_INDEX_URL, SERVICE_REG_URL, SERVICE_HELP_URL, SERVICE_DATABASE, oldid, SERVICE_UserAssignFormat, pageParamProcess };

            for (short i = 0; i < datas.Length; i++)
            {
                datas[i] = datas[i].ToString().Replace("'", "''").Replace("@", "");
            }

            string[] arrDB = "SYS_EnterSrvDB".GetSysParam<string>().Split('|');

            using (SqlConnection conn = new SqlConnection("Server=" + arrDB[0] + ";User ID=" + arrDB[2] + ";Password=" + arrDB[3] + ";Database=" + arrDB[1] + ""))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                try
                {
                    if (SERVICE_ID == oldid)
                    {
                        cmd.CommandText = string.Format(@"update GW_Proxy_SERVICE_CONFIG set SERVICE_ID='{0}', 
SERVICE_NAME='{1}',SERVICE_VERSION='{2}',SERVICE_ENCODE='{3}',TXT_IN_ENCODE='{4}',TXT_OUT_ENCODE='{5}',FIRST_MODE='{6}',FIRST_ACTION_TYPE='{7}', 
FIRST_ACTION='{8}',CLIENT_ROOT_URI='{9}',SERVICE_URL='{10}',CONNECT_TYPE='{11}',LINK_URL_PREFIX='{12}',RES_URL_PREFIX='{13}',SERVICE_INDEX_URL='{14}', 
SERVICE_REG_URL='{15}',SERVICE_HELP_URL='{16}',SERVICE_DATABASE='{17}', SERVICE_UserAssignFormat='{19}', PageParamProcess={20} 
where SERVICE_ID='{18}'", datas);
                    }
                    else
                    {
                        cmd.CommandText = string.Format(@"insert into GW_Proxy_SERVICE_CONFIG(SERVICE_ID, SERVICE_NAME, SERVICE_VERSION, SERVICE_ENCODE,
TXT_IN_ENCODE, TXT_OUT_ENCODE, FIRST_MODE, FIRST_ACTION_TYPE, FIRST_ACTION, 
CLIENT_ROOT_URI, SERVICE_URL, CONNECT_TYPE, LINK_URL_PREFIX, RES_URL_PREFIX, SERVICE_INDEX_URL, 
SERVICE_REG_URL, SERVICE_HELP_URL, SERVICE_DATABASE, SERVICE_UserAssignFormat, PageParamProcess) 
values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}', '{19}', {20})", datas);
                    }

                    cmd.ExecuteNonQuery();

                    #region 更新或插入缓存配置
                    cmd.CommandText = "select count(*) from [GW_Proxy_CacheContorl] where ServiceID=" + SERVICE_ID;
                    int cacheExists = Convert.ToInt32(cmd.ExecuteScalar());
                    cmd.CommandText = getCacheSQL((cacheExists > 0) ? false : true, SERVICE_ID, MemoryCacheTime, IsolatedCacheTime, cacheMode, splitRate);
                    cmd.ExecuteNonQuery();
                    #endregion

                    return new SvcSingleRecord<long>
                    {
                        Message = "更新接入服务器配置成功",
                        Status = 1,
                        Protocol = "1.3.99.1.1",
                        Data = 1
                    };
                }
                catch (Exception ex)
                {
                    return new SvcSingleRecord<long>
                    {
                        Message = "更新接入服务器配置失败" + ex.ToString(),
                        Status = 0,
                        Protocol = "1.3.99.1.1",
                        Data = 0
                    };
                }
                finally
                {
                    cmd.Dispose();
                    conn.Close();
                }
            }
        }

        [Protocol("1.3.99.1.2"), WebMethod(Description = "删除业务配置信息")]
        public SvcSingleRecord<long> Delete(int serviceid)
        {
            string[] arrDB = "SYS_EnterSrvDB".GetSysParam<string>().Split('|');

            using (SqlConnection conn = new SqlConnection("Server=" + arrDB[0] + ";User ID=" + arrDB[2] + ";Password=" + arrDB[3] + ";Database=" + arrDB[1] + ""))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("delete from GW_Proxy_SERVICE_CONFIG where SERVICE_ID=" + serviceid, conn);
                cmd.ExecuteNonQuery();
                cmd.CommandText = "delete from GW_Proxy_CacheContorl where ServiceID=" + serviceid;
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
            return new SvcSingleRecord<long>
            {
                Message = "删除接入服务器配置",
                Status = 1,
                Protocol = "1.3.99.1.2",
                Data = 1
            };
        }

        string getCacheSQL(bool isInsert, int sid, int mm, int im, byte cacheMode, float splitRate)
        {
            if (isInsert)
            {
                return string.Format("insert into [GW_Proxy_CacheContorl]([ServiceID], [MemoryCacheTime] ,[IsolatedCacheTime], [Created] ,[Modified], [CacheMode], [SplitRate]) values({0}, {1}, {2} , getdate(), getdate(), {3}, {4})", sid, mm, im, cacheMode, splitRate);
            }
            else
            {
                return string.Format("update [GW_Proxy_CacheContorl] set MemoryCacheTime={1}, IsolatedCacheTime={2}, Modified=getdate(),CacheMode={3},SplitRate={4}  where ServiceID={0}", sid, mm, im, cacheMode, splitRate);
            }
        }

        [Protocol("1.3.99.1.0"), WebMethod(Description = "添加业务配置信息")]
        public SvcSingleRecord<long> Add(string SERVICE_NAME, int SERVICE_VERSION, int SERVICE_ENCODE, int TXT_IN_ENCODE, int TXT_OUT_ENCODE, int FIRST_MODE,
            int FIRST_ACTION_TYPE, string FIRST_ACTION, string CLIENT_ROOT_URI, string SERVICE_URL, int CONNECT_TYPE, string LINK_URL_PREFIX, string RES_URL_PREFIX,
            string SERVICE_INDEX_URL, string SERVICE_REG_URL, string SERVICE_HELP_URL, string SERVICE_DATABASE, string SERVICE_UserAssignFormat, int MemoryCacheTime, int IsolatedCacheTime, byte cacheMode, float splitRate)
        {
            string[] arrDB = "SYS_EnterSrvDB".GetSysParam<string>().Split('|');

            using (SqlConnection conn = new SqlConnection("Server=" + arrDB[0] + ";User ID=" + arrDB[2] + ";Password=" + arrDB[3] + ";Database=" + arrDB[1] + ""))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("select count(0) from GW_Proxy_SERVICE_CONFIG", conn);
                int icount = Convert.ToInt32(cmd.ExecuteScalar());
                if (icount == 0)
                {
                    icount = 1;
                }
                else
                {
                    cmd.CommandText = "select max(SERVICE_ID) from GW_Proxy_SERVICE_CONFIG";
                    icount = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                }

                object[] datas = new object[] { icount, SERVICE_NAME, SERVICE_VERSION, SERVICE_ENCODE, TXT_IN_ENCODE, TXT_OUT_ENCODE,
                    FIRST_MODE, FIRST_ACTION_TYPE, FIRST_ACTION, CLIENT_ROOT_URI, SERVICE_URL, CONNECT_TYPE, LINK_URL_PREFIX, RES_URL_PREFIX, SERVICE_INDEX_URL,
                    SERVICE_REG_URL, SERVICE_HELP_URL, SERVICE_DATABASE, SERVICE_UserAssignFormat };
                for (short i = 0; i < datas.Length; i++)
                {
                    datas[i] = datas[i].ToString().Replace("'", "''").Replace("@", "");
                }

                try
                {
                    cmd.CommandText = string.Format(@"insert into GW_Proxy_SERVICE_CONFIG(SERVICE_ID, SERVICE_NAME, SERVICE_VERSION, SERVICE_ENCODE,
TXT_IN_ENCODE, TXT_OUT_ENCODE, FIRST_MODE, FIRST_ACTION_TYPE, FIRST_ACTION, 
CLIENT_ROOT_URI, SERVICE_URL, CONNECT_TYPE, LINK_URL_PREFIX, RES_URL_PREFIX, SERVICE_INDEX_URL, 
SERVICE_REG_URL, SERVICE_HELP_URL, SERVICE_DATABASE, SERVICE_UserAssignFormat) 
values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}', '{18}')", datas);

                    cmd.ExecuteNonQuery();

                    cmd.CommandText = getCacheSQL(true, icount, MemoryCacheTime, IsolatedCacheTime, cacheMode, splitRate);
                    cmd.ExecuteNonQuery();

                    return new SvcSingleRecord<long>
                    {
                        Message = "添加接入服务器配置成功",
                        Status = 1,
                        Protocol = "1.3.99.1.0",
                        Data = 1
                    };
                }
                catch (Exception ex)
                {
                    return new SvcSingleRecord<long>
                    {
                        Message = "添加接入服务器配置失败:" + ex.Message,
                        Status = 0,
                        Protocol = "1.3.99.1.0",
                        Data = 0
                    };
                }
                finally
                {
                    cmd.Dispose();
                    conn.Close();
                }
            }

        }

    }

    [Serializable]
    [GenerateScriptType(typeof(EntServerTreeItem), ScriptTypeId = "0")]
    public class EntServerTreeItem
    {
        /// <summary>
        /// 服务编号
        /// </summary>
        public decimal SERVICE_ID { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        public string SERVICE_NAME { get; set; }
    }

    /// <summary>
    /// 接入服务器业务配置模型
    /// </summary>
    [Serializable]
    public class EntServer
    {
        /// <summary>
        /// 服务编号
        /// </summary>
        public decimal SERVICE_ID { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        public string SERVICE_NAME { get; set; }

        /// <summary>
        /// 服务版本
        /// </summary>
        public decimal SERVICE_VERSION { get; set; }

        /// <summary>
        /// 服务编码（0－UTF8 1-Unicode 2-GB231）
        /// </summary>
        public decimal SERVICE_ENCODE { get; set; }

        /// <summary>
        /// 输入文本编码（0－UTF8 1-Unicode 2-GB231）
        /// </summary>
        public decimal TXT_IN_ENCODE { get; set; }

        /// <summary>
        /// 输出文本编码（0－UTF8 1-Unicode 2-GB2312）
        /// </summary>
        public decimal TXT_OUT_ENCODE { get; set; }

        /// <summary>
        /// 0 - 不附带免责,客户端不执行指定操作
        ///    1 -  附带免责,客户端不执行指定操作
        ///    2 -  附带免责,客户端执行指定操作
        ///    3 -  不附带免责,客户端执行指定操作
        ///    
        /// </summary>
        public decimal FIRST_MODE { get; set; }

        /// <summary>
        /// 0－无任何操作
        ///    1－发送短信 
        ///    2－调用WAP浏览器
        ///    3－拨打电话
        ///    4－主程序存在更新，下载主程序
        /// </summary>
        public decimal FIRST_ACTION_TYPE { get; set; }

        /// <summary>
        /// 操作类型为0时不存在，长度为0
        ///    操作类型为1时为短信指令
        ///    操作类型为2时为WAP链接
        ///    操作类型为3时为电话号码
        ///    操作类型为4时为主程序下载链接
        /// </summary>
        public string FIRST_ACTION { get; set; }

        /// <summary>
        /// 客户端跟地址（file:///）
        /// </summary>
        public string CLIENT_ROOT_URI { get; set; }

        /// <summary>
        /// 远程应用基础地址
        /// </summary>
        public string SERVICE_URL { set; get; }

        /// <summary>
        /// 连接类型
        /// </summary>
        public decimal CONNECT_TYPE { get; set; }

        /// <summary>
        /// 远程连接前缀
        /// </summary>
        public string LINK_URL_PREFIX { get; set; }

        /// <summary>
        /// 远程资源前缀
        /// </summary>
        public string RES_URL_PREFIX { get; set; }

        /// <summary>
        /// 服务索引页（起始页）
        /// </summary>
        public string SERVICE_INDEX_URL { get; set; }

        /// <summary>
        /// 服务注册URL
        /// </summary>
        public string SERVICE_REG_URL { get; set; }

        /// <summary>
        /// 服务帮助URL
        /// </summary>
        public string SERVICE_HELP_URL { get; set; }

        /// <summary>
        /// 服务连接数据库
        /// </summary>
        public string SERVICE_DATABASE { get; set; }

        /// <summary>
        /// 用户分配定义SQL统计语句，值为0则新分配。
        /// </summary>
        public string SERVICE_UserAssignFormat { get; set; }

        /// <summary>
        /// 内存缓存分钟数
        /// </summary>
        public int MemoryCacheTime { get; set; }

        /// <summary>
        /// 磁盘缓存分钟数
        /// </summary>
        public int IsolatedCacheTime { get; set; }

        /// <summary>
        /// 缓存模块使用模式
        /// </summary>
        public byte CacheMode { get; set; }

        /// <summary>
        /// 一级缓存使用率
        /// </summary>
        public float SplitRate { get; set; }


        /// <summary>
        /// 页面参数处理
        /// </summary>
        public byte PageParamProcess { get; set; }

    }

}
