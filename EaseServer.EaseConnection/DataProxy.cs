using System;
using CommonLib;
using Gwsoft.EaseMode;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 业务数据代理主入口
    /// </summary>
    public class DataProxy
    {
        private DataProxy() { }

        private static DataProxy _proxy = null;
        /// <summary>
        /// Socket数据代理实例
        /// </summary>
        public static DataProxy Instance
        {
            get
            {
                if (_proxy == null) _proxy = new DataProxy { IsSocketProxy = true };
                return _proxy;
            }
        }

        private static DataProxy _proxyHttp = null;
        /// <summary>
        /// HTTP数据代理实例
        /// </summary>
        public static DataProxy HttpInstance
        {
            get
            {
                if (_proxyHttp == null) _proxyHttp = new DataProxy { IsSocketProxy = false };
                return _proxyHttp;
            }
        }


        private bool _isSocketProxy = true;
        /// <summary>
        /// 是否是Socket请求数据代理
        /// </summary>
        public bool IsSocketProxy
        {
            get { return _isSocketProxy; }
            set { _isSocketProxy = value; }
        }

        /// <summary>
        /// 获取网关返回封装对象
        /// </summary>
        /// <param name="validRequest">有效的请求</param>
        /// <param name="subRetBytes">子级请求返回字节</param>
        /// <param name="isErrorResp">是否返回错误标识</param>
        /// <returns></returns>
        public static NetworkSwitchResponse GenerateSwitchResponse(NetworkSwitchRequest validRequest, byte[] subRetBytes, bool isErrorResp)
        {
            return GenerateSwitchResponse(validRequest, null, subRetBytes, isErrorResp);
        }

        /// <summary>
        /// Zlib压缩
        /// </summary>
        public static byte[] ZlibCompress(byte[] pBytes)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                ICSharpCode.SharpZipLib.Zip.Compression.Deflater mDeflater = new ICSharpCode.SharpZipLib.Zip.Compression.Deflater(ICSharpCode.SharpZipLib.Zip.Compression.Deflater.BEST_COMPRESSION);
                ICSharpCode.SharpZipLib.Zip.Compression.Streams.DeflaterOutputStream zls = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.DeflaterOutputStream(ms, mDeflater, 80920);
                zls.Write(pBytes, 0, pBytes.Length);
                //byte[] lenBytes = BitConverter.GetBytes(pBytes.Length).ReverseBytes();
                //ms.Write(lenBytes, 0, lenBytes.Length);
                zls.Close();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 判断是否记录当前业务调试信息
        /// </summary>
        /// <param name="totalLength">原始字节长度</param>
        /// <param name="businessId">业务编号</param>
        /// <returns></returns>
        static bool CompressCurrentBizData(int totalLength, short businessId)
        {
            string CurrentType = typeof(DataProxy).FullName;
            if (totalLength < (CurrentType + ".CompressBizMinBytes").AppSettings<int>(4096))
            {
                return false;
            }
            return true;

            //string sidsSetting = (CurrentType + ".CompressBizData").AppSettings<string>("");
            //if (string.IsNullOrEmpty(sidsSetting))
            //{
            //    return false;
            //}
            //else
            //{
            //    string crtId = businessId.ToString();
            //    return ("," + sidsSetting + ",").IndexOf("," + crtId + ",") != -1;
            //}
        }


        /// <summary>
        /// 获取网关返回封装对象
        /// </summary>
        /// <param name="validRequest">有效的请求</param>
        /// <param name="header">相关业务请求头</param>
        /// <param name="subRetBytes">子级请求返回字节</param>
        /// <param name="isErrorResp">是否返回错误标识</param>
        /// <returns></returns>
        public static NetworkSwitchResponse GenerateSwitchResponse(NetworkSwitchRequest validRequest, RequestHeader header, byte[] subRetBytes, bool isErrorResp)
        {
            if (subRetBytes == null) subRetBytes = new byte[0];

            NetworkSwitchResponse swResp = new NetworkSwitchResponse(validRequest.Context);
            #region 设置网关对象的值
            swResp.ESP_CustomCode = validRequest.ESP_CustomeCode;
            swResp.ESP_SuccessFlag = (isErrorResp) ? EaseSuccessFlag.Error : validRequest.ESP_SuccessFlag;

            #region 如果没有返回错误
            if (subRetBytes.Length > 0)
            {
                if (validRequest.ESP_DateEndIndex > 0
                    && validRequest.ESP_DateEndIndex >= validRequest.ESP_DataIndex)
                {
                    #region 断点续传应用数据

                    int idxEnd = validRequest.ESP_DateEndIndex;
                    if (idxEnd > subRetBytes.Length - 1) idxEnd = subRetBytes.Length - 1;

                    byte[] realRespBytes = new byte[idxEnd - validRequest.ESP_DataIndex + 1];
                    Buffer.BlockCopy(subRetBytes, validRequest.ESP_DataIndex, realRespBytes,
                        0, realRespBytes.Length);

                    #region 简易版将忽略以下属性
                    swResp.ESP_DataTotalLength = subRetBytes.Length;    //总数据长度
                    swResp.ESP_DataIndex = validRequest.ESP_DataIndex;
                    swResp.ESP_DateEndIndex = idxEnd;
                    #endregion
                    swResp.ESP_TransferData = realRespBytes;
                    swResp.ESP_LeaveLength = realRespBytes.Length;

                    #endregion
                }
                else
                {
                    #region 简易版将忽略以下属性
                    swResp.ESP_DataTotalLength = subRetBytes.Length;
                    swResp.ESP_DataIndex = validRequest.ESP_DataIndex;
                    swResp.ESP_DateEndIndex = validRequest.ESP_DateEndIndex;
                    #endregion

                    if (header != null && header.ESP_BusinessID > 0 && CompressCurrentBizData(subRetBytes.Length, header.ESP_BusinessID))
                    {
                        if (header.ESP_Compress == EaseCompress.Zlib)
                        {
                            int rawLen = subRetBytes.Length;  //附加4个字节的PC序列字节、表示原始数据长度。
                            subRetBytes = (byte[])ZlibCompress(subRetBytes).Combine(BitConverter.GetBytes(rawLen));
                            swResp.ESP_SuccessFlag = EaseSuccessFlag.SuccessCompressedZlib;
                        }
                    }
                    swResp.ESP_TransferData = subRetBytes;
                    swResp.ESP_LeaveLength = subRetBytes.Length;
                    swResp.ESP_DataTotalLength = subRetBytes.Length;
                }
            }
            #endregion

            if (validRequest.ESP_SuccessFlag == EaseSuccessFlag.Success
                || validRequest.ESP_SuccessFlag == EaseSuccessFlag.SuccessCompressedZlib
                || validRequest.ESP_SuccessFlag == EaseSuccessFlag.SuccessUserAgent)
            {
                swResp.ESP_LeaveLength += 12;
            }
            #endregion

            return swResp;
        }

        /// <summary>
        /// 在当前会话上下文中首次处理时间的件名称
        /// </summary>
        public const string PROXYPROCESS_REQUEST_FIRSTKEY = "DataProxy.GetRequestByteTime";

        /// <summary>
        /// Generics the exception handler.
        /// </summary>
        /// <param name="exceptionHandler">The exception handler.</param>
        /// <param name="exception">The exception.</param>
        public static void GenericExceptionHandler(HandlerException exceptionHandler, Exception exception)
        {
            if (exceptionHandler != null)
            {
                if (!exceptionHandler(exception)) throw exception;
            }
        }
    }

    /// <summary>
    /// 处理异常并是否终止下一步处理
    /// </summary>
    public delegate bool HandlerException(Exception exception);

    /// <summary>
    /// 易致代码过滤文件处理委托
    /// </summary>
    /// <param name="businessId">业务编码</param>
    /// <param name="rawEaseCode">原始易致代码</param>
    /// <param name="isHtmlText">是否是html代码</param>
    /// <returns>返回处理后的Ease代码</returns>
    public delegate string EaseCodeFilter(short businessId, string rawEaseCode, bool isHtmlText);

}
