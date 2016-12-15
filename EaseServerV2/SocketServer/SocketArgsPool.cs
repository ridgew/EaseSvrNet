using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace EaseServer.SocketServer
{
    /// <summary>
    /// Pools SocketAsyncEventArgs objects to avoid repeated allocations.
    /// </summary>
    public class SocketArgsPool : IDisposable
    {
        LockFreeQueue<SocketAsyncEventArgs> argsPool = null;
        //Queue<SocketAsyncEventArgs> argsPool;

        /// <summary>
        /// Pools SocketAsyncEventArgs objects to avoid repeated allocations.
        /// </summary>
        /// <param name="capacity">The ammount to SocketAsyncEventArgs to create and pool.</param>
        public SocketArgsPool(int capacity)
        {
            argsPool = new LockFreeQueue<SocketAsyncEventArgs>();
            //argsPool = new Queue<SocketAsyncEventArgs>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                //args.DisconnectReuseSocket = true;
                argsPool.Enqueue(args);
            }
        }

        /// <summary>
        /// Checks an SocketAsyncEventArgs back into the pool.
        /// </summary>
        /// <param name="item">The SocketAsyncEventsArgs to check in.</param>
        public void CheckIn(SocketAsyncEventArgs item)
        {
            //lock (argsPool)
            //{
            argsPool.Enqueue(item);
            //}
        }

        /// <summary>
        /// Check out an SocketAsyncEventsArgs from the pool.
        /// </summary>
        /// <returns>The SocketAsyncEventArgs.</returns>
        public SocketAsyncEventArgs CheckOut()
        {
            //lock (argsPool)
            //{
            //    return argsPool.Dequeue();
            //}

            SocketAsyncEventArgs args = null;
            if (!argsPool.TryDequeue(out args))
            {
                Server.Log.Error("* SocketArgsPool Dequeue failed!");
                return new SocketAsyncEventArgs();
            }
            return args;
        }

        /// <summary>
        /// The number of available objects in the pool.
        /// </summary>
        public long Available
        {
            get
            {
                //lock (this)
                //{
                return argsPool.Count;
                //}
            }
        }

        #region IDisposable Members
        private Boolean disposed = false;

        ~SocketArgsPool()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    foreach (SocketAsyncEventArgs args in argsPool)
                    {
                        args.Dispose();
                    }
                }
                disposed = true;
            }
        }
        #endregion
    }
}
