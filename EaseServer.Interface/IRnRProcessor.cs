
namespace EaseServer.Interface
{
    /// <summary>
    /// 基于Request &amp; Response设计的会话处理
    /// </summary>
    public interface IRnRProcessor
    {
        /// <summary>
        /// 判断是否已完成请求数据的发送
        /// </summary>
        bool HasFinishedRequest();

        /// <summary>
        /// 读取会话中发送的数据(主动模式)
        /// </summary>
        void ReadRequestData();

        /// <summary>
        /// 继续写入请求片段数据(被动模式)
        /// </summary>
        /// <param name="requestSnippet">片段数据字节序列</param>
        void WriteReqeustBytes(byte[] requestSnippet);

        /// <summary>
        /// 当前会话处理已完成(重置)
        /// </summary>
        void ResetRequest();

        /// <summary>
        /// 开始输出应答结果
        /// </summary>
        void ProcessResponse();
    }
}
