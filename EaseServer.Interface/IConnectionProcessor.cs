
using EaseServer.Configuration;
namespace EaseServer.Interface
{
    /// <summary>
    /// 连接扩展处理
    /// </summary>
    public interface IConnectionProcessor
    {
        /// <summary>
        /// 协议标识
        /// </summary>
        string ProtocolIdentity { get; }

        /// <summary>
        /// 获取或设置连接模型
        /// </summary>
        ConnectionMode SocketMode { get; set; }

        /// <summary>
        /// 判断是否接收当前连接处理
        /// </summary>
        /// <param name="firstReadBytes">首次读到的字节序列</param>
        /// <returns>如果处理则为true,否则为false。</returns>
        bool AcceptConnection(byte[] firstReadBytes);

        /// <summary>
        /// 通过配置文件配置实例支持
        /// </summary>
        void ConfigInstance(SessionConfig config);

        /// <summary>
        /// 获取或设置当前的服务连接对象
        /// </summary>
        IServerConnection ServerConnection { get; set; }

        /// <summary>
        /// 对当前连接的处理
        /// </summary>
        void ProcessRequest();

        /// <summary>
        ///输出错误码，并关闭连接。
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <param name="message">The message.</param>
        void WriteErrorAndClose(int statusCode, string message);

    }
}
