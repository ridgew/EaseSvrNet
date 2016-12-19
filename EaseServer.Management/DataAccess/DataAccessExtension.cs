using System;
using System.Data;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System.Data.Common;

namespace EaseServer.Management.DataAccess
{
    public static class DataAccessExtension
    {
        public static void AddInputParameters(IDbCommand cmd, string paramNames, params object[] paramValues)
        {
            string[] allParamNames = paramNames.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0, j = allParamNames.Length; i < j; i++)
            {
                IDbDataParameter param = cmd.CreateParameter();
                param.ParameterName = allParamNames[i];
                param.Value = paramValues[i];
                cmd.Parameters.Add(param);
            }
        }

        public static void SetParametersSize(IDbCommand cmd, string paramNames, params int[] paramSizes)
        {
            string[] allParamNames = paramNames.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0, j = allParamNames.Length; i < j; i++)
            {
                IDbDataParameter param = (IDbDataParameter)cmd.Parameters[allParamNames[i]];
                param.Size = paramSizes[i];
            }
        }

        /// <summary>
        /// 业务服务器直接执行数据命令
        /// </summary>
        public static DataTable ExecuteDataTable(this DbCommand cmd)
        {
            return ExecuteDataTable("gwease", cmd);
        }

        public static DataTable ExecuteDataTable(this string connKey, DbCommand cmd)
        {
            if (string.IsNullOrEmpty(connKey)) connKey = "DefaultDB";
            Database Database = DatabaseFactory.CreateDatabase(connKey);
            return Database.ExecuteDataSet(cmd).Tables[0];
        }

        /// <summary>
        /// 获取列表数据的数据表
        /// </summary>
        /// <param name="connKey">数据库连接字符键值,默认为DefaultDB。</param>
        /// <param name="pageIndex">当前请求页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <param name="sfArr">查询字段数组</param>
        /// <param name="scArr">查询条件数组</param>
        /// <param name="svArr">查询字段对应值数组</param>
        /// <param name="tbName">查询的表名</param>
        /// <param name="selectFields">查询的字段</param>
        /// <param name="order">排序</param>
        /// <returns>返回数据表</returns>
        public static DataTable GetInfoForListDataTable(this string connKey, int pageIndex, int pageSize,
            string[] sfArr, int[] scArr, object[] svArr, string tbName, string selectFields, string order)
        {
            CommoninfoForSearch seInfo = GetSearchInfoCommon(sfArr, scArr, svArr);
            string Sql = "Select Top {0} {1} From {2} {3} {4}";
            if (pageIndex > 1)
                Sql = "Select Top {0} * From(Select {1},Row_Number() Over({4}) As RowNum From {2} {3}) As List Where RowNum > " + (pageIndex - 1) * pageSize;

            if (string.IsNullOrEmpty(connKey)) connKey = "DefaultDB";
            Database Database = DatabaseFactory.CreateDatabase(connKey);
            return Database.ExecuteDataTable(string.Format(Sql, pageSize, selectFields, tbName, seInfo.WhereStr, order), seInfo.FieldParams, seInfo.ParamValues);
        }

        /// <summary>
        /// 获取查询条件公用方法
        /// </summary>
        /// <param name="sfArr">查询字段数组</param>
        /// <param name="scArr">查询条件数组 0:"=",1:"<=",2:">=",3:"<",4:">",5:"<>",6:"like",7:"charindex"。</param>
        /// <param name="svArr">查询字段对应值数组</param>
        /// <returns>返回查询条件</returns>
        public static CommoninfoForSearch GetSearchInfoCommon(string[] sfArr, int[] scArr, object[] svArr)
        {
            //查询条件
            string where = "Where 1 = 1";

            //重新复制数组进行一下步操作，以免更改数组之后，影响下一步操作---KEN 2009-03-16 这里的操作很重要
            string[] newsfArr = sfArr.Clone() as string[];
            object[] newsvArr = svArr.Clone() as object[];

            if (newsfArr.Length == scArr.Length
                && newsfArr.Length == newsvArr.Length
                && newsfArr.Length > 0)
            {
                for (int i = 0; i < newsfArr.Length; i++)
                {
                    #region 拼接字段及查询条件
                    if (!string.IsNullOrEmpty(newsfArr[i]))
                    {
                        string _whereFieldName = newsfArr[i];
                        if (newsfArr[i].IndexOf('.') >= 0)
                            _whereFieldName = newsfArr[i].Split('.')[1];

                        where += " And " + newsfArr[i] + " ";
                        if (scArr[i] <= 6)
                        {
                            #region 字段比较条件,默认为相等 = 。
                            switch (scArr[i])
                            {
                                case 0:
                                    where += "=";
                                    break;

                                case 1:
                                    where += "<=";
                                    break;
                                case 2:
                                    where += ">=";
                                    break;
                                case 3:
                                    where += "<";
                                    break;
                                case 4:
                                    where += ">";
                                    break;

                                case 5:
                                    where += "<>";
                                    break;
                                case 6:
                                    where += "Like";
                                    break;
                                default:
                                    where += "=";
                                    break;
                            }
                            #endregion

                            if (scArr[i] < 1 || scArr[i] > 4)
                            {
                                where += " @" + _whereFieldName;
                                newsfArr[i] = _whereFieldName;
                            }
                            else
                            {
                                if (scArr[i] == 2 || scArr[i] == 4)
                                {
                                    where += " @" + _whereFieldName + "_0";
                                    newsfArr[i] = _whereFieldName + "_0";
                                }
                                else
                                {
                                    where += " @" + _whereFieldName + "_1";
                                    newsfArr[i] = _whereFieldName + "_1";
                                }
                            }
                        }
                        else if (scArr[i] == 7)
                        {
                            where += "CharIndex(@" + _whereFieldName + "," + newsfArr[i] + ") > 0";
                        }

                        //LIKE型查询替换参数方法
                        if (scArr[i] == 6)
                            newsvArr[i] = "%" + newsvArr[i] + "%";
                    }
                    #endregion
                }
            }

            //返回结果
            return new CommoninfoForSearch
            {
                WhereStr = where,
                FieldParams = string.Join(",", newsfArr),
                ParamValues = newsvArr
            };
        }

        /// <summary>
        /// 手机用户管理列表总数查询公用方法
        /// </summary>
        /// <param name="connKey">数据库连接字符键值,默认为DefaultDB。</param>
        /// <param name="sfArr">查询字段数组</param>
        /// <param name="scArr">查询条件数组</param>
        /// <param name="svArr">查询字段对应值数组</param>
        /// <param name="tbName">查询的表名</param>
        /// <returns>返回信息总数</returns>
        public static int GetInfoForListRecordCount(this string connKey, string[] sfArr, int[] scArr, object[] svArr, string tbName)
        {
            CommoninfoForSearch seInfo = GetSearchInfoCommon(sfArr, scArr, svArr);
            string Sql = "Select Count(1) From {0} {1}";

            if (string.IsNullOrEmpty(connKey)) connKey = "DefaultDB";
            Database Database = DatabaseFactory.CreateDatabase(connKey);
            return Convert.ToInt32(Database.ExecuteScalarEx(string.Format(Sql, tbName, seInfo.WhereStr), seInfo.FieldParams, seInfo.ParamValues));
        }
    }

    #region 所有查询组织查询语句公用类
    /// <summary>
    /// 所有查询公用类
    /// </summary>
    [Serializable]
    public class CommoninfoForSearch
    {
        /// <summary>
        /// 条件
        /// </summary>
        public string WhereStr { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public string FieldParams { get; set; }

        /// <summary>
        /// 参数值数组
        /// </summary>
        public object[] ParamValues { get; set; }
    }
    #endregion
}
