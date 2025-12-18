using AntdUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TVCast
{
    public partial class frmMain : AntdUI.Window
    {
        private DLNADeviceFinder _deviceFinder;
        private bool _isSearching = false;

        private VideoCaster _videoCaster;

        AntdUI.IContextMenuStripItem[] _menulist = { };
        public frmMain()
        {
            InitializeComponent();
            this.pageHeader1.Text = GetAssemblyTitle();
            lblScreenTip.Text = string.Empty;
            // 禁止用户缩放窗体
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            _menulist = new AntdUI.IContextMenuStripItem[]
                {
                    new AntdUI.ContextMenuStripItem("主界面").SetIcon("AppstoreOutlined"),
                    new AntdUI.ContextMenuStripItem("关于").SetIcon("BulbOutlined"),
                    new AntdUI.ContextMenuStripItemDivider(),
                    new AntdUI.ContextMenuStripItem("退出程序").SetIcon("LogoutOutlined")
                };
        }

        // 获取 AssemblyTitle 的方法
        private string GetAssemblyTitle()
        {
            // 获取当前程序集
            Assembly assembly = Assembly.GetExecutingAssembly();

            // 获取 AssemblyTitle Attribute
            AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)Attribute.GetCustomAttribute(
                assembly, typeof(AssemblyTitleAttribute));

            // 返回 AssemblyTitle
            return titleAttribute?.Title ?? "未知标题";
        }

        private void frmMain_Load(object sender, System.EventArgs e)
        {
            // 初始化设备查找器
            _deviceFinder = new DLNADeviceFinder();
            _deviceFinder.DeviceDiscovered += OnDeviceDiscovered;
            _deviceFinder.SearchComplete += OnSearchComplete;

            // 初始化视频投屏器
            _videoCaster = new VideoCaster();
            _videoCaster.CastingStarted += OnCastingStarted;
            _videoCaster.CastingStopped += OnCastingStopped;
            _videoCaster.CastingError += OnCastingError;

            _videoCaster.CastingStarted += OnServerStarted;
            _videoCaster.CastingStopped += OnServerStopped;
            SearchDevices();
        }

        private void SearchDevices()
        {
            if (_isSearching)
                return;

            _isSearching = true;
            this.tvDevices.Items.Clear();
            tvDevices.Spin(AntdUI.Localization.Get("Loading2", "正在搜索DLNA设备..."), async config =>
            {
                tvDevices.EmptyText = "";
                // 开始异步搜索
                await _deviceFinder.StartSearchAsync();
            }, () =>
            {
                System.Diagnostics.Debug.WriteLine("搜索结束");
                _isSearching = false;
                tvDevices.EmptyText = "未搜索到设备";
            });
            return;
        }

        private void OnDeviceDiscovered(object sender, DeviceDiscoveredEventArgs e)
        {
            // 在UI线程中更新设备列表
            if (InvokeRequired)
            {
                Invoke(new EventHandler<DeviceDiscoveredEventArgs>(OnDeviceDiscovered), sender, e);
                return;
            }
            // 添加设备到列表
            tvDevices.Items.Add(new AntdUI.TreeItem(e.Device.ToString())
            {
                Tag = e.Device
            });
        }

        private void OnSearchComplete(object sender, SearchCompleteEventArgs e)
        {
            // 在UI线程中更新状态
            if (InvokeRequired)
            {
                Invoke(new EventHandler<SearchCompleteEventArgs>(OnSearchComplete), sender, e);
                return;
            }

            // 更新状态或其他UI元素
            if (e.IsSuccessful)
            {
                System.Diagnostics.Debug.WriteLine($"搜索完成，共发现 {e.Devices.Count} 个设备");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("搜索失败");
            }
        }

        private async void frmMain_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                var dialogResult = new frmCloseDialog();
                if (dialogResult.ShowDialog(this) == DialogResult.Yes)
                {
                    await CloseApp();

                }
                else
                {
                    e.Cancel = true;             // 取消关闭操作
                    this.Hide();                 // 隐藏主窗体
                    notifyIcon.Visible = true;  // 显示托盘图标
                }
            }
        }

        private async Task CloseApp()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(async () => await CloseApp()));
                return;
            }
            notifyIcon.Visible = false; // 隐藏托盘图标
            try
            {
                // 先停止视频投屏，但不等待
                if (_videoCaster != null && _videoCaster.IsCasting)
                {
                    try
                    {
                        await _videoCaster.StopCastingAsync();
                    }
                    catch (Exception tEx)
                    {
                        // 捕获停止投屏时的错误
                        System.Diagnostics.Debug.WriteLine($"停止投屏时出错: {tEx.Message}");
                    }
                }
                try
                {
                    _deviceFinder?.Dispose();
                    _videoCaster?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"关闭窗体时释放资源出错: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"关闭窗体时出错: {ex.Message}");
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            SearchDevices();
        }

        private void btnStopSearch_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查是否正在搜索
                if (!_isSearching)
                {
                    // 如果没有在搜索，可以选择禁用停止按钮或显示提示
                    System.Diagnostics.Debug.WriteLine("当前没有正在进行的搜索");
                    return;
                }

                // 停止搜索
                _deviceFinder?.StopSearch();

                // 重置搜索状态
                _isSearching = false;

                // 记录日志
                System.Diagnostics.Debug.WriteLine("用户手动停止了DLNA设备搜索");
            }
            catch (Exception ex)
            {
                // 处理可能的异常
                System.Diagnostics.Debug.WriteLine($"停止搜索时发生错误: {ex.Message}");
            }
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // 设置文件对话框标题
                openFileDialog.Title = "选择视频文件";

                // 设置常见视频格式的筛选器
                openFileDialog.Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v|" +
                                      "MP4 文件 (*.mp4)|*.mp4|" +
                                      "AVI 文件 (*.avi)|*.avi|" +
                                      "MKV 文件 (*.mkv)|*.mkv|" +
                                      "MOV 文件 (*.mov)|*.mov|" +
                                      "WMV 文件 (*.wmv)|*.wmv|" +
                                      "FLV 文件 (*.flv)|*.flv|" +
                                      "WebM 文件 (*.webm)|*.webm|" +
                                      "M4V 文件 (*.m4v)|*.m4v|" +
                                      "所有文件 (*.*)|*.*";

                // 默认筛选器索引
                openFileDialog.FilterIndex = 1;

                // 恢复上次目录
                openFileDialog.RestoreDirectory = true;

                // 检查用户是否选择了文件
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // 获取选择的文件路径
                    string filePath = openFileDialog.FileName;

                    // 只获取文件名（不包含路径）
                    string fileName = System.IO.Path.GetFileName(filePath);

                    // 将文件名显示在txtFileName中
                    txtFileName.Text = fileName;

                    // 将完整路径存储在txtFileName的Tag属性中
                    txtFileName.Tag = filePath;
                }
            }
        }

        private async void btnStartVideo_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查是否已选择文件
                if (string.IsNullOrEmpty(txtFileName.Text) || txtFileName.Tag == null)
                {
                    AntdUI.Message.warn(this, "请先选择视频文件");
                    return;
                }

                // 获取文件路径
                string filePath = txtFileName.Tag as string;

                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    AntdUI.Message.warn(this, "选择的文件不存在");
                    return;
                }

                // 检查是否已选择设备
                if (tvDevices.SelectItem == null)
                {
                    AntdUI.Message.warn(this, "请先选择DLNA设备");
                    return;
                }

                // 获取选中的设备
                var selectedDevice = tvDevices.SelectItem.Tag as DLNADevice;
                if (selectedDevice == null)
                {
                    AntdUI.Message.error(this, "无效的DLNA设备");
                    return;
                }

                // 开始投屏
                btnStartVideo.Enabled = false;
                btnStartVideo.Text = "连接中...";
                btnStartVideo.Loading = true;

                var success = await _videoCaster.StartCastingAsync(selectedDevice, filePath);

                if (success)
                {
                    btnStartVideo.Text = "开始投屏";
                }
                else
                {
                    btnStartVideo.Text = "开始投屏";
                    btnStartVideo.Enabled = true;
                }

                btnStartVideo.Loading = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"开始视频投屏时出错: {ex.Message}");
                AntdUI.Message.error(this, $"开始投屏失败: {ex.Message}");

                btnStartVideo.Text = "开始投屏";
                btnStartVideo.Enabled = true;
                btnStartVideo.Loading = false;
            }
        }

        private async void btnStopVideo_Click(object sender, EventArgs e)
        {
            try
            {
                // 停止投屏
                btnStopVideo.Enabled = false;
                btnStopVideo.Text = "停止中...";
                btnStopVideo.Loading = true;

                var success = await _videoCaster.StopCastingAsync();

                btnStopVideo.Text = "结束投屏";
                btnStopVideo.Enabled = true;
                btnStopVideo.Loading = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"停止视频投屏时出错: {ex.Message}");
                AntdUI.Message.error(this, $"停止投屏失败: {ex.Message}");

                btnStopVideo.Text = "结束投屏";
                btnStopVideo.Enabled = true;
                btnStopVideo.Loading = false;
            }
        }

        private async void btnStartScreen_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查是否已选择设备
                if (tvDevices.SelectItem == null)
                {
                    AntdUI.Message.warn(this, "请先选择DLNA设备");
                    return;
                }

                // 获取选中的设备
                var selectedDevice = tvDevices.SelectItem.Tag as DLNADevice;
                if (selectedDevice == null)
                {
                    AntdUI.Message.error(this, "无效的DLNA设备");
                    return;
                }

                // 开始投屏
                btnStartScreen.Enabled = false;
                chkEnableAudio.Enabled = false;
                btnStartScreen.Text = "连接中...";
                btnStartScreen.Loading = true;
                _videoCaster.EnableAudio(this.chkEnableAudio.Checked);
                var success = await _videoCaster.StartScreenMirroringAsync(selectedDevice);

                if (success)
                {
                    btnStartScreen.Text = "开始投屏";
                }
                else
                {
                    btnStartScreen.Text = "开始投屏";
                    btnStartScreen.Enabled = true;
                    chkEnableAudio.Enabled = true;
                }

                btnStartScreen.Loading = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"开始视频投屏时出错: {ex.Message}");
                AntdUI.Message.error(this, $"开始投屏失败: {ex.Message}");

                btnStartScreen.Text = "开始投屏";
                btnStartScreen.Enabled = true;
                chkEnableAudio.Enabled = true;
                btnStartScreen.Loading = false;
            }
        }

        private async void btnStopScreen_Click(object sender, EventArgs e)
        {
            try
            {
                // 停止投屏
                btnStopScreen.Enabled = false;
                btnStopScreen.Text = "停止中...";
                btnStopScreen.Loading = true;

                var success = await _videoCaster.StopCastingAsync();

                btnStopScreen.Text = "结束投屏";
                btnStopScreen.Enabled = true;
                btnStopScreen.Loading = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"停止视频投屏时出错: {ex.Message}");
                AntdUI.Message.error(this, $"停止投屏失败: {ex.Message}");

                btnStopScreen.Text = "结束投屏";
                btnStopScreen.Enabled = true;
                btnStopScreen.Loading = false;
            }
        }

        // 添加事件处理方法
        private void OnCastingStarted(object sender, EventArgs e)
        {
            // 在UI线程中更新状态
            if (InvokeRequired)
            {
                Invoke(new EventHandler(OnCastingStarted), sender, e);
                return;
            }

            // 更新UI状态
            btnStartVideo.Enabled = false;
            btnStopVideo.Enabled = true;
            btnStartScreen.Enabled = false;
            chkEnableAudio.Enabled = false;
            btnStopScreen.Enabled = true;
            // 显示提示
            AntdUI.Message.info(this, "视频投屏已开始");
        }

        private void OnCastingStopped(object sender, EventArgs e)
        {
            // 在UI线程中更新状态
            if (InvokeRequired)
            {
                Invoke(new EventHandler(OnCastingStopped), sender, e);
                return;
            }

            // 更新UI状态
            btnStartVideo.Enabled = true;
            btnStopVideo.Enabled = true;
            btnStartScreen.Enabled = true;
            chkEnableAudio.Enabled = true;
            btnStopScreen.Enabled = true;
            // 显示提示
            AntdUI.Message.info(this, "视频投屏已停止");
        }

        private void OnCastingError(object sender, CastingErrorEventArgs e)
        {
            // 在UI线程中显示错误
            if (InvokeRequired)
            {
                Invoke(new EventHandler<CastingErrorEventArgs>(OnCastingError), sender, e);
                return;
            }

            // 更新UI状态
            btnStartVideo.Enabled = true;
            btnStopVideo.Enabled = false;

            // 显示错误消息
            AntdUI.Message.error(this, $"投屏错误: {e.Message}");
        }

        // 添加新的事件处理方法
        private void OnServerStarted(object sender, CastingStartedEventArgs e)
        {
            var serverUrl = e.Url;
            // 在UI线程中更新状态
            if (InvokeRequired)
            {
                Invoke(new EventHandler<CastingStartedEventArgs>(OnServerStarted), sender, serverUrl);
                return;
            }

            // 显示服务器地址
            lblScreenTip.Text = $"地址: {serverUrl}";
            lblScreenTip.ForeColor = System.Drawing.Color.Green;
        }

        private void OnServerStopped(object sender, EventArgs e)
        {
            // 在UI线程中更新状态
            if (InvokeRequired)
            {
                Invoke(new EventHandler(OnServerStopped), sender, e);
                return;
            }

            // 清空显示
            lblScreenTip.Text = string.Empty;
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                AntdUI.ContextMenuStrip.open(this, notifyIcon, it =>
                {
                    if (it.Text == "主界面")
                    {
                        notifyIcon_MouseDoubleClick(null, null);
                    }
                    else if (it.Text == "关于")
                    {
                        AntdUI.Modal.open(this, "问题可反馈：", "公众号：非硬核开发\r\n开源地址：https://github.com/tivin001/TVCast");
                    }
                    else if (it.Text == "退出程序")
                    {
                        _ = CloseApp();
                    }
                }, _menulist);

            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;  // 恢复窗口状态（避免最小化）
            this.Activate();                 // 激活窗口（获取焦点）
        }

        private void hyperlinkLabel1_LinkClicked(object sender, HyperlinkLabel.LinkClickedEventArgs e)
        {

        }
    }
}
