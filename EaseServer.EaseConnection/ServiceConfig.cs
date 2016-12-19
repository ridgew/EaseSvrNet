using System;
using Gwsoft.SharpOrm;
using Gwsoft.SharpOrm.Config;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 服务配置
    /// </summary>
    [Serializable]
    [BindTable("ProxyServer", "GW_Proxy_Service_Config")]
    public class ServiceConfig
        : TableEntry
    {
        /// <summary>
        /// 服务编号
        /// </summary>
        [PrimaryKey, Nullable(false)]
        public decimal SERVICE_ID { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        [MaxLength(50)]
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
        [MaxLength(300)]
        public string FIRST_ACTION { get; set; }

        /// <summary>
        /// 客户端根地址（file:///）
        /// </summary>
        [MaxLength(100)]
        public string CLIENT_ROOT_URI { get; set; }

        /// <summary>
        /// 远程应用基础地址
        /// </summary>
        [MaxLength(50)]
        public string SERVICE_URL { get; set; }

        /// <summary>
        /// 连接类型
        /// </summary>
        public decimal CONNECT_TYPE { get; set; }

        /// <summary>
        /// 远程连接前缀
        /// </summary>
        [MaxLength(300)]
        public string LINK_URL_PREFIX { get; set; }

        /// <summary>
        /// 远程资源前缀
        /// </summary>
        [MaxLength(300)]
        public string RES_URL_PREFIX { get; set; }

        /// <summary>
        /// 服务索引页（起始页）
        /// </summary>
        [MaxLength(300)]
        public string SERVICE_INDEX_URL { get; set; }

        /// <summary>
        /// 服务注册URL
        /// </summary>
        [MaxLength(300)]
        public string SERVICE_REG_URL { get; set; }

        /// <summary>
        /// 服务帮助URL
        /// </summary>
        [MaxLength(300)]
        public string SERVICE_HELP_URL { get; set; }

        /// <summary>
        /// 服务连接数据库
        /// </summary>
        [MaxLength(50)]
        [Nullable(false)]
        public string SERVICE_DATABASE { get; set; }

        /// <summary>
        /// 用户分配定义SQL统计语句，值为0则新分配。
        /// </summary>
        [MaxLength(1200)]
        public string SERVICE_UserAssignFormat { get; set; }

        /// <summary>
        /// 页面参数处理方式
        /// </summary>
        public byte PageParamProcess { get; set; }

    }

    /// <summary>
    /// URL参数处理类型
    /// </summary>
    public enum UrlParameterProcess : byte
    {
        /// <summary>
        /// 没有设置
        /// </summary>
        UnKnown = 0,
        /// <summary>
        /// 附加参数，并以GET方式发送HTTP请求。
        /// </summary>
        AppendGet = 1,
        /// <summary>
        /// 附加参数，并以POST方式发送HTTP请求。
        /// </summary>
        AppendPost = 2,
        /// <summary>
        /// 强制所有URL参数都为POST参数
        /// </summary>
        PostAll = 4
    }
}