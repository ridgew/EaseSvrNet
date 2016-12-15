using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using CommonLib;

namespace EaseServer
{
    class Program
    {

        internal static char[] SplitChars = new char[] { ':', ',', '-', '|', ';' };

        // the web server
        internal static List<Server> AllRunServerList = new List<Server>();

        /// <summary>
        /// 当前应用正在运行委托
        /// </summary>
        internal static System.Threading.ManualResetEvent RuningHandler = new System.Threading.ManualResetEvent(false);

        /// <summary>
        /// 启动函数
        /// </summary>
        /// <param name="args">识别5个参数：应用程序路径 [端口8095] [应用程序目录/] [服务模式] [日志配置文件地址]</param>
        [STAThread]
        static void Main(string[] args)
        {
            Server.SynThreadPoolSeting();

            if (args == null || args.Length == 0)
            {
                if (!Environment.UserInteractive)
                    args = "EaseServer.ServiceArguments".AppSettings<string>("\"\" 8095 / Debug").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }

            Console.WriteLine(args.Length);
            Console.Read();

            string logConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
            if (args.Length == 5)
            {
                logConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, args[4]);
            }

            //配置日志文件
            if (File.Exists(logConfig))
            {
                log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(logConfig));
            }

            // web server settings
            string _appPath = "";
            string _portString = "8095";
            string _virtRoot = "/";


            //appdir port virapp debug
            if (args.Length >= 4 || !Environment.UserInteractive)
            {
                #region 无UI界面运行
                if (args.Length >= 1)
                {
                    string dirArg = args[0].Trim('"', '\'');
                    string dirName = (dirArg.Length > 0) ? Path.GetDirectoryName(dirArg) : "";
                    if (string.IsNullOrEmpty(dirName))
                    {
                        _appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dirArg);
                    }
                    else
                    {
                        _appPath = dirName;
                    }
                }
                if (args.Length >= 2) _portString = args[1];
                if (args.Length >= 3) _virtRoot = args[2];

                if (string.IsNullOrEmpty(_portString)) _portString = "8095";
                //允许同时监听多个端口
                string[] listenPorts = _portString.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length >= 4 && args[3].Equals("Dameon", StringComparison.InvariantCultureIgnoreCase))
                {
                    #region Windows服务方式运行
                    foreach (string onePort in listenPorts)
                    {
                        AllRunServerList.Add(new Server(Convert.ToInt32(onePort), _virtRoot, _appPath));
                    }
                    System.ServiceProcess.ServiceBase.Run(AllRunServerList.ToArray());
                    #endregion
                }
                else
                {
                    #region 控制台方式调试运行
                    Server serverRef = null;
                    foreach (string onePort in listenPorts)
                    {
                        serverRef = new Server(Convert.ToInt32(onePort), _virtRoot, _appPath);
                        AllRunServerList.Add(serverRef);
                        serverRef.Start();

                        if (Environment.UserInteractive)
                            Console.WriteLine("{0} v{1}已启动，监听端口:{2}...", Server.ServerName, Messages.VersionString, serverRef.Port);
                        else
                            Server.Log.InfoFormat("{0} v{1}已启动，监听端口:{2}...", Server.ServerName, Messages.VersionString, serverRef.Port);
                    }

                    if (Environment.UserInteractive)
                    {
                        Console.WriteLine("*输入字母'Q'退出监听服务");
                        while (Console.ReadLine() != "Q") { }
                        Server.StopAllServer();
                        Console.WriteLine("已退出所有监听服务.");
                    }
                    else
                    {
                        RuningHandler.WaitOne();
                    }
                    #endregion
                }
                #endregion
            }
            else
            {
                Process instance = InstanceManager.RunningInstance();
                if (instance == null)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new ServerUI(args));
                }
                else
                {
                    InstanceManager.HandleRunningProcess(instance);
                }
            }
        }
    }
}
