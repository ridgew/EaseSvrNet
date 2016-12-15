using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
using EaseServer.Performance;

namespace EaseServer.Interface
{
    /// <summary>
    /// 服务端连接对象
    /// </summary>
    public interface IServerConnection : IDisposable
    {
        /// <summary>
        /// 获取客户端与服务端通信的字节序列封装(网络字节序列)
        /// </summary>
        Stream ExchangeStream  { get; }

        /// <summary>
        /// 是否保持连接不主动断开
        /// </summary>
        bool KeepAlive { get; set; }

        /// <summary>
        /// 客户端要求的保持连接长度（单位秒）
        /// </summary>
        int KeepAliveSeconds { get; set; }

        /// <summary>
        /// 单次Socket上发送的字节数
        /// </summary>
        byte[] SocketBufferData { get; }

        /// <summary>
        /// 本地连接端点，用户连接服务端的标识。形如：192.168.8.91:8095
        /// </summary>
        string LocalEP { get; }

        /// <summary>
        /// 远程连接端点，用户连接客户端的标识。形如：192.168.8.119:3456
        /// </summary>
        string RemoteEP { get; }

        /// <summary>
        /// 客户端远程IP地址
        /// </summary>
        string RemoteIP { get; }

        /// <summary>
        /// 获取或设置当前协议的值，默认为HTTP。
        /// </summary>
        string Protocol { get; }

        /// <summary>
        /// 获取当前客户端的连接时间
        /// </summary>
        DateTime ConnectedTime { get; }

        /// <summary>
        /// 获取当前客户端的最近活动时间
        /// </summary>
        DateTime LastInteractive { get; }

        /// <summary>
        /// 获取连接模型
        /// </summary>
        ConnectionMode SocketMode { get; }

        /// <summary>
        /// 判断是否是首次获取Socket上新请求的数据
        /// </summary>
        bool IsFirstAccess { get; }

        /// <summary>
        /// 关闭建立的相关连接
        /// </summary>
        void Close();

        /// <summary>
        /// 获取服务端API暴露接口引用
        /// </summary>
        IServerAPI GetServerAPI();

        /// <summary>
        /// 获取当前连接的Socket对象
        /// </summary>
        Socket GetClientSocket();

        /// <summary>
        /// 发送应答序列视图
        /// </summary>
        /// <param name="resp">应答序列视图</param>
        /// <param name="bufferSize">下行缓冲字节大小</param>
        void SendResponseStream(RnRStream resp, int bufferSize);

        #region 日志辅助
        /// <summary>
        /// 如果存在字节监控则写入读取的字节数据
        /// </summary>
        void MonitorDump();

        /// <summary>
        /// 服务端记录日志(INFO)
        /// </summary>
        void ServerLog(string format, params object[] args);

        /// <summary>
        /// 服务端记录日志(DEBUG)
        /// </summary>
        void ServerDebug(string format, params object[] args);

        /// <summary>
        /// 服务端记录日志(ERROR)
        /// </summary>
        void ServerError(string format, params object[] args);
        #endregion

        #region 性能计数统计
        /// <summary>
        /// 获取当前连接的性能计数器
        /// </summary>
        PerformanceCounter PerfCounter { get; }

        /// <summary>
        /// 判断是否进行性能统计
        /// </summary>
        bool EnablePerformanceCounter { get; }

        /// <summary>
        /// 性能计数的统计点配置
        /// </summary>
        PerformancePoint PerfCounterPoint { get; }

        /// <summary>
        /// 开始统计特定的点
        /// </summary>
        /// <param name="point">统计点</param>
        /// <param name="appendActions">附加的其他操作</param>
        /// <returns></returns>
        PerfData BeginCounter(PerformancePoint point, params Action<PerfData>[] appendActions);

        /// <summary>
        /// 结束统计特定的点
        /// </summary>
        /// <param name="point">统计点</param>
        /// <param name="prefixActions">前置的其他操作</param>
        /// <returns></returns>
        PerfData EndCounter(PerformancePoint point, params Action<PerfData>[] prefixActions);

        /// <summary>
        /// 重置性能计数器
        /// </summary>
        void ResetCounter();
        #endregion
    }
}
