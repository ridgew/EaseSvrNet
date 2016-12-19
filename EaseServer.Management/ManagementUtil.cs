using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using CommonLib;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System.Reflection;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace EaseServer.Management
{
    public static class ManagementUtil
    {
        /// <summary>
        /// 获取系统配置参数
        /// </summary>
        /// <typeparam name="T">参数值要转换成的数据类型</typeparam>
        /// <param name="name">参数名称</param>
        /// <returns>参数值对象</returns>
        public static T GetSysParam<T>(this string name)
        {
            string appSet = ConfigurationManager.AppSettings[name];
            if (string.IsNullOrEmpty(appSet))
            {
                return typeof(T).Equals(typeof(string)) ? "".As<T>() : default(T);
            }
            else
            {
                return appSet.As<T>();
            }
        }

        /// <summary>
        /// Executes the data table.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="sql">The SQL.</param>
        /// <param name="fieldsArr">The fields arr.</param>
        /// <param name="fieldVals">The field vals.</param>
        /// <returns></returns>
        public static DataTable ExecuteDataTable(this Database db, string sql, string fieldsArr, object[] fieldVals)
        {
            using (SqlCommand cmd = new SqlCommand(sql))
            {
                cmd.CommandType = CommandType.Text;
                string[] pNameArr = fieldsArr.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0, j = pNameArr.Length; i < j; i++)
                {
                    cmd.Parameters.Add(new SqlParameter(pNameArr[i], fieldVals[i]));
                }
                return db.ExecuteDataSet(cmd).Tables[0];
            }
        }

        /// <summary>
        /// Executes the scalar.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="sql">The SQL.</param>
        /// <param name="fieldsArr">The fields arr.</param>
        /// <param name="fieldVals">The field vals.</param>
        /// <returns></returns>
        public static object ExecuteScalarEx(this Database db, string sql, string fieldsArr, object[] fieldVals)
        {
            using (SqlCommand cmd = new SqlCommand(sql))
            {
                cmd.CommandType = CommandType.Text;
                string[] pNameArr = fieldsArr.Split(new char[] { ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0, j = pNameArr.Length; i < j; i++)
                {
                    cmd.Parameters.Add(new SqlParameter(pNameArr[i], fieldVals[i]));
                }
                return db.ExecuteScalar(cmd);
            }
        }

        /// <summary>
        /// 获取程序集嵌入资源的文本形式
        /// </summary>
        /// <param name="assemblyType">程序集中的某一对象类型</param>
        /// <param name="charset">字符集编码</param>
        /// <param name="ResName">嵌入资源相对路径</param>
        /// <returns>如没找到该资源则返回空字符</returns>
        public static string GetManifestString(Type assemblyType, string charset, string ResName)
        {
            byte[] bytes = GetManifestBytes(assemblyType, ResName);
            return (bytes != null) ? Encoding.GetEncoding(charset).GetString(bytes) : "";
        }

        /// <summary>
        /// 获取程序集嵌入资源的二进制形式
        /// </summary>
        /// <param name="assemblyType">程序集中的某一对象类型</param>
        /// <param name="ResPath">嵌入资源相对路径</param>
        /// <returns>如没找到该资源则返回Null</returns>
        public static byte[] GetManifestBytes(Type assemblyType, string ResPath)
        {
            Assembly asm = Assembly.GetAssembly(assemblyType);
            Stream st = asm.GetManifestResourceStream(string.Concat(assemblyType.Namespace,
                ".", ResPath.Replace("/", ".")));

            if (st == null) { return null; }

            int iLen = (int)st.Length;
            byte[] bytes = new byte[iLen];
            st.Read(bytes, 0, iLen);

            return bytes;
        }

        /// <summary>
        /// 在限制秒数内的执行相关操作，并返回是否超时(默认20秒)。
        /// </summary>
        /// <param name="timeoutSeconds">超时秒数</param>
        /// <param name="act">相关方法操作</param>
        /// <returns>操作是否超时</returns>
        public static bool ExecTimeoutMethod(int? timeoutSeconds, Action act)
        {
            bool isTimeout = false;
            Thread workThread = new Thread(new ThreadStart(act));
            workThread.Start();
            if (!workThread.Join((timeoutSeconds.HasValue && timeoutSeconds.Value > 0) ? timeoutSeconds.Value * 1000 : 20000))
            {
                workThread.Abort();
                isTimeout = true;
            }
            return isTimeout;
        }

        /// <summary>
        /// 运行控制台命令程序并获取运行结果
        /// </summary>
        /// <param name="cmdPath">命令行程序完整路径</param>
        /// <param name="workDir">命令行程序的工作目录</param>
        /// <param name="strArgs">命令行参数</param>
        /// <param name="timeoutSeconds">执行超时秒数，至少为30秒以上。</param>
        /// <param name="output">命令行输出</param>
        /// <returns>命令行程序的状态退出码</returns>
        public static int RunCmd(string cmdPath, string workDir, string strArgs, int timeoutSeconds, ref string output)
        {
            int exitCode = -1;
            string strOutput = "";
            int newProcessID = 0;

            bool hasTimeout = ExecTimeoutMethod(timeoutSeconds, () =>
            {
                #region 限制时间运行
                using (Process proc = new Process())
                {
                    ProcessStartInfo psInfo = new ProcessStartInfo(cmdPath, strArgs);
                    psInfo.UseShellExecute = false;
                    psInfo.RedirectStandardError = true;
                    psInfo.RedirectStandardOutput = true;
                    psInfo.RedirectStandardInput = true;
                    psInfo.CreateNoWindow = true;
                    psInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    psInfo.WorkingDirectory = workDir;
                    proc.StartInfo = psInfo;
                    if (proc.Start())
                    {
                        newProcessID = proc.Id;
                    }
                    DateTime lastAccessDatetime = DateTime.Now;
                    while (!proc.HasExited)
                    {
                        strOutput += proc.StandardOutput.ReadToEnd().Replace("\r", "");
                        System.Threading.Thread.Sleep(100);
                    }
                    exitCode = proc.ExitCode;
                    proc.Close();
                }
                #endregion
            });

            if (hasTimeout)
            {
                if (newProcessID > 0)
                {
                    Process fp = null;
                    try
                    {
                        fp = Process.GetProcessById(newProcessID);
                        if (fp != null)
                        {
                            fp.Kill(); fp.Close();
                        }
                    }
                    catch (Exception) { }
                    finally
                    {
                        if (fp != null) fp.Dispose();
                    }
                }
                strOutput += "* 在指定时间内(" + timeoutSeconds + ")秒执行超时！";
            }
            output = strOutput;
            return exitCode;
        }


        /// <summary>
        /// 判断异常是否产生于文件锁定
        /// <para>http://stackoverflow.com/questions/1304/how-to-check-for-file-lock-in-c</para>
        /// </summary>
        public static bool IsFileLocked(this IOException exception)
        {
            int errorCode = System.Runtime.InteropServices.Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
        }
 
    }
}
