using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EaseServer.FtpConnection
{
    public partial class FTPConnectionProcessor
    {
        #region FTP命令实现
        Socket GetDataSocket()
        {
            Socket DataSocket = null;
            try
            {
                if (DataTransferEnabled)
                {
                    int Count = 0;
                    while (!DataListener.Pending())
                    {
                        Thread.Sleep(1000);
                        Count++;
                        // Time out after 30 seconds
                        if (Count > 29)
                        {
                            SendMessage("425 Data Connection Timed out");
                            return null;
                        }
                    }
                    DataSocket = DataListener.AcceptSocket();
                    SendMessage("125 Connected, Starting Data Transfer.");
                }
                else
                {
                    SendMessage("150 Connecting.");
                    DataSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    DataSocket.Connect(ClientEndPoint);
                }
            }
            catch
            {
                SendMessage("425 Can't open data connection.");
                return null;
            }
            finally
            {
                if (DataListener != null)
                {
                    DataListener.Stop();
                    DataListener = null;
                    GC.Collect();
                }
            }
            DataTransferEnabled = false;
            return DataSocket;
        }

        void CDUP(string CmdArguments)
        {
            string[] pathParts = ConnectedUser.CurrentWorkingDirectory.Split('\\');
            if (pathParts.Length > 1)
            {
                ConnectedUser.CurrentWorkingDirectory = "";
                for (int i = 0; i < (pathParts.Length - 2); i++)
                {
                    ConnectedUser.CurrentWorkingDirectory += pathParts[i] + "\\";
                }
            }
            SendMessage("250 CDUP command successful.");
        }

        void TYPE(string CmdArguments)
        {
            if ((CmdArguments = CmdArguments.Trim().ToUpper()) == "A" || CmdArguments == "I")
            {
                SendMessage("200 Type " + CmdArguments + " Accepted.");
            }
            else
            {
                SendMessage("500 Unknown Type.");
            }
        }

        // Used inside PORT method
        IPEndPoint ClientEndPoint = null;
        bool DataTransferEnabled = false;

        void PORT(string CmdArguments)
        {
            string[] IP_Parts = CmdArguments.Split(',');
            if (IP_Parts.Length != 6)
            {
                SendMessage("550 Invalid arguments.\r\n");
                return;
            }
            string ClientIP = IP_Parts[0] + "." + IP_Parts[1] + "." + IP_Parts[2] + "." + IP_Parts[3];
            int tmpPort = (Convert.ToInt32(IP_Parts[4]) << 8) | Convert.ToInt32(IP_Parts[5]);
            ClientEndPoint = new IPEndPoint(Dns.GetHostEntry(ClientIP).AddressList[0], tmpPort);
            DataTransferEnabled = false;
            SendMessage("200 Ready to connect to " + ClientIP + "\r\n");
        }

        TcpListener DataListener = null;
        Socket ClientSocket;

        void PASV(string CmdArguments)
        {
            // Open listener within the specified port range
            int tmpPort = ApplicationSettings.MinPassvPort;
        StartListener:
            if (DataListener != null) { DataListener.Stop(); DataListener = null; }
            try
            {
                DataListener = new TcpListener(IPAddress.Any, tmpPort);
                DataListener.Start();
            }
            catch
            {
                if (tmpPort < ApplicationSettings.MaxPassvPort)
                {
                    tmpPort++;
                    goto StartListener;
                }
                else
                {
                    SendMessage("500 Action Failed Retry");
                    return;
                }
            }

            //string tmpEndPoint = DataListener.LocalEndpoint.ToString();
            //tmpPort = Convert.ToInt32(tmpEndPoint.Substring(tmpEndPoint.IndexOf(':') + 1));

            string SocketEndPoint = ClientSocket.LocalEndPoint.ToString();
            SocketEndPoint = SocketEndPoint.Substring(0, SocketEndPoint.IndexOf(":")).Replace(".", ",") + "," + (tmpPort >> 8) + "," + (tmpPort & 255);
            DataTransferEnabled = true;

            SendMessage("227 Entering Passive Mode (" + SocketEndPoint + ").");
        }

        void RETR(string CmdArguments)
        {
            if (!ConnectedUser.CanCopyFiles)
            {
                SendMessage("426 Access Denied.");
                return;
            }

            string ReturnMessage = string.Empty;
            FileStream FS = null;
            Socket DataSocket = null;
            try
            {
                string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);
                if (!ConnectedUser.CanViewHiddenFiles
                    && (File.GetAttributes(Path) & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    SendMessage("550 Access Denied or invalid path.");
                    return;
                }
                FS = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch
            {
                ReturnMessage = "550 Access denied or invalid path!";
                goto FinaliseAll;
            }


            DataSocket = GetDataSocket();
            if (DataSocket == null)
                goto FinaliseAll;

            try
            {
                byte[] data = new byte[(FS.Length > 100000) ? 100000 : (int)FS.Length];
                while (DataSocket.Send(data, 0, FS.Read(data, 0, data.Length), SocketFlags.None) != 0) ;
                ReturnMessage = "226 Transfer Complete.";
            }
            catch
            {
                ReturnMessage = "426 Transfer aborted.";
            }

        FinaliseAll:
            if (FS != null)
            {
                FS.Close(); FS = null;
            }
            if (DataSocket != null && DataSocket.Connected)
            {
                DataSocket.Shutdown(SocketShutdown.Both);
                DataSocket.Close();
            }
            DataSocket = null;
            SendMessage(ReturnMessage);
        }

        void STOR(string CmdArguments)
        {
            if (!ConnectedUser.CanStoreFiles)
            {
                SendMessage("426 Access Denied.");
                return;
            }

            Stream FS = null;
            string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);
            Path = Path.Substring(0, Path.Length - 1);

            try
            {
                FS = new FileStream(Path, FileMode.Create, FileAccess.Write, FileShare.None);
            }
            catch (Exception Ex)
            {
                SendMessage("550 " + Ex.Message);
                return;
            }

            Socket DataSocket = GetDataSocket();
            if (DataSocket == null)
            {
                return;
            }
            try
            {
                int ReadBytes = 1;
                byte[] tmpBuffer = new byte[10000];

                do
                {
                    ReadBytes = DataSocket.Receive(tmpBuffer);
                    FS.Write(tmpBuffer, 0, ReadBytes);
                } while (ReadBytes > 0);

                tmpBuffer = null;

                SendMessage("226 Transfer Complete.");
            }
            catch
            {
                SendMessage("426 Connection closed unexpectedly.");
            }
            finally
            {
                if (DataSocket != null)
                {
                    DataSocket.Shutdown(SocketShutdown.Both);
                    DataSocket.Close();
                    DataSocket = null;
                }
                FS.Close(); FS = null;
            }
        }

        void APPE(string CmdArguments)
        {
            // Append the file if exists or create a new file.
            SendMessage("500 This functionality is currently Unavailable");
        }

        void LIST(string CmdArguments)
        {
            string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);
            if (!ConnectedUser.CanViewHiddenFolders && (new DirectoryInfo(Path).Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            {
                SendMessage("550 Invalid path specified.");
                return;
            }

            Socket DataSocket = GetDataSocket();
            if (DataSocket == null)
            {
                return;
            }

            try
            {
                string[] FilesList = Directory.GetFiles(Path, "*.*", SearchOption.TopDirectoryOnly);
                string[] FoldersList = Directory.GetDirectories(Path, "*.*", SearchOption.TopDirectoryOnly);
                string strFilesList = "";

                if (ConnectedUser.CanViewHiddenFolders)
                {
                    foreach (string Folder in FoldersList)
                    {
                        string date = Directory.GetCreationTime(Folder).ToString("MM-dd-yy hh:mmtt");
                        strFilesList += date + " <DIR> " + Folder.Substring(Folder.Replace('\\', '/').LastIndexOf('/') + 1) + "\r\n";
                    }
                }
                else
                {
                    foreach (string Folder in FoldersList)
                    {
                        if ((new DirectoryInfo(Folder).Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) continue;

                        string date = Directory.GetCreationTime(Folder).ToString("MM-dd-yy hh:mmtt");
                        strFilesList += date + " <DIR> " + Folder.Substring(Folder.Replace('\\', '/').LastIndexOf('/') + 1) + "\r\n";
                    }
                }

                if (ConnectedUser.CanViewHiddenFiles)
                {
                    foreach (string FileName in FilesList)
                    {
                        string date = File.GetCreationTime(FileName).ToString("MM-dd-yy hh:mmtt");
                        strFilesList += date + " " + new FileInfo(FileName).Length.ToString() + " " + FileName.Substring(FileName.Replace('\\', '/').LastIndexOf('/') + 1) + "\r\n";
                    }
                }
                else
                {
                    foreach (string FileName in FilesList)
                    {
                        if ((File.GetAttributes(FileName) & FileAttributes.Hidden) == FileAttributes.Hidden) continue;

                        string date = File.GetCreationTime(FileName).ToString("MM-dd-yy hh:mmtt");
                        strFilesList += date + " " + new FileInfo(FileName).Length.ToString() + " " + FileName.Substring(FileName.Replace('\\', '/').LastIndexOf('/') + 1) + "\r\n";
                    }
                }

                DataSocket.Send(System.Text.Encoding.Default.GetBytes(strFilesList));
                SendMessage("226 Transfer Complete.");
            }
            catch (DirectoryNotFoundException)
            {
                SendMessage("550 Invalid path specified.");
            }
            catch
            {
                SendMessage("426 Connection closed; transfer aborted.");
            }
            finally
            {
                DataSocket.Shutdown(SocketShutdown.Both);
                DataSocket.Close(); DataSocket = null;
            }
        }

        void NLST(string CmdArguments)
        {
            string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);
            if (!Directory.Exists(Path))
            {
                SendMessage("550 Invalid Path.");
                return;
            }

            Socket DataSocket = GetDataSocket();
            if (DataSocket == null)
            {
                return;
            }

            try
            {
                string[] FoldersList = Directory.GetDirectories(Path, "*.*", SearchOption.TopDirectoryOnly);
                string FolderList = "";
                foreach (string Folder in FoldersList)
                {
                    FolderList += Folder.Substring(Folder.Replace('\\', '/').LastIndexOf('/') + 1) + "\r\n";
                }
                DataSocket.Send(System.Text.Encoding.Default.GetBytes(FolderList));
                DataSocket.Shutdown(SocketShutdown.Both);
                DataSocket.Close();

                SendMessage("226 Transfer Complete.");
            }
            catch
            {
                SendMessage("426 Connection closed; transfer aborted.");
            }
        }

        void DELE(string CmdArguments)
        {
            string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);
            try
            {
                if (File.Exists(Path))
                {
                    if (ConnectedUser.CanDeleteFiles)
                    {
                        //if (ApplicationSettings.MoveDeletedFilesToRecycleBin)
                        //{
                        //    //Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(Path,
                        //    //    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        //    //    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                        //    string RecycleBinPath = Path.Substring(0, 2) + "\\RECYCLER\\";
                        //    if (!Directory.Exists(RecycleBinPath))
                        //        Directory.CreateDirectory(RecycleBinPath);
                        //    File.Move(Path, RecycleBinPath + System.IO.Path.GetFileName(Path));
                        //}
                        //else
                        FileInfo FI = new FileInfo(Path);
                        FI.Attributes = FileAttributes.Normal; // This is required to delete a readonly file
                        File.Delete(Path);
                        SendMessage("250 File deleted.");
                    }
                    else
                    {
                        SendMessage("550 Access Denied.");
                    }
                }
                else
                {
                    SendMessage("550 File dose not exist.");
                }
            }
            catch (Exception Ex)
            {
                SendMessage(Ex);
            }
        }

        string Rename_FilePath;

        void RNFR(string CmdArguments)
        {
            if (!ConnectedUser.CanRenameFiles)
            {
                SendMessage("550 Access Denied.");
                return;
            }
            string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);

            //ServerConnection.ServerDebug("DIR:{1} StartUp:{2} Args:{3} RNFR:{0}",
            //    Path, ConnectedUser.CurrentWorkingDirectory, ConnectedUser.StartUpDirectory, CmdArguments);

            if (Directory.Exists(Path) || File.Exists(Path))
            {
                Rename_FilePath = Path;
                SendMessage("350 Please specify destination name.");
            }
            else
            {
                SendMessage("550 File or directory doesn't exist.");
            }
        }

        void RNTO(string CmdArguments)
        {
            if (Rename_FilePath.Length == 0)
            {
                SendMessage("503 Bad sequence of commands.");
                return;
            }

            string destPath = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);

            if (Directory.Exists(destPath) || File.Exists(destPath))
            {
                SendMessage("550 File or folder with the same name already exists.");
            }
            else
            {
                try
                {
                    if (Directory.Exists(Rename_FilePath))
                    {
                        if (ConnectedUser.CanRenameFolders)
                        {
                            Directory.Move(Rename_FilePath, destPath); SendMessage("250 Folder renamed successfully.");
                        }
                        else
                        {
                            SendMessage("550 Access Denied.");
                        }
                    }
                    else if (File.Exists(Rename_FilePath))
                    {
                        if (ConnectedUser.CanRenameFiles)
                        {
                            //[TODO] 指定完整目录路径
                            File.Move(Rename_FilePath, destPath);
                            SendMessage("250 File renamed successfully.");
                        }
                        else
                        {
                            SendMessage("550 Access Denied.");
                        }
                    }
                    else
                        SendMessage("550 Source file dose not exists.");
                }
                catch (Exception Ex)
                {
                    //ServerConnection.ServerDebug("DIR:{2} \r\n{0} -> {1}", Rename_FilePath, destPath, ConnectedUser.CurrentWorkingDirectory);
                    SendMessage(Ex);
                }
            }
            Rename_FilePath = "";
        }

        #region 删除与创建目录
        void RMD(string CmdArguments)
        {
            if (!ConnectedUser.CanDeleteFolders)
            {
                SendMessage("550 Access Denied.");
                return;
            }
            string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);
            if (Directory.Exists(Path))
            {
                try
                {
                    Directory.Delete(Path, true);
                    SendMessage("250 \"" + Path + "\" deleted.");
                }
                catch (Exception Ex)
                {
                    SendMessage(Ex);
                }
            }
            else
            {
                SendMessage("550 Folder dose not exist.");
            }
        }

        void MKD(string CmdArguments)
        {
            if (!ConnectedUser.CanStoreFolder)
            {
                SendMessage("550 Access Denied.");
                return;
            }
            string Path = ConnectedUser.StartUpDirectory + GetExactPath(CmdArguments);
            if (Directory.Exists(Path) || File.Exists(Path))
                SendMessage("550 A file or folder with the same name already exists.");
            else
            {
                try
                {
                    Directory.CreateDirectory(Path);
                    SendMessage("257 \"" + Path + "\" directory created.");
                }
                catch (Exception Ex) { SendMessage("550 " + Ex.Message + "."); }
            }
        }
        #endregion
        #endregion
    }
}
