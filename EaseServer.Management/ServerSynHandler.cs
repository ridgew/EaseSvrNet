using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Data.SqlClient;
using System.IO;
using CommonLib;

namespace EaseServer.Management
{
    /// <summary>
    /// 服务器同步相关[TODO]
    /// </summary>
    public class ServerSynHandler : IHttpHandler
    {

        #region IHttpHandler 成员

        public bool IsReusable { get { return false; } }

        public void ProcessRequest(HttpContext context)
        {

        }

        #endregion

        /*
         目录同步
         
         文件同步
            静态文件同步
            配置文件同步
         
         数据库同步
            表结构同步
            数据库函数同步
            存储过程同步
         */


        /// <summary>
        /// 获取本地目录与远程目录的不异同数据
        /// </summary>
        /// <param name="baseDir">基础路径</param>
        /// <param name="localDir">本地目录</param>
        /// <param name="jsonRemoteDir">远程目录数据</param>
        /// <returns></returns>
        string getDiffJson(string baseDir, string localDir, string jsonRemoteDir)
        {
            if (String.IsNullOrEmpty(jsonRemoteDir)
                || !jsonRemoteDir.StartsWith("[") || !jsonRemoteDir.EndsWith("]"))
            {
                return "[]";
            }

            string jsonResult = "";
            SynFileItem[] remoteItems = jsonRemoteDir.LoadFromJson<SynFileItem[]>();
            if (!Directory.Exists(localDir))
            {
                for (int i = 0, j = remoteItems.Length; i < j; i++)
                {
                    remoteItems[i].SynState = "+";
                }
                jsonResult = remoteItems.ToJSON();
            }
            else
            {
                SynFileItem[] localItems = getLocalDirFiles(baseDir, localDir);
                //比较不同
                SynFileItem lItem, rItem;
                List<SynFileItem> resultList = new List<SynFileItem>();

                #region 本地对比远程
                for (int m = 0, n = localItems.Length; m < n; m++)
                {
                    lItem = localItems[m];
                    rItem = Array.Find<SynFileItem>(remoteItems, f => f.FileName.Equals(lItem.FileName, StringComparison.InvariantCultureIgnoreCase));
                    if (rItem != null)
                    {
                        if (lItem.SHA1Hash.Equals(rItem.SHA1Hash, StringComparison.InvariantCultureIgnoreCase))
                        {
                            lItem.SynState = "=";
                        }
                        else
                        {
                            lItem.SynState = "*";
                        }
                    }
                    else
                    {
                        lItem.SynState = "-";
                    }
                    resultList.Add(lItem);
                }
                #endregion

                #region 远程对比本地 得出+
                for (int p = 0, q = remoteItems.Length; p < q; p++)
                {
                    rItem = remoteItems[p];
                    lItem = Array.Find<SynFileItem>(localItems, f => f.FileName.Equals(rItem.FileName, StringComparison.InvariantCultureIgnoreCase));
                    if (lItem == null)
                    {
                        rItem.SynState = "+";
                        resultList.Add(rItem);
                    }
                }
                #endregion

                jsonResult = resultList.ToArray().ToJSON();
            }
            return jsonResult;
        }

        /// <summary>
        /// 获取本地目录结构数据
        /// </summary>
        /// <param name="baseDir">基础路径</param>
        /// <param name="localDir">本地目录</param>
        /// <returns></returns>
        string getLocalDirJson(string baseDir, string localDir)
        {
            return getLocalDirFiles(baseDir, localDir).ToJSON();
        }

        SynFileItem[] getLocalDirFiles(string baseDir, string localDir)
        {
            List<SynFileItem> fileList = new List<SynFileItem>();
            DirectoryInfo di = new DirectoryInfo(localDir);
            if (di.Exists)
            {
                foreach (DirectoryInfo sdi in di.GetDirectories())
                {
                    fileList.Add(new SynFileItem
                    {
                        IsDir = true,
                        FileFullPath = sdi.FullName.Substring(baseDir.Length).Replace('\\', '/'),
                        FileName = sdi.Name
                    });
                }

                foreach (FileInfo fi in di.GetFiles())
                {
                    fileList.Add(new SynFileItem
                    {
                        IsDir = false,
                        FileFullPath = fi.FullName.Substring(baseDir.Length).Replace('\\', '/'),
                        FileName = fi.Name,
                        FileSize = fi.Length,
                        LastModifiedTime = ZipStorer.DateTimeToDosTime(fi.LastWriteTimeUtc),
                        SHA1Hash = hashUtil.ComputeHash(File.ReadAllBytes(fi.FullName))
                    });
                }
            }
            return fileList.ToArray();
        }

        HashProvider hashUtil = new HashProvider(CommonLib.HashProvider.ServiceProviderEnum.SHA1, Encoding.UTF8);

        #region 同步文件

        /// <summary>
        /// 保存文件数据到更新包，并验证。
        /// </summary>
        /// <param name="zip">更新包</param>
        /// <param name="fs">上传文件数据</param>
        /// <param name="modifiedDostime">修改时间</param>
        /// <param name="storePath">内部存储路径</param>
        /// <param name="sumKey">数据SHA1验证</param>
        /// <returns></returns>
        string StoreUpdateFile(ZipStorer zip, Stream fs, uint modifiedDostime, string storePath, string sumKey)
        {
            string storeResult = "ok";
            using (MemoryStream ms = new MemoryStream())
            {
                #region 全部读取到内存字节序列
                byte[] buffer = new byte[4096];
                int readLen = 0;
                while ((readLen = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, readLen);
                }
                #endregion

                bool addStore = true;
                #region 字节验证
                if (!string.IsNullOrEmpty(sumKey))
                {
                    string saveSum = hashUtil.ComputeHash(ms.ToArray());
                    if (saveSum.Equals(sumKey, StringComparison.InvariantCultureIgnoreCase))
                    {
                        storeResult = "*文件验证失败";
                        addStore = false;
                    }
                }
                #endregion

                if (addStore)
                {
                    ms.Position = 0;
                    zip.AddStream(ZipStorer.Compression.Deflate, storePath, ms, ZipStorer.DosTimeToDateTime(modifiedDostime), "");
                }

                ms.Close();
            }
            return storeResult;
        }

        #endregion

    }

    /// <summary>
    /// 同步文件项
    /// </summary>
    [Serializable]
    public class SynFileItem
    {
        public string FileFullPath { get; set; }

        public string FileName { get; set; }

        public uint LastModifiedTime { get; set; }

        public string SHA1Hash { get; set; }

        public long FileSize { get; set; }

        public bool IsDir { get; set; }

        /// <summary>
        /// 同步状态: + - *
        /// </summary>
        public string SynState { get; set; }
    }
}
