using System;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Text;

namespace EaseServer.Performance
{
    /// <summary>
    /// 扩展辅助函数
    /// </summary>
    public static class Extension
    {
        /// <summary>
        /// 当前时间与协调世界时(utc)1970年1月1日午夜之间的时间差（以毫秒为单位测量）
        /// </summary>
        /// <param name="dateTimeUtc"></param>
        public static double GetTimeMilliseconds(this DateTime dateTimeUtc)
        {
            return (dateTimeUtc - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
        }

        public static uint DateTimeToDosTime(this DateTime _dt)
        {
            return (uint)(
                (_dt.Second / 2) | (_dt.Minute << 5) | (_dt.Hour << 11) |
                (_dt.Day << 16) | (_dt.Month << 21) | ((_dt.Year - 1980) << 25));
        }

        public static DateTime DosTimeToDateTime(this uint _dt)
        {
            return new DateTime(
                (int)(_dt >> 25) + 1980,
                (int)(_dt >> 21) & 15,
                (int)(_dt >> 16) & 31,

                (int)(_dt >> 11) & 31,
                (int)(_dt >> 5) & 63,
                (int)(_dt & 31) * 2);
        }

        public static string GetXmlDocString(this object pObj, bool noNamespaceAttr)
        {
            if (pObj == null) { return null; }
            XmlSerializer xs = new XmlSerializer(pObj.GetType(), string.Empty);
            StringBuilder targetBuilder = new StringBuilder(1024);
            using (StringWriter sw = new StringWriter(targetBuilder))
            {
                if (noNamespaceAttr)
                {
                    XmlSerializerNamespaces xn = new XmlSerializerNamespaces();
                    xn.Add("", "");
                    xs.Serialize(sw, pObj, xn);
                }
                else
                {
                    xs.Serialize(sw, pObj);
                }
                return targetBuilder.ToString();
            }
        }

    }

}
