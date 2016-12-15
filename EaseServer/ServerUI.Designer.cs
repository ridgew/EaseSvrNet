namespace EaseServer
{
    partial class ServerUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.logoPanel = new System.Windows.Forms.Panel();
            this.lblServer = new System.Windows.Forms.Label();
            this.tbxAppDir = new System.Windows.Forms.TextBox();
            this.tbxPort = new System.Windows.Forms.TextBox();
            this.tbxAppPath = new System.Windows.Forms.TextBox();
            this.lblAppDir = new System.Windows.Forms.Label();
            this.lblPort = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.browseLabel = new System.Windows.Forms.LinkLabel();
            this.lblForClick = new System.Windows.Forms.Label();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnHidden = new System.Windows.Forms.Button();
            this.btnDumpThread = new System.Windows.Forms.Button();
            this.logoPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // logoPanel
            // 
            this.logoPanel.BackColor = System.Drawing.Color.White;
            this.logoPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.logoPanel.Controls.Add(this.lblServer);
            this.logoPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.logoPanel.Location = new System.Drawing.Point(0, 0);
            this.logoPanel.Name = "logoPanel";
            this.logoPanel.Size = new System.Drawing.Size(624, 100);
            this.logoPanel.TabIndex = 0;
            // 
            // lblServer
            // 
            this.lblServer.AutoSize = true;
            this.lblServer.Font = new System.Drawing.Font("Arial", 18F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblServer.ForeColor = System.Drawing.Color.RoyalBlue;
            this.lblServer.Location = new System.Drawing.Point(43, 33);
            this.lblServer.Name = "lblServer";
            this.lblServer.Size = new System.Drawing.Size(242, 28);
            this.lblServer.TabIndex = 0;
            this.lblServer.Text = "易致接入服务器.NET";
            // 
            // tbxAppDir
            // 
            this.tbxAppDir.AllowDrop = true;
            this.tbxAppDir.Location = new System.Drawing.Point(155, 125);
            this.tbxAppDir.Name = "tbxAppDir";
            this.tbxAppDir.Size = new System.Drawing.Size(400, 21);
            this.tbxAppDir.TabIndex = 1;
            this.tbxAppDir.Text = "api";
            this.tbxAppDir.DragDrop += new System.Windows.Forms.DragEventHandler(this.tbxAppDir_DragDrop);
            this.tbxAppDir.DragEnter += new System.Windows.Forms.DragEventHandler(this.tbxAppDir_DragEnter);
            // 
            // tbxPort
            // 
            this.tbxPort.Location = new System.Drawing.Point(157, 157);
            this.tbxPort.Name = "tbxPort";
            this.tbxPort.Size = new System.Drawing.Size(127, 21);
            this.tbxPort.TabIndex = 1;
            this.tbxPort.Text = global::EaseServer.Properties.Settings.Default.ListenPorts;
            // 
            // tbxAppPath
            // 
            this.tbxAppPath.Location = new System.Drawing.Point(155, 194);
            this.tbxAppPath.Name = "tbxAppPath";
            this.tbxAppPath.Size = new System.Drawing.Size(259, 21);
            this.tbxAppPath.TabIndex = 1;
            this.tbxAppPath.Text = "/";
            // 
            // lblAppDir
            // 
            this.lblAppDir.AutoSize = true;
            this.lblAppDir.Location = new System.Drawing.Point(54, 128);
            this.lblAppDir.Name = "lblAppDir";
            this.lblAppDir.Size = new System.Drawing.Size(95, 12);
            this.lblAppDir.TabIndex = 2;
            this.lblAppDir.Text = "接口程序目录(&D)";
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(56, 160);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(71, 12);
            this.lblPort.TabIndex = 3;
            this.lblPort.Text = "监听端口(&P)";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(54, 197);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 12);
            this.label3.TabIndex = 3;
            this.label3.Text = "虚拟目录(&A)";
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(49, 264);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(69, 25);
            this.btnStart.TabIndex = 4;
            this.btnStart.Text = "启动(&S)";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(231, 264);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 25);
            this.btnExit.TabIndex = 5;
            this.btnExit.Text = "退出(&X)";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // browseLabel
            // 
            this.browseLabel.AutoSize = true;
            this.browseLabel.Location = new System.Drawing.Point(169, 236);
            this.browseLabel.Name = "browseLabel";
            this.browseLabel.Size = new System.Drawing.Size(137, 12);
            this.browseLabel.TabIndex = 6;
            this.browseLabel.TabStop = true;
            this.browseLabel.Text = "http://localhost:8095/";
            this.browseLabel.Visible = false;
            this.browseLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.browseLabel_LinkClicked);
            // 
            // lblForClick
            // 
            this.lblForClick.AutoSize = true;
            this.lblForClick.Location = new System.Drawing.Point(56, 236);
            this.lblForClick.Name = "lblForClick";
            this.lblForClick.Size = new System.Drawing.Size(107, 12);
            this.lblForClick.TabIndex = 7;
            this.lblForClick.Text = "点击右侧地址访问:";
            this.lblForClick.Visible = false;
            // 
            // btnPause
            // 
            this.btnPause.Location = new System.Drawing.Point(139, 264);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(63, 25);
            this.btnPause.TabIndex = 4;
            this.btnPause.Text = "暂停(&P)";
            this.btnPause.UseVisualStyleBackColor = true;
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
            // 
            // btnHidden
            // 
            this.btnHidden.Location = new System.Drawing.Point(328, 264);
            this.btnHidden.Name = "btnHidden";
            this.btnHidden.Size = new System.Drawing.Size(73, 25);
            this.btnHidden.TabIndex = 5;
            this.btnHidden.Text = "隐藏(&H)";
            this.btnHidden.UseVisualStyleBackColor = true;
            this.btnHidden.Click += new System.EventHandler(this.btnHidden_Click);
            // 
            // btnDumpThread
            // 
            this.btnDumpThread.Location = new System.Drawing.Point(425, 264);
            this.btnDumpThread.Name = "btnDumpThread";
            this.btnDumpThread.Size = new System.Drawing.Size(105, 23);
            this.btnDumpThread.TabIndex = 8;
            this.btnDumpThread.Text = "输出进程信息(&O)";
            this.btnDumpThread.UseVisualStyleBackColor = true;
            this.btnDumpThread.Click += new System.EventHandler(this.btnDumpThread_Click);
            // 
            // ServerUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 322);
            this.Controls.Add(this.btnDumpThread);
            this.Controls.Add(this.lblForClick);
            this.Controls.Add(this.browseLabel);
            this.Controls.Add(this.btnHidden);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnPause);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lblPort);
            this.Controls.Add(this.lblAppDir);
            this.Controls.Add(this.tbxAppPath);
            this.Controls.Add(this.tbxPort);
            this.Controls.Add(this.tbxAppDir);
            this.Controls.Add(this.logoPanel);
            this.MaximizeBox = false;
            this.Name = "ServerUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "易致接入服务器.NET";
            this.logoPanel.ResumeLayout(false);
            this.logoPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel logoPanel;
        private System.Windows.Forms.TextBox tbxAppDir;
        private System.Windows.Forms.TextBox tbxPort;
        private System.Windows.Forms.TextBox tbxAppPath;
        private System.Windows.Forms.Label lblAppDir;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.LinkLabel browseLabel;
        private System.Windows.Forms.Label lblForClick;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnHidden;
        private System.Windows.Forms.Button btnDumpThread;
    }
}