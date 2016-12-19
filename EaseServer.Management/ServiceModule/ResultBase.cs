using System;
using Gwsoft.Resource;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    ///通用数据返回接口
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// 数据返回协议
        /// </summary>
        string Protocol { get; set; }

        /// <summary>
        /// 接口调用状态
        /// </summary>
        int Status { get; set; }

        /// <summary>
        /// 接口返回消息
        /// </summary>
        string Message { get; set; }
    }

    /// <summary>
    ///通用返回数据基础类（协议1.x）
    /// </summary>
    public class Result : ResultBase
    {
        /// <summary>
        /// 获取或设置接口调用状态
        /// </summary>
        public override int Status
        {
            get
            {
                return base.Status;
            }
            set
            {
                base.Status = value;
                Gwsoft.Resource.Message msg = value.GetResouce();
                if (msg != null)
                    base.Message = msg.Title + msg.Body;
            }
        }
    }

    /// <summary>
    ///通用返回数据基类（协议1.x）
    /// </summary>
    [Serializable]
    public abstract class ResultBase : IResult
    {

        #region IResult 成员

        private string _protocol = string.Empty;
        /// <summary>
        /// 获取或设置数据返回协议
        /// </summary>
        public virtual string Protocol
        {
            get
            {
                return this._protocol;
            }
            set
            {
                this._protocol = value;
            }
        }

        private int _status = -1;
        /// <summary>
        /// 获取或设置接口调用状态
        /// </summary>
        public virtual int Status
        {
            get
            {
                return this._status;
            }
            set
            {
                this._status = value;
            }
        }

        private string _message = string.Empty;
        /// <summary>
        /// 获取或设置接口返回消息
        /// </summary>
        public virtual string Message
        {
            get
            {
                return _message;
            }
            set
            {
                this._message = value;
            }
        }

        #endregion
    }

    /// <summary>
    ///单记录数据返回类
    /// </summary>
    public class SvcSingleRecord<T> : ResultBase
    {
        /// <summary>
        /// 获取或设置返回数据内容
        /// </summary>
        public T Data { get; set; }
    }

    /// <summary>
    ///多记录数据返回类
    /// </summary>
    public class SvcMultiRecord<T> : ResultBase
    {
        /// <summary>
        /// 获取或设置返回数据内容
        /// </summary>
        public T[] Data { get; set; }
    }

    /// <summary>
    ///公共分页数据类
    /// </summary>
    public class SvcPagingRecord<T> : ResultBase
    {
        private int _pageIndex = 1;
        /// <summary>
        /// 获取或设置当前页码，默认值1表示第一页。
        /// </summary>
        public int PageIndex
        {
            get
            {
                return this._pageIndex;
            }
            set
            {
                this._pageIndex = (value < 1) ? 1 : value;
            }
        }

        private int _pageSize = 10;
        /// <summary>
        /// 获取或设置每页显示内容的条数
        /// </summary>
        public int PageSize
        {
            get
            {
                return this._pageSize;
            }
            set
            {
                if (value < 1)
                {
                    this._pageSize = 20;
                }
                else
                {
                    this._pageSize = value;
                }
            }
        }

        private int _recordCount;
        /// <summary>
        /// 获取或设置所有记录总数
        /// </summary>
        public int RecordCount
        {
            get
            {
                return this._recordCount;
            }
            set
            {
                this._recordCount = value;

                //计算总页数
                this.PageCount = value / this._pageSize;
                if (value % this._pageSize > 0)
                    this.PageCount++;
            }
        }

        /// <summary>
        /// 获取或设置总页数
        /// </summary>
        public int PageCount { get; set; }

        /// <summary>
        /// 获取或设置分页数据结果
        /// </summary>
        public T[] Data { get; set; }
    }

}
