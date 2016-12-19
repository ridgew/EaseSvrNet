using EaseServer.Management.ServiceModule;
using System.Data;

namespace EaseServer.Management.DataAccess
{
    /// <summary>
    /// Ease通用数据库访问器基类
    /// </summary>
    public abstract partial class EaseDataProvider
    {
        private static readonly EaseDataProvider _baseProvider;

        static EaseDataProvider()
        {
            _baseProvider = new SqlDataProvider();
        }

        /// <summary>
        /// 获取默认数据库访问实例
        /// </summary>
        public static EaseDataProvider Instance
        {
            get
            {
                return _baseProvider;
            }
        }

        #region 数据库访问业务

        /// <summary>
        /// 获取数据库版本信息
        /// </summary>
        /// <returns>版本信息</returns>
        public abstract string GetDatabaseVersion();

        /// <summary>
        /// 获取系统所有角色信息列表
        /// </summary>
        /// <returns>角色信息列表</returns>
        public abstract RoleInfo[] GetAllRoles();

        /// <summary>
        /// 获取菜单列表
        /// </summary>
        /// <param name="parentId">父级菜单ID</param>
        /// <param name="userId">用户ID对象(用户名称)</param>
        /// <returns>用户ID所属权限下、父级菜单ID下的子菜单列表</returns>
        public abstract MenuItem[] GetMenuList(int parentId, object userId);

        public virtual DataTable GetPagingRecords(out int recordCount, string tableName, string[] tableFields, string[] orderFields, bool[] ordersbyDesc, int pageIndex, int pageSize, string where)
        {
            return GetPagingRecords(out recordCount, tableName, tableFields, orderFields, ordersbyDesc, pageIndex, pageSize, false, where, null);
        }

        public virtual DataTable GetPagingRecords(out int recordCount, string tableName, string[] tableFields, string[] orderFields, bool[] ordersbyDesc, int pageIndex, int pageSize)
        {
            return GetPagingRecords(out recordCount, tableName, tableFields, orderFields, ordersbyDesc, pageIndex, pageSize, "");
        }

        public abstract DataTable GetPagingRecords(out int recordCount, string tableName, string[] tableFields, string[] orderFields, bool[] ordersbyDesc, int pageIndex, int pageSize, bool enableParameters, string where, params object[] parameters);

        #region SelectTable
        public virtual DataTable SelectTable(string tableName)
        {
            return SelectTable(tableName, "");
        }

        public virtual DataTable SelectTable(string tableName, string where)
        {
            return SelectTable(tableName, false, where, null);
        }

        public virtual DataTable SelectTable(string tableName, bool enableParameters, string where, params object[] parameters)
        {
            return SelectTable(tableName, new string[] { "*" }, enableParameters, where, parameters);
        }

        public virtual DataTable SelectTable(string tableName, string[] tableFields)
        {
            return SelectTable(tableName, tableFields, "");
        }

        public virtual DataTable SelectTable(string tableName, string[] tableFields, string where)
        {
            return SelectTable(tableName, tableFields, false, where, null);
        }

        public virtual DataTable SelectTable(string tableName, string[] tableFields, bool enableParameters, string where, params object[] parameters)
        {
            return SelectTable(tableName, tableFields, "", false, enableParameters, where, parameters);
        }

        public virtual DataTable SelectTable(string tableName, string orderField, bool orderbyDesc)
        {
            return SelectTable(tableName, orderField, orderbyDesc, "");
        }

        public virtual DataTable SelectTable(string tableName, string orderField, bool orderbyDesc, string where)
        {
            return SelectTable(tableName, orderField, orderbyDesc, false, where, null);
        }

        public virtual DataTable SelectTable(string tableName, string orderField, bool orderbyDesc, bool enableParameters, string where, params object[] parameters)
        {
            return SelectTable(tableName, new string[] { "*" }, orderField, orderbyDesc, enableParameters, where, parameters);
        }

        public virtual DataTable SelectTable(string tableName, string[] tableFields, string orderField, bool orderbyDesc)
        {
            return SelectTable(tableName, tableFields, orderField, orderbyDesc, "");
        }

        public virtual DataTable SelectTable(string tableName, string[] tableFields, string orderField, bool orderbyDesc, string where)
        {
            return SelectTable(tableName, tableFields, orderField, orderbyDesc, false, where, null);
        }

        public virtual DataTable SelectTable(string tableName, string[] tableFields, string orderField, bool orderbyDesc, bool enableParameters, string where, params object[] parameters)
        {
            return SelectTable(tableName, tableFields, new string[] { orderField }, new bool[] { orderbyDesc }, enableParameters, where, parameters);
        }



        public virtual DataTable SelectTable(string tableName, string[] orderFields, bool[] ordersbyDesc)
        {
            return SelectTable(tableName, orderFields, ordersbyDesc, "");
        }

        public virtual DataTable SelectTable(string tableName, string[] orderFields, bool[] ordersbyDesc, string where)
        {
            return SelectTable(tableName, orderFields, ordersbyDesc, false, where, null);
        }

        public virtual DataTable SelectTable(string tableName, string[] orderFields, bool[] ordersbyDesc, bool enableParameters, string where, params object[] parameters)
        {
            return SelectTable(tableName, new string[] { "*" }, orderFields, ordersbyDesc, enableParameters, where, parameters);
        }

        public virtual DataTable SelectTable(string tableName, string[] tableFields, string[] orderFields, bool[] ordersbyDesc)
        {
            return SelectTable(tableName, tableFields, orderFields, ordersbyDesc, "");
        }

        public virtual DataTable SelectTable(string tableName, string[] tableFields, string[] orderFields, bool[] ordersbyDesc, string where)
        {
            return SelectTable(tableName, tableFields, orderFields, ordersbyDesc, false, where, null);
        }

        public abstract DataTable SelectTable(string tableName, string[] tableFields, string[] orderFields, bool[] ordersbyDesc, bool enableParameters, string where, params object[] parameters);
        #endregion

        #endregion
    }



}
