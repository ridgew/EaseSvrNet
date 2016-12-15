using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using CommonLib;

namespace EaseServer.Performance
{
    /// <summary>
    /// 性能计数器
    /// </summary>
    [Serializable]
    public class PerformanceCounter
    {
        /// <summary>
        /// 单个会话日志编号
        /// </summary>
        [XmlAttribute]
        public long LogID { get; set; }

        /// <summary>
        /// 远程连接终端
        /// </summary>
        [XmlAttribute]
        public string RemoteEndpoint { get; set; }

        /// <summary>
        /// 本地连接终端
        /// </summary>
        [XmlAttribute]
        public string LocalEndPoint { get; set; }

        /// <summary>
        /// 业务编号
        /// </summary>
        [XmlAttribute]
        public string BussinessID { get; set; }

        /// <summary>
        /// 性能统计数据
        /// </summary>
        public PerfData RootPerfData { get; set; }
    }

    /// <summary>
    /// 统计数据
    /// </summary>
    [Serializable]
    public class PerfData
    {
        /* 临时测试
        public void test2()
        {
            PerfData root = new PerfData() { Point=PerformancePoint.WholeTime};
            root.BeginCounter(PerformancePoint.WholeTime);


            PerfData nData = root.GetOrCreateSubPerfData(PerformancePoint.ReceiveData, false, null);
            nData.BeginCounter(PerformancePoint.ReceiveData);
            nData.EndCounter();

            nData = root.GetOrCreateSubPerfData(PerformancePoint.ParseProcessor, false, null);
            nData.BeginCounter(PerformancePoint.ParseProcessor);
            nData.EndCounter();

            nData = root.GetOrCreateSubPerfData(PerformancePoint.ParseData, false, null);
            nData.BeginCounter(PerformancePoint.ParseData);
            nData.EndCounter();

            nData = root.GetOrCreateSubPerfData(PerformancePoint.PerpareData, false, null);
            nData.BeginCounter(PerformancePoint.PerpareData);
            nData.EndCounter();

            nData = root.GetOrCreateSubPerfData(PerformancePoint.SingleResource, false, null);
            nData.BeginCounter(PerformancePoint.SingleResource);
            nData.EndCounter();

            nData = root.GetOrCreateSubPerfData(PerformancePoint.RecordLog, false, null);
            nData.BeginCounter(PerformancePoint.RecordLog);
            nData.EndCounter();

            root.EndCounter();
            root.GetXmlDoc(true).WriteIndentedContent(Console.Out);
        }
         */

        /// <summary>
        /// 统计点
        /// </summary>
        [XmlAttribute]
        public PerformancePoint Point { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        [XmlAttribute]
        public double Begin { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        [XmlAttribute]
        public double End { get; set; }

        /// <summary>
        /// 统计资源相关信息
        /// </summary>
        [XmlAttribute]
        public string URI { get; set; }

        /// <summary>
        /// 是否使用了缓存
        /// </summary>
        [XmlAttribute]
        public bool UseCache { get; set; }

        /// <summary>
        /// 本段数据的总毫秒数
        /// </summary>
        [XmlAttribute]
        public Double TotalTime { get; set; }

        PerfData[] _sub = null;
        /// <summary>
        /// 子级统计数据
        /// </summary>
        public PerfData[] SubPerfData
        {
            get { return _sub; }
            set { _sub = value; }
        }

        List<PerfData> _subList = new List<PerfData>();
        /// <summary>
        /// 添加子级统计数据
        /// </summary>
        /// <param name="dat">统计数据</param>
        public void AddSubPerfData(PerfData dat)
        {
            _subList.Add(dat);
            _sub = _subList.ToArray();
        }

        /// <summary>
        /// 获取统计层次(最多8层)
        /// </summary>
        /// <returns></returns>
        public int GetLevel()
        {
            return getPointIndexes(Point).Length;
        }

        /// <summary>
        /// 判断是否有子级统计
        /// </summary>
        /// <returns></returns>
        public bool HasChild()
        {
            return _subList.Count > 0;
        }

        /// <summary>
        /// 开始统计
        /// </summary>
        /// <param name="point">统计点</param>
        /// <param name="appendActions">附加的其他操作</param>
        /// <returns></returns>
        public PerfData BeginCounter(PerformancePoint point, params Action<PerfData>[] appendActions)
        {
            Point = point;
            if (Begin.Equals(default(double)))
                Begin = DateTime.Now.GetTimeMilliseconds();

            if (appendActions != null && appendActions.Length > 0)
            {
                foreach (Action<PerfData> doA in appendActions)
                {
                    doA(this);
                }
            }
            return this;
        }

        /// <summary>
        /// 结束当前统计
        /// </summary>
        /// <param name="prefixActions">前置的其他操作</param>
        /// <returns></returns>
        public PerfData EndCounter(params Action<PerfData>[] prefixActions)
        {
            if (prefixActions != null && prefixActions.Length > 0)
            {
                foreach (Action<PerfData> doA in prefixActions)
                {
                    doA(this);
                }
            }
            End = DateTime.Now.GetTimeMilliseconds();
            TotalTime = End - Begin;
            return this;
        }

        /// <summary>
        /// 获取统计点的索引
        /// </summary>
        byte[] getPointIndexes(PerformancePoint point)
        {
            List<byte> idxList = new List<byte>();
            byte[] pBytes = BitConverter.GetBytes((long)point).ReverseBytes();
            for (int i = 0, j = pBytes.Length; i < j; i++)
            {
                if (pBytes[i] != (byte)0)
                {
                    idxList.Add(pBytes[i]);
                }
                else
                {
                    break;
                }
            }
            return idxList.ToArray();
        }

        PerfData findOrCreateIn(PerfData d, PerformancePoint point, bool isEnd)
        {
            bool doCreateNew = (point == PerformancePoint.SingleResource && !isEnd);
            PerfData tarDat = null;
            if (!doCreateNew && d.HasChild())
            {
                foreach (PerfData sd in d.SubPerfData)
                {
                    if (sd.Point == point)
                    {
                        tarDat = sd;
                        break;
                    }
                }
            }

            if (tarDat == null)
            {
                PerfData nDat = new PerfData() { Point = point };
                d.AddSubPerfData(nDat);
                tarDat = d.SubPerfData[d.SubPerfData.Length - 1];
            }
            return tarDat;
        }

        /// <summary>
        /// 在当前性能统计对象中查找统计点的性能计数，没有则创建。
        /// </summary>
        /// <param name="point">统计点</param>
        /// <param name="isEnd">是否获取结束统计</param>
        /// <param name="lastPerfData">最近使用的性能计数实例</param>
        /// <returns></returns>
        public PerfData GetOrCreateSubPerfData(PerformancePoint point, bool isEnd, PerfData lastPerfData)
        {
            if (lastPerfData != null && lastPerfData.Point == point && isEnd)
            {
                return lastPerfData;
            }

            byte[] pIndexes = getPointIndexes(point);
            int currentLev = 1;
            PerfData srcData = this;
            while (currentLev <= pIndexes.Length)
            {
                byte[] levBytes = new byte[8];
                Buffer.BlockCopy(pIndexes, 0, levBytes, 0, currentLev);
                srcData = findOrCreateIn(srcData, (PerformancePoint)BitConverter.ToInt64(levBytes.ReverseBytes(), 0), isEnd);
                currentLev++;
            }
            return srcData;
        }

        /// <summary>
        /// 在当前子统计中查找
        /// </summary>
        /// <returns></returns>
        public PerfData GetPerfDataInSub(PerformancePoint point)
        {
            return _subList.Find(p => p.Point == point);
        }

    }

    /// <summary>
    /// [long]性能计数控制点(最多支持8层，1字节一层。)
    /// </summary>
    [Flags, Serializable]
    public enum PerformancePoint : long
    {
        /// <summary>
        /// 整个会话时间 = 0
        /// </summary>
        WholeTime = 0x0000000000000000,

        #region [ 接收数据 01 ]
        /// <summary>
        /// 接收数据
        /// </summary>
        ReceiveData = 0x0100000000000000,
        /// <summary>
        /// 获取连接处理器
        /// </summary>
        ParseProcessor = 0x0101000000000000,
        #endregion

        #region [ 解析数据 02 ]
        /// <summary>
        /// 解析数据
        /// </summary>
        ParseData = 0x0200000000000000,

        /// <summary>
        /// 解析业务协议时间
        /// </summary>
        ParseProtocol = 0x0201000000000000,
        #endregion

        #region [ *准备数据 03 ]
        /// <summary>
        /// 准备数据
        /// </summary>
        PerpareData = 0x0300000000000000,

        #region 业务数据
        /// <summary>
        /// 获取页面内容时间
        /// </summary>
        PageContent = 0x0301000000000000,

        /// <summary>
        /// 解析页面时间
        /// </summary>
        PageParse = 0x0302000000000000,

        /// <summary>
        /// 获取页面资源时间
        /// </summary>
        PageResource = 0x0302010000000000,

        /// <summary>
        /// 获取单个资源时间
        /// </summary>
        SingleResource = 0x0302010100000000,

        #endregion

        /// <summary>
        /// 打包数据时间
        /// </summary>
        PackageData = 0x0304000000000000,
        #endregion

        #region [ 发送数据 04 ]
        /// <summary>
        /// 下发数据时间
        /// </summary>
        SendData = 0x0400000000000000,
        #endregion

        #region [ 日志记录 05 ]
        /// <summary>
        /// 记录访问日志
        /// </summary>
        RecordLog = 0x0500000000000000,
        #endregion

        /// <summary>
        /// 所有时间点
        /// </summary>
        ALL = WholeTime
            | ReceiveData | ParseProcessor
            | ParseData | ParseProtocol
            | PerpareData | PageContent | PageParse | PageResource | SingleResource
            | PackageData
            | SendData
            | RecordLog
    }
}
