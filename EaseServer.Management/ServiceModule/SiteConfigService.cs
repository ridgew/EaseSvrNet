using System;
using System.Web.Script.Services;
using System.Web.Services;
using System.Xml;
using System.Collections.Generic;
using System.Threading;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    ///站点信息配置管理服务
    /// </summary>
    [WebService(Name = "站点信息配置管理服务", Description = "提供用于显示站点信息及管理站点配置的相应方法")]
    public class SiteConfigService : WebServiceBase
    {
        static XmlDocument _siteConfigDoc = null;
        /// <summary>
        /// 站点配置文件的静态变量
        /// </summary>
        public static XmlDocument SiteConfigDocument
        {
            get
            {
                if (_siteConfigDoc == null)
                {
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.Load(System.Web.HttpContext.Current.Server.MapPath("/web.config"));
                    _siteConfigDoc = xDoc;
                }
                return _siteConfigDoc;
            }
        }

        /// <summary>
        /// 同步配置文件信息
        /// </summary>
        public void SynSiteConfig()
        {
            if (_siteConfigDoc != null)
            {
                _siteConfigDoc.Save(System.Web.HttpContext.Current.Server.MapPath("/web.config"));
            }
        }

        /// <summary>
        /// 获取系统站点参数列表信息
        /// </summary>
        /// <returns>系统站点参数列表信息</returns>
        [Protocol("1.3.5.5.1"), WebMethod(Description = "获取系统站点参数列表信息")]
        [GenerateScriptType(typeof(SvcSiteParameter), ScriptTypeId = "0")]
        public SvcMultiRecord<SvcSiteParameter> GetSiteParameters()
        {
            List<SvcSiteParameter> sitePList = new List<SvcSiteParameter>();

            XmlDocument xDoc = SiteConfigDocument;
            XmlNode appSettingNode = xDoc.SelectSingleNode("configuration/appSettings");
            if (appSettingNode != null && appSettingNode.ChildNodes != null)
            {
                XmlNode subNode = null, presubNode = null;
                string nodeDescription = string.Empty;
                for (int i = 0, j = appSettingNode.ChildNodes.Count; i < j; i++)
                {
                    subNode = appSettingNode.ChildNodes[i];
                    if (subNode.NodeType == XmlNodeType.Element)
                    {
                        if (i > 0) presubNode = appSettingNode.ChildNodes[i - 1];
                        if (presubNode.NodeType == XmlNodeType.Comment)
                        {
                            nodeDescription = presubNode.InnerText;
                        }
                        else
                        {
                            nodeDescription = string.Empty;
                        }
                        sitePList.Add(new SvcSiteParameter { Name = subNode.Attributes["key"].Value, Value = subNode.Attributes["value"].Value, Description = nodeDescription });
                    }
                }
            }

            SvcMultiRecord<SvcSiteParameter> result = new SvcMultiRecord<SvcSiteParameter>
            {
                Protocol = "1.3.5.5.1",
                Status = 1,
                Message = "系统配置参数列表",
                Data = sitePList.ToArray()
            };
            return result;
        }

        /// <summary>
        /// 添加或修改站点配置信息
        /// </summary>
        /// <param name="name">配置名称</param>
        /// <param name="value">配置数据值内容</param>
        /// <param name="description">配置相关描述信息</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.5.2"), WebMethod(Description = "添加或修改站点配置信息")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result CreateSiteParameter(string name, string value, string description)
        {
            Result result = new Result { Protocol = "1.3.5.5.2" };
            if (string.IsNullOrEmpty(name))
                result.Status = 10013;
            else
            {
                XmlNode targetNode = getTargetNodeByName(name);
                if (targetNode != null)
                {
                    XmlNode presubNode = targetNode.PreviousSibling;
                    string nodeDescription = string.Empty;
                    if (presubNode != null)
                    {
                        if (presubNode.NodeType == XmlNodeType.Comment)
                        {
                            presubNode.InnerText = description;
                        }
                        else
                        {
                            XmlComment cmt = SiteConfigDocument.CreateComment(description);
                            targetNode.ParentNode.InsertBefore(cmt, targetNode);
                        }
                    }
                    targetNode.Attributes["value"].Value = value;
                }
                else
                {
                    XmlNode parentNode = SiteConfigDocument.SelectSingleNode("configuration/appSettings");

                    XmlNode subChild = SiteConfigDocument.CreateNode(XmlNodeType.Element, "add", "");
                    XmlAttribute attr = SiteConfigDocument.CreateAttribute("key");
                    attr.Value = name;
                    subChild.Attributes.Append(attr);

                    attr = SiteConfigDocument.CreateAttribute("value");
                    attr.Value = value;
                    subChild.Attributes.Append(attr);

                    XmlNode locNode = parentNode.AppendChild(subChild);
                    if (!string.IsNullOrEmpty(description))
                    {
                        XmlComment nCmt = SiteConfigDocument.CreateComment(description);
                        parentNode.InsertBefore(nCmt, locNode);
                    }
                }
                result.Status = 10014;
                SynSiteConfig();
            }
            return result;
        }

        /// <summary>
        /// 获取系统参数信息
        /// </summary>
        /// <param name="name">系统参数名称</param>
        /// <returns>系统参数信息</returns>
        [Protocol("1.3.5.5.3"), WebMethod(Description = "获取系统参数信息")]
        [GenerateScriptType(typeof(SvcSiteParameter), ScriptTypeId = "0")]
        public SvcSingleRecord<SvcSiteParameter> GetSiteParameter(string name)
        {
            SvcSingleRecord<SvcSiteParameter> result = new SvcSingleRecord<SvcSiteParameter>
            {
                Protocol = "1.3.5.5.3",
                Message = "系统参数信息"
            };

            XmlNode targetNode = getTargetNodeByName(name);

            if (targetNode != null)
            {
                XmlNode presubNode = targetNode.PreviousSibling;
                string nodeDescription = string.Empty;
                if (presubNode != null)
                {
                    if (presubNode.NodeType == XmlNodeType.Comment)
                    {
                        nodeDescription = presubNode.InnerText;
                    }
                }
                result.Data = new SvcSiteParameter
                {
                    Name = name,
                    Value = targetNode.Attributes["value"].Value,
                    Description = nodeDescription
                };
                result.Status = 1;
            }
            else
            {
                result.Data = null;
                result.Status = 0;
            }
            return result;
        }

        XmlNode getTargetNodeByName(string keyName)
        {
            return SiteConfigDocument.SelectSingleNode("configuration/appSettings/add[@key='" + keyName + "']");
        }

        /// <summary>
        /// 删除系统参数信息
        /// </summary>
        /// <param name="name">参数名称</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.5.4"), WebMethod(Description = "删除系统参数信息")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result DeleteParameter(string name)
        {
            Result result = new Result { Protocol = "1.3.5.5.4" };
            XmlNode targetNode = getTargetNodeByName(name);
            if (targetNode != null)
            {
                XmlNode parentNode = targetNode.ParentNode;
                XmlNode presubNode = targetNode.PreviousSibling;
                string nodeDescription = string.Empty;
                if (presubNode != null)
                {
                    if (presubNode.NodeType == XmlNodeType.Comment)
                    {
                        parentNode.RemoveChild(presubNode);
                    }
                }
                parentNode.RemoveChild(targetNode);
                SynSiteConfig();
                result.Status = 10015;
            }
            else
            {
                result.Status = 10016;
            }
            return result;
        }
    }

    /// <summary>
    /// 站点参数信息
    /// </summary>
    [Serializable]
    public class SvcSiteParameter
    {
        /// <summary>
        /// 获取或设置参数名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置参数值内容
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 获取或设置参数描述信息
        /// </summary>
        public string Description { get; set; }
    }
}