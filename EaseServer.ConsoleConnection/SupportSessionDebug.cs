using System;
using System.IO;
using System.Text;
using CommonLib;
using EaseServer.Interface;

namespace EaseServer.ConsoleConnection
{
    public partial class FreeStyleConnection
    {
        class SupportSessionDebug
        {
            private SupportSessionDebug(IServerConnection conn, string[] args)
            {
                ServerConnection = conn; 
                if (args != null)
                {
                    if (args.Length > 0)
                    {
                        sessionKey = args[0];
                        if (args.Length > 1) sessionData = args[1];
                    }
                }
            }

            string sessionKey = "";
            string sessionData = null;

            internal static SupportSessionDebug Create(IServerConnection conn, string[] args)
            {
                return new SupportSessionDebug(conn, args);
            }

            public IServerConnection ServerConnection { get; set; }

            public bool ExecuteClose(Stream exchange)
            {
                byte[] retBytes = new byte[0];
                if (string.IsNullOrEmpty(sessionKey))
                {
                    IServerAPI svrAPI = ServerConnection.GetServerAPI();
                    if (svrAPI != null)
                    {
                        string[] allSupportSessions = svrAPI.GetSupportSessionKeys();
                        retBytes = Encoding.Default.GetBytes(string.Concat("* 支持如下会话标识:\r\n\t",
                            string.Join("\r\n\t", allSupportSessions), Environment.NewLine));
                    }
                }
                else
                {
                    if (sessionData == null)
                    {
                        retBytes = Encoding.Default.GetBytes(string.Format("* [{0}] 没有输入类似于0x03F2这样的调试数据\r\n", sessionKey));
                    }
                    else
                    {
                        retBytes = Encoding.Default.GetBytes(sessionData + Environment.NewLine);
                    }
                }
                exchange.Write(retBytes, 0, retBytes.Length);
                return false;
            }
        }
    }

    //public class TempTest
    //{
    //    public void doTest()
    //    {
    //        string[] resArr = @"session ""ease test snrr 'vf' b"" ok ""ease test"""
    //            .SplitString(" ", "\"", true);

    //        Console.WriteLine("***************");
    //        foreach (var item in resArr)
    //        {
    //            Console.WriteLine("【{0}】", item);
    //        }
    //    }
    //}
}
