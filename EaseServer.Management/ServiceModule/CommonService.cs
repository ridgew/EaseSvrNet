using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Web.Services;
using Gwsoft.SharpOrm;
using Gwsoft.SharpOrm.Config;
using Gwsoft.SharpOrm.SqlDb;
using Gwsoft.SharpOrm.Util;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    /// 提供相关实体数据添加、更新、列表操作
    /// </summary>
    [WebService(Name = "CommonService", Description = "提供相关实体数据添加、更新、列表操作")]
    public class CommonService : WebServiceBase
    {
        /// <summary>
        /// 通过协议获取类型名称
        /// </summary>
        public static string GetTypeStringFromConfig(string protocol)
        {
            string typeNameString = "Gwsoft.Ease.NotFoundTypeName";
            EntryMappingConfig configInstance = EntryMappingConfig.ConfigInstance;
            foreach (EntryMapping item in configInstance.MappingCollection)
            {
                if (item.Key == protocol)
                {
                    typeNameString = item.TypeFullName;
                    break;
                }
            }
            return typeNameString;
        }

        private bool HasImplementExtension(Type entryType)
        {
            Type CheckType = typeof(IEntryExtension<>);
            return (entryType.GetInterface(CheckType.Namespace + "." + CheckType.Name, true) != null);
        }

        /// <summary>
        /// 通过协议获取实例类型
        /// </summary>
        /// <param name="protocol">配置协议编号</param>
        /// <returns></returns>
        public static Type GetTypeFromConfig(string protocol)
        {
            Type instanceType = Type.GetType(GetTypeStringFromConfig(protocol), false, true);
            if (instanceType == null)
            {
                throw new NotSupportedException("没有找到相关码表配置数据，请检查/TypeMapping.config文件的配置！");
            }
            else
            {
                return instanceType;
            }
        }

        /// <summary>
        /// 根据条件限制获取满足条件的数据列表
        /// </summary>
        /// <param name="protocol">协议编号</param>
        /// <param name="selectFields">英文逗号分隔的字段列表</param>
        /// <param name="orderbyFields">排序字段列表</param>
        /// <param name="isDesc">是否是倒序</param>
        /// <param name="forceBindFields">强制绑定属性列表</param>
        /// <param name="demo">实例绑定数据</param>
        /// <returns></returns>
        [Protocol("0\\.8$", RegexPattern = true), WebMethod(Description = "获取满足相关条件的实例列表(*.8)")]
        public SvcEntryArrayRecord GetListByExample(string protocol, string selectFields, string orderbyFields, bool isDesc, string forceBindFields, DictionaryEntry[] demo)
        {
            SvcEntryArrayRecord svc = new SvcEntryArrayRecord();
            svc.Protocol = protocol;
            Type instanceType = GetTypeFromConfig(protocol.Substring(0, protocol.LastIndexOf(".8")));
            List<SqlOrderBy> ListOrder = new List<SqlOrderBy>();
            if (!string.IsNullOrEmpty(orderbyFields))
            {
                foreach (string order in orderbyFields.Split(','))
                {
                    if (order.Trim() != string.Empty)
                    {
                        ListOrder.Add(new SqlOrderBy(order, isDesc ? SqlOrderByDirection.DESC : SqlOrderByDirection.ASC));
                    }
                }
            }

            TableEntry Entry = Activator.CreateInstance(instanceType) as TableEntry;
            if (Entry == null)
            {
                svc.Status = 0;
                svc.Message = string.Format("实例化类型{0}失败!", instanceType.FullName);
            }
            else
            {
                try
                {
                    Entry = Entry.DataBind(demo, null);

                    #region 从客户端加载约束
                    Array.ForEach<DictionaryEntry>(demo, entry =>
                    {
                        if (entry.Key.ToString() == "{Constrains}")
                        {
                            Entry = Entry.WithConstrains(SqlNativeConstrains.LoadFromJsInvoke(entry.Value.ToString()));
                        }
                    });
                    #endregion

                    svc.Data = GetEntryList(instanceType, selectFields, forceBindFields, ListOrder.ToArray(), Entry);
                    svc.Status = 1;
                    svc.Message = "ok";

                }
                catch (Exception exp)
                {
                    svc.Status = 0;
                    svc.Message = "error" + exp.Message
                        + Environment.NewLine + exp.StackTrace;
                }
            }
            return svc;
        }


        private DictionaryEntry[][] GetEntryList(Type instanceType, string selectFields, string forceBindFields,
            SqlOrderBy[] orderSetting, TableEntry Example)
        {
            MethodInfo gm = typeof(OrmHelper).GetMethod("GetEntryArrayList",
                BindingFlags.Static | BindingFlags.Public);
            gm = gm.MakeGenericMethod(instanceType);

            return gm.Invoke(null, new object[] { Example,
                                string.IsNullOrEmpty(selectFields) ? null : selectFields.Split(','),
                                string.IsNullOrEmpty(forceBindFields) ? new string[0] : forceBindFields.Split(','),
                                orderSetting
                            }) as DictionaryEntry[][];
        }

        /// <summary>
        /// 码表数据列表
        /// </summary>
        /// <param name="protocol">协议编号</param>
        /// <param name="selectFields">英文逗号分隔的字段列表</param>
        /// <param name="orderbyFields">排序字段列表</param>
        /// <param name="isDesc">是否是倒序</param>
        /// <returns></returns>
        [WebMethod(Description = "获取码表数据列表，仅需指定协议号正确，后三参数可选。(码表配置参见：/TypeMapping.config)")]
        public SvcEntryArrayRecord GetList(string protocol, string selectFields, string orderbyFields, bool isDesc)
        {
            SvcEntryArrayRecord svc = new SvcEntryArrayRecord();
            svc.Protocol = protocol;
            Type instanceType = GetTypeFromConfig(protocol);
            List<SqlOrderBy> ListOrder = new List<SqlOrderBy>();
            if (!string.IsNullOrEmpty(orderbyFields))
            {
                foreach (string order in orderbyFields.Split(','))
                {
                    if (order.Trim() != string.Empty)
                    {
                        ListOrder.Add(new SqlOrderBy(order, isDesc ? SqlOrderByDirection.DESC : SqlOrderByDirection.ASC));
                    }
                }
            }

            TableEntry Entry = Activator.CreateInstance(instanceType) as TableEntry;
            if (Entry == null)
            {
                svc.Status = 0;
                svc.Message = string.Format("实例化类型{0}失败!", instanceType.FullName);
            }
            else
            {
                try
                {
                    svc.Data = GetEntryList(instanceType, selectFields, null, ListOrder.ToArray(), Entry);
                    svc.Status = 1;
                    svc.Message = "ok";
                }
                catch (Exception exp)
                {
                    svc.Status = 0;
                    svc.Message = "error" + exp.Message
                        + Environment.NewLine + exp.StackTrace;
                }
            }
            return svc;
        }

        private delegate DataTable RefTotalCountCallBack(TableEntry entry, string[] selectFields,
            int currentPage, int PageSize, DbConnection SharedOpenConnection, SqlOrderBy[] OrderSettings,
            ref long totalRecord);


        private DataTable GetDataTableHelper(TableEntry SqlTableEntry, string[] selectFields,
            int currentPage, int PageSize, DbConnection SharedOpenConnection, SqlOrderBy[] OrderSettings,
            ref long totalRecord)
        {
            return OrmHelper.GetDataTable(SqlTableEntry, selectFields, currentPage, PageSize, SharedOpenConnection, OrderSettings,
                ref totalRecord);
        }

        public static int GetPageCount(int total, int pagesize)
        {
            if (total < pagesize) return 1;
            return (total % pagesize == 0) ? total / pagesize : (total / pagesize + 1);
        }

        /// <summary>
        /// 获取码表数据分页列表
        /// </summary>
        /// <param name="protocol">协议编号</param>
        /// <param name="selectFields">英文逗号分隔的字段列表</param>
        /// <param name="orderbyFields">排序字段列表</param>
        /// <param name="isDesc">是否是倒序</param>
        /// <param name="pageSize">每页显示条数</param>
        /// <param name="pageIndex">当前页</param>
        /// <returns></returns>
        [Protocol("0\\.6$", RegexPattern = true), WebMethod(Description = "获取码表数据分页列表(*.6)")]
        public SvcPagingRecord<DataTable> GetPageList(string protocol, string selectFields, string orderbyFields, bool isDesc, int pageSize, int pageIndex)
        {
            SvcPagingRecord<DataTable> sp = new SvcPagingRecord<DataTable>();
            sp.Protocol = protocol;
            sp.PageSize = pageSize;
            sp.PageIndex = pageIndex;

            string entryFullName = GetTypeStringFromConfig(protocol.Substring(0, protocol.LastIndexOf(".6")));
            Type instanceType = Type.GetType(entryFullName, false, true);
            if (instanceType == null)
            {
                sp.Status = 0;
                sp.Message = "没有找到相关码表数据 ";
            }
            else
            {
                object Entry = Activator.CreateInstance(instanceType);
                if (Entry == null)
                {
                    sp.Status = 0;
                    sp.Message = string.Format("实例化类型{0}失败!", instanceType.FullName);
                }
                else
                {
                    List<SqlOrderBy> ListOrder = new List<SqlOrderBy>();
                    if (!string.IsNullOrEmpty(orderbyFields))
                    {
                        foreach (string order in orderbyFields.Split(','))
                        {
                            if (order.Trim() != string.Empty)
                            {
                                ListOrder.Add(new SqlOrderBy(order, isDesc ? SqlOrderByDirection.DESC : SqlOrderByDirection.ASC));
                            }
                        }
                    }

                    try
                    {
                        long totalRecord = 0;
                        MethodInfo gm = this.GetType().GetMethod("GetDataTableHelper",
                            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);

                        Type delegateType = typeof(RefTotalCountCallBack);
                        var callback = (RefTotalCountCallBack)Delegate.CreateDelegate(delegateType, this, gm);
                        DataTable dTab = callback(Entry as TableEntry, string.IsNullOrEmpty(selectFields) ? null : selectFields.Split(','),
                                        sp.PageIndex,
                                        sp.PageSize,
                                        null,
                                        ListOrder.ToArray(),
                                        ref totalRecord);

                        sp.RecordCount = Convert.ToInt32(totalRecord);
                        sp.PageCount = GetPageCount(sp.RecordCount, sp.PageSize);
                        sp.Status = 1;
                        sp.Message = "ok";
                        sp.Data = new DataTable[] { dTab };
                        dTab.Dispose();
                    }
                    catch (Exception exp)
                    {
                        sp.Status = 0;
                        exp = exp.GetTriggerException();
                        sp.Message = "error" + exp.Message
                            + Environment.NewLine + exp.StackTrace;
                    }
                }
            }
            return sp;
        }

        private static void BindHelper<T>(T instance, DictionaryEntry[] entryData)
            where T : new()
        {
            OrmHelper.DataBind<T>(instance, entryData, null);
        }


        /// <summary>
        /// 保存相关实例数据
        /// </summary>
        /// <param name="protocol">协议编号</param>
        /// <param name="entryData">实例数据</param>
        /// <returns>新增或更新后的实例数据</returns>
        [Protocol("0\\.[12]$", RegexPattern = true), WebMethod(Description = "对基于TabEntry类的实体自动进行增加、更新操作(*.[12])，并获取实例数据")]
        public SvcMultiRecord<DictionaryEntry> StoreEntry(string protocol, DictionaryEntry[] entryData)
        {
            SvcMultiRecord<DictionaryEntry> result = new SvcMultiRecord<DictionaryEntry>();
            result.Protocol = protocol;
            //创建1:C、获取4:R、更新2:U、删除3:D
            string entryFullName = GetTypeStringFromConfig(protocol.EndsWith(".1") ? protocol.Substring(0, protocol.LastIndexOf(".1")) : protocol.Substring(0, protocol.LastIndexOf(".2")));
            Type instanceType = Type.GetType(entryFullName, false, true);
            if (instanceType == null)
            {
                result.Status = 0;
                result.Message = "没有找到相关码表数据 ";
            }
            else
            {
                TableEntry Entry = Activator.CreateInstance(instanceType) as TableEntry;
                if (Entry == null)
                {
                    result.Status = 0;
                    result.Message = string.Format("实例化类型{0}失败!", instanceType.FullName);
                }
                else
                {
                    try
                    {
                        if (HasImplementExtension(instanceType))
                        {
                            #region 调用接口的扩展方法
                            MethodInfo extM = instanceType.GetMethod("SetWithExtension", new Type[] { typeof(DictionaryEntry[]) });
                            if (extM != null)
                            {
                                Entry = extM.Invoke(Entry, new object[] { entryData }) as TableEntry;
                            }
                            #endregion
                        }
                        else
                        {
                            #region 通用方法
                            MethodInfo mi = this.GetType().GetMethod("BindHelper", BindingFlags.Static | BindingFlags.NonPublic);
                            mi = mi.MakeGenericMethod(instanceType);
                            mi.Invoke(Entry, new object[] { Entry, entryData });
                            object identity = Entry.GetByAttribute(typeof(PrimaryKeyAttribute));
                            if (identity != null && identity.ToString() == "0")
                            {
                                Entry.SetByAttribute(typeof(PrimaryKeyAttribute), Entry.Insert(true));
                            }
                            else
                            {
                                Entry.Update();
                            }
                            #endregion
                        }
                        result.Status = 1;
                        result.Data = Entry.ToDictionaryEntryArray(instanceType);
                        result.Message = "ok";
                    }
                    catch (Exception exp)
                    {
                        exp = exp.GetTriggerException();
                        result.Status = 0;
                        result.Message = exp.Message + Environment.NewLine + exp.StackTrace;
                    }

                }
            }
            return result;
        }

        /// <summary>
        /// 设置相关实例数据
        /// </summary>
        /// <param name="protocol">协议编号</param>
        /// <param name="entryData">实例数据</param>
        /// <returns></returns>
        [WebMethod(Description = "对基于TabEntry类的实体自动进行增加、更新操作(*.[12])")]
        public Result SetEntry(string protocol, DictionaryEntry[] entryData)
        {
            Result result = new Result();
            result.Protocol = protocol;
            //创建1:C、获取4:R、更新2:U、删除3:D
            string entryFullName = GetTypeStringFromConfig(protocol.EndsWith(".1") ? protocol.Substring(0, protocol.LastIndexOf(".1")) : protocol.Substring(0, protocol.LastIndexOf(".2")));
            Type instanceType = Type.GetType(entryFullName, false, true);
            if (instanceType == null)
            {
                result.Status = 0;
                result.Message = "没有找到相关码表数据 ";
            }
            else
            {
                TableEntry Entry = Activator.CreateInstance(instanceType) as TableEntry;
                if (Entry == null)
                {
                    result.Status = 0;
                    result.Message = string.Format("实例化类型{0}失败!", instanceType.FullName);
                }
                else
                {
                    try
                    {
                        if (HasImplementExtension(instanceType))
                        {
                            #region 调用接口的扩展方法
                            MethodInfo extM = instanceType.GetMethod("SetWithExtension", new Type[] { typeof(DictionaryEntry[]) });
                            if (extM != null)
                            {
                                extM.Invoke(Entry, new object[] { entryData });
                            }
                            #endregion
                        }
                        else
                        {
                            #region 通用方法
                            MethodInfo mi = this.GetType().GetMethod("BindHelper", BindingFlags.Static | BindingFlags.NonPublic);
                            mi = mi.MakeGenericMethod(instanceType);
                            mi.Invoke(Entry, new object[] { Entry, entryData });
                            object identity = Entry.GetByAttribute(typeof(PrimaryKeyAttribute));
                            if (identity != null && identity.ToString() == "0")
                            {
                                Entry.Insert();
                            }
                            else
                            {
                                Entry.Update();
                            }
                            #endregion
                        }
                        result.Status = 1;
                        result.Message = "ok";
                    }
                    catch (Exception exp)
                    {
                        result.Status = 0;
                        if (exp.InnerException != null) exp = exp.InnerException;
                        result.Message = exp.Message + Environment.NewLine + exp.StackTrace;
                    }

                }
            }
            return result;
        }

        /// <summary>
        /// 获取相关实例数据
        /// </summary>
        /// <param name="protocol">协议编号</param>
        /// <param name="identity">实例标志值</param>
        /// <returns></returns>
        [Protocol("0\\.4$", RegexPattern = true), WebMethod(Description = "获取相关实例数据(*.4)")]
        public SvcSingleRecord<TableEntry> GetEntry(string protocol, string identity)
        {
            SvcSingleRecord<TableEntry> result = new SvcSingleRecord<TableEntry>();
            result.Protocol = protocol;
            //创建1:C、获取4:R、更新2:U、删除3:D
            string entryFullName = GetTypeStringFromConfig(protocol.Substring(0, protocol.LastIndexOf(".4")));
            Type instanceType = Type.GetType(entryFullName, false, true);
            if (instanceType == null)
            {
                result.Status = 0;
                result.Message = "没有找到相关码表数据 ";
            }
            else
            {
                try
                {
                    TableEntry Entry = Activator.CreateInstance(instanceType) as TableEntry;
                    if (Entry == null)
                    {
                        result.Status = 0;
                        result.Message = string.Format("实例化类型{0}失败!", instanceType.FullName);
                    }
                    else
                    {
                        try
                        {
                            Entry.BindByAttribute(typeof(PrimaryKeyAttribute), identity);
                            OrmHelper.DataBind(Entry);
                            result.Data = Entry;
                            result.Status = 1;
                            result.Message = "ok";
                        }
                        catch (NotExistException)
                        {
                            result.Status = 0;
                            result.Message = "数据库中不存在对应数据！";
                        }
                    }
                }
                catch (Exception exp)
                {
                    result.Status = 0;
                    result.Message = exp.Message;
                }
            }
            return result;
        }

        /// <summary>
        /// 获取相关实例的扩展数据
        /// </summary>
        /// <param name="protocol">协议编号</param>
        /// <param name="identity">实例标志值</param>
        /// <returns></returns>
        [Protocol("0\\.5$", RegexPattern = true), WebMethod(Description = "获取相关实例的扩展数据(*.5)")]
        public SvcMultiRecord<DictionaryEntry> GetEntryExtDictArray(string protocol, string identity)
        {
            SvcMultiRecord<DictionaryEntry> result = new SvcMultiRecord<DictionaryEntry>();
            result.Protocol = protocol;
            //创建1:C、获取4:R、更新2:U、删除3:D 获取扩展5:E
            string entryFullName = GetTypeStringFromConfig(protocol.Substring(0, protocol.LastIndexOf(".5")));
            Type instanceType = Type.GetType(entryFullName, false, true);
            if (instanceType == null)
            {
                result.Status = 0;
                result.Message = "没有找到相关码表数据 ";
            }
            else
            {
                try
                {
                    TableEntry Entry = Activator.CreateInstance(instanceType) as TableEntry;
                    if (Entry == null)
                    {
                        result.Status = 0;
                        result.Message = string.Format("实例化类型{0}失败!", instanceType.FullName);
                    }
                    else
                    {
                        try
                        {
                            Entry.BindByAttribute(typeof(PrimaryKeyAttribute), identity);
                            OrmHelper.DataBind(Entry);

                            if (!HasImplementExtension(instanceType))
                            {
                                result.Status = 0;
                                result.Message = "该对象没有实现获取扩展数据的接口";
                            }
                            else
                            {
                                MethodInfo extM = instanceType.GetMethod("GetExtenEntryArray");
                                try
                                {
                                    if (extM != null)
                                    {
                                        result.Data = extM.Invoke(Entry, null) as DictionaryEntry[];
                                    }
                                    result.Status = 1;
                                    result.Message = "ok";
                                }
                                catch (Exception invokeExp)
                                {
                                    result.Status = 0;
                                    invokeExp = invokeExp.GetTriggerException();
                                    result.Message = invokeExp.Message + Environment.NewLine + invokeExp.StackTrace;
                                }
                            }
                        }
                        catch (NotExistException)
                        {
                            result.Status = 0;
                            result.Message = "数据库中不存在对应数据！";
                        }
                    }
                }
                catch (Exception exp)
                {
                    result.Status = 0;
                    result.Message = exp.Message;
                }
            }
            return result;
        }


        /// <summary>
        /// 通过示例删除相关实例数据
        /// </summary>
        /// <param name="protocol">协议编号</param>
        /// <param name="demo">实例绑定标志</param>
        /// <returns></returns>
        [Protocol("0\\.7$", RegexPattern = true), WebMethod(Description = "通过示例删除相关实例数据(*.7)")]
        public Result RemoveEntryByExample(string protocol, DictionaryEntry[] demo)
        {
            Result result = new Result();
            result.Protocol = protocol;
            Type instanceType = GetTypeFromConfig(protocol.Substring(0, protocol.LastIndexOf(".7")));
            try
            {
                TableEntry Entry = Activator.CreateInstance(instanceType) as TableEntry;
                if (Entry == null)
                {
                    result.Status = 0;
                    result.Message = string.Format("实例化类型{0}失败!", instanceType.FullName);
                }
                else
                {
                    Entry = Entry.DataBind(demo, null);

                    #region 从客户端加载约束
                    Array.ForEach<DictionaryEntry>(demo, entry =>
                    {
                        if (entry.Key.ToString() == "{Constrains}")
                        {
                            Entry = Entry.WithConstrains(SqlNativeConstrains.LoadFromJsInvoke(entry.Value.ToString()));
                        }
                    });
                    #endregion

                    //始终以集合形式获取
                    TableEntry[] EntryList = (TableEntry[])typeof(OrmHelper).GetMethod("GetDataList", BindingFlags.Static | BindingFlags.Public, null,
                        new Type[] { typeof(TableEntry) }, null)
                        .MakeGenericMethod(instanceType)
                        .Invoke(null, new object[] { Entry });
                    MethodInfo extM = null;
                    if (HasImplementExtension(instanceType)) extM = instanceType.GetMethod("RemoveExtension");
                    if (extM != null)
                    {
                        foreach (var subEntry in EntryList)
                        {
                            #region 删除扩展关联数据
                            try
                            {
                                extM.Invoke(subEntry, null);
                            }
                            catch (Exception) { }
                            #endregion
                        }
                    }
                    Entry.Delete();

                    result.Status = 1;
                    result.Message = "ok";
                }
            }
            catch (Exception exp)
            {
                result.Status = 0;
                exp = exp.GetTriggerException();
                result.Message = exp.Message + Environment.NewLine + exp.StackTrace;
            }
            return result;
        }


        /// <summary>
        /// 通过条件判定存在性，不存在则增加，存在则更新。
        /// </summary>
        /// <param name="protocol">协议编号</param>
        /// <param name="exists">实例存在性判定条件</param>
        /// <param name="entryData">新实例数据</param>
        /// <returns></returns>
        [Protocol("0\\.10$", RegexPattern = true), WebMethod(Description = "对基于TabEntry类的实体进行判定后自动进行增加、更新操作(*.10)")]
        public Result SetEntryByExample(string protocol, DictionaryEntry[] exists, DictionaryEntry[] entryData)
        {
            Result result = new Result();
            result.Protocol = protocol;
            Type instanceType = GetTypeFromConfig(protocol.Substring(0, protocol.LastIndexOf(".10")));
            try
            {
                TableEntry Entry = Activator.CreateInstance(instanceType) as TableEntry;
                if (Entry == null)
                {
                    result.Status = 0;
                    result.Message = string.Format("实例化类型{0}失败!", instanceType.FullName);
                }
                else
                {
                    Entry = Entry.DataBind(exists, null);
                    TableEntry nInstance = Activator.CreateInstance(instanceType) as TableEntry;
                    using (DbConnection conn = nInstance.GetDbConnection())
                    {
                        nInstance = nInstance.DataBind(entryData, null);
                        if (Entry.GetTotalRecord(conn) == 0)
                        {
                            //insert
                            nInstance.Insert(conn);
                        }
                        else
                        {
                            //update
                            nInstance.Update(conn);
                        }
                    }
                    result.Status = 1;
                    result.Message = "ok";
                }
            }
            catch (Exception exp)
            {
                result.Status = 0;
                exp = exp.GetTriggerException();
                result.Message = exp.Message + Environment.NewLine + exp.StackTrace;
            }
            return result;
        }

        /// <summary>
        /// 删除相关实例数据
        /// </summary>
        /// <param name="protocol">协议编号</param>
        /// <param name="identity">实例标志</param>
        /// <returns></returns>
        [Protocol("0\\.3$", RegexPattern = true), WebMethod(Description = "删除相关实例数据(*.3)")]
        public Result RemoveEntry(string protocol, string identity)
        {
            Result result = new Result();
            result.Protocol = protocol;
            //创建1:C、获取4:R、更新2:U、删除3:D
            string entryFullName = GetTypeStringFromConfig(protocol.Substring(0, protocol.LastIndexOf(".3")));
            Type instanceType = Type.GetType(entryFullName, false, true);
            if (instanceType == null)
            {
                result.Status = 0;
                result.Message = "没有找到相关码表数据 ";
            }
            else
            {
                try
                {
                    TableEntry Entry = Activator.CreateInstance(instanceType) as TableEntry;
                    if (Entry == null)
                    {
                        result.Status = 0;
                        result.Message = string.Format("实例化类型{0}失败!", instanceType.FullName);
                    }
                    else
                    {
                        Entry.BindByAttribute(typeof(PrimaryKeyAttribute), identity);

                        #region 删除扩展关联数据
                        try
                        {
                            if (HasImplementExtension(instanceType))
                            {
                                MethodInfo extM = instanceType.GetMethod("RemoveExtension");
                                if (extM != null) extM.Invoke(Entry, null);
                            }
                        }
                        catch (Exception) { }
                        #endregion

                        Entry.Delete();

                        result.Status = 1;
                        result.Message = "ok";
                    }
                }
                catch (Exception exp)
                {
                    result.Status = 0;
                    if (exp.InnerException != null) exp = exp.InnerException;
                    result.Message = exp.Message;
                }
            }
            return result;
        }


        /// <summary>
        /// 判定相关条件的实例是否存在
        /// </summary>
        /// <param name="protocol">协议编号</param>
        /// <param name="entryData">实例绑定数据</param>
        /// <returns></returns>
        [Protocol("0\\.9$", RegexPattern = true), WebMethod(Description = "判定相关条件的实例是否存在(*.9)")]
        public SvcSingleRecord<bool> ExistsEntry(string protocol, DictionaryEntry[] entryData)
        {
            SvcSingleRecord<bool> result = new SvcSingleRecord<bool>();
            result.Protocol = protocol;
            //创建1:C、获取4:R、更新2:U、删除3:D
            string entryFullName = GetTypeStringFromConfig(protocol.Substring(0, protocol.LastIndexOf(".9")));
            Type instanceType = Type.GetType(entryFullName, false, true);
            if (instanceType == null)
            {
                result.Status = 0;
                result.Message = "没有找到相关码表数据 ";
            }
            else
            {
                try
                {
                    TableEntry Entry = Activator.CreateInstance(instanceType) as TableEntry;
                    if (Entry == null)
                    {
                        result.Status = 0;
                        result.Message = string.Format("实例化类型{0}失败!", instanceType.FullName);
                    }
                    else
                    {
                        try
                        {
                            Entry = Entry.DataBind(entryData, null);
                            OrmHelper.DataBind(Entry);
                            result.Data = true;
                            result.Status = 1;
                            result.Message = "ok";
                        }
                        catch (NotExistException)
                        {
                            result.Status = 0;
                            result.Data = false;
                            result.Message = "数据库中不存在对应数据！";
                        }
                    }
                }
                catch (Exception exp)
                {
                    result.Status = 0;
                    exp = exp.GetTriggerException();
                    result.Message = exp.Message + Environment.NewLine + exp.StackTrace;
                }
            }
            return result;
        }
    }
}
