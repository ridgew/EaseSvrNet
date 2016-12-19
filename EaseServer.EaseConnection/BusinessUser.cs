using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using CommonLib;
using Gwsoft.EaseMode;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 业务用户分配与获取
    /// </summary>
    public class BusinessUser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessUser"/> class.
        /// </summary>
        private BusinessUser() { }


        /// <summary>
        /// 当前用户请求
        /// </summary>
        /// <value>The request.</value>
        public RequestBase Request { get; set; }

        /// <summary>
        /// 业务编号
        /// </summary>
        public short BusinessID { get; set; }

        static object instanceLock = new object();

        static Dictionary<short, BusinessUser> BizAssignDict = new Dictionary<short, BusinessUser>();

        /// <summary>
        /// 获取相关业务的配置实例
        /// </summary>
        /// <param name="bid">相关业务代号</param>
        /// <returns></returns>
        public static BusinessUser GetInstance(short bid)
        {
            lock (instanceLock)
            {
                if (!BizAssignDict.ContainsKey(bid))
                {
                    BusinessUser biz = new BusinessUser();
                    biz.BusinessID = bid;
                    BizAssignDict.Add(bid, biz);
                }
                return BizAssignDict[bid];
            }
        }



        /// <summary>
        /// 获取当前请求用户的标识
        /// </summary>
        /// <returns></returns>
        public long GetCurrentUserID(RequestBase request, EaseUser currentUser)
        {
            Request = request;
            string sqlFormat = (string)request["DataExchange.UserAssignFormat"];

            if (string.IsNullOrEmpty(sqlFormat))
            {
                return currentUser.SOFTWARE_ID;
            }
            else
            {
                request["DataExchange.UserAssignFormat"] = null;
                SqlConnection sharedConn = request["DataExchange.SharedDbConnection"] as SqlConnection;
                string sql = sqlFormat.PropertyFormat(null, 1, currentUser);
                SqlCommand cmd = new SqlCommand(sql, sharedConn);
                long UserID = Convert.ToInt64(cmd.ExecuteScalar());
                cmd.Dispose();
                return UserID;
            }
        }
    }
}
