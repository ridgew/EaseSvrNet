using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EaseServer.ConsoleConnection
{
    public partial class FreeStyleConnection
    {
        /// <summary>
        /// SQL数据库查询
        /// <para>sqlserver "连接字符串" "select @@version"</para>
        /// <para>sqlserver bin:ServerDB "select @@version"</para>
        /// </summary>
        class SqlServerQuery
        {
            internal static SqlServerQuery Create(string[] args)
            {
                return new SqlServerQuery(args);
            }

            private SqlServerQuery(string[] args)
            {
                if (args != null)
                {
                    if (args.Length > 0)
                    {
                        if (args[0].StartsWith("bin:"))
                        {
                            buildInKey = args[0].Substring(4);
                            //生成连接字符串
                            ConnectionStringSettings cSet = ConfigurationManager.ConnectionStrings[buildInKey];
                            if (cSet != null) currentConnectionStr = cSet.ConnectionString;
                        }
                        else
                        {
                            currentConnectionStr = args[0];
                        }

                        if (args.Length > 1)
                        {
                            currentSqlExec = args[1];
                        }
                    }
                }
            }

            string buildInKey = null;
            string currentConnectionStr = null;
            string currentSqlExec = null;

            public void ExecuteClose(Stream exchange)
            {
                if (string.IsNullOrEmpty(currentConnectionStr) || string.IsNullOrEmpty(currentSqlExec))
                {
                    exchange.WriteLineWith("* 请依次指定连接字符串(bin:[内置键值])和需要运行的SQL语句");
                    return;
                }

                if (currentSqlExec.Equals("$", StringComparison.InvariantCultureIgnoreCase))
                {
                    exchange.WriteLineWith(currentConnectionStr);
                    return;
                }

                if (currentSqlExec.Equals("*", StringComparison.InvariantCultureIgnoreCase))
                {
                    ConnectionStringSettings cSet = null;
                    for (int i = 0, j = ConfigurationManager.ConnectionStrings.Count; i < j; i++)
                    {
                        cSet = ConfigurationManager.ConnectionStrings[i];
                        exchange.WriteLineWith("Name:{0}, Provider:{1}\r\nConnectionString:{2}", cSet.Name, cSet.ProviderName, cSet.ConnectionString);
                    }
                    return;
                }

                using (SqlConnection conn = new SqlConnection(currentConnectionStr))
                {
                    try
                    {
                        conn.Open();
                        #region 执行SQL语句命令
                        string[] sqlSnippets = Regex.Split(currentSqlExec, "GO(\\r?\\n)?$");
                        foreach (string sql in sqlSnippets)
                        {
                            #region 兼容GO语句终止符号
                            using (SqlCommand cmd = new SqlCommand(sql, conn))
                            {
                                exchange.WriteLineWith("--SQL:{0}", sql);
                                SqlDataReader reader = cmd.ExecuteReader();
                            readData:
                                #region 读取数据集
                                int rowCount = 0;
                                StringBuilder colBuilder = new StringBuilder();
                                while (reader.Read())
                                {
                                    if (colBuilder.Length < 1)
                                    {
                                        for (int i = 0, j = reader.FieldCount; i < j; i++)
                                        {
                                            colBuilder.AppendFormat("{0}", (i == 0) ? " " : " , ");
                                            colBuilder.AppendFormat("[{0}]", reader.GetName(i));
                                        }
                                        exchange.WriteLineWith(colBuilder.ToString());
                                    }

                                    StringBuilder rowBuilder = new StringBuilder();
                                    for (int i = 0, j = reader.FieldCount; i < j; i++)
                                    {
                                        rowBuilder.AppendFormat("{0}", (i == 0) ? "[" + (++rowCount) + "] " : " , ");
                                        if (reader.GetFieldType(i).Equals(typeof(string)))
                                        {
                                            rowBuilder.AppendFormat("\"{0}\"", reader[i]);
                                        }
                                        else
                                        {
                                            rowBuilder.AppendFormat("{0}", reader[i]);
                                        }
                                    }
                                    exchange.WriteLineWith(rowBuilder.ToString());
                                }
                                #endregion
                                if (reader.NextResult()) goto readData;
                                reader.Close();
                            }
                            #endregion
                        }
                        #endregion
                        conn.Close();
                    }
                    catch (Exception sqlEx)
                    {
                        exchange.WriteLineWith(sqlEx.ToString());
                    }
                }
            }
        }
    }
}
