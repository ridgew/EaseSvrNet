using System;
using System.IO;
using System.Net.Sockets;

namespace EaseServer.Interface
{
    /// <summary>
    /// 支持带监控的网络字节序列的读写
    /// </summary>
    public class SocketMonitorStream : NetworkStream
    {

        /// <summary>
        /// 初始化 <see cref="SocketMonitorStream"/> class.
        /// </summary>
        /// <param name="socket"><see cref="T:System.Net.Sockets.NetworkStream"/> 用来发送和接收数据的 <see cref="T:System.Net.Sockets.Socket"/>。</param>
        /// <param name="access"><see cref="T:System.IO.FileAccess"/> 值的按位组合，这些值指定授予所提供的 <see cref="T:System.Net.Sockets.Socket"/> 上的 <see cref="T:System.Net.Sockets.NetworkStream"/> 的访问类型。</param>
        /// <param name="ownsSocket">设置为 true 可指示 <see cref="T:System.Net.Sockets.NetworkStream"/> 将拥有 <see cref="T:System.Net.Sockets.Socket"/>；否则为 false。</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="socket"/> 参数为 null。
        /// </exception>
        /// <exception cref="T:System.IO.IOException">
        /// 未连接 <paramref name="socket"/> 参数。
        /// - 或 -
        /// <paramref name="socket"/> 参数的 <see cref="P:System.Net.Sockets.Socket.SocketType"/> 属性不为 <see cref="F:System.Net.Sockets.SocketType.Stream"/>。
        /// - 或 -
        /// <paramref name="socket"/> 参数处于非阻止状态。
        /// </exception>
        public SocketMonitorStream(Socket socket, FileAccess access, bool ownsSocket)
            : base(socket, access, ownsSocket)
        {

        }

        #region 扩展支持
        Stream internalDump = null;
        /// <summary>
        /// 会话数据记录跟踪序列流
        /// </summary>
        public Stream DumpStream
        {
            get { return internalDump; }
            set { internalDump = value; }
        }

        /// <summary>
        /// 获取或设置是否记录的模式
        /// </summary>
        public FileAccess RecordAccess { get; set; }

        long _position = 0L;
        /// <summary>
        /// 获取或设置流中的当前位置。此属性当前不受支持，总是引发 <see cref="T:System.NotSupportedException"/>。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 流中的当前新位置。
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">
        /// 此属性的任何用法。
        /// </exception>
        /// <PermissionSet>
        /// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
        /// </PermissionSet>
        public override long Position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// 获取或设置已缓冲字节序列流
        /// </summary>
        public MemoryStream ConnectionBufferStream { get; set; }

        /// <summary>
        /// 兼容有固定长度的会话序列(默认为-1L)
        /// </summary>
        protected long targetLength = -1L;

        /// <summary>
        /// 设置流的长度。此方法始终引发 <see cref="T:System.NotSupportedException"/>。
        /// </summary>
        /// <param name="value">未使用此参数。</param>
        /// <exception cref="T:System.NotSupportedException">
        /// 此属性的任何用法。
        /// </exception>
        public override void SetLength(long value)
        {
            targetLength = value;
        }

        /// <summary>
        /// 
        /// </summary>
        protected bool hasFinished = false;
        /// <summary>
        /// 判断单次会话(一个R&amp;R会话单位)状态是否完成，即已发送完所需数据
        /// </summary>
        public virtual bool HasFinished() { return hasFinished; }
        #endregion

        Action<SocketMonitorStream> onActiveFire = null;

        /// <summary>
        /// 当该字节流进行读写时发生
        /// </summary>
        public event Action<SocketMonitorStream> OnActiveFire
        {
            add { onActiveFire += value; }
            remove { onActiveFire -= value; }
        }

        /// <summary>
        /// 从 <see cref="T:System.Net.Sockets.NetworkStream"/> 读取数据。
        /// </summary>
        /// <param name="buffer">类型 <see cref="T:System.Byte"/> 的数组，它是内存中用于存储从 <see cref="T:System.Net.Sockets.NetworkStream"/> 读取的数据的位置。</param>
        /// <param name="offset"><paramref name="buffer"/> 中开始将数据存储到的位置。</param>
        /// <param name="size">要从 <see cref="T:System.Net.Sockets.NetworkStream"/> 中读取的字节数。</param>
        /// <returns>
        /// 从 <see cref="T:System.Net.Sockets.NetworkStream"/> 中读取的字节数。
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="buffer"/> 参数为 null。
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="offset"/> 参数小于 0。
        /// - 或 -
        /// <paramref name="offset"/> 参数大于 <paramref name="buffer"/> 的长度。
        /// - 或 -
        /// <paramref name="size"/> 参数小于 0。
        /// - 或 -
        /// <paramref name="size"/> 参数大于 <paramref name="buffer"/> 的长度减去 <paramref name="offset"/> 参数的值。
        /// - 或 -
        /// 访问套接字时出错。有关更多信息，请参见备注部分。
        /// </exception>
        /// <exception cref="T:System.IO.IOException">
        /// 基础 <see cref="T:System.Net.Sockets.Socket"/> 被关闭。
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// 	<see cref="T:System.Net.Sockets.NetworkStream"/> 是关闭的。
        /// - 或 -
        /// 从网络读取时出现错误。
        /// </exception>
        /// <PermissionSet>
        /// 	<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
        /// 	<IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// </PermissionSet>
        public override int Read(byte[] buffer, int offset, int size)
        {
            if (onActiveFire != null) onActiveFire(this);

            long oldPosition = _position;
            int currentRead = 0;

            //从缓冲区读取
            if (ConnectionBufferStream != null && oldPosition < ConnectionBufferStream.Length)
            {
                currentRead = ConnectionBufferStream.Read(buffer, offset, size);
                _position = ConnectionBufferStream.Position;
            }
            else
            {
                //从网络序列读取
                currentRead = base.Read(buffer, offset, size);

                //响应的读取字节写入
                if (internalDump != null && currentRead > 0
                    && (FileAccess.Read & RecordAccess) == FileAccess.Read)
                {
                    internalDump.Write(reqIDBytes, 0, reqIDBytes.Length);
                    internalDump.Write(buffer, offset, currentRead);
                    internalDump.Flush();
                }

                if (currentRead > 0 && oldPosition + currentRead > Position)
                {
                    Position = oldPosition + currentRead;
                }
            }
            return currentRead;
        }

        bool readFlag = false;
        /// <summary>
        /// 设置为读取流
        /// </summary>
        public void SetReadFlag()
        {
            readFlag = true;
        }

        /// <summary>
        /// 重置读取标识
        /// </summary>
        public void ResetReadFlag()
        {
            readFlag = !readFlag;
        }

        /// <summary>
        /// 响应字节标识段
        /// </summary>
        public byte[] respIDBytes = System.Text.Encoding.ASCII.GetBytes("<<<");

        /// <summary>
        /// 请求字节标识段
        /// </summary>
        public byte[] reqIDBytes = System.Text.Encoding.ASCII.GetBytes(">>>");

        /// <summary>
        /// 将数据写入 <see cref="T:System.Net.Sockets.NetworkStream"/>。
        /// </summary>
        /// <param name="buffer">类型 <see cref="T:System.Byte"/> 的数组，该数组包含要写入 <see cref="T:System.Net.Sockets.NetworkStream"/> 的数据。</param>
        /// <param name="offset"><paramref name="buffer"/> 中开始写入数据的位置。</param>
        /// <param name="size">要写入 <see cref="T:System.Net.Sockets.NetworkStream"/> 的字节数。</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="buffer"/> 参数为 null。
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="offset"/> 参数小于 0。
        /// - 或 -
        /// <paramref name="offset"/> 参数大于 <paramref name="buffer"/> 的长度。
        /// - 或 -
        /// <paramref name="size"/> 参数小于 0。
        /// - 或 -
        /// <paramref name="size"/> 参数大于 <paramref name="buffer"/> 的长度减去 <paramref name="offset"/> 参数的值。
        /// </exception>
        /// <exception cref="T:System.IO.IOException">
        /// 写入到网络时出现错误。
        /// - 或 -
        /// 访问套接字时出错。有关更多信息，请参见备注部分。
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">
        /// 	<see cref="T:System.Net.Sockets.NetworkStream"/> 是关闭的。
        /// - 或 -
        /// 从网络读取时出现错误。
        /// </exception>
        /// <PermissionSet>
        /// 	<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
        /// 	<IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
        /// </PermissionSet>
        public override void Write(byte[] buffer, int offset, int size)
        {
            if (onActiveFire != null) onActiveFire(this);

            long oldPosition = _position;
            bool hasError = false;
            try
            {
                base.Write(buffer, offset, size);
            }
            catch (Exception)
            {
                hasError = true;
            }

            if (hasError) return;
            if (internalDump != null
                     && (FileAccess.Write & RecordAccess) == FileAccess.Write)
            {
                if (readFlag)
                {
                    internalDump.Write(reqIDBytes, 0, reqIDBytes.Length);
                }
                else
                {
                    internalDump.Write(respIDBytes, 0, respIDBytes.Length);
                }
                internalDump.Write(buffer, offset, size);
                internalDump.Flush();
            }

            //Fix
            if (oldPosition + size > Position)
            {
                Position = oldPosition + size;
            }

        }

        /// <summary>
        /// 写入所有缓冲字节序列
        /// </summary>
        /// <param name="buffer">字节序列对象</param>
        public void WriteAllBytes(byte[] buffer)
        {
            if (buffer != null && buffer.Length > 0)
            {
                Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// 关闭当前流并释放与之关联的所有资源（如套接字和文件句柄）。
        /// </summary>
        public override void Close()
        {
            base.Close();
            if (ConnectionBufferStream != null) ConnectionBufferStream.Close();
        }

        /// <summary>
        /// 释放由 <see cref="T:System.Net.Sockets.NetworkStream"/> 占用的非托管资源，还可以另外再释放托管资源。
        /// </summary>
        /// <param name="disposing">如果为 true，则释放托管资源和非托管资源；如果为 false，则仅释放非托管资源。</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && ConnectionBufferStream != null) ConnectionBufferStream.Dispose();
        }

    }
}
