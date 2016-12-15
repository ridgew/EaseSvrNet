using System.IO;

namespace EaseServer.Interface
{
    /// <summary>
    /// 基于RnR处理的基类
    /// </summary>
    public abstract class RnRProcessorBase : IRnRProcessor
    {

        int _bufferSize = 4096;
        /// <summary>
        /// 获取或设置当前会话的缓冲字节长度
        /// </summary>
        public int BufferSize
        {
            get { return _bufferSize; }
            set { _bufferSize = value; }
        }

        /// <summary>
        /// RnR会话交互字节序列对象
        /// </summary>
        public Stream ExchangeStream { get; set; }

        /// <summary>
        /// 当前会话内存缓存
        /// </summary>
        protected MemoryStream sessionBuffer = new MemoryStream();

        /// <summary>
        /// 会话发送数据次数
        /// </summary>
        protected int requestAccessCount = 0;

        #region IRnRProcessor 成员
        /// <summary>
        /// 读取会话中发送的数据(主动模式)
        /// </summary>
        public virtual void ReadRequestData()
        {
            if (ExchangeStream != null)
            {
                byte[] buffer = new byte[BufferSize];
                int totalRead = ExchangeStream.Read(buffer, 0, buffer.Length);
                requestAccessCount++;
                sessionBuffer.Write(buffer, 0, totalRead);
            }
        }

        /// <summary>
        /// 继续写入请求片段数据(被动模式)
        /// </summary>
        /// <param name="requestSnippet">片段数据字节序列</param>
        public virtual void WriteReqeustBytes(byte[] requestSnippet)
        {
            requestAccessCount++;
            sessionBuffer.Write(requestSnippet, 0, requestSnippet.Length);
        }

        /// <summary>
        /// 当前会话处理已完成(重置)
        /// </summary>
        public virtual void ResetRequest()
        {
            sessionBuffer = new MemoryStream();
            requestAccessCount = 0;
        }

        /// <summary>
        /// 判断是否已完成请求数据的发送(实现)
        /// </summary>
        /// <returns></returns>
        public abstract bool HasFinishedRequest();

        /// <summary>
        /// 开始输出应答结果(实现)
        /// </summary>
        public abstract void ProcessResponse();

        #endregion
    }
}
