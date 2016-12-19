using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 处理Ease标签内容的步骤
    /// </summary>
    [Serializable]
    public class RefactContentStep
    {
        /// <summary>
        /// 支持编辑内容的实现类型名称
        /// </summary>
        [XmlAttribute(AttributeName = "type")]
        public string EditTypeName { get; set; }

        /// <summary>
        /// 处理类型的公共方法名
        /// </summary>
        [XmlAttribute(AttributeName = "method")]
        public string MethodName { get; set; }

        /// <summary>
        /// 是否是该类的静态方法
        /// </summary>
        [XmlAttribute(AttributeName = "static")]
        public bool IsStatic { get; set; }

        /// <summary>
        /// 是否是仅针对特别的业务编号
        /// </summary>
        [XmlAttribute(AttributeName = "onlyFor")]
        public string OnlyForBusinessIds { get; set; }

        private ReplaceAction[] _replaceSet = new ReplaceAction[0];
        /// <summary>
        /// 其他替换操作
        /// </summary>
        [XmlArray(ElementName = "BeforeAction"), XmlArrayItem(ElementName = "add")]
        public ReplaceAction[] ReplaceMents
        {
            get { return _replaceSet; }
            set { _replaceSet = value; }
        }

        private ReplaceAction[] _replaceSet2 = new ReplaceAction[0];
        /// <summary>
        /// 获取或设置后置操作
        /// </summary>
        /// <value>The replace ments after.</value>
        [XmlArray(ElementName = "AfterAction"), XmlArrayItem(ElementName = "add")]
        public ReplaceAction[] ReplaceMentsAfter
        {
            get { return _replaceSet2; }
            set { _replaceSet2 = value; }
        }

        /// <summary>
        /// 编辑内容并返回修改后的内容
        /// </summary>
        /// <param name="businessId">当前业务编号</param>
        /// <param name="content">原始内容</param>
        /// <param name="isHtml">文本内容是否是html代码</param>
        /// <returns>修改后的内容</returns>
        public string RefactContent(short businessId, string content, bool isHtml)
        {
            #region 前置替换操作
            if (ReplaceMents != null && ReplaceMents.Length > 0)
            {
                //System.Diagnostics.Trace.WriteLine(string.Format("前置操作：{0}" , ReplaceMents.Length));
                foreach (ReplaceAction act in ReplaceMents)
                {
                    if (act.IsPattern)
                    {
                        content = Regex.Replace(content, act.Replace, act.With, RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        content = content.Replace(act.Replace, act.With);
                    }
                }
            }
            #endregion

            #region 自定义函数处理
            Type editableType = Type.GetType(EditTypeName);
            if (editableType == null)
            {
                throw new System.Configuration.ConfigurationErrorsException("没有找到配置类型名称为" + EditTypeName + "的类型!");
            }
            MethodInfo tMethod = editableType.GetMethod(MethodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            if (tMethod == null)
            {
                throw new System.Configuration.ConfigurationErrorsException("没有找到配置类型为[" + editableType.FullName + "]方法名称为" + MethodName + "的处理方法!");
            }
            object instance = null;
            if (!IsStatic)
            {
                instance = Activator.CreateInstance(editableType);
            }

            content = (string)tMethod.Invoke(instance, new object[] { businessId, content, isHtml });
            #endregion

            #region 后置替换操作
            if (ReplaceMentsAfter != null && ReplaceMentsAfter.Length > 0)
            {
                //System.Diagnostics.Trace.WriteLine(string.Format("前置操作：{0}" , ReplaceMents.Length));
                foreach (ReplaceAction act in ReplaceMentsAfter)
                {
                    if (act.IsPattern)
                    {
                        content = Regex.Replace(content, act.Replace, act.With, RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        content = content.Replace(act.Replace, act.With);
                    }
                }
            }
            #endregion

            return content;
        }

    }

    /// <summary>
    /// 内容替换操作
    /// </summary>
    [Serializable]
    public struct ReplaceAction
    {
        /// <summary>
        /// 获取或设置需要查找的字符
        /// </summary>
        /// <value>The replace.</value>
        [XmlAttribute(AttributeName = "replace")]
        public string Replace { get; set; }

        /// <summary>
        /// 获取或设置是否是匹配模式
        /// </summary>
        [XmlAttribute(AttributeName = "pattern")]
        public bool IsPattern { get; set; }

        /// <summary>
        /// 获取火设置替换结果内容
        /// </summary>
        [XmlAttribute(AttributeName = "with")]
        public string With { get; set; }
    }
}
