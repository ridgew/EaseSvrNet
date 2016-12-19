using System;
using Gwsoft.EaseMode;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 数据交互异常
    /// </summary>
    public class DataExchangeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataExchangeException"/> class.
        /// </summary>
        public DataExchangeException()
            : base()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataExchangeException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public DataExchangeException(string message)
            : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataExchangeException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DataExchangeException(string message, Exception innerException)
            : base(message, innerException)
        { }


    }

    /// <summary>
    /// 无效的业务回复异常
    /// </summary>
    public class InvalidBizResponseException : Exception
    {
        /// <summary>
        /// 初始化一个 <see cref="InvalidBizResponseException"/> class 实例。
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="response">The response.</param>
        public InvalidBizResponseException(string message, ResponseBase response)
            : base(message)
        {
            ResponseDefault = response;
        }

        /// <summary>
        /// 出现异常时返回的默认回复
        /// </summary>
        public ResponseBase ResponseDefault { get; set; }

    }
}
