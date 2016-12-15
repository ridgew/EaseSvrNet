using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace EaseServer
{
    /// <summary>
    /// 只写入的文件字节序列
    /// </summary>
    public class FileDumpStream : Stream
    {
        /// <summary>
        /// 初始化一个 <see cref="FileDumpStream"/> class 实例。
        /// </summary>
        /// <param name="baseDir">The base dir.</param>
        /// <param name="sid">The sid.</param>
        /// <param name="format">写入格式</param>
        public FileDumpStream(string baseDir, string sid, DumpFormat format)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, baseDir);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            ds = new FileStream(dir + Path.DirectorySeparatorChar.ToString() + sid,
                FileMode.Create, FileAccess.Write, FileShare.Read);
            dumpFormat = format;
        }

        FileStream ds = null;
        DumpFormat dumpFormat = DumpFormat.Binary;

        #region 默认不支持的实现
        /// <summary>
        /// 当在派生类中重写时，获取指示当前流是否支持读取的值。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果流支持读取，为 true；否则为 false。</returns>
        public override bool CanRead
        {
            get { return false; }
        }

        /// <summary>
        /// 当在派生类中重写时，获取指示当前流是否支持查找功能的值。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果流支持查找，为 true；否则为 false。</returns>
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <summary>
        /// 当在派生类中重写时，获取指示当前流是否支持写入功能的值。
        /// </summary>
        /// <value></value>
        /// <returns>
        /// 如果流支持写入，为 true；否则为 false。</returns>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// 当在派生类中重写时，将清除该流的所有缓冲区，并使得所有缓冲数据被写入到基础设备。
        /// </summary>
        /// <exception cref="T:System.IO.IOException">发生 I/O 错误。</exception>
        public override void Flush()
        {
            if (ds != null) ds.Flush();
        }

        /// <summary>
        /// 当在派生类中重写时，获取用字节表示的流长度。
        /// </summary>
        /// <value></value>
        /// <returns>用字节表示流长度的长值。</returns>
        /// <exception cref="T:System.NotSupportedException">从 Stream 派生的类不支持查找。</exception>
        /// <exception cref="T:System.ObjectDisposedException">在流关闭后调用方法。</exception>
        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// 当在派生类中重写时，获取或设置当前流中的位置。
        /// </summary>
        /// <value></value>
        /// <returns>流中的当前位置。</returns>
        /// <exception cref="T:System.IO.IOException">发生 I/O 错误。</exception>
        /// <exception cref="T:System.NotSupportedException">流不支持查找。</exception>
        /// <exception cref="T:System.ObjectDisposedException">在流关闭后调用方法。</exception>
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 当在派生类中重写时，从当前流读取字节序列，并将此流中的位置提升读取的字节数。
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
            throw new NotSupportedException();
        }

        /// <summary>
        /// 当在派生类中重写时，设置当前流中的位置。
        /// </summary>
        /// <param name="offset">相对于 <paramref name="origin"/> 参数的字节偏移量。</param>
        /// <param name="origin"><see cref="T:System.IO.SeekOrigin"/> 类型的值，指示用于获取新位置的参考点。</param>
        /// <returns>当前流中的新位置。</returns>
        /// <exception cref="T:System.IO.IOException">发生 I/O 错误。</exception>
        /// <exception cref="T:System.NotSupportedException">流不支持查找，例如在流通过管道或控制台输出构造的情况下即为如此。</exception>
        /// <exception cref="T:System.ObjectDisposedException">在流关闭后调用方法。</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 当在派生类中重写时，设置当前流的长度。
        /// </summary>
        /// <param name="value">所需的当前流的长度（以字节表示）。</param>
        /// <exception cref="T:System.IO.IOException">发生 I/O 错误。</exception>
        /// <exception cref="T:System.NotSupportedException">流不支持写入和查找，例如在流通过管道或控制台输出构造的情况下即为如此。</exception>
        /// <exception cref="T:System.ObjectDisposedException">在流关闭后调用方法。</exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        #endregion

        void HexViewStringAppender(byte[] binDat, Action<string> appendHandler)
        {
            byte[] ascByte = new byte[16];
            int lastRead = 0;

            for (int i = 0, j = binDat.Length; i < j; i++)
            {
                if (i == 0) appendHandler("00000000  ");

                appendHandler(binDat[i].ToString("X2") + " ");
                lastRead = i % 16;
                ascByte[lastRead] = binDat[i];

                if (i > 0 && (i + 1) % 8 == 0 && (i + 1) % 16 != 0)
                {
                    appendHandler(" ");
                }

                if (i > 0 && (i + 1) % 16 == 0)
                {
                    appendHandler(" ");
                    foreach (byte chrB in ascByte)
                    {
                        if (chrB >= 0x20 && chrB <= 0x7E) //[32,126]
                        {
                            appendHandler(((char)chrB).ToString());
                        }
                        else
                        {
                            appendHandler(".");
                        }
                    }

                    if (i + 1 != j)
                    {
                        appendHandler(Environment.NewLine);
                        appendHandler((i + 1).ToString("X2").PadLeft(8, '0') + "  ");
                    }
                }
            }

            if (lastRead < 15)
            {
                appendHandler(new string(' ', (15 - lastRead) * 3));
                if (lastRead < 8) appendHandler(" ");
                appendHandler(" ");
                for (int m = 0; m <= lastRead; m++)
                {
                    byte charL = ascByte[m];
                    if (charL >= 0x20 && charL <= 0x7E) //[32,126]
                    {
                        appendHandler(((char)charL).ToString());
                    }
                    else
                    {
                        appendHandler(".");
                    }
                }
            }
        }

        /// <summary>
        /// 当在派生类中重写时，向当前流中写入字节序列，并将此流中的当前位置提升写入的字节数。
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
            if (ds == null) return;
            if (dumpFormat == DumpFormat.Binary)
            {
                ds.Write(buffer, offset, count);
            }
            else
            {
                byte[] wBytes = new byte[count];
                Buffer.BlockCopy(buffer, offset, wBytes, 0, count);

                Encoding enc = Encoding.Default;
                byte[] newLineBytes = enc.GetBytes(Environment.NewLine);
                if (ds.Length > 0)
                {
                    ds.Write(newLineBytes, 0, newLineBytes.Length);
                    ds.Write(newLineBytes, 0, newLineBytes.Length);
                }
                byte[] encodeBytes = null;

                switch (dumpFormat)
                {
                    case DumpFormat.HexString:
                        for (int i = 0; i < wBytes.Length; i++)
                        {
                            encodeBytes = enc.GetBytes(wBytes[i].ToString("X2") + " ");
                            ds.Write(encodeBytes, 0, encodeBytes.Length);
                            if (i > 0 && (i + 1) % 16 == 0)
                            {
                                ds.Write(newLineBytes, 0, newLineBytes.Length);
                            }
                        }
                        break;
                    case DumpFormat.HexViewString:
                        HexViewStringAppender(wBytes, s =>
                        {
                            encodeBytes = enc.GetBytes(s);
                            ds.Write(encodeBytes, 0, encodeBytes.Length);
                        });
                        break;
                    default:
                        break;
                }
            }

        }

        /// <summary>
        /// 关闭当前流并释放与之关联的所有资源（如套接字和文件句柄）。
        /// </summary>
        public override void Close()
        {
            if (ds != null) ds.Close();
        }

        /// <summary>
        /// 释放由 <see cref="T:System.IO.Stream"/> 占用的非托管资源，还可以另外再释放托管资源。
        /// </summary>
        /// <param name="disposing">为 true 则释放托管资源和非托管资源；为 false 则仅释放非托管资源。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && ds != null)
            {
                ds.Dispose();
            }
        }

    }

    /// <summary>
    /// 支持记录格式
    /// </summary>
    public enum DumpFormat
    {
        /// <summary>
        /// 二进制
        /// </summary>
        Binary = 0,

        /// <summary>
        /// 16进制显示的字符串
        /// </summary>
        HexString = 2,

        /// <summary>
        /// 字节序列的16进制显示视图
        /// </summary>
        HexViewString = 3
    }

}
