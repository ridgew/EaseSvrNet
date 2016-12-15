using System;
using System.Net.Sockets;

namespace EaseServer.SocketServer
{
    /// <summary>
    /// Represents one of the new Socket xxxAsync methods in .NET 3.5.
    /// </summary>
    /// <param name="args">The SocketAsyncEventArgs for use with the method.</param>
    /// <returns>Returns true if the operation completed asynchronously, false otherwise.</returns>
    public delegate Boolean SocketAsyncMethod(SocketAsyncEventArgs args);

    /// <summary>
    /// Represents a callback used to inform a listener that a ServerConnection has received data.
    /// </summary>
    /// <param name="sender">The sender of the callback.</param>
    /// <param name="e">The DataEventArgs object containging the received data.</param>
    public delegate void DataReceivedCallback(Connection sender, DataEventArgs e);

    /// <summary>
    /// Represents a callback used to inform a listener that a ServerConnection has disconnected.
    /// </summary>
    /// <param name="sender">The sender of the callback.</param>
    /// <param name="e">The SocketAsyncEventArgs object used by the ServerConnection.</param>
    public delegate void DisconnectedCallback(Connection sender, SocketAsyncEventArgs e);

    /// <summary>
    /// Holds helper methods for working with the new Socket xxxAsync methods in .NET 3.5.
    /// </summary>
    public static class ExtMethods
    {
        /// <summary>
        /// Extension method to simplyfiy the pattern required by the new Socket xxxAsync methods in .NET 3.5.
        /// See http://www.flawlesscode.com/post/2007/12/Extension-Methods-and-SocketAsyncEventArgs.aspx
        /// </summary>
        /// <param name="socket">The socket this method acts on.</param>
        /// <param name="method">The xxxAsync method to be invoked.</param>
        /// <param name="callback">The callback for the method. Note: The Completed event must already have been attached to the same.</param>
        /// <param name="args">The SocketAsyncEventArgs to be used with this call.</param>
        public static void InvokeAsyncMethod(Socket socket, SocketAsyncMethod method, EventHandler<SocketAsyncEventArgs> callback, SocketAsyncEventArgs args)
        {
            if (!method(args))
                callback(socket, args);
        }

        /// <summary>
        /// 停止Timer
        /// </summary>
        /// <param name="threadTimer"></param>
        public static void StopTimer(System.Threading.Timer threadTimer)
        {
            if (threadTimer != null)
                threadTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }
    }
}