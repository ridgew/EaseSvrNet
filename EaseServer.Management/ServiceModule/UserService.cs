using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Services;
using System.Web.Script.Services;
using System.Web.Script.Serialization;
using CommonLib;
using EaseServer.Management.Administration;
using System.Web.Security;
using EaseServer.Management.DataAccess;
using System.Data;
using System.Text.RegularExpressions;
using System.Web;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    ///系统用户管理服务
    /// </summary>
    [WebService(Name = "系统用户管理服务", Description = "提供用于管理系统用户的相应方法")]
    public class UserService : WebServiceBase
    {
        /// <summary>
        /// 获取当前用户id
        /// </summary>
        /// <returns></returns>
        [Protocol("1.3.5.3.9"), WebMethod(Description = "获取当前用户id")]
        [GenerateScriptType(typeof(currentUserId), ScriptTypeId = "0")]
        public currentUserId GetCurrentUserId()
        {
            return new currentUserId
            {
                Protocol = "1.3.5.3.9",
                Status = 1,
                Message = "获取当前用户id",
                UserId = 1
            };
        }

        /// <summary>
        /// 获取系统用户分页列表数据
        /// </summary>
        /// <param name="pageIndex">当前页码，1表示第一页。</param>
        /// <param name="pageSize">每页显示内容的条数</param>
        /// <returns>系统用户分页列表数据</returns>
        [Protocol("1.3.5.3.2"), WebMethod(Description = "获取系统用户分页列表数据")]
        [GenerateScriptType(typeof(UserInfo), ScriptTypeId = "0")]
        public SvcPagingRecord<UserInfo> GetUserList(int pageIndex, int pageSize)
        {
            if (pageIndex < 1)
                pageIndex = 1;
            if (pageSize < 1)
                pageSize = 20;

            int recordCount = 0;

            //DataTable tableRecords = EaseDataProvider.Instance.GetPagingRecords(out recordCount, "gw_Admin_Users",
            //    new string[] { "UserId", "UserName", "RealName", "Email", "Mobile", "IsApproved", "IsLockedOut", "CreateDate", "CreateIP", "LastLoginDate",
            //        "LastLoginIP", "LoginCount", "LastPasswordChangedDate", "LastLockedoutDate", "FailedPasswordAttemptCount", "FailedPasswordAttemptWindowStart", "Comment" },
            //        "UserId", true, pageIndex, pageSize, true, "ApplicationId=@0", ApplicationManager.GetApplicationId());

            SvcPagingRecord<UserInfo> pagingRecords = new SvcPagingRecord<UserInfo>
            {
                Protocol = "1.3.5.3.2",
                Status = 1,
                Message = "获取用户分页列表数据",
                PageIndex = pageIndex,
                PageSize = pageSize,
                RecordCount = recordCount,

                Data = new UserInfo[0] { }

                //Data = (from p in tableRecords.AsEnumerable()
                //        select new UserInfo
                //        {
                //            UserId = p.Field<int>("UserId"),
                //            UserName = p.Field<string>("UserName"),
                //            RealName = p.Field<string>("RealName"),
                //            Email = p.Field<string>("Email"),
                //            Mobile = p.Field<string>("Mobile"),
                //            IsApproved = p.Field<bool>("IsApproved"),
                //            IsLockedOut = p.Field<bool>("IsLockedOut"),
                //            CreateDate = p.Field<DateTime>("CreateDate"),
                //            CreateIP = p.Field<string>("CreateIP"),
                //            LastLoginDate = (p["LastLoginDate"] is DBNull) ? DateTime.MinValue : p.Field<DateTime>("LastLoginDate"),
                //            LastLoginIP = p.Field<string>("LastLoginIP"),
                //            LoginCount = p.Field<int>("LoginCount"),
                //            LastPasswordChangedDate = (p["LastPasswordChangedDate"] is DBNull) ? DateTime.MinValue : p.Field<DateTime>("LastPasswordChangedDate"),
                //            LastLockedoutDate = (p["LastLockedoutDate"] is DBNull) ? DateTime.MinValue : p.Field<DateTime>("LastLockedoutDate"),
                //            FailedPasswordAttemptCount = p.Field<int>("FailedPasswordAttemptCount"),
                //            FailedPasswordAttemptWindowStart = (p["FailedPasswordAttemptWindowStart"] is DBNull) ? DateTime.MinValue : p.Field<DateTime>("FailedPasswordAttemptWindowStart"),
                //            Comment = p.Field<string>("Comment")

                //        }).ToArray()

            };
            return pagingRecords;

        }

        /// <summary>
        /// 增加系统新用户
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="realname">真实姓名</param>
        /// <param name="email">电子邮件地址</param>
        /// <param name="mobile">移动电话</param>
        /// <param name="isApproved">是否审核通过</param>
        /// <param name="isLockedOut">是否锁定</param>
        /// <param name="lockedoutDate">锁定到期时间</param>
        /// <param name="comment">备注</param>
        /// <param name="roleNames">角色名称列表</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.3.3"), WebMethod(Description = "增加系统新用户")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result CreateUser(string username, string password, string realname, string email, string mobile, bool isApproved, bool isLockedOut,
            DateTime lockedoutDate, string comment, string[] roleNames)
        {
            Result result = new Result { Protocol = "1.3.5.3.3" };
            if (string.IsNullOrEmpty(username) || (!Regex.IsMatch(username, "^[a-zA-Z]{1}([a-zA-Z0-9]|[._-]){2,19}$")))
                result.Status = 10020;
            else if (string.IsNullOrEmpty(password))
                result.Status = 10021;
            else
            {
                MembershipProvider membership = MembershipManager.Instance;
                UserInfo admin = new UserInfo
                {
                    UserName = username,
                    Password = password,
                    RealName = realname,
                    Email = email,
                    Mobile = mobile,
                    IsApproved = isApproved,
                    IsLockedOut = isLockedOut,
                    Comment = comment,
                    CreateIP = HttpContext.Current.Request.UserHostAddress,
                    InviterId = membership.GetCurrentUser().Identity.Name
                };

                if (isLockedOut && lockedoutDate > DateTime.MinValue)
                    admin.LastLockedoutDate = lockedoutDate;

                //CreateUserStatus status = membership.CreateUser(admin);
                //if (status == CreateUserStatus.DuplicateUserName)
                //    result.Status = 10017;
                //else if (status == CreateUserStatus.Success)
                //{
                //    membership.AddUsersToRoles(new List<UserInfo> { admin }, roleNames);
                result.Status = 10019;
                //}
                //else if (status == CreateUserStatus.Error)
                //    result.Status = 10018;
            }
            return result;
        }

        /// <summary>
        /// 获取用户基本信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户基本信息</returns>
        [Protocol("1.3.5.3.4"), WebMethod(Description = "获取用户基本信息")]
        [GenerateScriptType(typeof(UserInfo), ScriptTypeId = "0")]
        public SvcSingleRecord<UserInfo> GetUserById(int userId)
        {
            SvcSingleRecord<UserInfo> result = new SvcSingleRecord<UserInfo>
            {
                Protocol = "1.3.5.3.4",
                Message = "获取用户基本信息",
                Data = new UserInfo
                {
                    UserId = userId,
                    UserName = MembershipManager.Instance.GetCurrentUser().Identity.Name, //"wangqj",
                    RealName = "测试者",
                    Email = "wangqj@gwsoft.com.cn",
                    Mobile = "",
                    IsApproved = true,
                    IsLockedOut = false,
                    CreateDate = "2008-06-16 10:14:45".As<DateTime>(),
                    CreateIP = "192.168.8.93",
                    LastLoginDate = "2010-08-13 08:52:32".As<DateTime>(),
                    LastLoginIP = "125.71.212.83",
                    LoginCount = 194,
                    LastPasswordChangedDate = "2008-07-23 09:35:17".As<DateTime>(),
                    LastLockedoutDate = DateTime.MinValue,
                    FailedPasswordAttemptCount = 0,
                    FailedPasswordAttemptWindowStart = DateTime.MinValue,
                    Comment = "",
                    Roles = new string[] { "administrators" }

                }
            };
            if (result.Data == null)
                result.Status = 0;
            else
                result.Status = 1;
            return result;
        }

        /// <summary>
        /// 更新用户信息[TODO]
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="password">新密码</param>
        /// <param name="realname">真实姓名</param>
        /// <param name="email">电子邮件地址</param>
        /// <param name="mobile">移动电话</param>
        /// <param name="isApproved">是否审核通过</param>
        /// <param name="isLockedOut">是否锁定</param>
        /// <param name="lockedoutDate">锁定到期时间</param>
        /// <param name="comment">备注</param>
        /// <param name="roleNames">角色名称列表</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.3.5"), WebMethod(Description = "更新用户信息")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result UpdateUser(int userId, string password, string realname, string email, string mobile, bool isApproved, bool isLockedOut,
            DateTime lockedoutDate, string comment, string[] roleNames)
        {
            Result result = new Result { Protocol = "1.3.5.3.5" };
            // EaseDataProvider.Instance.GetRecordsCount("gw_Admin_Users", true, "UserId=@0 AND ApplicationId=@1", userId, ApplicationManager.GetApplicationId()) == 1
            if (userId > 0)
            {
                MembershipProvider membership = MembershipManager.Instance;
                UserInfo admin = new UserInfo { UserId = userId };
                //if (!string.IsNullOrEmpty(password))
                //membership.ChangePassword(admin, password);

                //EaseDataProvider.Instance.UpdateTable("gw_Admin_Users", new string[] { "RealName", "Email", "Mobile", "IsApproved", "IsLockedOut", "LastLockedoutDate", "Comment" }, new object[] { realname, email, mobile, isApproved, isLockedOut, (lockedoutDate.Equals(DateTime.MinValue) ? (object)DBNull.Value : (object)lockedoutDate), comment }, true, "UserId=@0 AND ApplicationId=@1", userId, ApplicationManager.GetApplicationId());
                //IList<IUser> userList = new List<IUser> { admin };
                //membership.RemoveUsersFromRoles(userList, membership.GetRolesForUser(admin));
                //membership.AddUsersToRoles(userList, roleNames);

                result.Status = 10023;
            }
            else
                result.Status = 10022;
            return result;
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.3.6"), WebMethod(Description = "删除用户")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result DeleteUser(int userId)
        {
            Result result = new Result { Protocol = "1.3.5.3.6" };
            //if (EaseDataProvider.Instance.DeleteTable("gw_Admin_Users", true, "UserId=@0 AND ApplicationId=@1", userId, ApplicationManager.GetApplicationId()) == 1)
            result.Status = 10025;
            //else
            //    result.Status = 10024;
            return result;
        }
    }

    /// <summary>
    /// 当前用户Id类
    /// </summary>
    public class currentUserId : ResultBase
    {
        public int UserId { get; set; }
    }

    /// <summary>
    /// 系统用户类
    /// </summary>
    [Serializable]
    public class UserInfo
    {

        #region SiteAdministrator
        /// <summary>
        /// 获取或设置个人注释或评论
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// 获取或设置账号创建的时间
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// 获取或设置账号创建时客户端IP信息
        /// </summary>
        public string CreateIP { get; set; }
        //
        // 摘要:
        //     获取或设置电子邮箱
        public string Email { get; set; }

        /// <summary>
        /// 获取或设置当前提示问题答案连续错误的次数
        /// </summary>
        public int FailedPasswordAnswerAttemptCount { get; set; }

        /// <summary>
        /// 获取或设置当前提示问题答案连续错误第一次开始的时间
        /// </summary>
        public DateTime FailedPasswordAnswerAttemptWindowStart { get; set; }

        /// <summary>
        /// 获取或设置当前密码错误的连续次数
        /// </summary>
        public int FailedPasswordAttemptCount { get; set; }

        /// <summary>
        /// 获取或设置当前密码连续错误第一次开始的时间
        /// </summary>
        public DateTime FailedPasswordAttemptWindowStart { get; set; }

        /// <summary>
        ///  获取或设置用户账号是否已审核通过
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// 获取或设置用户账号是否已被锁定
        /// </summary>
        public bool IsLockedOut { get; set; }

        /// <summary>
        /// 获取或设置用户最近一次账户被锁定的时间
        /// </summary>
        /// <value>The last lockedout date.</value>
        public DateTime LastLockedoutDate { get; set; }

        /// <summary>
        /// 获取或设置用户最近一次登录的时间
        /// </summary>
        /// <value>The last login date.</value>
        public DateTime LastLoginDate { get; set; }

        /// <summary>
        /// 获取或设置用户最近一次登录的客户端IP信息
        /// </summary>
        /// <value>The last login IP.</value>
        public string LastLoginIP { get; set; }

        /// <summary>
        /// 获取或设置用户最近一次修改密码的时间
        /// </summary>
        /// <value>The last password changed date.</value>
        public DateTime LastPasswordChangedDate { get; set; }

        /// <summary>
        /// 获取或设置用户成功登录过的次数
        /// </summary>
        /// <value>The login count.</value>
        public int LoginCount { get; set; }

        /// <summary>
        /// 获取或设置移动电话号码
        /// </summary>
        /// <value>The mobile.</value>
        public string Mobile { get; set; }

        /// <summary>
        /// 获取或设置真实姓名
        /// </summary>
        /// <value>The name of the real.</value>
        public string RealName { get; set; }

        /// <summary>
        ///  获取或设置用户ID
        /// </summary>
        /// <value>The user id.</value>
        public object UserId { get; set; }

        /// <summary>
        /// 获取或设置用户名
        /// </summary>
        /// <value>The name of the user.</value>
        public string UserName { get; set; }
        #endregion

        /// <summary>
        /// 获取或设置WEB应用程序ID
        /// </summary>
        [ScriptIgnore]
        public int ApplicationId { get; set; }

        /// <summary>
        /// 获取或设置用户密码
        /// </summary>
        [ScriptIgnore]
        public string Password { get; set; }

        /// <summary>
        /// 获取或设置密码加密格式
        /// </summary>
        [ScriptIgnore]
        public PasswordFormat PasswordFormat { get; set; }

        /// <summary>
        /// 获取或设置密码加密前混淆字符串
        /// </summary>
        [ScriptIgnore]
        public string PasswordSalt { get; set; }

        /// <summary>
        /// 获取或设置找回密码提示问题
        /// </summary>
        [ScriptIgnore]
        public string PasswordQuestion { get; set; }

        /// <summary>
        /// 获取或设置找回密码提示问题的答案
        /// </summary>
        [ScriptIgnore]
        public string PasswordAnswer { get; set; }

        /// <summary>
        /// 获取或设置邀请人ID或创建本账户的用户ID
        /// </summary>
        [ScriptIgnore]
        public object InviterId { get; set; }

        /// <summary>
        /// 获取或设置用户所属角色列表信息
        /// </summary>
        public string[] Roles { get; set; }
    }

    /// <summary>
    /// 密码加密格式
    /// </summary>
    public enum PasswordFormat
    {
        /// <summary>
        /// 明文密码
        /// </summary>
        ClearText = 0,

        /// <summary>
        /// MD5加密
        /// </summary>
        MD5 = 1,

        /// <summary>
        /// Sha1加密
        /// </summary>
        Sha1 = 2,

        /// <summary>
        /// 加Salt后MD5加密
        /// </summary>
        MD5Hash = 3,

        /// <summary>
        /// 加Salt后Sha1加密
        /// </summary>
        Sha1Hash = 4,
    }

    /// <summary>
    /// 创建新用户枚举状态
    /// </summary>
    public enum CreateUserStatus
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success,

        /// <summary>
        /// 用户名无效
        /// </summary>
        InvalidUserName,

        /// <summary>
        /// 密码无效
        /// </summary>
        InvalidPassword,

        /// <summary>
        /// 提示问题无效
        /// </summary>
        InvalidQuestion,

        /// <summary>
        /// 提示问题答案无效
        /// </summary>
        InvalidAnswer,

        /// <summary>
        /// 电子邮箱无效
        /// </summary>
        InvalidEmail,

        /// <summary>
        /// 用户名已存在
        /// </summary>
        DuplicateUserName,

        /// <summary>
        /// 电子邮箱已存在
        /// </summary>
        DuplicateEmail,

        /// <summary>
        /// 用户被拒绝
        /// </summary>
        UserRejected,

        /// <summary>
        /// 处理出错
        /// </summary>
        Error
    }

}
