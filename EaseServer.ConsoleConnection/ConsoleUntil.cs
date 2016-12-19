using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;

namespace EaseServer.ConsoleConnection
{
    public static class ConsoleUntil
    {
        public static void WriteLineWith(this Stream exchangeStream, string format, params object[] args)
        {
            string message = string.Format(format, args) + Environment.NewLine;
            byte[] retBytes = Encoding.Default.GetBytes(message);
            exchangeStream.Write(retBytes, 0, retBytes.Length);
        }

        public static void WriteWith(this Stream exchangeStream, string format, params object[] args)
        {
            string message = string.Format(format, args);
            byte[] retBytes = Encoding.Default.GetBytes(message);
            exchangeStream.Write(retBytes, 0, retBytes.Length);
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
        /// 关闭相关名称的进程
        /// </summary>
        /// <param name="procName">Name of the proc.</param>
        /// <returns></returns>
        public static string KillProcess(string procName)
        {
            Process[] procs = Process.GetProcessesByName(procName);
            foreach (Process p in procs)
            {
                p.Kill();
                p.Close();
                p.Dispose();
            }
            return string.Format("总计{0}已关闭", procs.Length);
        }


        /// <summary>
        /// 直接调用内部对象的方法/函数或获取属性(支持重载调用)
        /// </summary>
        /// <param name="refType">目标数据类型</param>
        /// <param name="funName">函数名称，区分大小写。</param>
        /// <param name="objInitial">如果调用属性，则为相关对象的初始化数据，否则为Null。</param>
        /// <param name="funParams">函数参数信息</param>
        /// <returns>运行结果</returns>
        public static object InvokeMethodOrGetProperty(Type refType, string funName, object[] objInitial, params object[] funParams)
        {
            MemberInfo[] mis = refType.GetMember(funName);
            if (mis.Length < 1)
            {
                throw new InvalidProgramException(string.Concat("函数/方法 [", funName, "] 在指定类型(", refType.ToString(), ")中不存在！"));
            }
            else
            {
                MethodInfo targetMethod = null;
                StringBuilder pb = new StringBuilder();
                foreach (MemberInfo mi in mis)
                {
                    if (mi.MemberType != MemberTypes.Method)
                    {
                        if (mi.MemberType == MemberTypes.Property)
                        {
                            #region 调用属性方法Get
                            PropertyInfo pi = (PropertyInfo)mi;
                            targetMethod = pi.GetGetMethod();
                            break;
                            #endregion
                        }
                        else
                        {
                            throw new InvalidProgramException(string.Concat("[", funName, "] 不是有效的函数/属性方法！"));
                        }
                    }
                    else
                    {
                        #region 检查函数参数和数据类型 绑定正确的函数到目标调用
                        bool validParamsLen = false, validParamsType = false;

                        MethodInfo curMethod = (MethodInfo)mi;
                        ParameterInfo[] pis = curMethod.GetParameters();
                        if (pis.Length == funParams.Length)
                        {
                            validParamsLen = true;

                            pb = new StringBuilder();
                            bool paramFlag = true;
                            int paramIdx = 0;

                            #region 检查数据类型 设置validParamsType是否有效
                            foreach (ParameterInfo pi in pis)
                            {
                                pb.AppendFormat("Parameter {0}: Type={1}, Name={2}\n", paramIdx, pi.ParameterType, pi.Name);

                                //不对Null和接受Object类型的参数检查
                                if (funParams[paramIdx] != null && pi.ParameterType != typeof(object) &&
                                     (pi.ParameterType != funParams[paramIdx].GetType()))
                                {
                                    #region 检查类型是否兼容
                                    try
                                    {
                                        funParams[paramIdx] = Convert.ChangeType(funParams[paramIdx], pi.ParameterType);
                                    }
                                    catch (Exception)
                                    {
                                        paramFlag = false;
                                    }
                                    #endregion
                                    //break;
                                }
                                ++paramIdx;
                            }
                            #endregion

                            if (paramFlag == true)
                            {
                                validParamsType = true;
                            }
                            else
                            {
                                continue;
                            }

                            if (validParamsLen && validParamsType)
                            {
                                targetMethod = curMethod;
                                break;
                            }
                        }
                        #endregion
                    }
                }

                if (targetMethod != null)
                {
                    object objReturn = null;
                    #region 兼顾效率和兼容重载函数调用
                    try
                    {
                        object objInstance = System.Activator.CreateInstance(refType, objInitial);
                        objReturn = targetMethod.Invoke(objInstance, BindingFlags.InvokeMethod, Type.DefaultBinder, funParams,
                            System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        objReturn = refType.InvokeMember(funName, BindingFlags.InvokeMethod, Type.DefaultBinder, null, funParams);
                    }
                    #endregion
                    return objReturn;
                }
                else
                {
                    pb.AppendLine("---------------------------------------------");
                    pb.AppendLine("传递参数信息：");
                    foreach (object fp in funParams)
                    {
                        pb.AppendFormat("Type={0}, value={1}\n", fp.GetType(), fp);
                    }
                    throw new InvalidProgramException(string.Concat("函数/方法 [", refType.ToString(), ".", funName,
                        "(args ...) ] 参数长度和数据类型不正确！\n 引用参数信息参考：\n",
                        pb.ToString()));
                }
            }

        }

        /// <summary>
        /// 判断传递的字符是否全为数字
        /// </summary>
        public static bool IsNumbers(this string str4Test)
        {
            if (string.IsNullOrEmpty(str4Test))
            {
                return false;
            }
            else
            {
                return System.Text.RegularExpressions.Regex.IsMatch(str4Test, "^\\d+$");
            }
        }
    }
}
