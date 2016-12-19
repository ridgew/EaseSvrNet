using System;
using System.Reflection;
using System.Diagnostics;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    /// 协议标识
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ProtocolAttribute : Attribute
    {
        /// <summary>
        /// 指定webService方法的协议标识
        /// </summary>
        /// <param name="protoId"></param>
        public ProtocolAttribute(string protoId)
        {
            _pid = protoId;
        }

        string _pid = null;
        /// <summary>
        /// 协议标识
        /// </summary>
        public string Identity
        {
            get { return _pid; }
        }

        bool _isPatternIdentity = false;
        /// <summary>
        /// 协议标识是否是正则表达式的匹配模式
        /// </summary>
        public bool RegexPattern
        {
            get { return _isPatternIdentity; }
            set { _isPatternIdentity = value; }
        }

        /// <summary>
        /// 获取当前方法上应用的协议标识配置，如果没有应用则为null。
        /// </summary>
        public static ProtocolAttribute GetCurrentProtocol()
        {
            //1+1
            return GetCurrentProtocol(2);
        }

        /// <summary>
        /// 获取堆栈方法上应用的协议标识配置，如果没有应用则为null。
        /// </summary>
        /// <param name="skipFrames">忽略的堆栈帧树</param>
        public static ProtocolAttribute GetCurrentProtocol(int skipFrames)
        {
            MethodBase method = new StackFrame(skipFrames).GetMethod();
            object[] mAttrs = method.GetCustomAttributes(typeof(ProtocolAttribute), true);
            if (mAttrs != null && mAttrs.Length > 0)
            {
                return (ProtocolAttribute)mAttrs[0];
            }
            return null;
        }

    }
}
