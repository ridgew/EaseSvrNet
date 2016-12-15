using System;
using System.Collections.Generic;
using System.Text;

namespace EaseServer
{
    /// <summary>
    /// 功能扩展静态函数
    /// </summary>
    public static class ExtMethods
    {
        /// <summary>
        /// 停止Timer
        /// </summary>
        /// <param name="threadTimer"></param>
        public static void StopTimer(System.Threading.Timer threadTimer)
        {
            if (threadTimer != null)
                threadTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }
    }
}
