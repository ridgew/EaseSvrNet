using System;
using Gwsoft.SharpOrm.Config;
using Gwsoft.SharpOrm;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// EASE平台用户
    /// </summary>
    [Serializable]
    [BindTable("ProxyServer", "GW_Proxy_SoftWare")]
    public class EaseUser : TableEntry
    {
        /// <summary>
        /// 软件ID
        /// </summary>
        [PrimaryKey, Identity, Nullable(false)]
        public long SOFTWARE_ID { get; set; }

        /// <summary>
        /// 应用ID
        /// </summary>
        public long SERVICE_ID { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        public long DEVICE_ID { get; set; }

        /// <summary>
        /// 主程序版本号
        /// </summary>
        public long CLIENT_VERSION { get; set; }

        /// <summary>
        /// 首次访问时间
        /// </summary>
        [MaxLength(16)]
        public string FIRST_VISIT_TIME { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(20)]
        public string IMEI { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(20)]
        public string IMSI { get; set; }

        /// <summary>
        /// 用户手机号
        /// </summary>
        [MaxLength(20)]
        public string MSID { get; set; }

        /// <summary>
        /// 用户姓名
        /// </summary>
        [MaxLength(20)]
        public string USER_NAME { get; set; }

        /// <summary>
        /// 用户性别
        /// </summary>
        public byte USER_SEX { get; set; }

        /// <summary>
        /// 用户年龄
        /// </summary>
        public short USER_AGE { get; set; }

        /// <summary>
        /// 1：身份证，2：护照，3：军官证，4：其他
        /// </summary>
        public byte USER_CARD_TYPE { get; set; }

        /// <summary>
        /// 用户身份证号
        /// </summary>
        [MaxLength(50)]
        public string USER_ID_CARD { get; set; }

        /// <summary>
        /// 用户地址
        /// </summary>
        [MaxLength(200)]
        public string USER_ADDR { get; set; }

        /// <summary>
        /// 获取或设置浏览器UA信息
        /// </summary>
        [MaxLength(4000)]
        public string UserAgent { get; set; }

        /// <summary>
        /// 远程连接IP地址
        /// </summary>
        [MaxLength(200)]
        public string REMOTE_IP { get; set; }

        /// <summary>
        /// 原始注册标识
        /// </summary>
        [MaxLength(6)]
        public string RegionCode { get; set; }

        /// <summary>
        /// 省份标识
        /// </summary>
        public byte ProvinceId { get; set; }

        /// <summary>
        /// 原始注册时间
        /// </summary>
        public DateTime RegionDateCreated { get; set; }

        #region 2010-12-2添加
        /// <summary>
        /// 获取或设置业务交互Cookie信息
        /// </summary>
        [MaxLength(4000)]
        public string SessionCookie { get; set; }
        #endregion

    }
}
