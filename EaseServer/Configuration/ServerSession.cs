using System;
using System.Xml.Serialization;

namespace EaseServer.Configuration
{
    /// <summary>
    /// 服务端会话支持
    /// </summary>
    [Serializable]
    public class ServerSession
    {
        /// <summary>
        /// 获取或设置会话标识
        /// </summary>
        [XmlAttribute(AttributeName = "id")]
        public string Identity { get; set; }

        /// <summary>
        /// 获取或设置实现类型
        /// </summary>
        [XmlAttribute(AttributeName = "type")]
        public string ImplementTypeName { get; set; }

        /// <summary>
        /// 获取或设置运行时类型
        /// </summary>
        [XmlIgnore]
        public Type RuntimeType { get; set; }

        private bool _enable = true;
        /// <summary>
        /// 获取或设置当前配置是否可用
        /// </summary>
        [XmlAttribute(AttributeName = "enable")]
        public bool Enable
        {
            get { return _enable; }
            set { _enable = value; }
        }

        /// <summary>
        /// 获取或设置是否是响应式处理模块
        /// </summary>
        [XmlAttribute]
        public bool IsResponse { get; set; }

        /// <summary>
        /// 获取或设置当前会话的相关配置
        /// </summary>
        public SessionConfig Config { get; set; }
    }
}
