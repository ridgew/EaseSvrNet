using System;
using System.Xml.Serialization;

namespace EaseServer.Configuration
{
    /// <summary>
    /// 所有会话配置项
    /// </summary>
    [Serializable]
    public class SessionConfig
    {
        private SessionConfigItem[] _settings = new SessionConfigItem[0];
        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        /// <value>The settings.</value>
        [XmlElement("add")]
        public SessionConfigItem[] Settings
        {
            get { return _settings; }
            set { _settings = value; }
        }

        /// <summary>
        /// 配置项是否是只执行一次(Static)
        /// </summary>
        [XmlAttribute]
        public bool ConfigOnce { get; set; }

        /// <summary>
        /// 是否已应用配置
        /// </summary>
        [XmlIgnore]
        public bool HasConfiged { get; set; }
    }
}
