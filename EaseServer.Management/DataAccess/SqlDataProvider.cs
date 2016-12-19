using EaseServer.Management.ServiceModule;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Data;
using System.Data.SqlClient;

namespace EaseServer.Management.DataAccess
{
    /// <summary>
    /// SQL Server数据库访问器
    /// </summary>
    public partial class SqlDataProvider : EaseDataProvider
    {
        /// <summary>
        /// 获取数据库版本信息
        /// </summary>
        /// <returns>版本信息</returns>
        public override string GetDatabaseVersion()
        {
            return DatabaseFactory.CreateDatabase().ExecuteScalar("select @@version").ToString();
        }

        /// <summary>
        /// 获取系统所有角色信息列表
        /// </summary>
        /// <returns>角色信息列表</returns>
        public override RoleInfo[] GetAllRoles()
        {
            return new RoleInfo[] { 
                new RoleInfo { ApplicationId = 1, RoleId = 1, RoleName = "administrators", Description = "管理员" }
            };
        }

        /// <summary>
        /// 获取菜单列表
        /// </summary>
        /// <param name="parentId">父级菜单ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>用户ID所属权限下、父级菜单ID下的子菜单列表</returns>
        public override MenuItem[] GetMenuList(int parentId, object userId)
        {
            /*
            return (from p in Database.ExecuteDataTable(@"SELECT  a.*
FROM    [gw_Admin_Menus] a
        INNER JOIN ( SELECT DISTINCT
                            a.[MenuId]
                     FROM   [gw_Admin_Menus] a
                            INNER JOIN [gw_Admin_MenusInRoles] b ON a.[MenuId] = b.[MenuId]
                            INNER JOIN [gw_Admin_UsersInRoles] c ON c.[RoleId] = b.[RoleId]
                     WHERE  a.[ParentId] = @ParentId
                            AND a.[ApplicationId] = @ApplicationId
                            AND c.[UserId] = @UserId
                   ) b ON a.[MenuId] = b.[MenuId]
ORDER BY a.[OrderNum] DESC,
        a.[MenuId] ASC", "ParentId,ApplicationId,UserId", new object[] { parentId, ApplicationManager.GetApplicationId(), userId }).AsEnumerable()
                    select new MenuItem
                    {
                        ApplicationId = p.Field<int>("ApplicationId"),
                        CreateDate = p.Field<DateTime>("CreateDate"),
                        CreatorUserId = p.Field<int>("CreatorUserId"),
                        Depth = p.Field<int>("Depth"),
                        Enabled = p.Field<bool>("Enabled"),
                        KeyCode = p.Field<int>("KeyCode"),
                        LeftUrl = p.Field<string>("LeftUrl"),
                        MenuId = p.Field<int>("MenuId"),
                        MenuName = p.Field<string>("MenuName"),
                        OrderNum = p.Field<int>("OrderNum"),
                        ParentId = p.Field<int>("ParentId"),
                        RightUrl = p.Field<string>("RightUrl")
                    }).ToArray<MenuItem>();
             */
            return new MenuItem[0];

        }

        public override DataTable GetPagingRecords(out int recordCount, string tableName, string[] tableFields, string[] orderFields, bool[] ordersbyDesc, int pageIndex, int pageSize, bool enableParameters, string where, params object[] parameters)
        {
            string order = string.Empty;
            for (int i = 0; i < orderFields.Length; i++)
            {
                order += (orderFields[i] + (ordersbyDesc[i] ? " DESC" : " ASC")) + ",";
            }
            order = order.TrimEnd(',');
            using (SqlCommand cmd = new SqlCommand("gw_Table_ShowPaging"))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                DataAccessExtension.AddInputParameters(cmd, "tableName,where,outputType,paraType,tableFields,orderFields,pageIndex,pageSize",
                    tableName, where, 2, 0, string.Join(",", tableFields), order, pageIndex, pageSize);

                DataAccessExtension.SetParametersSize(cmd, "tableName,where,tableFields,orderFields", 64, 512, 512, 256);
                if (enableParameters)
                {
                    cmd.Parameters["paraType"].Value = 1;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        DataAccessExtension.AddInputParameters(cmd, i.ToString(), parameters[i]);
                        DataAccessExtension.SetParametersSize(cmd, i.ToString(), 128);
                    }
                }
                Database Database = DatabaseFactory.CreateDatabase("gwease");
                DataSet ds = Database.ExecuteDataSet(cmd);
                recordCount = Convert.ToInt32(ds.Tables[0].Rows[0][0]);
                return ds.Tables[1];
            }
        }

        public override DataTable SelectTable(string tableName, string[] tableFields, string[] orderFields, bool[] ordersbyDesc, bool enableParameters, string where, params object[] parameters)
        {
            string order = string.Empty;
            for (int i = 0; i < orderFields.Length; i++)
            {
                if (!string.IsNullOrEmpty(orderFields[i]))
                    order += (orderFields[i] + (ordersbyDesc[i] ? " DESC" : " ASC")) + ",";
            }
            order = order.TrimEnd(',');
            using (SqlCommand cmd = new SqlCommand())
            {
                string sql = "select " + string.Join(",", tableFields) + " from " + tableName;
                if (!string.IsNullOrEmpty(where))
                    sql += " where " + where;
                if (!string.IsNullOrEmpty(order))
                    sql += " order by " + order;
                if (enableParameters)
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        DataAccessExtension.AddInputParameters(cmd, i.ToString(), parameters[i]);
                    }
                }
                cmd.CommandText = sql;
                return cmd.ExecuteDataTable();
            }
        }
    
    }
}
