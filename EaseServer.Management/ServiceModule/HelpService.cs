using System;
using System.Web.Services;
using Gwsoft.SharpOrm;
using Gwsoft.SharpOrm.Config;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    ///帮助操作服务
    /// </summary>
    [WebService(Name = "帮助管理", Description = "帮助信息的添加、删除、修改、列表等操作")]
    public class HelpService : WebServiceBase
    {

        [Protocol("1.3.6.1.1"), WebMethod(Description = "添加/修改帮助信息")]
        public SvcHelp Edit(int MenuId, string HelpTitle, string HelpContent)
        {
            SvcHelp svchelp = new SvcHelp();
            svchelp.Edit(MenuId, HelpTitle, HelpContent);
            return svchelp;
        }


        [Protocol("1.3.6.1.2"), WebMethod(Description = "获取帮助信息")]
        public SvcHelp Get(int MenuId)
        {
            SvcHelp svchelp = new SvcHelp();
            svchelp.Get(MenuId);
            return svchelp;
        }

    }

    /// <summary>
    /// 实体类Help
    /// </summary>
    [Serializable]
    [BindTable("DefaultDB", "gw_Assist_Help")]
    public class Help : TableEntry
    {

        #region Model
        private int _menuid;
        private string _helptitle;
        private string _helpcontent;
        private DateTime _createdate;
        private DateTime _lastmodify;
        /// <summary>
        /// 关联的菜单ID
        /// </summary>
        [PrimaryKey(true)]
        public int MenuId
        {
            set { _menuid = value; }
            get { return _menuid; }
        }
        /// <summary>
        /// 帮助标题
        /// </summary>
        public string HelpTitle
        {
            set { _helptitle = value; }
            get { return _helptitle; }
        }
        /// <summary>
        /// 帮助内容
        /// </summary>
        public string HelpContent
        {
            set { _helpcontent = value; }
            get { return _helpcontent; }
        }
        /// <summary>
        /// 帮助创建时间
        /// </summary>
        public DateTime CreateDate
        {
            set { _createdate = value; }
            get { return _createdate; }
        }
        /// <summary>
        /// 最后一次修改时间
        /// </summary>
        public DateTime LastModify
        {
            set { _lastmodify = value; }
            get { return _lastmodify; }
        }
        #endregion Model

    }

    /// <summary>
    ///SvcHelp 的摘要说明
    /// </summary>
    [Serializable]
    public class SvcHelp : ResultBase
    {

        public Help Data;

        /// <summary>
        /// 添加/修改帮助信息
        /// </summary>
        /// <param name="MenuId">菜单ID</param>
        /// <param name="HelpTitle">帮助标题</param>
        /// <param name="HelpContent">帮助内容</param>
        public void Edit(int MenuId, string HelpTitle, string HelpContent)
        {
            this.Protocol = "1.3.6.1.1";
            try
            {
                Help help = new Help
                {
                    HelpTitle = HelpTitle,
                    HelpContent = HelpContent,
                    LastModify = DateTime.Now
                };
                int result = new Help { MenuId = MenuId }.ReplaceWith(help);
                if (result < 1)
                {
                    help.MenuId = MenuId;
                    help.Insert();
                }
                this.Message = "帮助信息修改成功";
                this.Status = 1;
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
                this.Status = 0;
            }
        }

        /// <summary>
        /// 获取单条帮助信息实例
        /// </summary>
        /// <param name="MenuId">菜单ID</param>
        public void Get(int MenuId)
        {
            this.Protocol = "1.3.6.1.2";
            try
            {
                Help help = new Help { MenuId = MenuId };
                help.DataBind();
                this.Data = help;
                this.Message = "";
                this.Status = 1;
            }
            catch (NotExistException)
            {
                this.Data = new Help();
                this.Message = "";
                this.Status = 1;
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
                this.Status = 0;
            }
        }
    }
}
