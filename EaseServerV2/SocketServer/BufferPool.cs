using System.Collections.Generic;
using System.Net.Sockets;

namespace EaseServer.SocketServer
{
    /// <summary>
    /// Pools data buffers to prevent both frequent allocation and memory fragmentation
    /// due to pinning in high volume scenarios.
    /// See https://blogs.msdn.com/yunjin/archive/2004/01/27/63642.aspx
    /// </summary>
    public class BufferPool
    {
        int _totalBufferLength, _currentIndex, _bufferSize;
        byte[] _fixedBuffer;
        Stack<int> freeIndexPool;

        /// <summary>
        /// Pools data buffers to prevent both frequent allocation and memory fragmentation
        /// due to pinning in high volume scenarios.
        /// </summary>
        /// <param name="numberOfBuffers">The total number of buffers that will be allocated.</param>
        /// <param name="bufferSize">The size of each buffer.</param>
        public BufferPool(int numberOfBuffers, int bufferSize)
        {
            this._totalBufferLength = numberOfBuffers * bufferSize;
            this._bufferSize = bufferSize;

            _currentIndex = 0;
            freeIndexPool = new Stack<int>();
            _fixedBuffer = new byte[_totalBufferLength];
        }

        /// <summary>
        /// Checks out some buffer space from the pool.
        /// </summary>
        /// <param name="args">The ScoketAsyncEventArgs which needs a buffer.</param>
        public void CheckOut(SocketAsyncEventArgs args)
        {
            lock (freeIndexPool)
            {
                if (freeIndexPool.Count > 0)
                    args.SetBuffer(_fixedBuffer, freeIndexPool.Pop(), _bufferSize);
                else
                {
                    args.SetBuffer(_fixedBuffer, _currentIndex, _bufferSize);
                    _currentIndex += _bufferSize;
                }
            }
        }

        /// <summary>
        /// Checks a buffer back in to the pool.
        /// </summary>
        /// <param name="args">The SocketAsyncEventArgs which has finished with it buffer.</param>
        public void CheckIn(SocketAsyncEventArgs args)
        {
            lock (freeIndexPool)
            {
                freeIndexPool.Push(args.Offset);
                args.SetBuffer(null, 0, 0);
            }
        }

        /// <summary>
        /// The number of available objects in the pool.
        /// </summary>
        public int Available
        {
            get
            {
                lock (freeIndexPool)
                {
                    return ((_totalBufferLength - _currentIndex) / _bufferSize) + freeIndexPool.Count;
                }
            }
        }
    }
}
