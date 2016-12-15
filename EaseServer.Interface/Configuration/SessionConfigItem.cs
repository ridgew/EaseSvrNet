using System;
using System.Xml.Serialization;

namespace EaseServer.Configuration
{
    /// <summary>
    /// 会话配置项
    /// </summary>
    [Serializable]
    public class SessionConfigItem
    {
        /// <summary>
        /// 获取或设置配置项名称
        /// </summary>
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置配置项的值
        /// </summary>
        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        /// <summary>
        /// 从属CLR类型
        /// </summary>
        [XmlAttribute(AttributeName = "type")]
        public string SubType { get; set; }
    }

}
