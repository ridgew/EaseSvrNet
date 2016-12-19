using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace EaseServer.FtpConnection
{
    /// <summary>
    /// 应用程序设置
    /// </summary>
    public class ApplicationSettings
    {
        internal static XmlDocument FtpSettingDoc = new XmlDocument();

        /// <summary>
        /// 用户文件路径
        /// </summary>
        internal static string UserSettingPath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\FtpSettings.dat";

        /// <summary>
        /// 直接构建新实例 <see cref="ApplicationSettings"/> class.
        /// </summary>
        static ApplicationSettings()
        {
            RefreshFtpSetting();
        }

        /// <summary>
        /// 刷新FTP文件设置
        /// </summary>
        public static void RefreshFtpSetting()
        {
            if (File.Exists(UserSettingPath))
            {
                using (FileStream RawStream = new FileStream(UserSettingPath, FileMode.Open, FileAccess.Read))
                {
                    byte[] Buffer = new byte[(int)RawStream.Length];
                    RawStream.Read(Buffer, 0, Buffer.Length);
                    RawStream.Close();

                    Buffer = Crypt(Buffer);
                    MemoryStream Stream = new MemoryStream(Buffer);
                    TextReader Reader = new StreamReader(Stream, Encoding.UTF8);
                    FtpSettingDoc.Load(Reader);
                    Reader.Close();
                    Stream.Close();
                    Stream.Dispose();
                    Buffer = null;
                }

            }
        }

        /// <summary>
        /// 提取设置
        /// </summary>
        public static void DumpSetting()
        {
            FtpSettingDoc.Save(Path.ChangeExtension(UserSettingPath, "config"));
        }

        /// <summary>
        /// 使用明文配置文档并保持设置
        /// </summary>
        public static void UseDumpSetting()
        {
            string filePath = Path.ChangeExtension(UserSettingPath, "config");
            if (File.Exists(filePath))
            {
                XmlDocument dumpDoc = new XmlDocument();
                dumpDoc.Load(filePath);
                FtpSettingDoc = dumpDoc;
                SaveSettings();
            }
        }

        internal static int MaxPassvPort
        {
            get
            {
                return Convert.ToInt32(GetSettingsAsString(SettingsKey.MAX_PASSV_PORT));
            }
            set
            {
                ChangeSettings(SettingsKey.MAX_PASSV_PORT, value.ToString());
            }
        }

        internal static int MinPassvPort
        {
            get
            {
                return Convert.ToInt32(GetSettingsAsString(SettingsKey.MIN_PASSV_PORT));
            }
            set
            {
                ChangeSettings(SettingsKey.MIN_PASSV_PORT, value.ToString());
            }
        }

        internal static string DateTimeFormat
        {
            get
            {
                return GetSettingsAsString(SettingsKey.DATE_TIME_FORMAT);
            }
            set
            {
                ChangeSettings(SettingsKey.DATE_TIME_FORMAT, value);
            }
        }

        #region General Methods

        internal static void SaveSettings()
        {
            MemoryStream Stream = new MemoryStream();
            TextWriter TxtWriter = new StreamWriter(Stream, Encoding.UTF8);
            FtpSettingDoc.Save(TxtWriter);
            byte[] Buff = Crypt(Stream.GetBuffer());
            FileStream FS = new FileStream(UserSettingPath, FileMode.Create, FileAccess.Write);
            FS.Write(Buff, 0, Buff.Length);
            FS.Close();
            FS = null;
            TxtWriter.Close();
            Stream.Close();
        }

        /// <summary>
        /// 混淆与反混淆
        /// </summary>
        static byte[] Crypt(byte[] Buffer)
        {
            for (int i = 0; i < Buffer.Length; i++)
            {
                Buffer[i] ^= 36;
            }
            return Buffer;
        }

        static string GetSettingsAsString(SettingsKey Key)
        {
            XmlNodeList SettingsList = FtpSettingDoc.DocumentElement.SelectSingleNode("SETTINGS").ChildNodes;
            string returnValue = string.Empty;

            foreach (XmlNode Setting in SettingsList)
            {
                if (Setting.Attributes["NAME"].Value != Key.ToString()) continue;

                returnValue = Setting.Attributes["VALUE"].Value;
                break;
            }
            return returnValue;
        }

        static bool GetSettingsAsBool(SettingsKey Key)
        {
            return GetSettingsAsString(Key) == "1";
        }

        static void ChangeSettings(SettingsKey Key, string Value)
        {
            XmlNode SettingsNode = FtpSettingDoc.DocumentElement.SelectSingleNode("SETTINGS");
            foreach (XmlNode Setting in SettingsNode.ChildNodes)
            {
                if (Setting.Attributes["NAME"].Value != Key.ToString()) continue;

                Setting.Attributes["VALUE"].Value = Value;
                return;
            }
            XmlNode NewSetting = FtpSettingDoc.CreateElement("KEY");
            XmlAttribute Attrib = FtpSettingDoc.CreateAttribute("NAME");
            Attrib.Value = Key.ToString();
            NewSetting.Attributes.Append(Attrib);

            Attrib = FtpSettingDoc.CreateAttribute("NAME");
            Attrib.Value = Value;
            NewSetting.Attributes.Append(Attrib);
            SettingsNode.AppendChild(NewSetting);
        }

        static void ChangeSettings(SettingsKey Key, bool Value)
        {
            ChangeSettings(Key, (Value) ? "1" : "0");
        }
        #endregion

        #region User Account Methods

        /// <summary>
        /// 创建FTP用户
        /// </summary>
        /// <param name="UserName">用户名</param>
        /// <param name="Password">用户密码</param>
        /// <param name="RootPath">FTP根目录</param>
        /// <param name="PermissionSet">权限设置</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].</param>
        /// <returns></returns>
        public static bool CreateFTPUser(string UserName, string Password, string RootPath, string PermissionSet, bool Enabled)
        {
            if (IsUserExists(UserName)) return false;

            XmlNodeList Users = GetUserList();
            XmlNode User = ApplicationSettings.FtpSettingDoc.CreateElement("User");
            User.Attributes.Append(ApplicationSettings.FtpSettingDoc.CreateAttribute("UserName"));
            User.Attributes.Append(ApplicationSettings.FtpSettingDoc.CreateAttribute("Password"));
            User.Attributes.Append(ApplicationSettings.FtpSettingDoc.CreateAttribute("Root"));
            User.Attributes.Append(ApplicationSettings.FtpSettingDoc.CreateAttribute("PermissionSet"));
            User.Attributes.Append(ApplicationSettings.FtpSettingDoc.CreateAttribute("Enabled"));
            FtpSettingDoc.DocumentElement.SelectSingleNode("UserAccount").AppendChild(User);

            User.Attributes[0].Value = UserName.ToUpper();
            User.Attributes[1].Value = Password;
            User.Attributes[2].Value = RootPath;
            User.Attributes[3].Value = PermissionSet;
            User.Attributes[4].Value = (Enabled) ? "1" : "0";
            SaveSettings();
            return true;
        }

        /// <summary>
        /// 编辑FTP用户
        /// </summary>
        /// <param name="OldUserName">旧用户名称</param>
        /// <param name="UserName">用户名</param>
        /// <param name="Password">用户密码</param>
        /// <param name="StartUpPath">The start up path.</param>
        /// <param name="PermissionSet">权限设置</param>
        /// <param name="Enabled">if set to <c>true</c> [enabled].</param>
        /// <returns></returns>
        public static bool EditUser(string OldUserName, string UserName, string Password, string StartUpPath, string PermissionSet, bool Enabled)
        {
            string OldRootPath = string.Empty;
            XmlNodeList Users = GetUserList();
            XmlNode User = GetUser(OldUserName);

            if (User == null)
                return CreateFTPUser(UserName, Password, StartUpPath, PermissionSet, Enabled);
            else
            {
                if (UserName != OldUserName && IsUserExists(UserName)) return false;
                else
                {
                    User.Attributes["UserName"].Value = UserName.ToUpper();
                    User.Attributes["Password"].Value = Password;
                    User.Attributes["Root"].Value = StartUpPath;
                    User.Attributes["PermissionSet"].Value = PermissionSet;
                    User.Attributes["Enabled"].Value = (Enabled) ? "1" : "0";
                    SaveSettings();
                    return true;
                }
            }
        }

        /// <summary>
        /// 检查某个FTP用户是否存在
        /// </summary>
        /// <param name="UserName">用户名</param>
        /// <returns>
        /// 	<c>true</c> if [is user exists] [the specified user name]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsUserExists(string UserName)
        {
            return GetUser(UserName) != null;
        }

        internal static bool GetUser(string UserName, out string Password, out string RootPath, out string PermissionSet, out bool Enabled)
        {
            XmlNode User = GetUser(UserName);
            Password = PermissionSet = RootPath = null;
            Enabled = false;
            if (User == null) return false;

            Password = User.Attributes[1].Value;
            PermissionSet = User.Attributes[3].Value;
            RootPath = User.Attributes[2].Value;
            Enabled = User.Attributes[4].Value == "1";

            return true;
        }

        internal static XmlNode GetUser(string UserName)
        {
            XmlNodeList Users = GetUserList();
            XmlNode User = null;
            for (int i = 0; i < Users.Count; i++)
            {
                if (Users[i].Attributes[0].Value.ToUpper() != UserName) continue;
                User = Users[i]; break;
            }

            return User;
        }

        /// <summary>
        /// 删除FTP用户
        /// </summary>
        /// <param name="UserName">用户名</param>
        public static void DeleteFTPUser(string UserName)
        {
            XmlNode User = GetUser(UserName);
            if (User != null)
            {
                FtpSettingDoc.DocumentElement.SelectSingleNode("UserAccount").RemoveChild(User);
                SaveSettings();
            }
        }

        internal static XmlNodeList GetUserList()
        {
            return FtpSettingDoc.DocumentElement.SelectNodes("UserAccount/User");
        }

        #endregion
    }

    enum SettingsKey
    {
        MAX_PASSV_PORT,
        MIN_PASSV_PORT,
        FTP_PORT,
        AUTO_START_FTP,
        ENABLE_FTP_LOGGING,
        HTTP_PORT,
        AUTO_START_HTTP,
        HTTP_LOGIN_ID,
        HTTP_PASSWORD,
        ENABLE_NOTIFY_ICON,
        ENABLE_FTPFOLDER_ICON,
        ENABLE_QUICK_CONFIG_MENU,
        AUTO_SEND_ERROR_REPORT,
        ENABLE_APD,
        MOVE_FILES_TO_RECYCLE_BIN,
        DATE_TIME_FORMAT
    }
}
