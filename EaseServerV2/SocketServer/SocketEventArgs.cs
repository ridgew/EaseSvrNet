using System;
using System.Net.Sockets;

namespace EaseServer.SocketServer
{
    /// <summary>
    /// EventArgs class holding a Socket.
    /// </summary>
    public class SocketEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SocketEventArgs"/> class.
        /// </summary>
        public SocketEventArgs()
            : base()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketEventArgs"/> class.
        /// </summary>
        /// <param name="socket">The socket.</param>
        public SocketEventArgs(Socket socket)
        {
            Socket = socket;
        }

        public Socket Socket { get; set; }
    }
}