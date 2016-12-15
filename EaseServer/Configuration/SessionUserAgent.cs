using System;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace EaseServer.Configuration
{
    /// <summary>
    /// 会话用户代理
    /// </summary>
    [Serializable]
    public class SessionUserAgent
    {
        /// <summary>
        /// 原始提交字符
        /// </summary>
        [XmlIgnore]
        public string RawString { get; set; }

        /// <summary>
        /// 最低版本
        /// </summary>
        [XmlAttribute(AttributeName = "minVer")]
        public string MinVersion { get; set; }

        /// <summary>
        /// 当前用户代理的版本
        /// </summary>
        [XmlIgnore]
        public string CurrentVersion { get; set; }

        /// <summary>
        /// 友好名称
        /// </summary>
        [XmlAttribute(AttributeName = "name")]
        public string FriendlyName { get; set; }

        /// <summary>
        /// UA主要标识
        /// </summary>
        [XmlAttribute(AttributeName = "id")]
        public string Identity { get; set; }

        /// <summary>
        /// 主要匹配模式，包含组名id和ver的正则匹配模式。
        /// </summary>
        [XmlAttribute(AttributeName = "pattern")]
        public string MatchPattern { get; set; }

        /// <summary>
        /// 判定当前会话代理是否支持
        /// </summary>
        public bool IsNotSupported(ref bool skipped)
        {
            Match m = Regex.Match(RawString, MatchPattern, RegexOptions.IgnoreCase);
            bool notSupport = true;
            if (!m.Success)
            {
                skipped = true;
                notSupport = false;
            }
            else
            {
                string uaId = m.Groups["id"].Value;
                CurrentVersion = m.Groups["ver"].Value;
                if (!uaId.Equals(Identity, StringComparison.InvariantCultureIgnoreCase))
                {
                    skipped = true;
                }
                else
                {
                    skipped = false;
                    if (new Version(CurrentVersion).CompareTo(new Version(MinVersion)) >= 0)
                    {
                        notSupport = false;
                    }
                }
            }
            return notSupport;
        }

    }
}
