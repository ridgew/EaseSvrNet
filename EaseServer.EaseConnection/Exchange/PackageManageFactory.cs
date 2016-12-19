using System;
using System.Web;
using System.Web.Caching;

namespace EaseServer.EaseConnection
{
    /// <summary>
    /// 内容包管理
    /// </summary>
    public class PackageManageFactory
    {
        private PackageManageFactory()
        {

        }

        static PackageManageFactory _instance = new PackageManageFactory();
        /// <summary>
        /// 唯一静态实例
        /// </summary>
        public static PackageManageFactory Instance { get { return _instance; } }

        /// <summary>
        /// 是否包含指定包键值
        /// </summary>
        /// <param name="pkgKey">键值</param>
        /// <param name="pkgBytes">更行当前包数据</param>
        /// <returns>
        /// 	<c>true</c> if [contains] [the specified PKG key]; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string pkgKey, ref byte[] pkgBytes)
        {
            object pkgDat = HttpRuntime.Cache.Get(pkgKey);
            if (pkgDat == null)
            {
                return false;
            }
            else
            {
                pkgBytes = (byte[])pkgDat;
                return true;
            }
        }

        /// <summary>
        /// 插入包到缓存中
        /// </summary>
        /// <param name="pkgKey">键值</param>
        /// <param name="pkgBytes">包数据</param>
        public void Insert(string pkgKey, byte[] pkgBytes)
        {
            HttpRuntime.Cache.Insert(pkgKey, pkgBytes, null,
                Cache.NoAbsoluteExpiration,
                TimeSpan.FromMinutes(10));
        }
    }
}
