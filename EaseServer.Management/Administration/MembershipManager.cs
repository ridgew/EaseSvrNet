using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.Security;
using System.Xml;

namespace EaseServer.Management.Administration
{
    /// <summary>
    /// 成员管理类
    /// </summary>
    public static class MembershipManager
    {
        private static readonly MembershipProvider _baseProvider;

        static MembershipManager()
        {
            _baseProvider = new XmlMembershipProvider();
        }

        /// <summary>
        /// 获取成员管理默认实例
        /// </summary>
        public static MembershipProvider Instance
        {
            get
            {
                return _baseProvider;
            }
        }

        /// <summary>
        /// 获取成员关系提供者的当前用户身份
        /// </summary>
        public static GenericPrincipal GetCurrentUser(this MembershipProvider provider)
        {
            GenericPrincipal user = null;
            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                FormsIdentity identity = HttpContext.Current.User.Identity as FormsIdentity;
                //FormsAuthenticationTicket ticket = identity.Ticket;
                //string[] data = ticket.UserData.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                //user = new GenericPrincipal { UserId = int.Parse(data[0]), UserName = identity.Name };
                user = new GenericPrincipal(new GenericIdentity(identity.Name, identity.AuthenticationType), new string[] { "administrators" });
            }
            else
            {
                user = new GenericPrincipal(new GenericIdentity(HttpContext.Current.Session.SessionID, "Forms"), new string[] { "guests" });
            }
            return user;
        }
    }

    /// <summary>
    /// XML数据格式的成员类型提供者
    /// <para>http://madskristensen.net/post/XML-membership-provider-for-ASPNET-20.aspx</para>
    /// </summary>
    public class XmlMembershipProvider : MembershipProvider
    {
        private Dictionary<string, MembershipUser> _Users;
        private string _XmlFileName;

        #region Properties

        // MembershipProvider Properties
        public override string ApplicationName
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// 指示成员资格提供程序是否配置为允许用户检索其密码。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果成员资格提供程序配置为支持密码检索，则为 true，否则为 false。默认值为 false。</returns>
        public override bool EnablePasswordRetrieval
        {
            get { return false; }
        }

        /// <summary>
        /// 指示成员资格提供程序是否配置为允许用户重置其密码。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果成员资格提供程序支持密码重置，则为 true；否则为 false。默认值为 true。</returns>
        public override bool EnablePasswordReset
        {
            get { return false; }
        }

        /// <summary>
        /// 获取锁定成员资格用户前允许的无效密码或无效密码提示问题答案尝试次数。
        /// </summary>
        /// <value></value>
        /// <returns>锁定成员资格用户之前允许的无效密码或无效密码提示问题答案尝试次数。</returns>
        public override int MaxInvalidPasswordAttempts
        {
            get { return 5; }
        }

        /// <summary>
        /// 获取有效密码中必须包含的最少特殊字符数。
        /// </summary>
        /// <value></value>
        /// <returns>有效密码中必须包含的最少特殊字符数。</returns>
        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return 0; }
        }

        /// <summary>
        /// 获取密码所要求的最小长度。
        /// </summary>
        /// <value></value>
        /// <returns>密码所要求的最小长度。 </returns>
        public override int MinRequiredPasswordLength
        {
            get { return 8; }
        }

        /// <summary>
        /// 获取在锁定成员资格用户之前允许的最大无效密码或无效密码提示问题答案尝试次数的分钟数。
        /// </summary>
        /// <value></value>
        /// <returns>在锁定成员资格用户之前允许的最大无效密码或无效密码提示问题答案尝试次数的分钟数。</returns>
        public override int PasswordAttemptWindow
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// 获取一个值，该值指示在成员资格数据存储区中存储密码的格式。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 	<see cref="T:System.Web.Security.MembershipPasswordFormat"/> 值之一，该值指示在数据存储区中存储密码的格式。</returns>
        public override MembershipPasswordFormat PasswordFormat
        {
            get { return MembershipPasswordFormat.Clear; }
        }

        /// <summary>
        /// 获取用于计算密码的正则表达式。
        /// </summary>
        /// <value></value>
        /// <returns>用于计算密码的正则表达式。</returns>
        public override string PasswordStrengthRegularExpression
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// 获取一个值，该值指示成员资格提供程序是否配置为要求用户在进行密码重置和检索时回答密码提示问题。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果密码重置和检索需要提供密码提示问题答案，则为 true；否则为 false。默认值为 true。</returns>
        public override bool RequiresQuestionAndAnswer
        {
            get { return false; }
        }

        /// <summary>
        /// 获取一个值，指示成员资格提供程序是否配置为要求每个用户名具有唯一的电子邮件地址。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果成员资格提供程序要求唯一的电子邮件地址，则返回 true；否则返回 false。默认值为 true。</returns>
        public override bool RequiresUniqueEmail
        {
            get { return false; }
        }

        #endregion

        #region Supported methods

        /// <summary>
        /// 初始化提供程序。
        /// </summary>
        /// <param name="name">该提供程序的友好名称。</param>
        /// <param name="config">名称/值对的集合，表示在配置中为该提供程序指定的、提供程序特定的属性。</param>
        /// <exception cref="T:System.ArgumentNullException">提供程序的名称是 null。</exception>
        /// <exception cref="T:System.ArgumentException">提供程序的名称长度为零。</exception>
        /// <exception cref="T:System.InvalidOperationException">提供程序初始化完成后，将尝试调用该提供程序上的 <see cref="M:System.Configuration.Provider.ProviderBase.Initialize(System.String,System.Collections.Specialized.NameValueCollection)"/>。</exception>
        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            if (String.IsNullOrEmpty(name))
                name = "XmlMembershipProvider";

            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "XML membership provider");
            }

            base.Initialize(name, config);

            // Initialize _XmlFileName and make sure the path
            // is app-relative
            string path = config["xmlFileName"];

            if (String.IsNullOrEmpty(path))
                path = "~/App_Data/Users.config";

            if (!VirtualPathUtility.IsAppRelative(path))
                throw new ArgumentException("xmlFileName must be app-relative");

            string fullyQualifiedPath = VirtualPathUtility.Combine
                (VirtualPathUtility.AppendTrailingSlash
                (HttpRuntime.AppDomainAppVirtualPath), path);

            _XmlFileName = HostingEnvironment.MapPath(fullyQualifiedPath);
            config.Remove("xmlFileName");

            // Make sure we have permission to read the XML data source and
            // throw an exception if we don't
            FileIOPermission permission = new FileIOPermission(FileIOPermissionAccess.Write, _XmlFileName);
            permission.Demand();

            // Throw an exception if unrecognized attributes remain
            if (config.Count > 0)
            {
                string attr = config.GetKey(0);
                if (!String.IsNullOrEmpty(attr))
                    throw new ProviderException("Unrecognized attribute: " + attr);
            }
        }

        /// <summary>
        /// Returns true if the username and password match an exsisting user.
        /// </summary>
        public override bool ValidateUser(string username, string password)
        {
            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
                return false;

            try
            {
                ReadMembershipDataStore();

                // Validate the user name and password
                MembershipUser user;
                if (_Users.TryGetValue(username, out user))
                {
                    if (user.Comment == Encrypt(password)) // Case-sensitive
                    {
                        user.LastLoginDate = DateTime.Now;
                        UpdateUser(user);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves a user based on his/hers username.
        /// the userIsOnline parameter is ignored.
        /// </summary>
        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            if (String.IsNullOrEmpty(username))
                return null;

            ReadMembershipDataStore();

            // Retrieve the user from the data source
            MembershipUser user;
            if (_Users.TryGetValue(username, out user))
                return user;

            return null;
        }

        /// <summary>
        /// Retrieves a collection of all the users.
        /// This implementation ignores pageIndex and pageSize,
        /// and it doesn't sort the MembershipUser objects returned.
        /// </summary>
        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            ReadMembershipDataStore();
            MembershipUserCollection users = new MembershipUserCollection();

            foreach (KeyValuePair<string, MembershipUser> pair in _Users)
            {
                users.Add(pair.Value);
            }

            totalRecords = users.Count;
            return users;
        }

        /// <summary>
        /// Changes a users password.
        /// </summary>
        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(_XmlFileName);
            XmlNodeList nodes = doc.GetElementsByTagName("User");
            foreach (XmlNode node in nodes)
            {
                if (node["UserName"].InnerText.Equals(username, StringComparison.OrdinalIgnoreCase)
                  || node["Password"].InnerText.Equals(Encrypt(oldPassword), StringComparison.OrdinalIgnoreCase))
                {
                    node["Password"].InnerText = Encrypt(newPassword);
                    doc.Save(_XmlFileName);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a new user store he/she in the XML file
        /// </summary>
        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(_XmlFileName);

            XmlNode xmlUserRoot = doc.CreateElement("User");
            XmlNode xmlUserName = doc.CreateElement("UserName");
            XmlNode xmlPassword = doc.CreateElement("Password");
            XmlNode xmlEmail = doc.CreateElement("Email");
            XmlNode xmlLastLoginTime = doc.CreateElement("LastLoginTime");

            xmlUserName.InnerText = username;
            xmlPassword.InnerText = Encrypt(password);
            xmlEmail.InnerText = email;
            xmlLastLoginTime.InnerText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            xmlUserRoot.AppendChild(xmlUserName);
            xmlUserRoot.AppendChild(xmlPassword);
            xmlUserRoot.AppendChild(xmlEmail);
            xmlUserRoot.AppendChild(xmlLastLoginTime);

            doc.SelectSingleNode("Users").AppendChild(xmlUserRoot);
            doc.Save(_XmlFileName);

            status = MembershipCreateStatus.Success;
            MembershipUser user = new MembershipUser(Name, username, username, email, passwordQuestion, Encrypt(password), isApproved, false, DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now, DateTime.MaxValue);
            _Users.Add(username, user);
            return user;
        }

        /// <summary>
        /// Deletes the user from the XML file and 
        /// removes him/her from the internal cache.
        /// </summary>
        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(_XmlFileName);

            foreach (XmlNode node in doc.GetElementsByTagName("User"))
            {
                if (node.ChildNodes[0].InnerText.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    doc.SelectSingleNode("Users").RemoveChild(node);
                    doc.Save(_XmlFileName);
                    _Users.Remove(username);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get a user based on the username parameter.
        /// the userIsOnline parameter is ignored.
        /// </summary>
        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(_XmlFileName);

            foreach (XmlNode node in doc.SelectNodes("//User"))
            {
                if (node.ChildNodes[0].InnerText.Equals(providerUserKey.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    string userName = node.ChildNodes[0].InnerText;
                    string password = node.ChildNodes[1].InnerText;
                    string email = node.ChildNodes[2].InnerText;
                    DateTime lastLoginTime = DateTime.Parse(node.ChildNodes[3].InnerText);
                    return new MembershipUser(Name, providerUserKey.ToString(), providerUserKey, email, string.Empty, password, true, false, DateTime.Now, lastLoginTime, DateTime.Now, DateTime.Now, DateTime.MaxValue);
                }
            }

            return default(MembershipUser);
        }

        /// <summary>
        /// Retrieves a username based on a matching email.
        /// </summary>
        public override string GetUserNameByEmail(string email)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(_XmlFileName);

            foreach (XmlNode node in doc.GetElementsByTagName("User"))
            {
                if (node.ChildNodes[2].InnerText.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return node.ChildNodes[0].InnerText;
                }
            }

            return null;
        }

        /// <summary>
        /// Updates a user. The username will not be changed.
        /// </summary>
        public override void UpdateUser(MembershipUser user)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(_XmlFileName);

            foreach (XmlNode node in doc.GetElementsByTagName("User"))
            {
                if (node.ChildNodes[0].InnerText.Equals(user.UserName, StringComparison.OrdinalIgnoreCase))
                {
                    if (user.Comment.Length > 30)
                    {
                        node.ChildNodes[1].InnerText = Encrypt(user.Comment);
                    }
                    node.ChildNodes[2].InnerText = user.Email;
                    node.ChildNodes[3].InnerText = user.LastLoginDate.ToString("yyyy-MM-dd HH:mm:ss");
                    doc.Save(_XmlFileName);
                    _Users[user.UserName] = user;
                }
            }
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Builds the internal cache of users.
        /// </summary>
        private void ReadMembershipDataStore()
        {
            lock (this)
            {
                if (_Users == null)
                {
                    _Users = new Dictionary<string, MembershipUser>(16, StringComparer.InvariantCultureIgnoreCase);
                    XmlDocument doc = new XmlDocument();
                    doc.Load(_XmlFileName);
                    XmlNodeList nodes = doc.GetElementsByTagName("User");

                    foreach (XmlNode node in nodes)
                    {
                        MembershipUser user = new MembershipUser(
                            Name,                       // Provider name
                            node["UserName"].InnerText, // Username
                            node["UserName"].InnerText, // providerUserKey
                            node["Email"].InnerText,    // Email
                            String.Empty,               // passwordQuestion
                            node["Password"].InnerText, // Comment
                            true,                       // isApproved
                            false,                      // isLockedOut
                            DateTime.Now,               // creationDate
                            DateTime.Parse(node["LastLoginTime"].InnerText), // lastLoginDate
                            DateTime.Now,               // lastActivityDate
                            DateTime.Now, // lastPasswordChangedDate
                            new DateTime(1980, 1, 1)    // lastLockoutDate
                        );

                        _Users.Add(user.UserName, user);
                    }
                }
            }
        }

        /// <summary>
        /// Encrypts a string using the SHA256 algorithm.
        /// </summary>
        private static string Encrypt(string plainMessage)
        {
            byte[] data = Encoding.UTF8.GetBytes(plainMessage);
            using (HashAlgorithm sha = new SHA256Managed())
            {
                byte[] encryptedBytes = sha.TransformFinalBlock(data, 0, data.Length);
                return Convert.ToBase64String(sha.Hash);
            }
        }

        #endregion

        #region Unsupported methods

        public override string ResetPassword(string username, string answer)
        {
            throw new NotSupportedException();
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotSupportedException();
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotSupportedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotSupportedException();
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotSupportedException();
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotSupportedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotSupportedException();
        }

        #endregion

    }

}
