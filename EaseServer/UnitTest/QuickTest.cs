#if UnitTest
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;
using Gwsoft.DataSpec;

namespace EaseServer.EaseConnection.UnitTest
{
    public class QuickTest
    {
        //string serverAddress = "118.123.205.165";
        string serverAddress = "127.0.0.1";
        //string serverAddress = "118.123.205.163";
        int serverPort = 8095;//9000;

        //[网关数据]用户ID为3145的，业务编号为24的应用请求数据
        byte[] espRequestData = @"03 F2 00 00 00 01 00 00 01 43 00 03 00 00 00 00 
00 00 00 00 00 00 01 00 00 00 00 01 30 03 F2 00 
00 01 30 00 12 00 00 0C 49 03 00 00 00 49 00 00 
00 00 00 00 00 01 01 00 00 6F 88 34 00 00 00 00 
16 12 01 00 18 00 13 33 36 36 36 3A 32 30 30 39 
30 37 31 36 31 34 31 32 33 33 00 00 00 07 30 2C 
30 2C 30 2C 30 04 00 00 00 00 00 00 00 00 00 18 
00 00 00 D9 7B 48 65 61 64 3A 7B 55 73 65 72 41 
67 65 6E 74 3A 22 48 75 61 77 65 69 20 43 38 31 
30 30 22 2C 43 6F 6F 6B 69 65 3A 22 22 2C 49 4D 
53 49 3A 22 30 30 30 30 30 30 30 30 30 30 30 30 
30 30 30 22 2C 55 6E 69 71 75 65 49 64 3A 22 22 
2C 50 72 6F 74 6F 63 6F 6C 3A 22 42 4D 50 5F 32 
30 31 30 22 2C 43 6F 6D 6D 61 6E 64 3A 22 4C 61 
75 6E 63 68 22 2C 42 69 7A 43 6F 64 65 3A 22 46 
75 6E 4D 61 72 74 22 2C 56 65 72 73 69 6F 6E 3A 
22 31 2E 30 22 2C 50 75 62 6C 69 63 4B 65 79 3A 
22 67 77 73 6F 66 74 22 7D 2C 42 6F 64 79 3A 7B 
43 6F 72 65 56 65 72 3A 31 2E 30 7D 7D 62 66 31 
64 61 32 30 65 65 33 35 63 37 64 34 64 33 61 31 
64 66 33 38 64 32 36 38 65 39 38 35 31".HexPatternStringToByteArray();

        /// <summary>
        /// [应用数据]用户ID为3145的，业务编号为24的应用请求数据
        /// </summary>
        byte[] espSubRequestData = @"03 F2 00 
00 01 30 00 12 00 00 0C 49 03 00 00 00 49 00 00 
00 00 00 00 00 01 01 00 00 6F 88 34 00 00 00 00 
16 12 01 00 18 00 13 33 36 36 36 3A 32 30 30 39 
30 37 31 36 31 34 31 32 33 33 00 00 00 07 30 2C 
30 2C 30 2C 30 04 00 00 00 00 00 00 00 00 00 18 
00 00 00 D9 7B 48 65 61 64 3A 7B 55 73 65 72 41 
67 65 6E 74 3A 22 48 75 61 77 65 69 20 43 38 31 
30 30 22 2C 43 6F 6F 6B 69 65 3A 22 22 2C 49 4D 
53 49 3A 22 30 30 30 30 30 30 30 30 30 30 30 30 
30 30 30 22 2C 55 6E 69 71 75 65 49 64 3A 22 22 
2C 50 72 6F 74 6F 63 6F 6C 3A 22 42 4D 50 5F 32 
30 31 30 22 2C 43 6F 6D 6D 61 6E 64 3A 22 4C 61 
75 6E 63 68 22 2C 42 69 7A 43 6F 64 65 3A 22 46 
75 6E 4D 61 72 74 22 2C 56 65 72 73 69 6F 6E 3A 
22 31 2E 30 22 2C 50 75 62 6C 69 63 4B 65 79 3A 
22 67 77 73 6F 66 74 22 7D 2C 42 6F 64 79 3A 7B 
43 6F 72 65 56 65 72 3A 31 2E 30 7D 7D 62 66 31 
64 61 32 30 65 65 33 35 63 37 64 34 64 33 61 31 
64 66 33 38 64 32 36 38 65 39 38 35 31".HexPatternStringToByteArray();


        public void Exec()
        {
            //SocketMonitorStream mns = new SocketMonitorStream();

            //byte[] testBytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

            //mns.ConnectionBufferStream = new MemoryStream();

            //byte[] buffer = new byte[6];
            //mns.Read(buffer, 0, 3);

            //Console.WriteLine(mns.Position);
            //Console.WriteLine(BitConverter.ToString(buffer));

            #region 远程Socket连接测试
            TcpClient simpleTcp = null;

            //NetworkStream tcpStream = null;
            SocketMonitorStream tcpStream = null;
            byte[] retBytes = new byte[0];

            try
            {
                simpleTcp = new TcpClient(serverAddress, serverPort);

                byte[] bufferBytes = @"03 F2 00 00 00 01 00 00 00 FE 00 00 00 F2 00 00 
00 00 00 00 00 00 03 F2 00 00 00 EC 00 00 00 20 
00 13 33 36 36 36 3A 32 30 30 39 30 37 31 36 31 
34 31 32 33 33 04 00 00 00 00 00 00 00 00 00 00 
C7 7B 22 48 65 61 64 22 3A 7B 22 43 6F 6F 6B 69 
65 22 3A 22 74 65 73 74 5F 63 6F 6F 6B 69 65 22 
2C 22 55 6E 69 71 75 65 49 64 22 3A 22 74 65 73 
74 5F 69 64 22 2C 22 50 72 6F 74 6F 63 6F 6C 22 
3A 22 42 4D 50 5F 32 30 31 30 22 2C 22 43 6F 6D 
6D 61 6E 64 22 3A 22 4C 61 75 6E 63 68 22 2C 22 
53 74 61 74 75 73 22 3A 31 30 30 33 2C 22 4D 65 
73 73 61 67 65 22 3A 22 E6 81 AD E5 96 9C E4 BD 
A0 E7 99 BB E5 BD 95 E6 88 90 E5 8A 9F 22 7D 2C 
22 42 6F 64 79 22 3A 7B 22 41 70 70 4C 69 73 74 
22 3A 6E 75 6C 6C 7D 7D 32 61 61 62 62 36 39 37 
66 65 34 64 30 62 30 32 32 37 30 63 32 31".HexPatternStringToByteArray();

                //tcpStream = simpleTcp.GetStream();
                tcpStream = new SocketMonitorStream(simpleTcp.Client, FileAccess.ReadWrite, false);
                tcpStream.ConnectionBufferStream = new MemoryStream(bufferBytes);

                tcpStream.WriteTimeout = 30 * 1000; //30秒
                tcpStream.ReadTimeout = 30 * 1000; //30秒

                tcpStream.Write(espRequestData, 0, espRequestData.Length);
                tcpStream.Position = 0;

                MemoryStream ms = new MemoryStream();
                byte[] buffer = new byte[2048];// new byte[20480];
                int rc = 0;

                rc = tcpStream.Read(buffer, 0, buffer.Length);
                int totalRead = 10, currentRead = 0;
                if (rc > 10 && SpecUtil.BytesStartWith(buffer, new byte[] { 0x03, 0xF2 }))
                {
                    byte[] leaveLengthBytes = new byte[4];
                    Buffer.BlockCopy(buffer, 6, leaveLengthBytes, 0, leaveLengthBytes.Length);

                    totalRead = BitConverter.ToInt32(leaveLengthBytes.ReverseBytes(), 0) + 10;
                    currentRead = rc;

                    ms.Write(buffer, 0, rc);
                    Console.WriteLine("字节长度{0}, 本次读取字节长度{1}.", totalRead, rc);
                }

                int readLen = buffer.Length;
                if (totalRead - currentRead < readLen) readLen = totalRead - currentRead;

                while (simpleTcp.Connected && tcpStream.CanRead &&
                    currentRead < totalRead &&
                    (rc = tcpStream.Read(buffer, 0, readLen)) > 0)
                {
                    ms.Write(buffer, 0, rc);
                    currentRead += rc;
                    Console.WriteLine("{1}:{2}返回二进制序列，长度:{0}...", rc, serverAddress, serverPort);
                }

                retBytes = ms.ToArray();
                ms.Dispose();
            }
            catch (SocketException err)
            {
                Console.WriteLine("TCP client: Socket error occured: {0}", err.Message);
            }
            catch (System.IO.IOException err)
            {
                Console.WriteLine("TCP client: I/O error: {0} \r\n{1}", err.Message, err.StackTrace);
            }
            finally
            {
                Console.WriteLine("释放连接相关资源.");
                if (tcpStream != null)
                    tcpStream.Close();
                if (simpleTcp != null)
                    simpleTcp.Close();
            }
            #endregion

            Console.WriteLine(retBytes.GetHexViewString());

        }

        /// <summary>
        /// 以HTTP方式连接接入服务器
        /// </summary>
        public void EaseHttpRequestTest()
        {
            using (WebClient c = new WebClient())
            {
                int softwareid = 3145;
                //HTTP头手机号码
                c.Headers.Set("X-Up-Calling-Line-Id", "13398164474");

                //HTTP头远程ip地址
                c.Headers.Set("X-Source-Id", "115.168.85.17");

                byte[] retBytes = c.UploadData(string.Format("http://{0}:{1}/ease/servlet/ease?sid={2}", serverAddress, serverPort, softwareid),
                    espRequestData);

                Console.WriteLine(retBytes.GetHexViewString());

            }
        }


        public void TimeSpanTest()
        {
            TimeSpan ts = new TimeSpan(1, 0, 52, 11, 253);
            Console.WriteLine(ts);
        }
    }

}
#endif