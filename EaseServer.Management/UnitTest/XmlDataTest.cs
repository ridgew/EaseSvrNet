#if UnitTest
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonLib;
using EaseServer.Management.ServiceModule;
using EaseServer.Management.Package;
using System.IO;

namespace EaseServer.Management.UnitTest
{

    public class XmlDataTest
    {
        public void WebMenuItemTest()
        {
            WebMenuItem item = new WebMenuItem();
            //item.ApplicationId = 1;
            //item.CreateDate = DateTime.Now;
            //item.CreatorUserId = 1;

            item.Children = new WebMenuItem[] {

                new WebMenuItem {
                    MenuName = "系统管理(M)", MenuId = "148", KeyCode=77,
                    Children = new WebMenuItem[] {
                        new WebMenuItem { MenuName = "系统菜单管理",
                            LeftUrl = "System/MenuTree.html", RightUrl = "System/EditMenu.html?MenuId=0", MenuId="149" },
                        new WebMenuItem { MenuName = "系统参数配置", MenuId="252",
                            RightUrl = "./System/Settings.html"},
                        new WebMenuItem { MenuName = "系统用户管理", MenuId="253",
                            RightUrl = "./System/UserList.html"},
                        new WebMenuItem { MenuName = "系统角色管理", MenuId="254",
                            LeftUrl="./System/RoleTree.html", RightUrl = "./System/EditRole.html?RoleId=0"}
                    }
                },

                new WebMenuItem {
                    MenuName = "接入服务器管理(I)", LeftUrl = "", RightUrl = "",MenuId = "298", KeyCode=73,
                    Children = new WebMenuItem[] {
                        new WebMenuItem { MenuName = "业务配置管理",
                            LeftUrl = "./Assist/entSrvTree.html", RightUrl = "/admin/Assist/HelpEdit.html?MenuId=303", MenuId="303" },
                        new WebMenuItem { MenuName = "刷新服务器缓存",
                            LeftUrl = "", RightUrl = "./getLanPage.aspx?url=http%3A%2F%2F118%2E123%2E205%2E163%3A7001%2Fease%2Fservlet%2Fease%3Fcmd%3D100", MenuId="299" }
                    }
                }


            };

            SvcCurrentMainMenuList mList = new SvcCurrentMainMenuList();
            mList.Data = new WebMenuItem[] { item };

            mList.Data.GetXmlDoc(true).WriteIndentedContent(Console.Out);
        }

        public void CompomentTest()
        {

            Root root = new Root() { ModifiedDateTime = DateTime.Now.DateTimeToDosTime(),
                PackageData = false, Reversion = 1, 
                BaseDirectory = @"D:\DevRoot\EaseServer\EaseServer\EaseServer\bin\Debug" };

            List<Component> cptList = new List<Component>();

            //Component cpt = new Component(root);
            //cpt.CompomentID = Guid.NewGuid();
            //cpt.Name = "管理平台框架前台";
            //cpt.Version = "0.0.1";
            //cpt.ModifiedDateTime = DateTime.Now.DateTimeToDosTime();

            //byte[] testBytes = Encoding.UTF8.GetBytes("Hello Word!");
            //List<ResourceFile> cptFileList = new List<ResourceFile>();
            //cptFileList.Add(new ResourceFile
            //{
            //    Name = "index.html",
            //    Type = ResourceType.HTML,
            //    Version = "1.0",
            //    HashCode = new HashProvider(HashProvider.ServiceProviderEnum.SHA1, Encoding.UTF8).ComputeHash(testBytes),
            //    Description = "主要操作界面",
            //    Content = testBytes
            //});

            //cpt.Files = cptFileList.ToArray();

            //cptList.Add(cpt);

            cptList.Add(Component.CreateFromDirectory(root, Path.Combine(root.BaseDirectory, ""), fsys => {
                return fsys.Attributes.Has(FileAttributes.Hidden);
            }));

            //cptList.Add(Component.CreateFromDirectory(root, Path.Combine(root.BaseDirectory, "API"), fsys =>
            //{
            //    return fsys.Attributes.Has(FileAttributes.Hidden);
            //})); 
            
            //cptList.Add(Component.CreateFromDirectory(root, Path.Combine(root.BaseDirectory, "App_Code"), fsys =>
            //{
            //    return fsys.Attributes.Has(FileAttributes.Hidden);
            //}));

            //cptList.Add(Component.CreateFromDirectory(root, Path.Combine(root.BaseDirectory, "bin"), fsys =>
            //{
            //    return fsys.Attributes.Has(FileAttributes.Hidden);
            //}));

            //cptList.Add(Component.CreateFromDirectory(root, Path.Combine(root.BaseDirectory, "Languages"), fsys =>
            //{
            //    return fsys.Attributes.Has(FileAttributes.Hidden);
            //}));

            //cptList.Add(Component.CreateFromDirectory(root, Path.Combine(root.BaseDirectory, "Tool"), fsys =>
            //{
            //    return fsys.Attributes.Has(FileAttributes.Hidden);
            //}));

            root.Compoments = cptList.ToArray();
            root.GetXmlDoc(true).Save(@"D:\DevRoot\EaseServer\EaseServer\EaseServer.ConsoleConnection\App_Data\package.config");
            //root.GetXmlDoc(true).WriteIndentedContent(Console.Out);
        }

        public void test()
        {
            Console.WriteLine(Guid.NewGuid().ToString("N"));
        }
    }
}
#endif