using System;

namespace EaseServer.Interface
{
    /// <summary>
    /// 服务公开接口
    /// </summary>
    public interface IServerAPI
    {
        #region 相关信息
        /// <summary>
        /// 获取服务监听端口
        /// </summary>
        int Port { get; }

        /// <summary>
        /// 获取服务名称
        /// </summary>
        string GetServerName();

        /// <summary>
        /// 获取服务版本
        /// </summary>
        string GetServerVersion();

        /// <summary>
        /// 允许的最大连接数
        /// </summary>
        int MaxClientCount { get; }

        /// <summary>
        /// 获取服务端所支持的所有会话标识
        /// </summary>
        string[] GetSupportSessionKeys();

        /// <summary>
        /// 当前服务实例启动时间
        /// </summary>
        DateTime StartDateTime { get; }

        /// <summary>
        /// ASPX服务承载物理路径
        /// </summary>
        string PhysicalPath { get; }

        #endregion

        #region 服务段维护基本操作
        /// <summary>
        /// 当前服务的所有连接总数
        /// </summary>
        int ConnectionCount { get; }

        /// <summary>
        /// 强制断开指定客户端
        /// </summary>
        /// <param name="clientEndPointStr">客户端的ip:port标识字符串</param>
        void DisconnectClient(string clientEndPointStr);

        /// <summary>
        /// 使用正则匹配模式断开符合条件的客户端
        /// </summary>
        /// <param name="clientEndPattern">正则匹配模式</param>
        void DisconnectBatchClient(string clientEndPattern);

        /// <summary>
        /// 以索引分页方式获取客户端连接集合
        /// </summary>
        /// <param name="startIdx">起始索引(0开始)</param>
        /// <param name="pageSize">每次显示多少条</param>
        /// <param name="totalClient">当前总共有多少客户端</param>
        /// <returns></returns>
        IServerConnection[] GetConnectionList(int startIdx, int pageSize, out int totalClient);

        /// <summary>
        /// 按照提供的客户端匹配模式列出客户端
        /// </summary>
        /// <param name="clientPattern">客户端匹配模式</param>
        /// <param name="listHandler">列表项委托</param>
        void ListClientStatus(string clientPattern, ListClientWriter listHandler);

        #endregion

        #region 与客户端连接交互
        /// <summary>
        /// 建立客户端连接
        /// </summary>
        /// <param name="conn">客户端连接</param>
        void RegisterClient(IServerConnection conn);

        /// <summary>
        /// 取消建立客户端连接
        /// </summary>
        /// <param name="conn">客户端连接</param>
        void UnRegisterClient(IServerConnection conn);
        #endregion

    }
}
