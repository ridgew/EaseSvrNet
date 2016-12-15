using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace EaseServer
{
    public partial class ServerUI : Form
    {
        // web server settings
        string _appPath ,_virtRoot;
        string _portString = "8095";

        public ServerUI(String[] args)
        {
            if (Environment.UserInteractive)
                initialUI();

            try
            {
                if (args.Length >= 1)
                {
                    string dirName = Path.GetDirectoryName(args[0]);
                    if (string.IsNullOrEmpty(dirName))
                    {
                        _appPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, args[0]);
                        tbxAppDir.Text = _appPath;
                    }
                    else
                    {
                        _appPath = dirName;
                    }
                }
                if (args.Length >= 2) _portString = args[1];
                if (args.Length >= 3) _virtRoot = args[2];
            }
            catch { }

            if (string.IsNullOrEmpty(_appPath))
            {
                tbxAppDir.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppSettings["EaseServer.HostRoot"] ?? "");

                tbxAppDir.Focus();
                return;
            }
            Start();
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) { Server.Log.Error("UnhandledException", (Exception)e.ExceptionObject); }

        void stopInternalServerHander(object sender, EventArgs e) { Stop(true); }

        void Start()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(stopInternalServerHander);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(stopInternalServerHander);

            _appPath = tbxAppDir.Text;
            if (_appPath.Length == 0 || !Directory.Exists(_appPath))
            {
                ShowError("Invalid Application Directory");
                tbxAppDir.SelectAll();
                tbxAppDir.Focus();
                return;
            }

            _virtRoot = tbxAppPath.Text;
            if (_virtRoot.Length == 0 || !_virtRoot.StartsWith("/"))
            {
                ShowError("Invalid Virtual Root");
                tbxAppPath.SelectAll();
                tbxAppPath.Focus();
                return;
            }

            _portString = tbxPort.Text;
            try
            {

                //允许同时监听多个端口
                string[] listenPorts = _portString.Split(Program.SplitChars, StringSplitOptions.RemoveEmptyEntries);
                int portNumber = -1;
                foreach (string onePort in listenPorts)
                {
                    if (!int.TryParse(onePort, out portNumber))
                    {
                        ShowError("Invalid Port");
                        tbxPort.SelectAll();
                        tbxPort.Focus();
                        return;
                    }
                    else
                    {
                        Server _server = new Server(portNumber, _virtRoot, _appPath);
                        Program.AllRunServerList.Add(_server);
                        _server.Start();
                    }
                }
            }
            catch (Exception errEx)
            {
                ShowError(Server.ServerName + " failed to start listening on port " + _portString + ".\r\n" +
                    "Possible conflict with another Web Server on the same port.\r\n" + errEx.ToString());
                tbxPort.SelectAll();
                tbxPort.Focus();
                return;
            }

            btnStart.Enabled = false;
            tbxAppDir.Enabled = false;
            tbxPort.Enabled = false;
            tbxAppPath.Enabled = false;

            browseLabel.Visible = true;
            lblForClick.Visible = browseLabel.Visible;
            browseLabel.Text = GetLinkText();
            browseLabel.Focus();
        }

        void Stop(bool exitWin)
        {

            AppDomain.CurrentDomain.UnhandledException -= new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            AppDomain.CurrentDomain.DomainUnload -= new EventHandler(stopInternalServerHander);
            AppDomain.CurrentDomain.ProcessExit -= new EventHandler(stopInternalServerHander);

            try
            {
                Server.StopAllServer();
            }
            catch { }
            finally
            {
                Program.AllRunServerList.Clear();
            }

            if (exitWin)
            {
                Close();
            }
            else
            {
                btnStart.Enabled = tbxAppDir.Enabled = tbxPort.Enabled = tbxAppPath.Enabled = true;
            }
        }

        string GetLinkText()
        {
            string s = "http://localhost";
            int idx = _portString.IndexOfAny(Program.SplitChars);
            if (idx != -1)
            {
                if (_portString.Substring(0, idx) != "80")
                {
                    s += ":" + _portString.Substring(0, idx);
                }
            }
            else
            {
                if (_portString != "80")
                {
                    s += ":" + _portString;
                }
            }
            s += _virtRoot;
            if (!s.EndsWith("/")) s += "/";
            return s;
        }

        void ShowError(String err) { MessageBox.Show(err, Server.ServerName + " v" + Messages.VersionString, MessageBoxButtons.OK, MessageBoxIcon.Error); }

        void initialUI()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception) { }
            Text = Server.ServerName + " v" + Messages.VersionString;
            Icon = Properties.Resources.Cassini;
        }

        private void btnStart_Click(object sender, EventArgs e) { Start(); }

        private void btnPause_Click(object sender, EventArgs e) { Stop(false); }

        private void btnExit_Click(object sender, EventArgs e) { Stop(true); }

        private void btnHidden_Click(object sender, EventArgs e) { Hide(); }

        private void browseLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            browseLabel.Links[browseLabel.Links.IndexOf(e.Link)].Visited = true;
            System.Diagnostics.Process.Start(browseLabel.Text);
        }

        private void tbxAppDir_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] data = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string str in data)
                {
                    if (Directory.Exists(str))
                    {
                        tbxAppDir.Text = str;
                        break;
                    }
                }
            }
        }

        private void tbxAppDir_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void btnDumpThread_Click(object sender, EventArgs e)
        {
            SaveBinDataToFileDialog(this, DumpProcessDetail);
        }

        /// <summary>
        /// 保存字节序列为文件对话框
        /// </summary>
        static void SaveBinDataToFileDialog(IWin32Window owner, Action<Stream> dumpAct)
        {
            using (SaveFileDialog FDialog = new SaveFileDialog())
            {
                FDialog.Filter = "二进制文件 (*.bin)|*.bin|所有文件 (*.*)|*.*";
                FDialog.FilterIndex = 1;
                FDialog.RestoreDirectory = true;
                if (FDialog.ShowDialog(owner) == DialogResult.OK)
                {
                    using (Stream fStream = FDialog.OpenFile())
                    {
                        dumpAct(fStream);
                    }
                }
            }
        }

        static void DumpProcessDetail(Stream targetStream)
        {
            using (StreamWriter sw = new StreamWriter(targetStream, System.Text.Encoding.UTF8))
            {
                Process Proc = Process.GetCurrentProcess();
                sw.WriteLine("进程启动时间:{0}, 句柄总数{1}, 线程总数{2}。", Proc.StartTime, Proc.HandleCount, Proc.Threads.Count);
                foreach (ProcessThread t in Proc.Threads)
                {
                    if (t.ThreadState == ThreadState.Wait)
                    {
                        sw.WriteLine("* 线程{0}, 开始时间:{1}, 已处理{4}, 线程状态:{2}, 等待原因:{3}。", t.Id, t.StartTime, t.ThreadState, t.WaitReason, t.TotalProcessorTime);
                    }
                    else
                    {
                        sw.WriteLine("* 线程{0}, 开始时间:{1}, 已处理{3}, 线程状态:{2}。", t.Id, t.StartTime, t.ThreadState, t.TotalProcessorTime);
                    }
                }
                sw.Flush();
                sw.Close();
            }

        }

    }
}
