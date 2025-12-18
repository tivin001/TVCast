namespace TVCast
{
    partial class frmMain
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.pageHeader1 = new AntdUI.PageHeader();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tvDevices = new AntdUI.Tree();
            this.panel1 = new AntdUI.Panel();
            this.btnStopSearch = new AntdUI.Button();
            this.btnSearch = new AntdUI.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtFileName = new AntdUI.Input();
            this.btnStopVideo = new AntdUI.Button();
            this.btnStartVideo = new AntdUI.Button();
            this.btnSelectFile = new AntdUI.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.chkEnableAudio = new AntdUI.Checkbox();
            this.btnStopScreen = new AntdUI.Button();
            this.btnStartScreen = new AntdUI.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.lblScreenTip = new AntdUI.Label();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.groupBox1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // pageHeader1
            // 
            this.pageHeader1.DividerMargin = 3;
            this.pageHeader1.DividerShow = true;
            this.pageHeader1.Dock = System.Windows.Forms.DockStyle.Top;
            this.pageHeader1.Location = new System.Drawing.Point(10, 0);
            this.pageHeader1.Margin = new System.Windows.Forms.Padding(5);
            this.pageHeader1.MaximizeBox = false;
            this.pageHeader1.Name = "pageHeader1";
            this.pageHeader1.ShowButton = true;
            this.pageHeader1.ShowIcon = true;
            this.pageHeader1.Size = new System.Drawing.Size(721, 40);
            this.pageHeader1.TabIndex = 0;
            this.pageHeader1.Text = "电视投屏";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tvDevices);
            this.groupBox1.Controls.Add(this.panel1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(320, 431);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "设备列表";
            // 
            // tvDevices
            // 
            this.tvDevices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvDevices.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tvDevices.Location = new System.Drawing.Point(3, 72);
            this.tvDevices.Name = "tvDevices";
            this.tvDevices.Size = new System.Drawing.Size(314, 356);
            this.tvDevices.TabIndex = 0;
            this.tvDevices.Text = "tree1";
            // 
            // panel1
            // 
            this.panel1.Back = System.Drawing.Color.Transparent;
            this.panel1.BackColor = System.Drawing.Color.Transparent;
            this.panel1.Controls.Add(this.btnStopSearch);
            this.panel1.Controls.Add(this.btnSearch);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(3, 25);
            this.panel1.Name = "panel1";
            this.panel1.Radius = 2;
            this.panel1.Size = new System.Drawing.Size(314, 47);
            this.panel1.TabIndex = 2;
            this.panel1.Text = "panel1";
            // 
            // btnStopSearch
            // 
            this.btnStopSearch.IconSvg = "StopOutlined";
            this.btnStopSearch.Location = new System.Drawing.Point(127, 8);
            this.btnStopSearch.Name = "btnStopSearch";
            this.btnStopSearch.Radius = 4;
            this.btnStopSearch.Size = new System.Drawing.Size(88, 32);
            this.btnStopSearch.TabIndex = 1;
            this.btnStopSearch.Text = "停止";
            this.btnStopSearch.Type = AntdUI.TTypeMini.Warn;
            this.btnStopSearch.WaveSize = 0;
            this.btnStopSearch.Click += new System.EventHandler(this.btnStopSearch_Click);
            // 
            // btnSearch
            // 
            this.btnSearch.IconSvg = "SearchOutlined";
            this.btnSearch.Location = new System.Drawing.Point(19, 8);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Radius = 4;
            this.btnSearch.Size = new System.Drawing.Size(88, 32);
            this.btnSearch.TabIndex = 1;
            this.btnSearch.Text = "刷新";
            this.btnSearch.Type = AntdUI.TTypeMini.Primary;
            this.btnSearch.WaveSize = 0;
            this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtFileName);
            this.groupBox2.Controls.Add(this.btnStopVideo);
            this.groupBox2.Controls.Add(this.btnStartVideo);
            this.groupBox2.Controls.Add(this.btnSelectFile);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox2.Location = new System.Drawing.Point(320, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(401, 243);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "视频投屏";
            // 
            // txtFileName
            // 
            this.txtFileName.Enabled = false;
            this.txtFileName.JoinMode = AntdUI.TJoinMode.Left;
            this.txtFileName.Location = new System.Drawing.Point(16, 72);
            this.txtFileName.Name = "txtFileName";
            this.txtFileName.Radius = 4;
            this.txtFileName.Size = new System.Drawing.Size(368, 32);
            this.txtFileName.TabIndex = 2;
            this.txtFileName.WaveSize = 0;
            // 
            // btnStopVideo
            // 
            this.btnStopVideo.IconSvg = "LogoutOutlined";
            this.btnStopVideo.Location = new System.Drawing.Point(222, 139);
            this.btnStopVideo.Name = "btnStopVideo";
            this.btnStopVideo.Radius = 4;
            this.btnStopVideo.Size = new System.Drawing.Size(102, 55);
            this.btnStopVideo.TabIndex = 1;
            this.btnStopVideo.Text = "结束投屏";
            this.btnStopVideo.Type = AntdUI.TTypeMini.Error;
            this.btnStopVideo.WaveSize = 0;
            this.btnStopVideo.Click += new System.EventHandler(this.btnStopVideo_Click);
            // 
            // btnStartVideo
            // 
            this.btnStartVideo.IconSvg = "FundProjectionScreenOutlined";
            this.btnStartVideo.Location = new System.Drawing.Point(68, 139);
            this.btnStartVideo.Name = "btnStartVideo";
            this.btnStartVideo.Radius = 4;
            this.btnStartVideo.Size = new System.Drawing.Size(102, 55);
            this.btnStartVideo.TabIndex = 1;
            this.btnStartVideo.Text = "开始投屏";
            this.btnStartVideo.Type = AntdUI.TTypeMini.Success;
            this.btnStartVideo.WaveSize = 0;
            this.btnStartVideo.Click += new System.EventHandler(this.btnStartVideo_Click);
            // 
            // btnSelectFile
            // 
            this.btnSelectFile.IconSvg = "FolderOpenOutlined";
            this.btnSelectFile.Location = new System.Drawing.Point(16, 33);
            this.btnSelectFile.Name = "btnSelectFile";
            this.btnSelectFile.Radius = 4;
            this.btnSelectFile.Size = new System.Drawing.Size(102, 32);
            this.btnSelectFile.TabIndex = 1;
            this.btnSelectFile.Text = "选择文件";
            this.btnSelectFile.Type = AntdUI.TTypeMini.Success;
            this.btnSelectFile.WaveSize = 0;
            this.btnSelectFile.Click += new System.EventHandler(this.btnSelectFile_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.chkEnableAudio);
            this.groupBox3.Controls.Add(this.btnStopScreen);
            this.groupBox3.Controls.Add(this.btnStartScreen);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(320, 243);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(401, 188);
            this.groupBox3.TabIndex = 3;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "屏幕投屏";
            // 
            // chkEnableAudio
            // 
            this.chkEnableAudio.Location = new System.Drawing.Point(68, 28);
            this.chkEnableAudio.Name = "chkEnableAudio";
            this.chkEnableAudio.Size = new System.Drawing.Size(102, 23);
            this.chkEnableAudio.TabIndex = 4;
            this.chkEnableAudio.Text = "启用音频";
            // 
            // btnStopScreen
            // 
            this.btnStopScreen.IconSvg = "LogoutOutlined";
            this.btnStopScreen.Location = new System.Drawing.Point(222, 75);
            this.btnStopScreen.Name = "btnStopScreen";
            this.btnStopScreen.Radius = 4;
            this.btnStopScreen.Size = new System.Drawing.Size(102, 55);
            this.btnStopScreen.TabIndex = 1;
            this.btnStopScreen.Text = "结束投屏";
            this.btnStopScreen.Type = AntdUI.TTypeMini.Error;
            this.btnStopScreen.WaveSize = 0;
            this.btnStopScreen.Click += new System.EventHandler(this.btnStopScreen_Click);
            // 
            // btnStartScreen
            // 
            this.btnStartScreen.IconSvg = "PlayCircleOutlined";
            this.btnStartScreen.Location = new System.Drawing.Point(68, 75);
            this.btnStartScreen.Name = "btnStartScreen";
            this.btnStartScreen.Radius = 4;
            this.btnStartScreen.Size = new System.Drawing.Size(102, 55);
            this.btnStartScreen.TabIndex = 1;
            this.btnStartScreen.Text = "开始投屏";
            this.btnStartScreen.Type = AntdUI.TTypeMini.Success;
            this.btnStartScreen.WaveSize = 0;
            this.btnStartScreen.Click += new System.EventHandler(this.btnStartScreen_Click);
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.groupBox3);
            this.panel2.Controls.Add(this.groupBox2);
            this.panel2.Controls.Add(this.groupBox1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(10, 40);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(721, 431);
            this.panel2.TabIndex = 4;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.lblScreenTip);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel3.Location = new System.Drawing.Point(10, 471);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(721, 26);
            this.panel3.TabIndex = 5;
            // 
            // lblScreenTip
            // 
            this.lblScreenTip.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblScreenTip.Location = new System.Drawing.Point(6, 1);
            this.lblScreenTip.Name = "lblScreenTip";
            this.lblScreenTip.Size = new System.Drawing.Size(712, 23);
            this.lblScreenTip.TabIndex = 0;
            this.lblScreenTip.Text = "地址：";
            // 
            // notifyIcon
            // 
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "轻控-电视投屏";
            this.notifyIcon.Visible = true;
            this.notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseClick);
            this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseDoubleClick);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(741, 507);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.pageHeader1);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(5);
            this.Name = "frmMain";
            this.Padding = new System.Windows.Forms.Padding(10, 0, 10, 10);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "轻控-电视投屏";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.groupBox1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private AntdUI.PageHeader pageHeader1;
        private System.Windows.Forms.GroupBox groupBox1;
        private AntdUI.Tree tvDevices;
        private AntdUI.Button btnStopSearch;
        private AntdUI.Button btnSearch;
        private AntdUI.Panel panel1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private AntdUI.Button btnSelectFile;
        private AntdUI.Input txtFileName;
        private AntdUI.Button btnStartVideo;
        private AntdUI.Button btnStopVideo;
        private AntdUI.Button btnStopScreen;
        private AntdUI.Button btnStartScreen;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private AntdUI.Label lblScreenTip;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private AntdUI.Checkbox chkEnableAudio;
    }
}

