using System;
using System.Xml.Serialization;
using CommonLib;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// Ease标签内容处理配置
    /// </summary>
    [Serializable]
    public class RefactContentConfig
    {
        /// <summary>
        /// 获取配置设置
        /// </summary>
        public static RefactContentConfig Instance
        {
            get
            {
                if (System.Configuration.ConfigurationManager.GetSection("RefactContentConfig") != null)
                {
                    return XmlSerializeSectionHandler.GetObject<RefactContentConfig>("RefactContentConfig");
                }
                else
                {
                    return new RefactContentConfig();
                }
            }
        }

        private RefactContentStep[] _steps = new RefactContentStep[0];
        /// <summary>
        /// Ease标签内容处理操作步骤设置
        /// </summary>
        /// <value>The react content steps.</value>
        [XmlElement(ElementName = "step")]
        public RefactContentStep[] RefactContentSteps
        {
            get { return _steps; }
            set { _steps = value; }
        }


    }
}
