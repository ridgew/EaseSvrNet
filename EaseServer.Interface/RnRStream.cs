using System;
using System.IO;

namespace EaseServer.Interface
{
    /// <summary>
    /// 基于Request &amp; Response机制建立的会话序列
    /// </summary>
    public abstract class RnRStream : Stream
    {
        /// <summary>
        /// 是否是请求会话
        /// </summary>
        protected bool isRequestSession = true;

        /// <summary>
        /// 获取是否是请求会话的序列封装
        /// </summary>
        public bool RequestSession
        {
            get { return isRequestSession; }
        }

        /// <summary>
        /// 
        /// </summary>
        protected MemoryStream sBufferField = new MemoryStream();
        /// <summary>
        /// 获取计算序列总长度等获取的缓冲序列视图(未完成单次会话)
        /// </summary>
        public MemoryStream SessionBuffer
        {
            get { return sBufferField; }
            protected set
            {
                if (sBufferField != null) sBufferField.Dispose();
                sBufferField = value;
            }
        }

        /// <summary>
        /// 兼容有固定长度的会话序列(默认为-1L)
        /// </summary>
        protected long targetLength = -1L;

        /// <summary>
        /// 内部包装的序列流
        /// </summary>
        protected Stream internalStream;

        /// <summary>
        /// 会话数据记录跟踪序列流
        /// </summary>
        internal Stream internalDump = null;
        /// <summary>
        /// 记录到跟踪
        /// </summary>
        internal void LazyDump()
        {
            if (internalDump != null
                && internalDump.Length != Position
                && SessionBuffer.Length == Position)
            {
                byte[] buffer = SessionBuffer.ToArray();
                internalDump.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// 判断单次会话(一个R&amp;R会话单位)状态是否完成，即已发送完所需数据
        /// </summary>
        public abstract bool HasFinished();

        /// <summary>
        /// 释放由 <see cref="T:System.IO.Stream"/> 占用的非托管资源，还可以另外再释放托管资源。
        /// </summary>
        /// <param name="disposing">为 true 则释放托管资源和非托管资源；为 false 则仅释放非托管资源。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (sBufferField != null) sBufferField.Dispose();
                if (internalDump != null) internalDump.Dispose();
                internalStream.Dispose();
            }
        }

    }
}
