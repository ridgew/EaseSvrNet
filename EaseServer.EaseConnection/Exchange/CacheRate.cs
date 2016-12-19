using System;
using System.Xml.Serialization;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 缓存命中率
    /// </summary>
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{GetPercentString(),nq}")]
    public struct CacheRate
    {
        /// <summary>
        /// 使用计数
        /// </summary>
        [XmlAttribute]
        public float UseCount { get; set; }

        /// <summary>
        /// 总数目
        /// </summary>
        [XmlAttribute]
        public float TotalCount { get; set; }

        /// <summary>
        /// 获取计算比率
        /// </summary>
        /// <returns></returns>
        public float GetRate()
        {
            return UseCount / TotalCount;
        }

        /// <summary>
        /// 获取计算的百分比率，形如(2.94%)。
        /// </summary>
        public string GetPercentString()
        {
            return GetRate().ToString("P2");
        }

        /// <summary>
        /// 是否没有缓存计数
        /// </summary>
        public bool IsEmpty()
        {
            return UseCount == TotalCount && TotalCount == 0;
        }
    }
}
