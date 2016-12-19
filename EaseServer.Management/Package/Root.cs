using System;
using System.Web;
using System.Xml.Serialization;
using CommonLib;
using System.Xml;

namespace EaseServer.Management.Package
{
    /// <summary>
    /// 资源、组件包根
    /// </summary>
    [Serializable]
    public class Root
    {
        /// <summary>
        /// 所有组件
        /// </summary>
        public Component[] Compoments
        {
            get;
            set;
        }

        /// <summary>
        /// 修改更新时间
        /// </summary>
        [XmlAttribute]
        public uint ModifiedDateTime { get; set; }


        [XmlAttribute]
        public long Reversion { get; set; }

        /// <summary>
        /// 基础目录
        /// </summary>
        [XmlAttribute]
        public string BaseDirectory { get; set; }

        /// <summary>
        /// 打包数据内容
        /// </summary>
        [XmlIgnore]
        public bool PackageData { get; set; }

        /// <summary>
        /// 远程同步基础网址
        /// </summary>
        [XmlAttribute]
        public string SynRemoteUrl { get; set; }


        static Root _rootInstance = null;
        /// <summary>
        /// 资源包的根目录配置实例
        /// </summary>
        public static Root RootInstance
        {
            get
            {
                if (_rootInstance == null) Reload();
                return _rootInstance;
            }
        }

        /// <summary>
        /// 刷新静态实例
        /// </summary>
        public static void Reload()
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(HttpContext.Current.Server.MapPath("/App_Data/package.config"));
            _rootInstance = xDoc.GetObject<Root>();
        }

        /// <summary>
        /// 同步资源包的根目录
        /// </summary>
        public static void SynRoot()
        {
            if (_rootInstance == null)
            {
                _rootInstance = new Root();
            }
            _rootInstance.GetXmlDoc().Save(HttpContext.Current.Server.MapPath("/App_Data/package.config"));
        }

    }

}
