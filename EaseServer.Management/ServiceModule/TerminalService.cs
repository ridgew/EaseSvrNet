using System;
using System.Data;
using System.Linq;
using System.Web.Services;
using EaseServer.Management.DataAccess;
using Gwsoft.SharpOrm;
using Gwsoft.SharpOrm.Config;

namespace EaseServer.Management.ServiceModule
{
    [WebService(Description = "虚拟设备列表")]
    public class TerminalService : WebServiceBase
    {
        [Protocol("1.3.9.3.4"), WebMethod(Description = "虚拟设备列表")]
        public SvcMultiRecord<Terminal> List()
        {
            SvcMultiRecord<Terminal> svcterminal = new SvcMultiRecord<Terminal>() { Protocol = "1.3.9.3.4" };

            try
            {
                Terminal terminal = new Terminal();

                svcterminal.Data = terminal.GetDataList<Terminal>(new string[] { "MT_ID", "MT_Name", "MT_OrderNum" })
                    .OrderBy<Terminal, int>(t=>t.MT_ID)
                    .ToArray<Terminal>();

                svcterminal.Message = "";
                svcterminal.Status = 1;
            }
            catch (Exception ex)
            {
                svcterminal.Message = ex.Message;
                svcterminal.Status = 0;
            }
            return svcterminal;
        }

        [Protocol("1.3.9.2.8"), WebMethod(Description = "某厂商的机型分页列表")]
        public SvcPagingRecord<Mobile> ListByBID(int MT_ID, int pageIndex, int pageSize)
        {
            var result = new SvcPagingRecord<Mobile>
            {
                Protocol = "1.3.9.2.8",
                Status = 0,
                Message = "",
                PageSize = pageSize,
                PageIndex = pageIndex,
                Data = null
            };
            try
            {
                int recordCount;

                DataTable tableRecords = EaseDataProvider.Instance.GetPagingRecords(out recordCount, "gw_Device_Mobile",
                    new string[] { "M_ID", "M_Name", "M_BID", "M_OrderNum", "M_BrewTF", "M_WAPTF", "M_JavaTF", "M_MobileTF", "M_WinceTF", "M_UACode", "M_Info" },
                    new string[] { "M_OrderNum", "M_ID" }, new bool[] { true, true },
                    result.PageIndex, result.PageSize, true, "M_ID IN (SELECT M_ID FROM GW_Device_Mobile_Terminal WHERE MT_ID=@0)", MT_ID);

                result.Status = 1;
                result.Message = "获取虚拟设备组件列表";
                result.RecordCount = recordCount;
                result.Data = (from p in tableRecords.AsEnumerable()
                               select new Mobile
                               {
                                   M_ID = p.Field<int>("M_ID"),
                                   M_Name = p.Field<string>("M_Name"),
                                   M_BID = p.Field<short>("M_BID"),
                                   M_OrderNum = p.Field<short>("M_OrderNum"),
                                   M_BrewTF = p.Field<bool>("M_BrewTF"),
                                   M_MobileTF = p.Field<bool>("M_MobileTF"),
                                   M_WinceTF = p.Field<bool>("M_WinceTF"),
                                   M_WAPTF = p.Field<bool>("M_WAPTF"),
                                   M_JavaTF = p.Field<bool>("M_JavaTF"),
                                   M_UACode = p.Field<string>("M_UACode"),
                                   M_Info = p.Field<string>("M_Info")
                               }).ToArray();

            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }
            return result;
        }

        [Protocol("1.3.9.2.11"), WebMethod(Description = "获取虚拟设备子菜单")]
        public SvcPagingRecord<Mobile> ListTerminalsChild(int MT_ID)
        {
            var result = new SvcPagingRecord<Mobile>
            {
                Protocol = "1.3.9.2.11",
                Status = 0,
                Message = "",
                Data = null
            };
            try
            {
                DataTable tableRecords = EaseDataProvider.Instance.SelectTable("gw_Device_Mobile",
                    new string[] { "M_ID", "M_Name" }, new string[] { "M_OrderNum", "M_ID" }, new bool[] { true, true }, true,
                    "M_ID IN (SELECT M_ID FROM GW_Device_Mobile_Terminal WHERE MT_ID=@0)", MT_ID);

                result.Status = 1;
                result.Message = "虚拟设备子菜单";
                result.Data = (from p in tableRecords.AsEnumerable()
                               select new Mobile
                               {
                                   M_ID = p.Field<int>("M_ID"),
                                   M_Name = p.Field<string>("M_Name")
                               }).ToArray();

            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }
            return result;
        }

        [Protocol("1.3.9.2.5"), WebMethod(Description = "某厂商的机型分页列表")]
        public SvcPagingRecord<Mobile> ListByBrandID(short M_BID, int pageIndex, int pageSize)
        {
            var result = new SvcPagingRecord<Mobile>
            {
                Protocol = M_BID == 0 ? "1.3.9.2.8" : "1.3.9.2.5",
                Status = 0,
                Message = "",
                PageSize = pageSize,
                PageIndex = pageIndex,
                Data = null
            };

            try
            {
                int recordCount;
                DataTable tableRecords;
                if (M_BID == 0)
                {
                    tableRecords = EaseDataProvider.Instance.GetPagingRecords(out recordCount,
                        "gw_Device_Mobile", new string[] { "M_ID", "M_Name", "M_BID", "M_OrderNum", "M_BrewTF", "M_WAPTF", "M_JavaTF", "M_MobileTF", "M_WinceTF", "M_UACode", "M_Info" },
                        new string[] { "M_OrderNum", "M_ID" }, new bool[] { true, true },
                        result.PageIndex, result.PageSize);
                }
                else
                {
                    tableRecords = EaseDataProvider.Instance.GetPagingRecords(out recordCount,
                        "gw_Device_Mobile", new string[] { "M_ID", "M_Name", "M_BID", "M_OrderNum", "M_BrewTF", "M_WAPTF", "M_JavaTF", "M_MobileTF", "M_WinceTF", "M_UACode", "M_Info" },
                        new string[] { "M_OrderNum", "M_ID" }, new bool[] { true, true },
                        result.PageIndex, result.PageSize, true, "M_BID=@0", M_BID);
                }

                result.Status = 1;
                result.Message = "某厂商的机型列表";
                result.RecordCount = recordCount;
                result.Data = (from p in tableRecords.AsEnumerable()
                               select new Mobile
                               {
                                   M_ID = p.Field<int>("M_ID"),
                                   M_Name = p.Field<string>("M_Name"),
                                   M_BID = p.Field<short>("M_BID"),
                                   M_OrderNum = p.Field<short>("M_OrderNum"),
                                   M_BrewTF = p.Field<bool>("M_BrewTF"),
                                   M_MobileTF = p.Field<bool>("M_MobileTF"),
                                   M_WinceTF = p.Field<bool>("M_WinceTF"),
                                   M_WAPTF = p.Field<bool>("M_WAPTF"),
                                   M_JavaTF = p.Field<bool>("M_JavaTF"),
                                   M_UACode = p.Field<string>("M_UACode"),
                                   M_Info = p.Field<string>("M_Info")
                               }).ToArray();

            }
            catch (Exception ex)
            {
                result.Message = ex.Message;

            }
            return result;
        }
    }

    /// <summary>
    /// 实体类Terminal
    /// </summary>
    /// [Serializable]
    [BindTable("gwease", "gw_Device_Terminal")]
    public class Terminal : TableEntry
    {
        #region Model
        private int _mt_id;
        private string _mt_name;
        private string _mt_code;
        private int _mt_screenw;
        private int _mt_screenh;
        private short _mt_memory;
        private short _mt_fontsze;
        private DateTime _mt_addtime;
        private string _mt_info;
        private int _mt_ordernum;
        /// <summary>
        /// 自动编号
        /// </summary>
        [PrimaryKey]
        public int MT_ID
        {
            set { _mt_id = value; }
            get { return _mt_id; }
        }
        /// <summary>
        /// 虚拟设备名称
        /// </summary>
        public string MT_Name
        {
            set { _mt_name = value; }
            get { return _mt_name; }
        }
        /// <summary>
        /// 虚拟设备编号
        /// </summary>
        public string MT_Code
        {
            set { _mt_code = value; }
            get { return _mt_code; }
        }
        /// <summary>
        /// 屏幕宽度
        /// </summary>
        public int MT_ScreenW
        {
            set { _mt_screenw = value; }
            get { return _mt_screenw; }
        }
        /// <summary>
        /// 屏幕高度
        /// </summary>
        public int MT_ScreenH
        {
            set { _mt_screenh = value; }
            get { return _mt_screenh; }
        }
        /// <summary>
        /// 虚拟设备内存大小，1为小于100K的小内存机型，2为大于100K的大内存机型
        /// </summary>
        public short MT_Memory
        {
            set { _mt_memory = value; }
            get { return _mt_memory; }
        }
        /// <summary>
        /// 虚拟设备字体大小，1为小字体，2为大字体
        /// </summary>
        public short MT_FontSze
        {
            set { _mt_fontsze = value; }
            get { return _mt_fontsze; }
        }
        /// <summary>
        /// 虚拟设备添加日期
        /// </summary>
        public DateTime MT_AddTime
        {
            set { _mt_addtime = value; }
            get { return _mt_addtime; }
        }
        /// <summary>
        /// 虚拟设备说明信息
        /// </summary>
        public string MT_Info
        {
            set { _mt_info = value; }
            get { return _mt_info; }
        }
        /// <summary>
        /// 虚拟设备排序码
        /// </summary>
        public int MT_OrderNum
        {
            set { _mt_ordernum = value; }
            get { return _mt_ordernum; }
        }
        #endregion Model
    }

    /// <summary>
    /// 实体类Mobile
    /// </summary>
    [Serializable]
    [BindTable("gwease", "gw_Device_Mobile")]
    public class Mobile : TableEntry
    {
        #region Model
        /// <summary>
        /// 自动编号
        /// </summary>
        [PrimaryKey]
        [Identity]
        public int M_ID { set; get; }
        /// <summary>
        /// 手机型号
        /// </summary>
        public string M_Name { set; get; }
        /// <summary>
        /// 所属厂商编号
        /// </summary>
        public int M_BID { set; get; }
        /// <summary>
        /// 支持WAP，0为否，1为是
        /// </summary>
        public bool M_WAPTF { set; get; }
        /// <summary>
        /// 支持JAVA，0为否，1为是
        /// </summary>
        public bool M_JavaTF { set; get; }
        /// <summary>
        /// 支持BREW，0为否，1为是
        /// </summary>
        public bool M_BrewTF { set; get; }
        /// <summary>
        /// Mobile支持，0为否，1为是
        /// </summary>
        public bool M_MobileTF { set; get; }
        /// <summary>
        /// Windows CE 支持，0为否，1为是
        /// </summary>
        public bool M_WinceTF { set; get; }
        /// <summary>
        /// 手机UA编码
        /// </summary>
        public string M_UACode { set; get; }
        /// <summary>
        /// 排序码
        /// </summary>
        public int M_OrderNum { set; get; }
        /// <summary>
        /// 其他说明
        /// </summary>
        public string M_Info { set; get; }
        /// <summary>
        /// 客户端软件列表显示个数
        /// </summary>
        public int AppPageSize { get; set; }
        /// <summary>
        /// 屏幕宽度
        /// </summary>
        public int M_ScreenW { set; get; }
        /// <summary>
        /// 屏幕高度
        /// </summary>
        public int M_ScreenH { set; get; }
        /// <summary>
        /// 字体大小
        /// </summary>
        public int M_FontSize { set; get; }
        #endregion Model

    }

}
