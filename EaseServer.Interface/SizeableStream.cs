using System;
using System.IO;

namespace EaseServer.Interface
{
    /// <summary>
    /// 固定长度会话字节序列流(默认长度为-1L),在使用前需设置该会话序列的长度。
    /// </summary>
    public sealed class SizeableStream : RnRStream
    {
        #region 静态方法
        /// <summary>
        /// 从来源直接创建会话序列流
        /// </summary>
        /// <param name="rawStream">原始序列流来源</param>
        /// <param name="isRequest">是否是请求序列流</param>
        /// <param name="totalLength">会话序列总长度</param>
        /// <returns></returns>
        public static SizeableStream Create(Stream rawStream, bool isRequest, long totalLength)
        {
            return Create(rawStream, isRequest, totalLength, null);
        }

        /// <summary>
        /// 从来源直接创建会话序列流
        /// </summary>
        /// <param name="rawStream">原始序列流来源</param>
        /// <param name="isRequest">是否是请求序列流</param>
        /// <param name="totalLength">会话序列总长度</param>
        /// <param name="sizeBuffer">计算序列总长度获取的缓冲序列</param>
        /// <returns></returns>
        public static SizeableStream Create(Stream rawStream, bool isRequest, long totalLength, byte[] sizeBuffer)
        {
            SizeableStream s = new SizeableStream(rawStream, isRequest);
            s.SessionBuffer = new MemoryStream(sizeBuffer);
            s.SetLength(totalLength);

            if (sizeBuffer != null)
            {
                s.SetPosition(sizeBuffer.LongLength);
            }
            else
            {
                s.SetPosition(0L);
            }
            return s;
        }

        /// <summary>
        /// 从来源直接创建会话请求(Request)序列流
        /// </summary>
        /// <param name="rawStream">原始序列流来源</param>
        /// <param name="totalLength">会话序列总长度</param>
        public static SizeableStream CreateRequest(Stream rawStream, long totalLength)
        {
            return Create(rawStream, true, totalLength, null);
        }

        /// <summary>
        /// 从来源直接创建会话请求(Request)序列流
        /// </summary>
        /// <param name="rawStream">原始序列流来源</param>
        /// <param name="totalLength">会话序列总长度</param>
        /// <param name="sizeBuffer">计算序列总长度获取的缓冲序列</param>
        public static SizeableStream CreateRequest(Stream rawStream, long totalLength, byte[] sizeBuffer)
        {
            return Create(rawStream, true, totalLength, sizeBuffer);
        }

        /// <summary>
        /// 从来源直接创建会话序列流
        /// </summary>
        /// <param name="rawStream">原始序列流来源</param>
        /// <param name="isRequest">是否是请求序列流</param>
        /// <param name="sizeBufferSize">包含长度信息的缓冲字节大小</param>
        /// <param name="calcSizeFunc">统计流总长度的计算方法,如果计算结果小于缓冲的长度则总长度为缓冲字节长度。</param>
        /// <returns>一个定长的包含当前位置信息的序列流</returns>
        public static SizeableStream CreateNew(Stream rawStream, bool isRequest, int sizeBufferSize, Func<byte[], long> calcSizeFunc)
        {
            byte[] buffer = ReadSpecialLength(rawStream, sizeBufferSize);
            //设置流的当前位置
            long position = buffer.LongLength;

            //设置流的总长度
            long totalLength = position;
            long calcSize = calcSizeFunc(buffer);
            if (calcSize > position)
            {
                totalLength = calcSize;
            }
            return SizeableStream.Create(rawStream, isRequest, totalLength, buffer);
        }

        static byte[] ReadSpecialLength(Stream sourceStream, long totalLength)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                if (totalLength > 0)
                {
                    long bufferSize = (totalLength > 4096L) ? 4096L : totalLength;
                    byte[] buffer = new byte[bufferSize];
                    int crtRead = 0, totalRead = 0;
                    while (totalRead < totalLength)
                    {
                        if (totalLength - totalRead < bufferSize) bufferSize = totalLength - totalRead;
                        crtRead = sourceStream.Read(buffer, 0, (int)bufferSize);

                        if (crtRead > 0)
                        {
                            ms.Write(buffer, 0, crtRead);
                            totalRead += crtRead;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                return ms.ToArray();
            }
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SizeableStream"/> class.
        /// </summary>
        /// <param name="rawStream">原始序列流来源</param>
        /// <param name="isRequest">是否是请求序列流</param>
        public SizeableStream(Stream rawStream, bool isRequest)
        {
            internalStream = rawStream;
            isRequestSession = isRequest;
        }

        /// <summary>
        /// 初始化一个会话请求序列流
        /// </summary>
        /// <param name="rawStream">原始序列流来源</param>
        public SizeableStream(Stream rawStream)
        {
            internalStream = rawStream;
        }

        /// <summary>
        /// 判断单次会话(一个R&amp;R会话单位)状态是否完成，即已发送完所需数据
        /// </summary>
        public override bool HasFinished()
        {
            return !(LeaveLength > 0);
        }

        #region Seek
        /// <summary>
        /// 获取指示当前流是否支持查找功能的值。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果流支持查找，为 true；否则为 false。</returns>
        public override bool CanSeek
        {
            get { return internalStream.CanSeek; }
        }

        /// <summary>
        /// 设置当前流中的位置。
        /// </summary>
        /// <param name="offset">相对于 <paramref name="origin"/> 参数的字节偏移量。</param>
        /// <param name="origin"><see cref="T:System.IO.SeekOrigin"/> 类型的值，指示用于获取新位置的参考点。</param>
        /// <returns>当前流中的新位置。</returns>
        /// <exception cref="T:System.IO.IOException">发生 I/O 错误。</exception>
        /// <exception cref="T:System.NotSupportedException">流不支持查找，例如在流通过管道或控制台输出构造的情况下即为如此。</exception>
        /// <exception cref="T:System.ObjectDisposedException">在流关闭后调用方法。</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (!CanSeek)
            {
                throw new NotSupportedException();
            }
            else
            {
                long newPosition = internalStream.Seek(offset, origin);
                if (newPosition <= Length && Position != newPosition)
                {
                    //Console.WriteLine("Seek Set ..{0}, Raw:{1}", newPosition, InternalStream.Position);
                    SetPosition(newPosition);
                }
                return newPosition;
            }

        }

        long _position = 0L;

        /// <summary>
        /// 获取或设置当前流中的位置。
        /// </summary>
        /// <value></value>
        /// <returns>流中的当前位置。</returns>
        public override long Position
        {

            get
            {
                long tryPos = -1;
                try
                {
                    tryPos = internalStream.Position;
                }
                catch { }

                if (tryPos != -1) _position = tryPos;
                return _position;
            }

            set
            {
                try
                {
                    internalStream.Position = value;
                }
                catch { }

                _position = value;
            }
        }

        /// <summary>
        /// 内部设置当前会话序列流所在位置
        /// </summary>
        /// <param name="value">流中的当前位置</param>
        public void SetPosition(long value)
        {
            Position = value;
            if (_position > targetLength) _position = targetLength;
        }

        /// <summary>
        /// 获取序列流在目前状态下还剩下的字节长度
        /// </summary>
        public long LeaveLength
        {
            get { return targetLength - _position; }
        }
        #endregion

        /// <summary>
        /// 获取用字节表示的流长度。
        /// </summary>
        /// <value></value>
        /// <returns>用字节表示流长度的长值。</returns>
        /// <exception cref="T:System.NotSupportedException">从 Stream 派生的类不支持查找。</exception>
        /// <exception cref="T:System.ObjectDisposedException">在流关闭后调用方法。</exception>
        public override long Length
        {
            get { return targetLength; }
        }

        /// <summary>
        /// 设置当前流的长度。
        /// </summary>
        /// <param name="value">所需的当前流的长度（以字节表示）。</param>
        /// <exception cref="T:System.IO.IOException">发生 I/O 错误。</exception>
        /// <exception cref="T:System.NotSupportedException">流不支持写入和查找，例如在流通过管道或控制台输出构造的情况下即为如此。</exception>
        /// <exception cref="T:System.ObjectDisposedException">在流关闭后调用方法。</exception>
        public override void SetLength(long value)
        {
            targetLength = value;
        }

        /// <summary>
        /// 将清除该流的所有缓冲区，并使得所有缓冲数据被写入到基础设备。
        /// </summary>
        /// <exception cref="T:System.IO.IOException">发生 I/O 错误。</exception>
        public override void Flush()
        {
            internalStream.Flush();
            if (internalDump != null) internalDump.Flush();
        }

        /// <summary>
        /// 获取指示当前流是否支持读取的值。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果流支持读取，为 true；否则为 false。</returns>
        public override bool CanRead
        {
            get { return (targetLength != -1L); }
        }

        /// <summary>
        /// 从当前流读取字节序列，并将此流中的位置提升读取的字节数。
        /// </summary>
        /// <param name="buffer">字节数组。此方法返回时，该缓冲区包含指定的字符数组，该数组的 <paramref name="offset"/> 和 (<paramref name="offset"/> + <paramref name="count"/> -1) 之间的值由从当前源中读取的字节替换。</param>
        /// <param name="offset"><paramref name="buffer"/> 中的从零开始的字节偏移量，从此处开始存储从当前流中读取的数据。</param>
        /// <param name="count">要从当前流中最多读取的字节数。</param>
        /// <returns>
        /// 读入缓冲区中的总字节数。如果当前可用的字节数没有请求的字节数那么多，则总字节数可能小于请求的字节数，或者如果已到达流的末尾，则为零 (0)。
        /// </returns>
        /// <exception cref="T:System.ArgumentException">
        /// 	<paramref name="offset"/> 与 <paramref name="count"/> 的和大于缓冲区长度。</exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="buffer"/> 为 null。</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="offset"/> 或 <paramref name="count"/> 为负。</exception>
        /// <exception cref="T:System.IO.IOException">发生 I/O 错误。</exception>
        /// <exception cref="T:System.NotSupportedException">流不支持读取。</exception>
        /// <exception cref="T:System.ObjectDisposedException">在流关闭后调用方法。</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            long oldPosition = _position;

            int currentRead = internalStream.Read(buffer, offset, count);
            //响应的读取即写入
            if (currentRead > 0 && internalDump != null && !RequestSession)
            {
                internalDump.Write(buffer, offset, currentRead);
            }

            if (currentRead > 0
                && oldPosition + currentRead > Position)
            {
                //Console.WriteLine("Read Set ..{0}, Raw:{1}", oldPosition + currentRead, Position);
                SetPosition(oldPosition + currentRead);
            }

            return currentRead;
        }

        /// <summary>
        /// 获取指示当前流是否支持写入功能的值。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果流支持写入，为 true；否则为 false。</returns>
        public override bool CanWrite
        {
            get { return (targetLength != -1L); }
        }

        /// <summary>
        /// 向当前流中写入字节序列，并将此流中的当前位置提升写入的字节数。
        /// </summary>
        /// <param name="buffer">字节数组。此方法将 <paramref name="count"/> 个字节从 <paramref name="buffer"/> 复制到当前流。</param>
        /// <param name="offset"><paramref name="buffer"/> 中的从零开始的字节偏移量，从此处开始将字节复制到当前流。</param>
        /// <param name="count">要写入当前流的字节数。</param>
        /// <exception cref="T:System.ArgumentException">
        /// 	<paramref name="offset"/> 与 <paramref name="count"/> 的和大于缓冲区长度。</exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="buffer"/> 为 null。</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="offset"/> 或 <paramref name="count"/> 为负。</exception>
        /// <exception cref="T:System.IO.IOException">发生 I/O 错误。</exception>
        /// <exception cref="T:System.NotSupportedException">流不支持写入。</exception>
        /// <exception cref="T:System.ObjectDisposedException">在流关闭后调用方法。</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            long oldPosition = _position;

            internalStream.Write(buffer, offset, count);

            if (internalDump != null)
            {
                internalDump.Write(buffer, offset, count);
            }

            //Fix
            if (oldPosition + count > Position)
            {
                SetPosition(oldPosition + count);
                //Console.WriteLine("Write Set ..{0}, Raw:{1}", oldPosition + count, oldPosition);
            }

        }

        /// <summary>
        /// 获取一个值，该值确定当前流是否可以超时。
        /// </summary>
        /// <value></value>
        /// <returns>一个确定当前流是否可以超时的值。</returns>
        public override bool CanTimeout
        {
            get
            {
                return internalStream.CanTimeout;
            }
        }

        /// <summary>
        /// 获取或设置一个值（以毫秒为单位），该值确定流在超时前尝试读取多长时间。
        /// </summary>
        /// <value></value>
        /// <returns>一个确定流在超时前尝试读取多长时间的值（以毫秒为单位）。</returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// 	<see cref="P:System.IO.Stream.ReadTimeout"/> 方法总是引发 <see cref="T:System.InvalidOperationException"/>。</exception>
        public override int ReadTimeout
        {
            get
            {
                return internalStream.ReadTimeout;
            }
            set
            {
                internalStream.ReadTimeout = value;
            }
        }

        /// <summary>
        /// 获取或设置一个值（以毫秒为单位），该值确定流在超时前尝试写入多长时间。
        /// </summary>
        /// <value></value>
        /// <returns>一个确定流在超时前尝试写入多长时间的值（以毫秒为单位）。</returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// 	<see cref="P:System.IO.Stream.WriteTimeout"/> 方法总是引发 <see cref="T:System.InvalidOperationException"/>。</exception>
        public override int WriteTimeout
        {
            get
            {
                return internalStream.WriteTimeout;
            }
            set
            {
                internalStream.WriteTimeout = value;
            }
        }

        /// <summary>
        /// 获取控制此实例的生存期策略的生存期服务对象。
        /// </summary>
        /// <returns>
        /// 	<see cref="T:System.Runtime.Remoting.Lifetime.ILease"/> 类型的对象，用于控制此实例的生存期策略。这是此实例当前的生存期服务对象（如果存在）；否则为初始化为 <see cref="P:System.Runtime.Remoting.Lifetime.LifetimeServices.LeaseManagerPollTime"/> 属性的值的新生存期服务对象。
        /// </returns>
        /// <exception cref="T:System.Security.SecurityException">直接调用方没有基础结构权限。</exception>
        /// <PermissionSet>
        /// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="RemotingConfiguration, Infrastructure"/>
        /// </PermissionSet>
        public override object InitializeLifetimeService()
        {
            return internalStream.InitializeLifetimeService();
        }

        /// <summary>
        /// 创建一个对象，该对象包含生成用于与远程对象进行通信的代理所需的全部相关信息。
        /// </summary>
        /// <param name="requestedType">新的 <see cref="T:System.Runtime.Remoting.ObjRef"/> 将引用的对象的 <see cref="T:System.Type"/>。</param>
        /// <returns>生成代理所需要的信息。</returns>
        /// <exception cref="T:System.Runtime.Remoting.RemotingException">此实例不是有效的远程处理对象。</exception>
        /// <exception cref="T:System.Security.SecurityException">直接调用方没有基础结构权限。</exception>
        /// <PermissionSet>
        /// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="Infrastructure"/>
        /// </PermissionSet>
        public override System.Runtime.Remoting.ObjRef CreateObjRef(Type requestedType)
        {
            return internalStream.CreateObjRef(requestedType);
        }

        /// <summary>
        /// 向目标流写入全部序列内容，并获取放回的定长字节序列流对象
        /// </summary>
        /// <param name="targetWriter">目标写入序列流</param>
        /// <param name="sizeBufferSize">包含长度信息的缓冲字节大小</param>
        /// <param name="calcSizeFunc">统计流总长度的计算方法,如果计算结果小于缓冲的长度则总长度为缓冲字节长度。</param>
        /// <returns>一个定长的包含当前位置信息的序列流</returns>
        public SizeableStream GetResponseStream(Stream targetWriter, int sizeBufferSize, Func<byte[], long> calcSizeFunc)
        {
            return GetResponseStream(0L, targetWriter, sizeBufferSize, calcSizeFunc);
        }

        /// <summary>
        /// 向目标流写入从偏移量开始的序列内容，并获取返回的定长字节序列流对象
        /// </summary>
        /// <param name="offSize">当前写入数据开始的偏移位置</param>
        /// <param name="targetWriter">目标写入序列流</param>
        /// <param name="sizeBufferSize">包含长度信息的缓冲字节大小</param>
        /// <param name="calcSizeFunc">统计流总长度的计算方法,如果计算结果小于缓冲的长度则总长度为缓冲字节长度。</param>
        /// <returns>一个定长的包含当前位置信息的序列流</returns>
        public SizeableStream GetResponseStream(long offSize, Stream targetWriter, int sizeBufferSize, Func<byte[], long> calcSizeFunc)
        {
            SetPosition(offSize);

            long currentWrite = offSize;
            int crtRead = 0;

            long bufferSize = (Length < 4096) ? Length : 4096;
            byte[] buffer = new byte[bufferSize];
            while (currentWrite < Length)
            {
                if (Length - currentWrite < bufferSize) buffer = new byte[Length - currentWrite];
                crtRead = Read(buffer, 0, buffer.Length);

                if (crtRead > 0)
                    targetWriter.Write(buffer, 0, crtRead);
                else
                    break;

                currentWrite += crtRead;
            }

            return SizeableStream.CreateNew(targetWriter, false, sizeBufferSize, calcSizeFunc);
        }
    }
}
