
namespace EaseServer.Interface
{
    /// <summary>
    /// 服务器端首次连接消息接口
    /// </summary>
    public interface IHELOFirstRnrProcessor : IRnRProcessor
    {
        /// <summary>
        /// 首次连接消息
        /// </summary>
        void SayHello();
    }
}
