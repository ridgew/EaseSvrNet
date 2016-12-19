using System;
using System.IO;
using System.Text;

namespace EaseServer.FtpConnection
{
    public static class FtpUtil
    {
        public static void WriteLineWith(this Stream exchangeStream, string format, params object[] args)
        {
            string message = string.Format(format, args) + Environment.NewLine;
            byte[] retBytes = Encoding.Default.GetBytes(message);
            exchangeStream.Write(retBytes, 0, retBytes.Length);
        }

        public static void WriteWith(this Stream exchangeStream, string format, params object[] args)
        {
            string message = string.Format(format, args);
            byte[] retBytes = Encoding.Default.GetBytes(message);
            exchangeStream.Write(retBytes, 0, retBytes.Length);
        }

        public static string ForceStartWith(this string srcString, string startWith)
        {
            return ForceStartWith(srcString, startWith, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string ForceStartWith(this string srcString, string startWith, StringComparison compare)
        {
            if (string.IsNullOrEmpty(srcString))
            {
                return srcString;
            }
            else
            {
                if (!srcString.StartsWith(startWith, compare))
                {
                    srcString = startWith + srcString;
                }
                return srcString;
            }
        }

        public static string ForceEndWith(this string srcString, string endWith, StringComparison compare)
        {
            if (string.IsNullOrEmpty(srcString))
            {
                return srcString;
            }
            else
            {
                if (!srcString.EndsWith(endWith, compare))
                {
                    srcString = srcString + endWith;
                }
                return srcString;
            }
        }

        public static string ForceEndWith(this string srcString, string endWith)
        {
            return ForceEndWith(srcString, endWith, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
