using System;
using System.Net;
using System.Net.Sockets;

namespace EaseServer.SocketServer
{
    /// <summary>
    /// Listens for socket connection on a given address and port.
    /// </summary>
    public class TcpSocketListener : IDisposable
    {
        #region Fields
        private Int32 connectionBacklog;
        private IPEndPoint endPoint;

        private Socket listenerSocket;
        private SocketAsyncEventArgs svrArgs;
        #endregion

        #region Properties
        /// <summary>
        /// Length of the connection backlog.
        /// </summary>
        public Int32 ConnectionBacklog
        {
            get { return connectionBacklog; }
            set
            {
                lock (this)
                {
                    if (IsRunning)
                        throw new InvalidOperationException("Property cannot be changed while server running.");
                    else
                        connectionBacklog = value;
                }
            }
        }
        /// <summary>
        /// The IPEndPoint to bind the listening socket to.
        /// </summary>
        public IPEndPoint EndPoint
        {
            get { return endPoint; }
            set
            {
                lock (this)
                {
                    if (IsRunning)
                        throw new InvalidOperationException("Property cannot be changed while server running.");
                    else
                        endPoint = value;
                }
            }
        }

        /// <summary>
        /// Is the class currently listening.
        /// </summary>
        public Boolean IsRunning
        {
            get { return listenerSocket != null; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Listens for socket connection on a given address and port.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="connectionBacklog">The connection backlog.</param>
        public TcpSocketListener(String address, Int32 port, Int32 connectionBacklog)
            : this(IPAddress.Parse(address), port, connectionBacklog)
        { }

        /// <summary>
        /// Listens for socket connection on a given address and port.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="connectionBacklog">The connection backlog.</param>
        public TcpSocketListener(IPAddress address, Int32 port, Int32 connectionBacklog)
            : this(new IPEndPoint(address, port), connectionBacklog)
        { }

        /// <summary>
        /// Listens for socket connection on a given address and port.
        /// </summary>
        /// <param name="endPoint">The endpoint to listen on.</param>
        /// <param name="backlog">The connection backlog.</param>
        public TcpSocketListener(IPEndPoint endPoint, Int32 backlog)
        {
            this.endPoint = endPoint;
            connectionBacklog = backlog;

            svrArgs = new SocketAsyncEventArgs();
            //args.DisconnectReuseSocket = true;
            svrArgs.Completed += SocketAccepted;
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Start listening for socket connections.
        /// </summary>
        public void Start()
        {
            lock (this)
            {
                if (!IsRunning)
                {
                    listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    listenerSocket.Bind(endPoint);
                    listenerSocket.Listen(connectionBacklog);
                    ListenForConnection(svrArgs); //启动时监听
                }
                else
                    throw new InvalidOperationException("The Server is already running.");
            }

        }

        /// <summary>
        /// Stop listening for socket connections.
        /// </summary>
        public void Stop()
        {
            lock (this)
            {
                if (listenerSocket == null)
                    return;

                listenerSocket.Close();
                listenerSocket = null;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Asynchronously listens for new connections.
        /// </summary>
        /// <param name="args"></param>
        private void ListenForConnection(SocketAsyncEventArgs args)
        {
            //Server.Log.DebugFormat("☆{0}", "ListenForConnection");
            args.AcceptSocket = null;
            try
            {
                if (listenerSocket != null)
                    ExtMethods.InvokeAsyncMethod(listenerSocket, new SocketAsyncMethod(listenerSocket.AcceptAsync), SocketAccepted, args);
            }
            catch (Exception ivkEx)
            {
                Server.Log.Error("* ListenForConnection Error:", ivkEx);
            }
        }

        /// <summary>
        /// Invoked when an asynchrounous accept completes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The SocketAsyncEventArgs for the operation.</param>
        private void SocketAccepted(object sender, SocketAsyncEventArgs e)
        {
            lock (this)
            {
                //Server.Log.DebugFormat("☆{0}", "SocketAccepted");
                SocketError error = e.SocketError;
                if (e.SocketError == SocketError.OperationAborted)
                    return; //Server was stopped

                if (e.SocketError == SocketError.Success)
                {
                    Socket handler = e.AcceptSocket;
                    if (handler != null)
                        OnSocketConnected(handler);
                }

                ListenForConnection(e); //消耗掉后监听
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Fired when a new connection is received.
        /// </summary>
        public event EventHandler<SocketEventArgs> SocketConnected;

        /// <summary>
        /// Fires the SocketConnected event.
        /// </summary>
        /// <param name="client">The new client socket.</param>
        private void OnSocketConnected(Socket client)
        {
            if (SocketConnected != null)
                SocketConnected(this, new SocketEventArgs(client));
        }
        #endregion

        #region IDisposable Members
        private Boolean disposed = false;

        ~TcpSocketListener()
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
                    Stop();
                    if (svrArgs != null)
                    {
                        svrArgs.Completed -= SocketAccepted;
                        svrArgs.Dispose();
                    }
                }
                disposed = true;
            }
        }
        #endregion
    }
}