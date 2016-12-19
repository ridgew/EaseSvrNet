using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace EaseServer.Management.Package
{
    /// <summary>
    /// 功能组件
    /// </summary>
    [Serializable]
    public class Component
    {
        /// <summary>
        /// 初始化 <see cref="Component"/> class.
        /// </summary>
        public Component()
        {
        }

        /// <summary>
        /// 初始化一个 <see cref="Component"/> class 实例。
        /// </summary>
        /// <param name="rootPkg">完整包引用</param>
        public Component(Root rootPkg)
        {
            _root = rootPkg;
        }

        Root _root;
        [XmlIgnore]
        public Root RootPackage
        {
            get { return _root; }
            set { _root = value; }
        }

        /// <summary>
        /// 组件标识
        /// </summary>
        [XmlAttribute]
        public Guid CompomentID { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        [XmlAttribute]
        public string Version { get; set; }

        /// <summary>
        /// 是否已使用
        /// </summary>
        [XmlAttribute]
        public bool Enabled { get; set; }

        /// <summary>
        /// 修改更新时间
        /// </summary>
        [XmlAttribute]
        public uint ModifiedDateTime { get; set; }

        /// <summary>
        /// 相关资源文件
        /// </summary>
        public ResourceFile[] Files { get; set; }

        /// <summary>
        /// 相关引用
        /// </summary>
        public ComponentReference[] References { get; set; }

        /// <summary>
        /// 子级模块
        /// </summary>
        public Component[] Children { get; set; }


        #region 静态辅助方法
        static ResourceType GetFromExtension(FileSystemInfo fInfo)
        {
            ResourceType unknownType = ResourceType.Unknown;
            switch (fInfo.Extension.ToLower())
            {
                case ".htm":
                case ".html":
                    unknownType = ResourceType.HTML;
                    break;

                case ".css":
                    unknownType = ResourceType.CSS;
                    break;

                case ".js":
                    unknownType = ResourceType.Javascript;
                    break;

                case ".xml":
                case ".xaml":
                    unknownType = ResourceType.XML;
                    break;

                case ".swf":
                case ".xap":
                case ".zip":
                case ".asax":
                case ".asa":
                    unknownType = ResourceType.WebResource;
                    break;

                case ".gif":
                case ".png":
                case ".jpg":
                case ".jpeg":
                    unknownType = ResourceType.Image;
                    break;

                case ".aspx":
                case ".asp":
                case ".ashx":
                case ".asmx":
                case ".ascx":
                    unknownType = ResourceType.DynamicPage;
                    break;

                case ".dll":
                case ".exe":
                    unknownType = ResourceType.Assembly;
                    break;

                case ".pdb":
                    unknownType = ResourceType.DebugDatabase;
                    break;

                case ".cs":
                case ".vb":
                    unknownType = ResourceType.SourceCode;
                    break;

                case ".config":
                    unknownType = ResourceType.ConfigFile;
                    break;

                default:
                    break;
            }
            return unknownType;
        }

        /// <summary>
        /// 从文件目录创建组件
        /// </summary>
        /// <param name="rootPkg">根对象引用</param>
        /// <param name="dirPath">目录地址</param>
        /// <param name="skipFilter">忽略文件的过滤器</param>
        /// <returns></returns>
        public static Component CreateFromDirectory(Root rootPkg, string dirPath, Func<FileSystemInfo, bool> skipFilter)
        {
            List<ResourceFile> fileList = new List<ResourceFile>();

            DirectoryInfo dInfo = new DirectoryInfo(dirPath);
            //fileList.Add(new ResourceFile { Name = dInfo.Name, Type = ResourceType.Directory, ModifiedDateTime = dInfo.LastWriteTime.DateTimeToDosTime() });

            CommonLib.HashProvider hashUtil = new CommonLib.HashProvider(CommonLib.HashProvider.ServiceProviderEnum.SHA1, Encoding.UTF8);
            foreach (FileSystemInfo fi in dInfo.GetFiles())
            {
                if (skipFilter != null && skipFilter(fi)) continue;

                byte[] fileBytes = File.ReadAllBytes(fi.FullName);
                fileList.Add(new ResourceFile
                {
                    Name = fi.Name,
                    SynPath = fi.FullName.Replace(rootPkg.BaseDirectory, ""),
                    FileLength = (ulong)fileBytes.LongLength,
                    ModifiedDateTime = fi.LastWriteTime.DateTimeToDosTime(),
                    Type = GetFromExtension(fi),
                    Content = (rootPkg.PackageData) ? fileBytes : null,
                    HashCode = hashUtil.ComputeHash(fileBytes)
                });
            }

            List<Component> childCptList = new List<Component>();
            foreach (FileSystemInfo fi in dInfo.GetDirectories())
            {
                if (skipFilter != null && skipFilter(fi)) continue;
                childCptList.Add(Component.CreateFromDirectory(rootPkg, fi.FullName, skipFilter));
            }

            return new Component(rootPkg)
            {
                CompomentID = Guid.NewGuid(),
                Name = dInfo.Name,
                ModifiedDateTime = DateTime.Now.DateTimeToDosTime(),
                Children = childCptList.ToArray(),
                Files = fileList.ToArray()
            };
        }

        /// <summary>
        /// 从文件创建组件
        /// </summary>
        /// <param name="rootPkg">根对象引用</param>
        /// <param name="type">组件文件类型</param>
        /// <param name="filePath">文件地址</param>
        /// <returns></returns>
        public static Component CreateFromFile(Root rootPkg, ResourceType type, string filePath)
        {
            FileInfo fInfo = new FileInfo(filePath);
            byte[] fileBytes = File.ReadAllBytes(fInfo.FullName);
            CommonLib.HashProvider hashUtil = new CommonLib.HashProvider(CommonLib.HashProvider.ServiceProviderEnum.SHA1, Encoding.UTF8);
            uint lastModified = fInfo.LastWriteTime.DateTimeToDosTime();
            return new Component(rootPkg)
            {
                CompomentID = Guid.NewGuid(),
                Name = fInfo.Name,
                ModifiedDateTime = lastModified,
                Files = new ResourceFile[] { new ResourceFile
                    {
                        Name = fInfo.Name,
                        SynPath = filePath.Replace(rootPkg.BaseDirectory, ""),
                        HashCode = hashUtil.ComputeHash(fileBytes), 
                        Content = (rootPkg.PackageData) ? fileBytes : null,
                        FileLength = (ulong)fileBytes.LongLength,
                        ModifiedDateTime = lastModified,
                        Type = type
                    }
                }
            };
        }

        /// <summary>
        /// 从远程URL地址创建
        /// </summary>
        /// <param name="rootPkg">根对象引用</param>
        /// <param name="type">组件文件类型</param>
        /// <param name="filePath">文件远程地址</param>
        /// <returns></returns>
        public static Component CreateFromUrl(Root rootPkg, ResourceType type, string fileUrl)
        {
            byte[] fileBytes = new byte[0];
            using (System.Net.WebClient c = new System.Net.WebClient())
            {
                fileBytes = c.DownloadData(fileUrl);
            }

            uint lastModified = DateTime.Now.DateTimeToDosTime();
            CommonLib.HashProvider hashUtil = new CommonLib.HashProvider(CommonLib.HashProvider.ServiceProviderEnum.SHA1, Encoding.UTF8);
            return new Component(rootPkg)
            {
                CompomentID = Guid.NewGuid(),
                Name = Path.GetFileName(fileUrl),
                ModifiedDateTime = lastModified,
                Files = new ResourceFile[] { new ResourceFile
                    {
                        Name = Path.GetFileName(fileUrl),
                        SynPath = fileUrl,
                        HashCode = hashUtil.ComputeHash(fileBytes), 
                        Content = (rootPkg.PackageData) ? fileBytes : null,
                        FileLength = (ulong)fileBytes.LongLength,
                        ModifiedDateTime = lastModified,
                        Type = type
                    }
                }
            };

        }
        #endregion
    }

    /// <summary>
    /// 组件引用
    /// </summary>
    [Serializable]
    public struct ComponentReference
    {
        /// <summary>
        /// 组件标识
        /// </summary>
        [XmlAttribute]
        public Guid CompomentID { get; set; }

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
