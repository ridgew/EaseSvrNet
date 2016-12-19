using System;
using System.IO;
using Gwsoft.EaseMode;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 当前连接扩展
    /// </summary>
    public static class ConnectionExtension
    {
        /// <summary>
        /// 新建一个GUID的字符串
        /// </summary>
        /// <param name="fileName">路径文件名</param>
        /// <param name="maxLen">The max len.</param>
        /// <returns></returns>
        public static EaseString FileNameAsEaseString(this string fileName, int maxLen)
        {
            return EaseString.Get(FileNameAsGuidString(fileName, maxLen));
        }

        /// <summary>
        /// Files the name as GUID string.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="maxLen">The max len.</param>
        /// <returns></returns>
        public static String FileNameAsGuidString(this string fileName, int maxLen)
        {
            if (fileName.Length <= maxLen)
            {
                return fileName;
            }
            else
            {
                string ext = fileName.GetUrlExtension();
                if (string.IsNullOrEmpty(ext)) { ext = ".png"; }
                return String.Concat(Guid.NewGuid().ToString("N"), ext);
            }
        }

        /// <summary>
        /// 删除非法的文件名字符
        /// </summary>
        public static string TrimInvalidFilenameChars(this string rawFileName)
        {
            char[] allChars = Path.GetInvalidFileNameChars();
            foreach (char a in allChars)
            {
                rawFileName = rawFileName.Replace(a.ToString(), "");
            }
            return rawFileName;
        }

        /// <summary>
        /// 获取URL地址的文件扩展名
        /// </summary>
        /// <param name="url">URL地址</param>
        /// <returns></returns>
        public static string GetUrlExtension(this string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }
            else
            {
                int idx = url.IndexOfAny(new char[] { '?', '&' });
                if (idx != -1)
                {
                    return Path.GetExtension(url.Substring(0, idx));
                }
                return Path.GetExtension(url);
            }
        }

    }
}
