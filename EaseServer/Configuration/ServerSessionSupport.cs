using System;
using System.Configuration;
using System.Xml;
using System.Xml.Serialization;

namespace EaseServer.Configuration
{
    /// <summary>
    /// 服务会话支持配置
    /// </summary>
    [Serializable]
    public class ServerSessionSupport : IConfigurationSectionHandler
    {

        SessionUserAgent[] _agents = new SessionUserAgent[0];
        /// <summary>
        /// 访问代理配置
        /// </summary>
        [XmlArrayItem(ElementName = "ua")]
        public SessionUserAgent[] Agents
        {
            get { return _agents; }
            set { _agents = value; }
        }

        private ServerSession[] _items = new ServerSession[0];
        /// <summary>
        /// 获取或设置所有的支持会话类型
        /// </summary>
        [XmlElement(ElementName = "add")]
        public ServerSession[] SupportItems
        {
            get { return _items; }
            set { _items = value; }
        }

        bool _enableAspxHost = true;
        /// <summary>
        /// 获取或设置是否允许内置的Asp.NET服务(默认开启)
        /// </summary>
        [XmlAttribute(AttributeName = "aspx")]
        public bool EnableInternalAspxHost
        {
            get { return _enableAspxHost; }
            set { _enableAspxHost = value; }
        }

        bool _enableEmptyUserAgent = true;
        /// <summary>
        /// 获取或设置是否允许空的用户代理标识(默认允许)
        /// </summary>
        [XmlAttribute(AttributeName = "emptyUA")]
        public bool EnableEmptyUserAgent
        {
            get { return _enableEmptyUserAgent; }
            set { _enableEmptyUserAgent = value; }
        }

        bool _enableMixed = false;
        /// <summary>
        /// 是否允许混合主/被动会话模式(默认关闭，只支持主动式会话。)
        /// </summary>
        [XmlAttribute(AttributeName = "mixed")]
        public bool EnableMixedSession
        {
            get { return _enableMixed; }
            set { _enableMixed = value; }
        }

        /// <summary>
        /// 更新配置信息
        /// </summary>
        public void Refresh()
        {
            _instance = null;
        }

        private static ServerSessionSupport _instance = null;
        /// <summary>
        /// 获取配置实例信息
        /// </summary>
        /// <value>The instance.</value>
        public static ServerSessionSupport ConfigInstance
        {
            get
            {
                if (_instance == null)
                {
                    object sectionObj = System.Configuration.ConfigurationManager.GetSection("ServerSessionSupport");
                    if (sectionObj != null)
                    {
                        _instance = (ServerSessionSupport)sectionObj;
                    }
                    else
                    {
                        _instance = new ServerSessionSupport();
                    }
                }
                return _instance;
            }
        }

        #region IConfigurationSectionHandler 成员

        /// <summary>
        /// 创建配置节处理程序。
        /// </summary>
        /// <param name="parent">父对象。</param>
        /// <param name="configContext">配置上下文对象。</param>
        /// <param name="section">节 XML 节点。</param>
        /// <returns>创建的节处理程序对象。</returns>
        public object Create(object parent, object configContext, XmlNode section)
        {
            //XPathNavigator navigator = section.CreateNavigator();
            //string typeName = (string)navigator.Evaluate("string(@type)");
            //Type type = Type.GetType(typeName, true);

            Type type = typeof(ServerSessionSupport);
            XmlSerializer serializer = new XmlSerializer(type);
            return serializer.Deserialize(new XmlNodeReader(section));
        }

        #endregion
    }

}
