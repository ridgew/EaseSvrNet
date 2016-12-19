using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using EaseServer.Interface;

namespace EaseServer.FtpConnection
{
    class FTPUser
    {
        internal bool CanDeleteFiles, CanDeleteFolders, CanRenameFiles,
            CanRenameFolders, CanStoreFiles, CanStoreFolder, CanViewHiddenFiles,
            CanViewHiddenFolders, CanCopyFiles;

        internal string UserName = "";

        /// <summary>
        /// 当前用户的启动目录
        /// </summary>
        internal string StartUpDirectory = "";

        /// <summary>
        /// 当前用户文件目录
        /// </summary>
        internal string CurrentWorkingDirectory = "\\";

        /// <summary>
        /// 是否已认证用户
        /// </summary>
        internal bool IsAuthenticated = false;
        string Password;
        IServerConnection _svrConn = null;

        public FTPUser() { }

        public FTPUser(string UserName, IServerConnection conn)
        {
            _svrConn = conn;
            try
            {
                if (UserName == this.UserName) return;
                if ((this.UserName = UserName).Length == 0) return;
                XmlNodeList Users = ApplicationSettings.GetUserList();
                IsAuthenticated = false;

                foreach (XmlNode User in Users)
                {
                    if (User.Attributes[0].Value != UserName) 
                        continue;

                    Password = User.Attributes[1].Value;
                    StartUpDirectory = User.Attributes[2].Value;

                    char[] Permissions = User.Attributes[3].Value.ToCharArray();
                    CanStoreFiles = Permissions[0] == '1';
                    CanStoreFolder = Permissions[1] == '1';
                    CanRenameFiles = Permissions[2] == '1';
                    CanRenameFolders = Permissions[3] == '1';
                    CanDeleteFiles = Permissions[4] == '1';
                    CanDeleteFolders = Permissions[5] == '1';
                    CanCopyFiles = Permissions[6] == '1';
                    CanViewHiddenFiles = Permissions[7] == '1';
                    CanViewHiddenFolders = Permissions[8] == '1';

                    break;
                }
            }
            catch (Exception Ex)
            {
                _svrConn.ServerError("{0}", Ex.ToString());
            }
        }

        internal bool Authenticate(string Password)
        {
            if (Password == this.Password) 
                IsAuthenticated = true;
            else 
                IsAuthenticated = false;
            return IsAuthenticated;
        }

        internal bool ChangeDirectory(string Dir)
        {
            CurrentWorkingDirectory = Dir;
            return true;
        }
    }

}
