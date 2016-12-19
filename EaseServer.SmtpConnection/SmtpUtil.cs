using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EaseServer.SmtpConnection
{
    public static class SmtpUtil
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
    }
}
