using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace EaseServer.Management.Package
{
    /// <summary>
    /// 资源文件
    /// </summary>
    [Serializable]
    public class ResourceFile
    {
        [XmlAttribute]
        public ResourceType Type { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        [XmlAttribute]
        public string Version { get; set; }

        /// <summary>
        /// 修改更新时间
        /// </summary>
        [XmlAttribute]
        public uint ModifiedDateTime { get; set; }

        /// <summary>
        /// 文件长度
        /// </summary>
        [XmlAttribute]
        public ulong FileLength { get; set; }

        /// <summary>
        /// 文件哈希标识码(SHA-1)
        /// </summary>
        [XmlAttribute]
        public string HashCode { get; set; }

        /// <summary>
        /// 资源文件内容
        /// </summary>
        public byte[] Content { get; set; }

        /// <summary>
        /// 下级资源文件
        /// </summary>
        public ResourceFile[] Children { get; set; }

        /// <summary>
        /// 同步地址
        /// </summary>
        [XmlAttribute]
        public string SynPath { get; set; }
    }

    /// <summary>
    /// 资源文件类型
    /// </summary>
    [Serializable]
    public enum ResourceType
    { 
        /// <summary>
        /// 未知
        /// </summary>
        Unknown = 0,

        /// 网页文件
        /// </summary>
        HTML = 1,

        /// <summary>
        /// XML文件
        /// </summary>
        XML = 2,

        /// <summary>
        /// 脚本文件
        /// </summary>
        Javascript = 3,

        /// <summary>
        /// 样式表文件
        /// </summary>
        CSS,
        /// <summary>
        /// 图片文件
        /// </summary>
        Image,

        /// <summary>
        /// 程序集文件
        /// </summary>
        Assembly,

        /// <summary>
        /// 动态网页文件
        /// </summary>
        DynamicPage,

        /// <summary>
        /// 源代码
        /// </summary>
        SourceCode,

        /// <summary>
        /// 调试数据文件
        /// </summary>
        DebugDatabase,

        /// <summary>
        /// Web资源
        /// </summary>
        WebResource,

        /// <summary>
        /// 配置文件
        /// </summary>
        ConfigFile,

        /// <summary>
        /// 资源目录
        /// </summary>
        Directory
    }

    /// <summary>
    /// 文件引用
    /// </summary>
    [Serializable]
    public struct FileReference
    {
        /// <summary>
        /// 在包内的完整路径
        /// </summary>
        [XmlAttribute]
        public string PackageRootPath { get; set; }

        /// <summary>
        /// 是否可选引用
        /// </summary>
        [XmlAttribute]
        public bool Optional { get; set; }

        /// <summary>
        /// 引用时版本
        /// </summary>
        [XmlAttribute]
        public long Reversion { get; set; }

    }
}
