
namespace EaseServer.Interface
{
    /// <summary>
    /// 连接模型
    /// </summary>
    public enum ConnectionMode
    {
        /// <summary>
        /// 内部自动控制
        /// </summary>
        Auto = 0,

        /// <summary>
        /// 调用后自动关闭连接
        /// </summary>
        SingleCall = 1,

        /// <summary>
        /// 保持连接，由调用方控制
        /// </summary>
        KeepAlive = 2,

        /// <summary>
        /// 保持连接的自定义处理（接管Socket连接之后)
        /// </summary>
        SelfHosting = 3

    }
}
