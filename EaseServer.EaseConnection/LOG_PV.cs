using System;
using Gwsoft.SharpOrm;
using Gwsoft.SharpOrm.Config;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 数据库日志
    /// </summary>
    [Serializable]
    [BindTable("ProxyServer", "GW_Proxy_LOG_PV")]
    public class LOG_PV : TableEntry
    {
        /// <summary>
        /// 日志主键
        /// </summary>
        [PrimaryKey, Identity, Nullable(false)]
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
        [MaxLength(14)]
        public string VISIT_TIME { get; set; }

        /// <summary>
        /// 原始请求地址
        /// </summary>
        [MaxLength(1500)]
        public string KEY_URL { get; set; }

        /// <summary>
        /// 实际地址
        /// </summary>
        [MaxLength(4000)]
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
        [MaxLength(20)]
        public string ReceiveByteLength { get; set; }

        /// <summary>
        /// 返回的数据字节长度
        /// </summary>
        [MaxLength(20)]
        public string SendByteLength { get; set; }

        /// <summary>
        /// 服务器响应码(1010:200)
        /// </summary>
        [MaxLength(20)]
        public string StatusCode { get; set; }

        /// <summary>
        /// 备注(比如异常消息等)
        /// </summary>
        [MaxLength(200)]
        public string Message { get; set; }

        #endregion

        #region 2010-11-30添加
        /// <summary>
        /// 远程连接终端地址
        /// </summary>
        [MaxLength(50)]
        public string RemoteEndpoint { get; set; }
        #endregion

        #region 2010-12-1添加
        /// <summary>
        /// 缓存命中字符描述
        /// </summary>
        [MaxLength(20)]
        public string CacheRate { get; set; }
        #endregion

        #region 2010-12-22添加
        /// <summary>
        /// 易致协议名称
        /// </summary>
        [MaxLength(20)]
        public string Protocol { get; set; }
        #endregion

        #region 2011-4-27添加
        /// <summary>
        /// 本地连接端点地址
        /// </summary>
        [MaxLength(50)]
        public string LocalEndpoint { get; set; }
        #endregion

    }
}