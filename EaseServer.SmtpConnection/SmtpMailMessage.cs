using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EaseServer.SmtpConnection
{
    [Serializable]
    public class SmtpMailMessage
    {
        private bool inHeader = true;
        private string lastField = string.Empty;
        private string message = string.Empty;

        /// <summary>
        /// 邮件日期
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// 发件人
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// 收件人
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// 主题
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// 原始邮件内容
        /// </summary>
        public string Message
        {
            get { return Decode(message); }
            set { message = value; }
        }

        /// <summary>
        /// 添加收件人
        /// </summary>
        /// <param name="emailAddress">邮件地址</param>
        public void AddRecipient(string emailAddress)
        {
            if (string.IsNullOrEmpty(To))
            {
                To = emailAddress;
            }
            else
            {
                To = To + "; " + emailAddress;
            }
        }

        /// <summary>
        /// 添加邮件内容文本行
        /// </summary>
        /// <param name="line"></param>
        public void AddMessageLine(string line)
        {
            if (string.IsNullOrEmpty(line) && inHeader)
            {
                inHeader = false;
            }
            else
            {
                if (inHeader)
                {
                    #region 邮件头处理
                    string field = string.Empty;
                    string value = string.Empty;

                    if (line[0] == ' ' || line[0] == '\t')
                    {
                        field = lastField;
                        value = line;
                    }
                    else
                    {
                        if (line.Contains(':'))
                        {
                            int pos = line.IndexOf(':');
                            field = line.Substring(0, pos);
                            value = line.Substring(pos + 1).Trim();
                        }
                    }

                    switch (field.ToLower())
                    {
                        case "date":
                            Date = value;
                            break;
                        case "subject":
                            Subject += value;
                            break;
                    }

                    lastField = field;
                    #endregion
                }
                else
                {
                    message += line + Environment.NewLine;
                }
            }
        }

        // The following decode code was found at http://www.aspemporium.com 
        // It was written by Bill Gearhart.
        // These methods decode the Quoted-Printable encoding used in email messages.

        static string HexDecoderEvaluator(Match m)
        {
            string hex = m.Groups[2].Value;
            int iHex = Convert.ToInt32(hex, 16);
            char c = (char)iHex;
            return c.ToString();
        }

        static string HexDecoder(string line)
        {
            if (line == null)
                throw new ArgumentNullException();

            //parse looking for =XX where XX is hexadecimal
            Regex re = new Regex(
                "(\\=([0-9A-F][0-9A-F]))",
                RegexOptions.IgnoreCase
            );
            return re.Replace(line, new MatchEvaluator(HexDecoderEvaluator));
        }

        public static string Decode(string encoded)
        {
            if (encoded == null)
                throw new ArgumentNullException();

            string line;
            StringWriter sw = new StringWriter();
            StringReader sr = new StringReader(encoded);
            try
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.EndsWith("="))
                        sw.Write(HexDecoder(line.Substring(0, line.Length - 1)));
                    else
                        sw.WriteLine(HexDecoder(line));

                    sw.Flush();
                }
                return sw.ToString();
            }
            finally
            {
                sw.Close();
                sr.Close();
                sw = null;
                sr = null;
            }
        }

    }
}
