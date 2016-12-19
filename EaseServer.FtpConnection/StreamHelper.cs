using System;
using System.IO;
using System.Text;

namespace EaseServer.FtpConnection
{
    /// <summary>
    /// Command helper for net transfers
    /// </summary>
    internal static class StreamHelper
    {
        /// <summary>
        /// Send a network command and verify return state
        /// </summary>
        /// <param name="netstream">network stream</param>
        /// <param name="command">network command</param>
        /// <param name="state">network state</param>
        /// <param name="code">return code</param>
        /// <returns>flag</returns>
        internal static bool Command(this Stream netstream, string command, string state, out string code)
        {
            bool flag = false;
            WriteString(netstream, command);
            code = ReadString(netstream).Trim();
            if (code.IndexOf(state) != -1)
            {
                flag = true;
            }
            return flag;
        }

        /// <summary>
        /// Read server retrun code from network
        /// </summary>
        /// <param name="netstream">network stream</param>
        /// <returns>server return code</returns>
        internal static string ReadString(this Stream netstream)
        {
            StreamReader reader = new StreamReader(netstream);
            string str = string.Empty;
            string str2 = reader.ReadLine();
            str = str2;
            while (((str2 != null) && (str2.Length > 3)) && (str2[3] == '-'))
            {
                str2 = reader.ReadLine();
                str = str + "\r\n" + str2;
            }
            return str;
        }

        /// <summary>
        /// Convert text to base64 string
        /// </summary>
        /// <param name="languageEncoding">language encoding</param>
        /// <param name="text">text</param>
        /// <returns>base64 string</returns>
        internal static string ToBase64(string languageEncoding, string text)
        {
            text = Convert.ToBase64String(Encoding.GetEncoding(languageEncoding).GetBytes(text));
            return text;
        }

        /// <summary>
        /// Write command to network [for small data]
        /// </summary>
        /// <param name="netstream">network stream</param>
        /// <param name="cmd">network command</param>
        internal static void WriteString(this Stream netstream, string cmd)
        {
            cmd = cmd + "\r\n";
            byte[] bytes = Encoding.Default.GetBytes(cmd);
            netstream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Write command to network [for large data]
        /// </summary>
        /// <param name="tcp">tcp client</param>
        /// <param name="cmd">network command</param>
        internal static void WriteStringLarge(this Stream writer, string cmd)
        {
            int startIndex = 0;
            do
            {
                int length = Math.Min(0x40, cmd.Length - startIndex);
                WriteString(writer, cmd.Substring(startIndex, length));
                startIndex += length;
            }
            while (startIndex < cmd.Length);
        }
    }
}
