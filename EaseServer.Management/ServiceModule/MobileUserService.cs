using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Services;
using System.Web.Script.Services;
using EaseServer.Management.DataAccess;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System.Data;
using System.Data.SqlClient;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    ///手机用户管理服务
    /// </summary>
    [WebService(Name = "手机用户管理服务", Description = "提供手机用户信息管理、用户订单管理、用户收藏信息管理等服务")]
    public class MobileUserService : WebServiceBase
    {
        /// <summary>
        /// 手机用户信息列表请求
        /// </summary>
        /// <param name="pageIndex">当前请求页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <param name="SearchField">搜索字段数组</param>
        /// <param name="SearchCondition">搜索条件数组</param>
        /// <param name="SearchValue">搜索字段对应值数组</param>
        /// <returns>返回列表数据</returns>
        [Protocol("1.3.6.4.1"), WebMethod(Description = "手机用户信息列表请求")]
        [GenerateScriptType(typeof(MobileUserInfoForList), ScriptTypeId = "0")]
        public SvcPagingRecord<MobileUserInfoForList> GetMobileUserInfoForList(int pageIndex, int pageSize, string[] SearchField, int[] SearchCondition, object[] SearchValue)
        {
            string userTableName = "GW_Proxy_Software";
            string connKeyName = "DefaultDB";

            SvcPagingRecord<MobileUserInfoForList> result = new SvcPagingRecord<MobileUserInfoForList>
            {
                Protocol = "1.3.6.4.1",
                Status = 1,
                PageIndex = pageIndex,
                PageSize = pageSize,
                RecordCount = connKeyName.GetInfoForListRecordCount(SearchField, SearchCondition, SearchValue, userTableName)
            };

            string Fields = "SOFTWARE_ID,SERVICE_ID,DEVICE_ID,CLIENT_VERSION,FIRST_VISIT_TIME,IMEI,IMSI,MSID,USER_NAME,USER_SEX,USER_AGE,USER_CARD_TYPE,USER_ID_CARD,USER_ADDR";
            DataTable dt = connKeyName.GetInfoForListDataTable(pageIndex, pageSize, SearchField, SearchCondition, SearchValue,
                userTableName, Fields,
                "Order By SOFTWARE_ID Desc");

            result.Data = (from dr in dt.AsEnumerable()
                           select new MobileUserInfoForList
                           {
                               SOFTWARE_ID = dr.Field<long>("SOFTWARE_ID"),
                               SERVICE_ID = dr.Field<long>("SERVICE_ID"),
                               DEVICE_ID = dr["DEVICE_ID"] == DBNull.Value ? 0L : dr.Field<long>("DEVICE_ID"),
                               CLIENT_VERSION = dr["CLIENT_VERSION"] == DBNull.Value ? 0L : dr.Field<long>("CLIENT_VERSION"),
                               FIRST_VISIT_TIME = dr.Field<string>("FIRST_VISIT_TIME"),
                               IMEI = dr.Field<string>("IMEI"),
                               IMSI = dr.Field<string>("IMSI"),
                               MSID = dr.Field<string>("MSID"),
                               USER_NAME = dr.Field<string>("USER_NAME"),
                               USER_SEX = dr["USER_SEX"] == DBNull.Value ? (byte)0 : dr.Field<byte>("USER_SEX"),
                               USER_ADDR = dr.Field<string>("USER_ADDR"),
                               USER_AGE = dr["USER_AGE"] == DBNull.Value ? (short)0 : dr.Field<short>("USER_AGE"),
                               USER_CARD_TYPE = dr["USER_CARD_TYPE"] == DBNull.Value ? (byte)0 : dr.Field<byte>("USER_CARD_TYPE"),
                               USER_ID_CARD = dr.Field<string>("USER_ID_CARD")
                           }).ToArray();

            return result;
        }

        /// <summary>
        /// 手机用户日志列表请求
        /// </summary>
        /// <param name="pageIndex">当前请求页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <param name="SearchField">搜索字段数组</param>
        /// <param name="SearchCondition">搜索条件数组</param>
        /// <param name="SearchValue">搜索字段对应值数组</param>
        /// <returns>返回列表数据</returns>
        [Protocol("1.3.6.4.2"), WebMethod(Description = "手机用户日志列表请求")]
        [GenerateScriptType(typeof(MobileUserLogForList), ScriptTypeId = "0")]
        public SvcPagingRecord<MobileUserLogForList> GetMobileUserLogForList(int pageIndex, int pageSize, string[] SearchField, int[] SearchCondition, object[] SearchValue)
        {
            string userTableName = "GW_Proxy_LOG_PV";
            string connKeyName = "DefaultDB";

            SvcPagingRecord<MobileUserLogForList> result = new SvcPagingRecord<MobileUserLogForList>
            {
                Protocol = "1.3.6.4.2",
                Status = 1,
                PageIndex = pageIndex,
                PageSize = pageSize,
                RecordCount = connKeyName.GetInfoForListRecordCount(SearchField, SearchCondition, SearchValue, userTableName)
            };

            string Fields = "ID, SOFTWARE_ID, LocalEndpoint, RemoteEndpoint, SERVICE_ID, VISIT_TIME, KEY_URL, REAL_URL, HTML_TIME, PARSE_TIME, ReceiveByteLength, SendByteLength, StatusCode, Message, CacheRate, Protocol";
            DataTable dt = connKeyName.GetInfoForListDataTable(pageIndex, pageSize, SearchField, SearchCondition, SearchValue,
                userTableName, Fields,
                "Order By ID Desc");

            result.Data = (from dr in dt.AsEnumerable()
                           select new MobileUserLogForList
                           {
                               ID = dr.Field<long>("ID"),
                               SERVICE_ID = dr["SERVICE_ID"] == DBNull.Value ? 0 : dr.Field<decimal>("SERVICE_ID"),
                               LocalEndpoint = dr.Field<string>("LocalEndpoint"),
                               RemoteEndPoint = dr.Field<string>("RemoteEndpoint"),
                               CacheRate = dr.Field<string>("CacheRate"),
                               SOFTWARE_ID = dr["SOFTWARE_ID"] == DBNull.Value ? 0 : dr.Field<decimal>("SOFTWARE_ID"),
                               VISIT_TIME = dr.Field<string>("VISIT_TIME"),
                               KEY_URL = dr.Field<string>("KEY_URL"),
                               REAL_URL = dr.Field<string>("REAL_URL"),
                               HTML_TIME = dr["HTML_TIME"] == DBNull.Value ? 0 : dr.Field<Decimal>("HTML_TIME"),
                               PARSE_TIME = dr["PARSE_TIME"] == DBNull.Value ? 0 : dr.Field<decimal>("PARSE_TIME"),
                               ReceiveByteLength = dr.Field<string>("ReceiveByteLength"),
                               SendByteLength = dr.Field<string>("SendByteLength"),
                               StatusCode = dr.Field<string>("StatusCode"),
                               Protocol = dr.Field<string>("Protocol"),
                               Message = dr.Field<string>("Message")
                           }).ToArray();

            return result;
        }



        /// <summary>
        /// 由ID获取手机用户信息
        /// </summary>
        /// <param name="SOFTWARE_ID">用户编号</param>
        /// <returns>返回信息</returns>
        [Protocol("1.3.6.4.4"), WebMethod(Description = "由ID获取手机用户信息")]
        [GenerateScriptType(typeof(MobileUserInfoForList), ScriptTypeId = "0")]
        public SvcSingleRecord<MobileUserInfoForList> GetMobileUserInfoById(long SOFTWARE_ID)
        {
            SqlCommand cmd = new SqlCommand(@"Select SOFTWARE_ID,SERVICE_ID,DEVICE_ID,CLIENT_VERSION,FIRST_VISIT_TIME,IMEI,
            IMSI,MSID,USER_NAME,USER_SEX,USER_AGE,USER_CARD_TYPE,USER_ID_CARD,USER_ADDR,REMOTE_IP,UserAgent From GW_Proxy_Software Where SOFTWARE_ID = @ID");
            cmd.Parameters.Add(new SqlParameter("ID", SOFTWARE_ID));
            DataTable dt = DatabaseFactory.CreateDatabase().ExecuteDataSet(cmd).Tables[0];
            cmd.Dispose();

            int uNum = dt.Rows.Count;
            var _tmpData = from dr in dt.AsEnumerable()
                           select new MobileUserInfoForList
                           {
                               SOFTWARE_ID = dr.Field<long>("SOFTWARE_ID"),
                               SERVICE_ID = dr.Field<long>("SERVICE_ID"),
                               DEVICE_ID = dr["DEVICE_ID"] == DBNull.Value ? 0L : dr.Field<long>("DEVICE_ID"),
                               CLIENT_VERSION = dr["CLIENT_VERSION"] == DBNull.Value ? 0L : dr.Field<long>("CLIENT_VERSION"),
                               FIRST_VISIT_TIME = dr.Field<string>("FIRST_VISIT_TIME"),
                               IMEI = dr.Field<string>("IMEI"),
                               IMSI = dr.Field<string>("IMSI"),
                               MSID = dr.Field<string>("MSID"),
                               USER_NAME = dr.Field<string>("USER_NAME"),
                               USER_SEX = dr.Field<byte>("USER_SEX"),
                               USER_ADDR = dr.Field<string>("USER_ADDR"),
                               USER_AGE = dr["USER_AGE"] == DBNull.Value ? (short)0 : dr.Field<short>("USER_AGE"),
                               USER_CARD_TYPE = dr.Field<byte>("USER_CARD_TYPE"),
                               USER_ID_CARD = dr.Field<string>("USER_ID_CARD"),
                               REMOTE_IP = dr.Field<string>("REMOTE_IP"),
                               UserAgent = dr.Field<string>("UserAgent")
                           };

            MobileUserInfoForList uInfo = uNum == 0 ? null : _tmpData.Single();
            return new SvcSingleRecord<MobileUserInfoForList>
            {
                Protocol = "1.3.6.4.4",
                Status = uInfo == null ? 0 : 1,
                Data = uInfo
            };
        }

    }

    /// <summary>
    /// 访问日志
    /// </summary>
    [Serializable]
    public class MobileUserLogForList
    {
        /// <summary>
        /// 日志序号
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        /// 用户标识号
        /// </summary>
        public decimal SOFTWARE_ID { get; set; }

        /// <summary>
        /// 业务编号
        /// </summary>
        public decimal SERVICE_ID { get; set; }

        /// <summary>
        /// 时间格式示例:20090404111158
        /// </summary>
        public string VISIT_TIME { get; set; }

        /// <summary>
        /// 原始请求地址
        /// </summary>
        public string KEY_URL { get; set; }

        /// <summary>
        /// 实际地址
        /// </summary>
        public string REAL_URL { get; set; }

        /// <summary>
        /// EASE代码处理时间？
        /// </summary>
        public decimal HTML_TIME { get; set; }

        /// <summary>
        /// 总共解析时间？
        /// </summary>
        public decimal PARSE_TIME { get; set; }

        #region 2010-11-19 添加
        /// <summary>
        /// 接收到的字节长度
        /// </summary>
        public string ReceiveByteLength { get; set; }

        /// <summary>
        /// 返回的数据字节长度
        /// </summary>
        public string SendByteLength { get; set; }

        /// <summary>
        /// 服务器响应码(1010:200)
        /// </summary>
        public string StatusCode { get; set; }

        /// <summary>
        /// 备注(比如异常消息等)
        /// </summary>
        public string Message { get; set; }

        #endregion

        #region 2010-11-30添加
        /// <summary>
        /// 远程连接端点
        /// </summary>
        public string RemoteEndPoint { get; set; }
        #endregion

        #region 2010-12-1添加
        /// <summary>
        /// 接入缓存命中描述
        /// </summary>
        public string CacheRate { get; set; }
        #endregion

        #region 2010-12-22添加
        /// <summary>
        /// 易致协议名称
        /// </summary>
        public string Protocol { get; set; }
        #endregion

        #region 2011-4-27添加
        /// <summary>
        /// 本地服务连接端点
        /// </summary>
        public string LocalEndpoint { get; set; }
        #endregion
    }

    #region 手机用户基本信息类
    /// <summary>
    /// 手机用户基本信息类
    /// </summary>
    [Serializable]
    public class MobileUserInfoForList
    {
        /// <summary>
        /// 软件ID，同时作为用户的唯一标示
        /// </summary>
        public long SOFTWARE_ID { get; set; }

        /// <summary>
        /// 应用ID
        /// </summary>
        public long SERVICE_ID { get; set; }

        /// <summary>
        /// 终端ID
        /// </summary>
        public long DEVICE_ID { get; set; }

        /// <summary>
        /// 主程序版本号
        /// </summary>
        public long CLIENT_VERSION { get; set; }

        /// <summary>
        /// 首次访问时间
        /// </summary>
        public string FIRST_VISIT_TIME { get; set; }

        /// <summary>
        /// SIM卡识别码
        /// </summary>
        public string IMEI { get; set; }

        /// <summary>
        /// 手机机身识别码
        /// </summary>
        public string IMSI { get; set; }

        /// <summary>
        /// 用户手机号码
        /// </summary>
        public string MSID { get; set; }

        /// <summary>
        /// 用户姓名
        /// </summary>
        public string USER_NAME { get; set; }

        /// <summary>
        /// 用户性别，0男1女
        /// </summary>
        public byte USER_SEX { get; set; }

        /// <summary>
        /// 用户年龄
        /// </summary>
        public short USER_AGE { get; set; }

        /// <summary>
        /// 证件类型，1：身份证，2：护照，3：军官证，4：其他
        /// </summary>
        public byte USER_CARD_TYPE { get; set; }

        /// <summary>
        /// 身份证号码
        /// </summary>
        public string USER_ID_CARD { get; set; }

        /// <summary>
        /// 用户地址
        /// </summary>
        public string USER_ADDR { get; set; }

        /// <summary>
        /// 手机用户代理
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// 远程IP地址
        /// </summary>
        public string REMOTE_IP { get; set; }
    }
    #endregion
}
