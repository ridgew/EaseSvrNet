using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Services;
using System.Web.Script.Services;
using EaseServer.Management.Package;

namespace EaseServer.Management.ServiceModule
{
    /// <summary>
    /// 系统模块包管理服务
    /// </summary>
    [WebService(Name = "系统模块包管理服务", Description = "提供用于管理系统模块包的相应方法")]
    public class PackageManageService : WebServiceBase
    {
        /// <summary>
        /// 获取系统模块包单级菜单列表
        /// </summary>
        /// <param name="parentId">父级菜单ID</param>
        /// <returns>父级菜单ID下的子菜单列表</returns>
        [Protocol("1.4.0.0.1"), WebMethod(Description = "获取系统模块包单级菜单列表")]
        [GenerateScriptType(typeof(SvcMenuList), ScriptTypeId = "0")]
        public SvcMenuList GetMenuList(string parentId)
        {
            SvcMenuList list = new SvcMenuList();
            List<SvcMenuList.SingleMenuItem> itemList = new List<SvcMenuList.SingleMenuItem>();
            if (parentId.ToString() == "0")
            {
                foreach (Component cpt in Root.RootInstance.Compoments)
                {
                    itemList.Add(new SvcMenuList.SingleMenuItem
                    {
                        MenuId = cpt.CompomentID.ToString(),
                        MenuName = cpt.Name,
                        Enabled = cpt.Enabled,
                        HasChildren = (cpt.Children != null && cpt.Children.Length > 0)
                    });
                }
            }
            else
            {
                //通过GUID字符查找组件
                Component parentCpt = GetTargetComponent(parentId);
                if (parentCpt != null && parentCpt.Children != null)
                {
                    foreach (Component cpt in parentCpt.Children)
                    {
                        itemList.Add(new SvcMenuList.SingleMenuItem
                        {
                            MenuId = cpt.CompomentID.ToString(),
                            MenuName = cpt.Name,
                            Enabled = cpt.Enabled,
                            HasChildren = (cpt.Children != null && cpt.Children.Length > 0)
                        });
                    }
                }
            }
            list.Data = itemList.ToArray();
            return list;
        }

        Component GetTargetComponent(string menuId)
        {
            foreach (Component rootItem in Root.RootInstance.Compoments)
            {
                if (rootItem.CompomentID.ToString() == menuId.ToString())
                {
                    return rootItem;
                }
                else
                {
                    Component item = FoundParentComponent(menuId, rootItem);
                    if (item != null)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        Component FoundParentComponent(string parentId, Component currentItem)
        {
            if (currentItem.CompomentID.ToString() == parentId.ToString())
            {
                return currentItem;
            }
            else
            {
                if (currentItem.Children != null && currentItem.Children.Length > 0)
                {
                    Component parentItem = null;
                    foreach (Component subItem in currentItem.Children)
                    {
                        parentItem = FoundParentComponent(parentId, subItem);
                        if (parentItem != null)
                        {
                            return parentItem;
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 设置系统模块可用属性
        /// </summary>
        /// <param name="menuIds">菜单ID列表</param>
        /// <param name="menuEnableds">显示属性列表</param>
        /// <returns>操作结果</returns>
        [Protocol("1.4.0.0.2"), WebMethod(Description = "设置系统模块可用属性")]
        [GenerateScriptType(typeof(Result), ScriptTypeId = "0")]
        public Result SetMenuVisible(string[] menuIds, bool[] menuEnableds)
        {
            return new Result { Protocol = "1.4.0.0.2", Status = 10012 };
        }

    }
}
