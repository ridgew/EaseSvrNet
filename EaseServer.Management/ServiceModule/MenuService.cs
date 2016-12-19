using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using System.Xml;
using System.Xml.Serialization;
using CommonLib;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    ///系统菜单管理服务
    /// </summary>
    [WebService(Name = "系统菜单管理服务", Description = "提供用于管理系统菜单的相应方法")]
    public class MenuService : WebServiceBase
    {
        /// <summary>
        /// 获取当前用户下要显示的所有系统菜单列表
        /// </summary>
        /// <returns>当前用户下要显示的系统菜单列表</returns>
        [Protocol("1.3.5.1.1"), WebMethod(Description = "获取当前用户下要显示的所有系统菜单列表")]
        [GenerateScriptType(typeof(SvcCurrentMainMenuList), ScriptTypeId = "0")]
        public SvcCurrentMainMenuList GetCurrentMainMenuList()
        {
            SvcCurrentMainMenuList list = new SvcCurrentMainMenuList();
            WebMenuItem root = new WebMenuItem { Children = WebMenuItem.RootInstance };
            WebMenuItem[] items = ((WebMenuItem)root.Clone()).Children;
            GetCurrentAllDisplayMenuList(items);
            list.Data = items.Where<WebMenuItem>(t => t.Enabled).ToArray<WebMenuItem>();
            return list;
        }

        /// <summary>
        /// 递归获取当前用户下所有系统菜单数据
        /// </summary>
        /// <param name="items">当前用户下要显示的系统菜单列表</param>
        private static void GetCurrentAllDisplayMenuList(WebMenuItem[] items)
        {
            foreach (var item in items)
            {
                if (item.Children != null && item.Children.Length >= 0)
                {
                    item.Children = filterDisplayMenuList(item.Children);
                    GetCurrentAllDisplayMenuList(item.Children);
                }
            }
        }

        /// <summary>
        /// 获取当前用户下系统菜单列表
        /// </summary>
        /// <param name="items">系统原始菜单列表</param>
        /// <returns>当前用户下要显示的系统菜单列表</returns>
        private static WebMenuItem[] filterDisplayMenuList(MenuItem[] items)
        {
            return (from p in items
                    where p.Enabled == true
                    select new WebMenuItem
                    {
                        MenuId = p.MenuId,
                        KeyCode = p.KeyCode,
                        Enabled = p.Enabled,
                        MenuName = p.MenuName,
                        LeftUrl = p.LeftUrl,
                        RightUrl = p.RightUrl
                    }).ToArray();
        }

        /// <summary>
        /// 获取系统单级菜单列表
        /// </summary>
        /// <param name="parentId">父级菜单ID</param>
        /// <returns>父级菜单ID下的子菜单列表</returns>
        [Protocol("1.3.5.1.3"), WebMethod(Description = "获取系统单级菜单列表")]
        [GenerateScriptType(typeof(SvcMenuList), ScriptTypeId = "0")]
        public SvcMenuList GetMenuList(string parentId)
        {
            SvcMenuList list = new SvcMenuList();

            List<SvcMenuList.SingleMenuItem> itemList = new List<SvcMenuList.SingleMenuItem>();
            foreach (WebMenuItem item in WebMenuItem.RootInstance)
            {
                if (parentId == "0")
                {
                    itemList.Add(new SvcMenuList.SingleMenuItem
                    {
                        MenuId = item.MenuId,
                        MenuName = item.MenuName,
                        Enabled = item.Enabled,
                        OrderNum = item.OrderNum,
                        HasChildren = (item.Children != null && item.Children.Length > 0)
                    });
                }
                else
                {
                    FoudItemOrSubItemThenDoAction(parentId, item,
                        t =>
                        {
                            if (t.Children != null && t.Children.Length > 0)
                            {
                                foreach (WebMenuItem subItem in t.Children)
                                {
                                    itemList.Add(new SvcMenuList.SingleMenuItem
                                    {
                                        MenuId = subItem.MenuId,
                                        MenuName = subItem.MenuName,
                                        Enabled = subItem.Enabled,
                                        OrderNum = item.OrderNum,
                                        HasChildren = (subItem.Children != null && subItem.Children.Length > 0)
                                    });
                                }
                            }
                        });
                }
            }
            SvcMenuList.SingleMenuItem[] allItems = itemList.ToArray();
            Array.Sort<SvcMenuList.SingleMenuItem>(allItems);
            list.Data = allItems;
            return list;
        }

        bool FoudItemOrSubItemThenDoAction(string parentId, WebMenuItem item, Action<WebMenuItem> itemAction)
        {
            bool hasFound = false;
            if (item.MenuId == parentId.ToString())
            {
                itemAction(item);
                hasFound = true;
            }
            else
            {
                #region 从子级菜单中查找菜单
                if (item.Children != null && item.Children.Length > 0)
                {
                    foreach (WebMenuItem subItem in item.Children)
                    {
                        if (FoudItemOrSubItemThenDoAction(parentId, subItem, itemAction))
                        {
                            hasFound = true;
                            break;
                        }
                    }
                }
                #endregion
            }
            return hasFound;
        }

        WebMenuItem GetTargetMenuItem(string menuId)
        {
            foreach (WebMenuItem rootItem in WebMenuItem.RootInstance)
            {
                if (rootItem.MenuId == menuId.ToString())
                {
                    return rootItem;
                }
                else
                {
                    WebMenuItem item = FoundParentItem(menuId, rootItem);
                    if (item != null)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        WebMenuItem FoundParentItem(string parentId, WebMenuItem currentItem)
        {
            if (currentItem.MenuId == parentId.ToString())
            {
                return currentItem;
            }
            else
            {
                if (currentItem.Children != null && currentItem.Children.Length > 0)
                {
                    WebMenuItem parentItem = null;
                    foreach (WebMenuItem subItem in currentItem.Children)
                    {
                        parentItem = FoundParentItem(parentId, subItem);
                        if (parentItem != null)
                        {
                            return parentItem;
                        }
                    }
                }
                return null;
            }
        }

        void GetPageingChildMenuItem(string parentId, int pageIndex, int pageSize, Action<WebMenuItem> itemFetchAct, out int recordCount)
        {
            if (pageIndex < 1) pageIndex = 1;
            int startIndex = (pageIndex - 1) * pageSize;
            WebMenuItem[] targetIems = new WebMenuItem[0];
            if (parentId == "0")
            {
                targetIems = WebMenuItem.RootInstance;
            }
            else
            {
                foreach (WebMenuItem item in WebMenuItem.RootInstance)
                {
                    WebMenuItem parentItem = FoundParentItem(parentId, item);
                    if (parentItem != null)
                    {
                        if (parentItem.Children != null)
                            targetIems = parentItem.Children;
                        break;
                    }
                }
            }

            recordCount = targetIems.Length;
            if (targetIems != null && startIndex < recordCount)
            {
                for (int i = startIndex, j = targetIems.Length; i < j; i++)
                {
                    itemFetchAct(targetIems[i]);
                }
            }
        }

        /// <summary>
        /// 获取系统单项菜单详情
        /// </summary>
        /// <param name="menuId">菜单ID</param>
        /// <returns>系统单项菜单详情</returns>
        [Protocol("1.3.5.1.4"), WebMethod(Description = "获取系统单项菜单详情")]
        [GenerateScriptType(typeof(SvcMenuContent), ScriptTypeId = "0")]
        public SvcMenuContent GetMenuContent(string menuId)
        {
            SvcMenuContent menu = new SvcMenuContent();
            MenuItem item = GetTargetMenuItem(menuId);
            if (item == null)
                menu.Data = new SvcMenuContent.MenuItemInfo
                {
                    Exists = false
                };
            else
                menu.Data = new SvcMenuContent.MenuItemInfo
                {
                    Exists = true,
                    ApplicationId = item.ApplicationId,
                    CreateDate = item.CreateDate,
                    CreatorUserId = item.CreatorUserId,
                    Depth = item.Depth,
                    Enabled = item.Enabled,
                    KeyCode = item.KeyCode,
                    LeftUrl = item.LeftUrl,
                    MenuName = item.MenuName,
                    OrderNum = item.OrderNum,
                    RightUrl = item.RightUrl,
                    MenuId = item.MenuId,
                    ParentId = item.ParentId,
                    Roles = new RoleInfo[0] //EaseDataProvider.Instance.GetRolesInMenu(menuId)
                };
            return menu;
        }

        /// <summary>
        /// 获取子菜单分页列表数据
        /// </summary>
        /// <param name="parentId">父菜单ID</param>
        /// <param name="pageIndex">当前页码，1表示第一页。</param>
        /// <param name="pageSize">每页显示内容的条数</param>
        /// <returns>子菜单分页列表数据</returns>
        [Protocol("1.3.5.1.5"), WebMethod(Description = "获取子菜单分页列表数据")]
        [GenerateScriptType(typeof(ChildMenuItem), ScriptTypeId = "0")]
        public SvcPagingRecord<ChildMenuItem> GetChildMenuPage(string parentId, int pageIndex, int pageSize)
        {
            if (pageIndex < 1)
                pageIndex = 1;
            if (pageSize < 1)
                pageSize = 20;

            int recordCount = 0;

            List<ChildMenuItem> itemList = new List<ChildMenuItem>();
            GetPageingChildMenuItem(parentId, pageIndex, pageSize, t =>
            {
                itemList.Add(new ChildMenuItem
                {
                    MenuId = t.MenuId,
                    MenuName = t.MenuName,
                    LeftUrl = t.LeftUrl,
                    RightUrl = t.RightUrl,
                    OrderNum = t.OrderNum,
                    CreateDate = t.CreateDate,
                    Enabled = t.Enabled,
                    ParentId = t.ParentId
                });

            }, out recordCount);


            //DataTable tableRecords = EaseDataProvider.Instance.GetPagingRecords(out recordCount, "gw_Admin_Menus",
            //    new string[] { "MenuId", "MenuName", "LeftUrl", "RightUrl", "OrderNum", "CreateDate", "Enabled", "ParentId" },
            //    "OrderNum", true, pageIndex, pageSize, true, "ParentId=@0 AND ApplicationId=@1", parentId, ApplicationManager.GetApplicationId());

            ChildMenuItem[] allItems = itemList.ToArray();
            Array.Sort<ChildMenuItem>(allItems);
            SvcPagingRecord<ChildMenuItem> pagingRecords = new SvcPagingRecord<ChildMenuItem>
            {
                Protocol = "1.3.5.1.5",
                Status = 1,
                Message = "获取子菜单分页列表数据",
                PageIndex = pageIndex,
                PageSize = pageSize,
                RecordCount = recordCount,
                Data = allItems

                //Data = (from p in tableRecords.AsEnumerable()
                //        select new ChildMenuItem
                //        {
                //            MenuId = p.Field<int>("MenuId"),
                //            MenuName = p.Field<string>("MenuName"),
                //            LeftUrl = p.Field<string>("LeftUrl"),
                //            RightUrl = p.Field<string>("RightUrl"),
                //            OrderNum = p.Field<int>("OrderNum"),
                //            CreateDate = p.Field<DateTime>("CreateDate"),
                //            Enabled = p.Field<bool>("Enabled"),
                //            ParentId = p.Field<int>("ParentId")
                //        }).ToArray()

            };
            return pagingRecords;
        }

        /// <summary>
        /// 修改系统菜单内容
        /// </summary>
        /// <param name="menuId">菜单ID</param>
        /// <param name="menuName">菜单名称</param>
        /// <param name="leftUrl">菜单左侧导航链接</param>
        /// <param name="rightUrl">菜单右侧主页面链接</param>
        /// <param name="keyCode">快递键值</param>
        /// <param name="enabled">是否显示</param>
        /// <param name="enabledCloneToChildren">显示属性是否同步更改到所有子菜单</param>
        /// <param name="roleIdList">菜单所属角色ID</param>
        /// <param name="roleCloneToChildren">菜单所属角色是否同步更改到所有子菜单</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.1.6"), WebMethod(Description = "修改系统菜单内容")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result UpdateMenuContent(string menuId, string menuName, string leftUrl, string rightUrl, int keyCode, bool enabled,
            bool enabledCloneToChildren, int[] roleIdList, bool roleCloneToChildren)
        {
            Result result = new Result { Protocol = "1.3.5.1.6" };

            MenuItem menu = GetTargetMenuItem(menuId);
            if (menu == null)
                result.Status = 10008;
            else
            {
                if (keyCode == 0 || (keyCode >= 65 && keyCode <= 90))
                {
                    if ((!string.IsNullOrEmpty(menuName)) && (menu.ParentId == "0" || ((!string.IsNullOrEmpty(leftUrl)) || (!string.IsNullOrEmpty(rightUrl)))))
                    {
                        #region 更新菜单对象并同步
                        menu.MenuName = menuName;
                        menu.LeftUrl = leftUrl;
                        menu.RightUrl = rightUrl;
                        menu.KeyCode = keyCode;
                        menu.Enabled = enabled;
                        WebMenuItem.SynRootMenu();
                        #endregion

                        //EaseDataProvider.Instance.UpdateTable("gw_Admin_Menus", new string[] { "MenuName", "LeftUrl", "RightUrl", "KeyCode", "Enabled" }, new object[] { menuName, leftUrl, rightUrl, keyCode, enabled }, true, "MenuId=@0 AND ApplicationId=@1", menuId, ApplicationManager.GetApplicationId());
                        //EaseDataProvider.Instance.DeleteTable("gw_Admin_MenusInRoles", true, "MenuId=@0", menuId);
                        //for (int i = 0; i < roleIdList.Length; i++)
                        //{
                        //    EaseDataProvider.Instance.InsertTable("gw_Admin_MenusInRoles", new string[] { "MenuId", "RoleId" }, new object[] { menuId, roleIdList[i] });
                        //}
                        //if (enabledCloneToChildren)
                        //    SetChildMenuVisible(EaseDataProvider.Instance.GetMenuList(menuId), enabled);
                        //if (roleCloneToChildren)
                        //    SetChildMenuRoles(EaseDataProvider.Instance.GetMenuList(menuId), roleIdList);

                        result.Status = 10001;
                    }
                    else
                        result.Status = 10010;
                }
                else
                    result.Status = 10009;
            }
            return result;
        }

        /// <summary>
        /// 递归设置子菜单显示属性
        /// </summary>
        /// <param name="childMenus">子菜单列表</param>
        /// <param name="enabled">是否在系统中显示</param>
        private static void SetChildMenuVisible(MenuItem[] childMenus, bool enabled)
        {
            foreach (var childMenu in childMenus)
            {
                //EaseDataProvider.Instance.UpdateTable("gw_Admin_Menus", new string[] { "Enabled" }, new object[] { enabled }, true, "MenuId=@0", childMenu.MenuId);
                //SetChildMenuVisible(EaseDataProvider.Instance.GetMenuList(childMenu.MenuId), enabled);
            }
        }

        /// <summary>
        /// 递归设置子菜单所属角色
        /// </summary>
        /// <param name="childMenus">子菜单列表</param>
        /// <param name="roleIds">菜单所属角色列表</param>
        private static void SetChildMenuRoles(MenuItem[] childMenus, params int[] roleIds)
        {
            foreach (var childMenu in childMenus)
            {
                //EaseDataProvider.Instance.DeleteTable("gw_Admin_MenusInRoles", true, "MenuId=@0", childMenu.MenuId);
                //for (int i = 0; i < roleIds.Length; i++)
                //{
                //    EaseDataProvider.Instance.InsertTable("gw_Admin_MenusInRoles", new string[] { "MenuId", "RoleId" }, new object[] { childMenu.MenuId, roleIds[i] });
                //}
                //SetChildMenuRoles(EaseDataProvider.Instance.GetMenuList(childMenu.MenuId), roleIds);
            }
        }

        /// <summary>
        /// 添加系统菜单
        /// </summary>
        /// <param name="parentId">所属父级ID</param>
        /// <param name="menuName">菜单名称</param>
        /// <param name="leftUrl">菜单左侧导航链接</param>
        /// <param name="rightUrl">菜单右侧主页面链接</param>
        /// <param name="keyCode">快递键值</param>
        /// <param name="enabled">是否显示</param>
        /// <param name="roleIdList">菜单所属角色ID</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.1.7"), WebMethod(Description = "添加系统菜单")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result CreateMenu(string parentId, string menuName, string leftUrl, string rightUrl, int keyCode, bool enabled, int[] roleIdList)
        {
            Result result = new Result { Protocol = "1.3.5.1.7" };

            MenuItem parentMenu = (parentId == "0") ? null : GetTargetMenuItem(parentId);
            if (parentId == "0" || parentMenu != null)
            {
                if (keyCode == 0 || (keyCode >= 65 && keyCode <= 90))
                {
                    if ((!string.IsNullOrEmpty(menuName)) && (parentId == "0" || ((!string.IsNullOrEmpty(leftUrl)) || (!string.IsNullOrEmpty(rightUrl)))))
                    {

                        WebMenuItem subItem = new WebMenuItem
                        {
                            ParentId = parentId,
                            MenuName = menuName,
                            LeftUrl = leftUrl,
                            RightUrl = rightUrl,
                            KeyCode = keyCode,
                            CreateDate = DateTime.Now,
                            Enabled = enabled
                        };

                        if (parentId == "0")
                        {
                            subItem.MenuId = Guid.NewGuid().ToString("N");
                            WebMenuItem[] newRoot = new WebMenuItem[WebMenuItem.RootInstance.Length + 1];
                            Array.Copy(WebMenuItem.RootInstance, newRoot, newRoot.Length - 1);
                            newRoot[newRoot.Length - 1] = subItem;
                            WebMenuItem.UpdateRootInstance(newRoot);
                            WebMenuItem.SynRootMenu();
                        }
                        else
                        {
                            ((WebMenuItem)parentMenu).AppendChild(subItem);
                            WebMenuItem.SynRootMenu();
                        }

                        //int menuId = Convert.ToInt32(EaseDataProvider.Instance.InsertTable("gw_Admin_Menus",
                        //    new string[] { "ApplicationId", "KeyCode", "MenuName", "LeftUrl", "RightUrl", "ParentId", "Depth", "CreatorUserId", "Enabled" },
                        //    new object[] { ApplicationManager.GetApplicationId(), keyCode, menuName, leftUrl, rightUrl, parentId,
                        //        (parentId == 0 ? 0 : (parentMenu.Depth + 1)),
                        //        MembershipManager.Instance.Current.UserId, enabled },
                        //        true));

                        //for (int i = 0; i < roleIdList.Length; i++)
                        //{
                        //    EaseDataProvider.Instance.InsertTable("gw_Admin_MenusInRoles", new string[] { "MenuId", "RoleId" }, new object[] { menuId, roleIdList[i] });
                        //}

                        result.Status = 10005;
                    }
                    else
                        result.Status = 10004;
                }
                else
                    result.Status = 10003;
            }
            else
                result.Status = 10002;
            return result;
        }

        /// <summary>
        /// 删除系统菜单(包括所有子菜单)
        /// </summary>
        /// <param name="menuId">菜单ID</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.1.8"), WebMethod(Description = "删除系统菜单(包括所有子菜单)")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result DeleteMenu(string menuId)
        {
            Result result = new Result { Protocol = "1.3.5.1.8" };
            WebMenuItem item = GetTargetMenuItem(menuId);
            if (item != null)
            {
                //if (menuId > 0)
                //    EaseDataProvider.Instance.DeleteTable("gw_Admin_Menus", true, "MenuId=@0", menuId);

                //SvcMainMenuList.MainMenuItem[] items = GetMenuList(EaseDataProvider.Instance.GetMenuList(menuId));
                //GetAllMenuList(items);
                //DeleteAllMenuList(items);

                if (item.Depth == 0)
                {
                    WebMenuItem root = new WebMenuItem();
                    root.Children = WebMenuItem.RootInstance;
                    root.RemoveChild(menuId);
                    WebMenuItem.UpdateRootInstance(root.Children);
                }
                else
                {
                    WebMenuItem pItem = GetTargetMenuItem(item.ParentId);
                    pItem.RemoveChild(menuId);
                }
                WebMenuItem.SynRootMenu();

                result.Status = 10007;
            }
            else
                result.Status = 10006;
            return result;
        }

        /// <summary>
        /// 删除系统菜单列表(包括所有子菜单)
        /// </summary>
        /// <param name="menuIds">系统菜单ID列表</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.1.9"), WebMethod(Description = "删除系统菜单列表(包括所有子菜单)")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result DeleteMenus(string[] menuIds)
        {
            foreach (string menuId in menuIds)
            {
                DeleteMenu(menuId);
            }
            return new Result
            {
                Protocol = "1.3.5.1.9",
                Status = 10007
            };
        }

        /// <summary>
        /// 设置系统菜单排序码
        /// </summary>
        /// <param name="menuIds">菜单ID列表</param>
        /// <param name="menuOrderNums">菜单排序码列表</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.1.10"), WebMethod(Description = "设置系统菜单排序码")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result SetOrderNums(string[] menuIds, int[] menuOrderNums)
        {
            bool hasUpdate = false;
            for (int i = 0; i < menuIds.Length; i++)
            {
                //EaseDataProvider.Instance.UpdateTable("gw_Admin_Menus",
                //    new string[] { "OrderNum" }, new object[] { menuOrderNums[i] }, true, "MenuId=@0 AND ApplicationId=@1", menuIds[i],
                //    ApplicationManager.GetApplicationId());
                WebMenuItem item = GetTargetMenuItem(menuIds[i]);
                if (item != null)
                {
                    item.OrderNum = menuOrderNums[i];
                    hasUpdate = true;
                }
            }
            if (hasUpdate) WebMenuItem.SynRootMenu();
            return new Result { Protocol = "1.3.5.1.10", Status = 10011 };
        }

        /// <summary>
        /// 设置系统菜单显示属性
        /// </summary>
        /// <param name="menuIds">菜单ID列表</param>
        /// <param name="menuEnableds">显示属性列表</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.1.11"), WebMethod(Description = "设置系统菜单显示属性")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result SetMenuVisible(string[] menuIds, bool[] menuEnableds)
        {
            //for (int i = 0; i < menuIds.Length; i++)
            //{
            //    EaseDataProvider.Instance.UpdateTable("gw_Admin_Menus", new string[] { "Enabled" }, new object[] { menuEnableds[i] }, true, "MenuId=@0 AND ApplicationId=@1", menuIds[i], ApplicationManager.GetApplicationId());
            //}
            return new Result { Protocol = "1.3.5.1.11", Status = 10012 };
        }

        /// <summary>
        /// 删除快捷菜单
        /// </summary>
        /// <param name="menuId">菜单ID</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.1.14"), WebMethod(Description = "删除快捷菜单")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result DeleteShortcutMenu(string menuId)
        {
            Result result = new Result { Protocol = "1.3.5.1.14" };
            //EaseDataProvider.Instance.DeleteShortcut(menuId);
            result.Status = 1;
            return result;
        }

        /// <summary>
        /// 添加快捷菜单
        /// </summary>
        /// <param name="menuId">菜单ID</param>
        /// <returns>操作结果</returns>
        [Protocol("1.3.5.1.13"), WebMethod(Description = "添加快捷菜单")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result InsertShortcutMenu(string menuId)
        {
            Result result = new Result { Protocol = "1.3.5.1.13" };
            //EaseDataProvider.Instance.InsertShortcut(menuId);
            result.Status = 1;
            return result;
        }

        /// <summary>
        /// 检查快捷菜单是否存在
        /// </summary>
        /// <param name="menuId">菜单ID</param>
        /// <returns></returns>
        [Protocol("1.3.5.1.12"), WebMethod(Description = "检查快捷菜单是否存在")]
        [GenerateScriptType(typeof(IsShortcutMenu), ScriptTypeId = "0")]
        public IsShortcutMenu GetIsShortcut(string menuId)
        {
            //bool IsShort = EaseDataProvider.Instance.IsShortcut(menuId);
            return new IsShortcutMenu
            {
                Protocol = "1.3.5.1.12",
                Status = 1,
                Message = "检查快捷菜单是否存在",
                IsShortcut = false //IsShort
            };
        }


        /// <summary>
        /// 获取快捷菜单详情
        /// </summary>
        /// <returns></returns>
        [Protocol("1.3.5.1.15"), WebMethod(Description = "获取快捷菜单详情")]
        [GenerateScriptType(typeof(ShortcutMenu), ScriptTypeId = "0")]
        public SvcMultiRecord<ShortcutMenu> GetShortcutMenu()
        {
            return new SvcMultiRecord<ShortcutMenu>
            {
                Protocol = "1.3.5.1.15",
                Status = 1,
                Message = "获取快捷菜单详情",
                Data = new ShortcutMenu[0]
            };
        }

    }

    /// <summary>
    /// 当前用户下系统主菜单显示项
    /// </summary>
    [Serializable]
    public class WebMenuItem : MenuItem, ICloneable, IComparable<WebMenuItem>
    {
        /// <summary>
        /// 获取或设置子菜单列表
        /// </summary>
        public WebMenuItem[] Children { get; set; }

        /// <summary>
        /// 同级排序索引
        /// </summary>
        [XmlIgnore, ScriptIgnore]
        public int ItemIndex { get; set; }

        public void AppendChild(WebMenuItem item)
        {
            item.CreateDate = DateTime.Now;
            item.Depth = Depth + 1;

            if (Children == null || Children.Length == 0)
            {
                item.MenuId = Guid.NewGuid().ToString("N");
                Children = new WebMenuItem[] { item };
            }
            else
            {
                item.MenuId = Guid.NewGuid().ToString("N");
                WebMenuItem[] newChildren = new WebMenuItem[Children.Length + 1];
                Array.Copy(Children, newChildren, Children.Length);
                newChildren[newChildren.Length - 1] = item;
                Array.Sort<WebMenuItem>(newChildren);
                Children = newChildren;
            }
        }

        public void RemoveChild(string menuId)
        {
            if (Children != null && Children.Length > 0)
            {
                int idx = Array.FindIndex<WebMenuItem>(Children, t => t.MenuId == menuId.ToString());
                if (idx != -1)
                {
                    List<WebMenuItem> newList = new List<WebMenuItem>();
                    for (int i = 0, j = Children.Length; i < j; i++)
                    {
                        if (i != idx)
                        {
                            newList.Add(Children[i]);
                        }
                    }
                    Children = newList.ToArray();
                }
            }
        }

        public static void UpdateRootInstance(WebMenuItem[] root)
        {
            _rootInstance = root;
        }

        static WebMenuItem[] _rootInstance = null;
        /// <summary>
        /// 菜单的跟目录配置实例
        /// </summary>
        public static WebMenuItem[] RootInstance
        {
            get
            {
                if (_rootInstance == null)
                {
                    System.Xml.XmlDocument xDoc = new System.Xml.XmlDocument();
                    xDoc.Load(HttpContext.Current.Server.MapPath("/App_Data/Menus.config"));
                    _rootInstance = xDoc.GetObject<WebMenuItem[]>();
                }
                return _rootInstance;
            }
        }

        /// <summary>
        /// 同步菜单根目录
        /// </summary>
        public static void SynRootMenu()
        {
            if (_rootInstance == null)
            {
                _rootInstance = new WebMenuItem[0];
            }
            _rootInstance.GetXmlDoc().Save(HttpContext.Current.Server.MapPath("/App_Data/Menus.config"));
        }


        #region ICloneable 成员

        /// <summary>
        /// 创建作为当前实例副本的新对象。
        /// </summary>
        /// <returns>作为此实例副本的新对象。</returns>
        public object Clone()
        {
            XmlDocument itemDoc = this.GetXmlDoc(true);
            WebMenuItem newItem = itemDoc.GetObject<WebMenuItem>();
            return newItem;
        }

        #endregion

        #region IComparable<WebMenuItem> 成员

        /// <summary>
        /// 比较当前对象和同一类型的另一对象。
        /// </summary>
        /// <param name="other">与此对象进行比较的对象。</param>
        /// <returns>
        /// 一个 32 位有符号整数，指示要比较的对象的相对顺序。返回值的含义如下：
        /// 值
        /// 含义
        /// 小于零
        /// 此对象小于 <paramref name="other"/> 参数。
        /// 零
        /// 此对象等于 <paramref name="other"/>。
        /// 大于零
        /// 此对象大于 <paramref name="other"/>。
        /// </returns>
        public int CompareTo(WebMenuItem other)
        {
            return OrderNum - other.OrderNum;
        }

        #endregion
    }

    /// <summary>
    /// 当前用户下系统主菜单列表类
    /// </summary>
    [Serializable]
    public class SvcCurrentMainMenuList : ResultBase
    {

        /// <summary>
        /// 获取或设置系统主菜单所有列表数据
        /// </summary>
        public WebMenuItem[] Data { get; set; }

        /// <summary>
        /// 构造当前用户下系统主菜单列表类
        /// </summary>
        public SvcCurrentMainMenuList()
        {
            this.Protocol = "1.3.5.1.1";
            this.Status = 1;
            this.Message = "当前用户系统主菜单列表";
        }
    }

    /// <summary>
    ///系统菜单类
    /// </summary>
    [Serializable]
    public class MenuItem : IComparable
    {
        /// <summary>
        /// 获取或设置菜单ID
        /// </summary>
        [XmlAttribute]
        public string MenuId { get; set; }

        /// <summary>
        /// 获取或设置应用程序ID
        /// </summary>
        [ScriptIgnore, XmlIgnore]
        public int ApplicationId { get; set; }

        /// <summary>
        /// 获取或设置快捷键键位值
        /// </summary>
        [XmlAttribute]
        public virtual int KeyCode { get; set; }

        /// <summary>
        /// 获取或设置菜单名称
        /// </summary>
        [XmlAttribute]
        public string MenuName { get; set; }

        /// <summary>
        /// 获取或设置左侧导航页链接地址
        /// </summary>
        public virtual string LeftUrl { get; set; }

        /// <summary>
        /// 获取或设置右侧主页面链接地址
        /// </summary>
        public string RightUrl { get; set; }

        /// <summary>
        /// 获取或设置菜单父级ID（0表示当前菜单为顶级）
        /// </summary>
        [ScriptIgnore]
        [XmlAttribute]
        public virtual string ParentId { get; set; }

        /// <summary>
        /// 获取或设置菜单的深度（0表示顶级菜单，1表示一级子菜单，2表示二级子菜单，以此类推。）
        /// </summary>
        [ScriptIgnore]
        [XmlAttribute]
        public virtual int Depth { get; set; }

        /// <summary>
        /// 获取或设置菜单排序码（同级菜单以排序码降序排列显示）
        /// </summary>
        [ScriptIgnore]
        [XmlAttribute]
        public virtual int OrderNum { get; set; }

        /// <summary>
        /// 获取或设置菜单创建时间
        /// </summary>
        [ScriptIgnore]
        [XmlAttribute]
        public virtual DateTime CreateDate { get; set; }

        /// <summary>
        /// 获取或设置菜单创建者用户ID
        /// </summary>
        [ScriptIgnore, XmlIgnore]
        [XmlAttribute]
        public virtual int CreatorUserId { get; set; }

        /// <summary>
        /// 获取或设置菜单是否已启用
        /// </summary>
        [ScriptIgnore]
        [XmlAttribute]
        public virtual bool Enabled { get; set; }

        #region IComparable 成员

        /// <summary>
        /// 将当前实例与同一类型的另一个对象进行比较，并返回一个整数，该整数指示当前实例在排序顺序中的位置是位于另一个对象之前、之后还是与其位置相同。
        /// </summary>
        /// <param name="obj">与此实例进行比较的对象。</param>
        /// <returns>
        /// 一个 32 位有符号整数，指示要比较的对象的相对顺序。返回值的含义如下：
        /// 值
        /// 含义
        /// 小于零
        /// 此实例小于 <paramref name="obj"/>。
        /// 零
        /// 此实例等于 <paramref name="obj"/>。
        /// 大于零
        /// 此实例大于 <paramref name="obj"/>。
        /// </returns>
        /// <exception cref="T:System.ArgumentException">
        /// 	<paramref name="obj"/> 不具有与此实例相同的类型。
        /// </exception>
        public int CompareTo(object obj)
        {
            if (obj is MenuItem)
            {
                return OrderNum - ((MenuItem)obj).OrderNum;
            }
            return 0;
        }

        #endregion
    }

    /// <summary>
    ///快捷菜单类
    /// </summary>
    [Serializable]
    public class ShortcutMenu
    {
        /// <summary>
        /// 获取或设置菜单ID
        /// </summary>
        public int MenuId { get; set; }

        /// <summary>
        /// 获取或设置菜单名称
        /// </summary>
        public string MenuName { get; set; }

        /// <summary>
        /// 获取或设置左侧导航页链接地址
        /// </summary>
        public virtual string LeftUrl { get; set; }

        /// <summary>
        /// 获取或设置右侧主页面链接地址
        /// </summary>
        public string RightUrl { get; set; }

    }

    /// <summary>
    /// 系统单级菜单列表类
    /// </summary>
    [Serializable]
    public class SvcMenuList : ResultBase
    {
        /// <summary>
        /// 系统主菜单显示项
        /// </summary>
        public class SingleMenuItem : MenuItem, IComparable<SingleMenuItem>
        {
            /// <summary>
            /// 获取或设置菜单是否已启用
            /// </summary>
            public override bool Enabled
            {
                get
                {
                    return base.Enabled;
                }
                set
                {
                    base.Enabled = value;
                }
            }

            /// <summary>
            /// 获取或设置快捷键键位值
            /// </summary>
            [ScriptIgnore]
            public override int KeyCode
            {
                get
                {
                    return base.KeyCode;
                }
                set
                {
                    base.KeyCode = value;
                }
            }

            /// <summary>
            /// 获取或设置左侧导航页链接地址
            /// </summary>
            [ScriptIgnore]
            public override string LeftUrl
            {
                get
                {
                    return base.LeftUrl;
                }
                set
                {
                    base.LeftUrl = value;
                }
            }

            /// <summary>
            /// 获取或设置当前菜单项是否包含子菜单
            /// </summary>
            public bool HasChildren { get; set; }

            #region IComparable<SingleMenuItem> 成员

            /// <summary>
            /// 比较当前对象和同一类型的另一对象。
            /// </summary>
            /// <param name="other">与此对象进行比较的对象。</param>
            /// <returns>
            /// 一个 32 位有符号整数，指示要比较的对象的相对顺序。返回值的含义如下：
            /// 值
            /// 含义
            /// 小于零
            /// 此对象小于 <paramref name="other"/> 参数。
            /// 零
            /// 此对象等于 <paramref name="other"/>。
            /// 大于零
            /// 此对象大于 <paramref name="other"/>。
            /// </returns>
            public int CompareTo(SingleMenuItem other)
            {
                return OrderNum - other.OrderNum;
            }

            #endregion
        }

        /// <summary>
        /// 获取或设置系统单级菜单列表数据
        /// </summary>
        public SingleMenuItem[] Data { get; set; }

        /// <summary>
        /// 构造系统单级菜单列表类
        /// </summary>
        public SvcMenuList()
        {
            this.Protocol = "1.3.5.1.3";
            this.Status = 1;
            this.Message = "系统单级菜单列表";
        }
    }

    /// <summary>
    /// 检查快捷菜单类
    /// </summary>
    [Serializable]
    public class IsShortcutMenu : ResultBase
    {
        public bool IsShortcut { get; set; }
    }

    /// <summary>
    /// 系统菜单内容
    /// </summary>
    [Serializable]
    public class SvcMenuContent : ResultBase
    {
        /// <summary>
        /// 系统单项菜单详情
        /// </summary>
        public class MenuItemInfo : MenuItem
        {
            /// <summary>
            /// 获取或设置菜单父级ID（0表示当前菜单为顶级）
            /// </summary>
            public override string ParentId
            {
                get
                {
                    return base.ParentId;
                }
                set
                {
                    base.ParentId = value;
                    string parentId = value;
                    if (parentId == "0")
                        this.MenuPath = this.MenuName;
                    else
                    {
                        while (parentId != "0")
                        {
                            MenuItem item = null;// EaseDataProvider.Instance.GetMenu(parentId);
                            if (item == null)
                                parentId = "0";
                            else
                            {
                                parentId = item.ParentId;
                                if (string.IsNullOrEmpty(this.MenuPath))
                                    this.MenuPath = item.MenuName + " - " + this.MenuName;
                                else
                                    this.MenuPath = item.MenuName + " - " + this.MenuPath;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// 获取或设置菜单是否已启用
            /// </summary>
            public override bool Enabled
            {
                get
                {
                    return base.Enabled;
                }
                set
                {
                    base.Enabled = value;
                }
            }

            /// <summary>
            /// 获取或设置菜单完整路径
            /// </summary>
            public string MenuPath { get; set; }

            /// <summary>
            /// 获取或设置菜单所属角色列表
            /// </summary>
            public RoleInfo[] Roles { get; set; }

            /// <summary>
            /// 获取或设置当前请求菜单是否存在
            /// </summary>
            public bool Exists { get; set; }
        }

        /// <summary>
        /// 获取或设置单项菜单详情
        /// </summary>
        public MenuItemInfo Data { get; set; }

        /// <summary>
        /// 构造菜单详情
        /// </summary>
        public SvcMenuContent()
        {
            this.Protocol = "1.3.5.1.4";
            this.Status = 1;
            this.Message = "系统单项菜单详情";
        }
    }

    /// <summary>
    /// 系统子菜单实体类
    /// </summary>
    [Serializable]
    public class ChildMenuItem : MenuItem
    {
        /// <summary>
        /// 获取或设置快捷键键位值
        /// </summary>
        [ScriptIgnore]
        public override int KeyCode
        {
            get
            {
                return base.KeyCode;
            }
            set
            {
                base.KeyCode = value;
            }
        }

        /// <summary>
        /// 获取或设置菜单排序码（同级菜单以排序码降序排列显示）
        /// </summary>
        public override int OrderNum
        {
            get
            {
                return base.OrderNum;
            }
            set
            {
                base.OrderNum = value;
            }
        }

        /// <summary>
        /// 获取或设置菜单创建时间
        /// </summary>
        public override DateTime CreateDate
        {
            get
            {
                return base.CreateDate;
            }
            set
            {
                base.CreateDate = value;
            }
        }

        /// <summary>
        /// 获取或设置菜单是否已启用
        /// </summary>
        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                base.Enabled = value;
            }
        }

        /// <summary>
        /// 获取父级id
        /// </summary>
        public override string ParentId
        {
            get
            {
                return base.ParentId;
            }
            set
            {
                base.ParentId = value;
            }
        }
    }

}
