using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EaseServer
{
    /// <summary>
    /// 应用程序每次只能运行一个实例（C#）
    /// http://chhaj5236.blog.163.com/blog/static/112881081201022664712661/
    /// </summary>
    public class InstanceManager
    {
        [DllImport("user32.dll")]  //使用user32.dll中提供的两个函数实现显示和激活
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        private const int WS_SHOWNORMAL = 1;

        public static Process RunningInstance()
        {  //查找是否有同名的进程并比对信息
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);
            foreach (Process process in processes)
            {
                if (current.Id != process.Id &&
                    current.MainModule.FileName == process.MainModule.FileName)
                {
                    return process;
                }
            }
            return null;
        }

        public static void HandleRunningProcess(Process instance)
        {
            //确保窗口没有被最小化和最大化
            ShowWindowAsync(instance.MainWindowHandle, WS_SHOWNORMAL);

            //将窗体显示在前面
            SetForegroundWindow(instance.MainWindowHandle);
        }

        //static void Main()
        //{
        //    Process instance = InstanceManager.RunningInstance();
        //    if (instance == null)
        //    {   //下面的三行代码是之前Main函数中的
        //        Application.EnableVisualStyles();
        //        Application.SetCompatibleTextRenderingDefault(false);
        //        Application.Run(new Form1());
        //    }
        //    else
        //    {
        //        InstanceManager.HandleRunningProcess(instance);
        //    }
        //}

    }
}
