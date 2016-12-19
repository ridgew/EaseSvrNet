using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Services;
using System.Web.Script.Services;
using System.Web.Script.Serialization;
using EaseServer.Management.DataAccess;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    ///系统角色管理服务
    /// </summary>
    [WebService(Name = "系统角色管理服务", Description = "提供用于管理系统角色的相应方法")]
    public class RoleService : WebServiceBase
    {
        [Protocol("1.3.5.2.1"), WebMethod(Description = "获取系统所有角色信息列表")]
        [GenerateScriptType(typeof(SvcRoleListContent), ScriptTypeId = "0")]
        public SvcRoleListContent GetAllRoles()
        {
            return new SvcRoleListContent
            {
                Data = EaseDataProvider.Instance.GetAllRoles()
            };
        }
    }

    /// <summary>
    /// 系统角色列表内容
    /// </summary>
    [Serializable]
    public class SvcRoleListContent : ResultBase
    {
        /// <summary>
        /// 获取或设置系统角色列表信息
        /// </summary>
        public RoleInfo[] Data { get; set; }

        /// <summary>
        /// 构造系统角色列表内容
        /// </summary>
        public SvcRoleListContent()
        {
            this.Protocol = "1.3.5.2.1";
            this.Status = 1;
            this.Message = "系统角色列表内容";
        }
    }

    /// <summary>
    ///角色基本信息
    /// </summary>
    [Serializable]
    public class RoleInfo
    {
        /// <summary>
        /// 获取或设置角色ID
        /// </summary>
        public virtual int RoleId { get; set; }

        /// <summary>
        /// 获取或设置应用程序ID
        /// </summary>
        [ScriptIgnore]
        public int ApplicationId { get; set; }

        /// <summary>
        /// 获取或设置角色名称
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// 获取或设置角色描述信息
        /// </summary>
        public string Description { get; set; }
    }

}
