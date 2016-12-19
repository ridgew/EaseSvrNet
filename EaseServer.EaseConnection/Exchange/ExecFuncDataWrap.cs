using System;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 运行函数数据封装
    /// </summary>
    [Serializable]
    public struct ExecFuncDataWrap
    {
        /// <summary>
        /// 获取或设置应该返回的字节数据
        /// </summary>
        public byte[] RetureBytes { get; set; }

        /// <summary>
        /// 获取或设置当前操作是否超时
        /// </summary>
        public bool IsTimeout { get; set; }
    }
}
