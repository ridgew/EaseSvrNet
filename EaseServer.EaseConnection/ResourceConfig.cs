using System;
using Gwsoft.SharpOrm;
using Gwsoft.SharpOrm.Config;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 资源配置
    /// </summary>
    [Serializable]
    [BindTable("ProxyServer", "RESOURCE_CONFIG")]
    public class ResourceConfig : TableEntry
    {
        /// <summary>
        /// 
        /// </summary>
        [PrimaryKey]
        [Identity]
        [Nullable(false)]
        public long RES_ID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public decimal SERVICE_ID { get; set; }

        /// <summary>
        /// 0－页面
        ///    1－资源文件
        /// </summary>
        public decimal RES_TYPE { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [MaxLength(300)]
        public string KEY_URL { get; set; }

        /// <summary>
        /// http://.....
        /// </summary>
        [MaxLength(300)]
        public string RES_URL { get; set; }

        /// <summary>
        /// file:///....
        /// </summary>
        [MaxLength(300)]
        public string RES_CLIENT_URI { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public decimal VERSION { get; set; }

    }

}
