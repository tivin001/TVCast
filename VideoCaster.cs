using NAudio.Wave;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace TVCast
{
    #region Core Controller
    /// <summary>
    /// 投屏控制器：负责协调设备发现、文件服务和直播服务的生命周期。
    /// </summary>
    public class VideoCaster : IDisposable
    {
        private const string AV_TRANSPORT = "urn:schemas-upnp-org:service:AVTransport:1";

        private readonly DLNACommandSender _commandSender;
        private readonly CancellationTokenSource _cts;

        private DLNADevice _device;
        private FastSocketServer _fileServer;
        private ScreenLiveServer _liveServer;

        private bool _casting;
        private bool _isEnableAudio;

        public string AudioDeviceName { get; set; } = null;
        public bool IsCasting => _casting;

        public event EventHandler<CastingStartedEventArgs> CastingStarted;
        public event EventHandler<CastingStoppedEventArgs> CastingStopped;
        public event EventHandler<CastingErrorEventArgs> CastingError;

        public VideoCaster()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            _commandSender = new DLNACommandSender();
            _cts = new CancellationTokenSource();
        }

        public void EnableAudio(bool enable) => _isEnableAudio = enable;

        /// <summary>
        /// 启动屏幕镜像投屏
        /// </summary>
        public async Task<bool> StartScreenMirroringAsync(DLNADevice device)
        {
            if (_casting) throw new InvalidOperationException("正在投屏中");
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _casting = true;

            try
            {
                _liveServer = new ScreenLiveServer(_isEnableAudio);
                string streamUrl = _liveServer.Start();
                Debug.WriteLine($"[TVCast] Live Stream: {streamUrl}");

                string metadata = DLNAMetaBuilder.BuildLiveDidlLite(streamUrl);
                await PlayOnDeviceAsync(streamUrl, metadata);

                return true;
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return false;
            }
        }

        /// <summary>
        /// 启动本地文件投屏
        /// </summary>
        public async Task<bool> StartCastingAsync(DLNADevice device, string filePath)
        {
            if (_casting) throw new InvalidOperationException("正在投屏中");
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (!File.Exists(filePath)) throw new FileNotFoundException("文件不存在", filePath);

            _device = device;
            _casting = true;

            try
            {
                _fileServer = new FastSocketServer(filePath);
                string mediaUrl = _fileServer.Start();
                Debug.WriteLine($"[TVCast] File Stream: {mediaUrl}");

                string metadata = DLNAMetaBuilder.BuildDidlLite(mediaUrl, filePath);
                await PlayOnDeviceAsync(mediaUrl, metadata);

                return true;
            }
            catch (Exception ex)
            {
                HandleError(ex);
                return false;
            }
        }

        private async Task PlayOnDeviceAsync(string streamUrl, string metadata)
        {
            var service = _device.GetService(AV_TRANSPORT) ?? throw new Exception("设备不支持 AVTransport 服务");
            string controlUrl = DLNAUrlHelper.Combine(_device.Location, service.ControlURL);

            await _commandSender.SetAVTransportURIAsync(controlUrl, streamUrl, metadata);
            CastingStarted?.Invoke(this, new CastingStartedEventArgs(streamUrl));

            await Task.Delay(500); // 等待电视解析 Header
            await _commandSender.PlayAsync(controlUrl);
        }

        public async Task<bool> StopCastingAsync()
        {
            if (!_casting || _device == null) return false;
            try
            {
                if (_device.HasService(AV_TRANSPORT))
                {
                    var svc = _device.GetService(AV_TRANSPORT);
                    string url = DLNAUrlHelper.Combine(_device.Location, svc.ControlURL);
                    await _commandSender.StopAsync(url);
                }
            }
            catch { /* 忽略网络停止错误 */ }

            StopInternal();
            return true;
        }

        private void StopInternal()
        {
            _casting = false;
            _fileServer?.Stop();
            _liveServer?.Stop();
            _fileServer = null;
            _liveServer = null;
            CastingStopped?.Invoke(this, new CastingStoppedEventArgs());
        }

        private void HandleError(Exception ex)
        {
            StopInternal();
            CastingError?.Invoke(this, new CastingErrorEventArgs(ex.Message));
        }

        public void Dispose()
        {
            StopInternal();
            _cts.Cancel();
            _cts.Dispose();
        }
    }
    #endregion

    #region Servers (File & Live)
    /// <summary>
    /// 高性能 Socket 文件服务器 (支持 Range 请求和 DLNA 协议头)
    /// </summary>
    public class FastSocketServer
    {
        private readonly string _filePath;
        private TcpListener _listener;
        private bool _isRunning;

        public FastSocketServer(string filePath) => _filePath = filePath;

        public string Start()
        {
            string ip = NetworkHelper.GetBestLocalIPAddress();
            int port = NetworkHelper.GetFreePort();

            _listener = new TcpListener(IPAddress.Parse(ip), port);
            _listener.Start(50);
            _isRunning = true;

            Task.Run(AcceptLoopAsync);

            // URL 必须包含文件名，部分电视依赖后缀判断格式
            return $"http://{ip}:{port}/{Uri.EscapeDataString(Path.GetFileName(_filePath))}";
        }

        public void Stop()
        {
            _isRunning = false;
            try { _listener?.Stop(); } catch { }
        }

        private async Task AcceptLoopAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    ConfigureClient(client);
                    _ = Task.Run(() => HandleClient(client));
                }
                catch { if (!_isRunning) break; }
            }
        }

        private void ConfigureClient(TcpClient client)
        {
            client.NoDelay = true; // 关键：禁用 Nagle 算法以提升流式传输性能
            client.SendBufferSize = 64 * 1024;
            client.ReceiveBufferSize = 8192;
            client.SendTimeout = 10000;
        }

        private void HandleClient(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) return;

                    string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    if (request.StartsWith("HEAD")) return; // 电视探测请求

                    long fileLength = new FileInfo(_filePath).Length;
                    ParseRange(request, fileLength, out long start, out long end);

                    SendHeaders(stream, _filePath, start, end, fileLength);
                    SendFileContent(stream, _filePath, start, end - start + 1, client);
                }
                catch (Exception ex) { Debug.WriteLine($"Server Error: {ex.Message}"); }
            }
        }

        private void SendHeaders(NetworkStream stream, string path, long start, long end, long total)
        {
            string mime = MimeTypeHelper.Get(path);
            var sb = new StringBuilder();

            bool isPartial = start > 0 || end < total - 1;
            sb.Append(isPartial ? "HTTP/1.1 206 Partial Content\r\n" : "HTTP/1.1 200 OK\r\n");

            if (isPartial) sb.Append($"Content-Range: bytes {start}-{end}/{total}\r\n");

            sb.Append($"Content-Length: {end - start + 1}\r\n");
            sb.Append($"Content-Type: {mime}\r\n");
            sb.Append("Connection: keep-alive\r\n");
            sb.Append("Accept-Ranges: bytes\r\n");
            // DLNA 专用头
            sb.Append("transferMode.dlna.org: Streaming\r\n");
            sb.Append($"contentFeatures.dlna.org: DLNA.ORG_PN={DLNAMetaBuilder.GetProfile(mime)};DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01700000000000000000000000000000\r\n");
            sb.Append("\r\n");

            byte[] headerBytes = Encoding.ASCII.GetBytes(sb.ToString());
            stream.Write(headerBytes, 0, headerBytes.Length);
        }

        private void SendFileContent(NetworkStream stream, string path, long start, long length, TcpClient client)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (start > 0) fs.Seek(start, SeekOrigin.Begin);
                byte[] buffer = new byte[64 * 1024];
                long remaining = length;

                while (remaining > 0 && client.Connected)
                {
                    int toRead = (int)Math.Min(buffer.Length, remaining);
                    int read = fs.Read(buffer, 0, toRead);
                    if (read == 0) break;

                    try
                    {
                        stream.Write(buffer, 0, read);
                        remaining -= read;
                    }
                    catch { break; }
                }
            }
        }

        private void ParseRange(string req, long total, out long start, out long end)
        {
            start = 0; end = total - 1;
            var match = Regex.Match(req, @"Range: bytes=(\d+)-(\d*)");
            if (match.Success)
            {
                start = long.Parse(match.Groups[1].Value);
                if (!string.IsNullOrEmpty(match.Groups[2].Value))
                    end = long.Parse(match.Groups[2].Value);
            }
        }
    }

    /// <summary>
    /// 屏幕直播服务器 (Screen -> FFmpeg -> HTTP Stream)
    /// </summary>
    public class ScreenLiveServer : IDisposable
    {
        private TcpListener _listener;
        private bool _isRunning;
        private Process _ffmpegProcess;
        private readonly object _ffmpegLock = new object();
        private CancellationTokenSource _streamingCts;

        // 采集组件
        private IScreenCapturer _videoCapturer;
        private AudioLoopbackCapturer _audioCapturer;
        private readonly string _videoPipeName = "tvcast_v_" + Guid.NewGuid().ToString("N");
        private readonly string _audioPipeName = "tvcast_a_" + Guid.NewGuid().ToString("N");

        public ScreenLiveServer(bool enableAudio)
        {
            if (enableAudio) _audioCapturer = new AudioLoopbackCapturer();
        }

        public string Start()
        {
            string ip = NetworkHelper.GetBestLocalIPAddress();
            int port = NetworkHelper.GetFreePort();

            _listener = new TcpListener(IPAddress.Parse(ip), port);
            _listener.Start();
            _isRunning = true;

            Task.Run(AcceptLoopAsync);
            return $"http://{ip}:{port}/live.ts";
        }

        public void Stop()
        {
            _isRunning = false;
            _streamingCts?.Cancel();
            ResetPipeline();
            try { _listener?.Stop(); } catch { }
        }

        private void ResetPipeline()
        {
            lock (_ffmpegLock)
            {
                _videoCapturer?.Stop();
                _audioCapturer?.Stop();
                if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
                {
                    try { _ffmpegProcess.Kill(); _ffmpegProcess.WaitForExit(1000); } catch { }
                }
                _ffmpegProcess = null;
            }
        }

        private async Task AcceptLoopAsync()
        {
            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();

                    // 低延迟 TCP 配置
                    client.NoDelay = true; // 禁用 Nagle 算法
                    client.SendBufferSize = 32 * 1024; // 降低发送缓冲区以减少延迟
                    client.ReceiveBufferSize = 8 * 1024; // 接收缓冲区可以较小
                    client.SendTimeout = 5000;
                    client.LingerState = new System.Net.Sockets.LingerOption(false, 0);

                    // 每次新连接，重置推流管道
                    ResetPipeline();
                    _streamingCts = new CancellationTokenSource();

                    _ = Task.Run(() => HandleClient(client, _streamingCts.Token));
                }
                catch { if (!_isRunning) break; }
            }
        }

        private void HandleClient(TcpClient client, CancellationToken token)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    if (stream.Read(buffer, 0, buffer.Length) == 0) return;

                    // 发送 TS 流头部 - 优化低延迟参数
                    string headers = "HTTP/1.1 200 OK\r\n" +
                                     "Content-Type: video/mpeg\r\n" +
                                     "Connection: close\r\n" +
                                     "Cache-Control: no-cache, no-store, must-revalidate\r\n" +
                                     "Pragma: no-cache\r\n" +
                                     "Expires: 0\r\n" +
                                     "X-Content-Duration: 0\r\n" +
                                     "transferMode.dlna.org: Streaming\r\n" +
                                     "contentFeatures.dlna.org: DLNA.ORG_PN=MPEG_TS_SD_EU_ISO;DLNA.ORG_OP=01;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=01700000000000000000000000000000\r\n\r\n";
                    byte[] hBytes = Encoding.ASCII.GetBytes(headers);
                    stream.Write(hBytes, 0, hBytes.Length);
                    stream.Flush(); // 立即发送响应头

                    StartFFmpegPipeline(stream);
                }
                catch (Exception ex) { Debug.WriteLine($"Stream Error: {ex.Message}"); }
                finally { ResetPipeline(); }
            }
        }

        private void StartFFmpegPipeline(Stream clientStream)
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;

            // 自适应分辨率：高分辨率降级到 720p 以减少编码和网络负担
            int captureWidth, captureHeight;
            if (bounds.Width > 1920 || bounds.Height > 1080)
            {
                // 4K/2K 降到 720p
                captureWidth = 1280;
                captureHeight = 720;
                Debug.WriteLine($"[TVCast] Downscaling from {bounds.Width}x{bounds.Height} to 720p for better performance");
            }
            else if (bounds.Width > 1280)
            {
                // 1080p 降到 720p
                captureWidth = 1280;
                captureHeight = 720;
                Debug.WriteLine($"[TVCast] Downscaling from {bounds.Width}x{bounds.Height} to 720p");
            }
            else
            {
                // 720p 及以下保持原始分辨率
                captureWidth = bounds.Width % 2 == 0 ? bounds.Width : bounds.Width - 1;
                captureHeight = bounds.Height % 2 == 0 ? bounds.Height : bounds.Height - 1;
            }

            using (var vServer = new NamedPipeServerStream(_videoPipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (var aServer = _audioCapturer != null ? new NamedPipeServerStream(_audioPipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous) : null)
            {
                // 启动 FFmpeg
                var ffmpegArgs = HardwareCapabilityDetector.BuildFFmpegArgs(
                    $@"\\.\pipe\{_videoPipeName}",
                    $@"\\.\pipe\{_audioPipeName}",
                    captureWidth, captureHeight, _audioCapturer != null);

                Debug.WriteLine($"[TVCast] Starting FFmpeg {captureWidth}x{captureHeight} with audio: {_audioCapturer != null}");
                Debug.WriteLine($"[TVCast] FFmpeg args: {ffmpegArgs}");

                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = ffmpegArgs,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true, // 捕获日志
                        CreateNoWindow = true
                    }
                };

                if (!proc.Start()) return;
                proc.BeginErrorReadLine(); // 防止 stderr 填满缓冲区导致挂起

                lock (_ffmpegLock) _ffmpegProcess = proc;

                // 等待管道连接 (超时保护)
                var vTask = Task.Factory.FromAsync(vServer.BeginWaitForConnection, vServer.EndWaitForConnection, null);
                if (!vTask.Wait(3000))
                {
                    Debug.WriteLine("[TVCast] Video pipe connection timeout");
                    proc.Kill();
                    return;
                }

                if (aServer != null)
                {
                    var aTask = Task.Factory.FromAsync(aServer.BeginWaitForConnection, aServer.EndWaitForConnection, null);
                    if (!aTask.Wait(1000))
                    {
                        Debug.WriteLine("[TVCast] Audio pipe connection timeout (non-critical)");
                    }
                }

                // 启动采集器（直接以目标分辨率采集）
                try
                {
                    _videoCapturer = new DirectXScreenCapturer();
                    _videoCapturer.Start(captureWidth, captureHeight, vServer);
                    Debug.WriteLine("[TVCast] DirectX screen capturer started successfully");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TVCast] DirectX failed: {ex.Message}, switching to GDI.");
                    try
                    {
                        _videoCapturer = new GdiScreenCapturer();
                        _videoCapturer.Start(captureWidth, captureHeight, vServer);
                        Debug.WriteLine("[TVCast] GDI screen capturer started successfully");
                    }
                    catch (Exception gdiEx)
                    {
                        Debug.WriteLine($"[TVCast] GDI fallback also failed: {gdiEx.Message}");
                        proc.Kill();
                        throw;
                    }
                }

                // 启动音频采集器 (如果启用)
                if (_audioCapturer != null)
                {
                    try
                    {
                        _audioCapturer.Start(aServer);
                        Debug.WriteLine("[TVCast] Audio capturer started successfully");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[TVCast] Audio capture failed (non-critical): {ex.Message}");
                        // 音频失败不中断投屏,继续视频传输
                    }
                }

                // 将 FFmpeg 输出转发给 TCP 客户端
                try { proc.StandardOutput.BaseStream.CopyTo(clientStream); } catch { }
            }
        }

        public void Dispose() => Stop();
    }
    #endregion

    #region Capture Components
    public interface IScreenCapturer
    {
        void Start(int width, int height, Stream outputStream);
        void Stop();
    }

    /// <summary>
    /// DirectX (DXGI) 桌面采集器 - 高性能
    /// </summary>
    public class DirectXScreenCapturer : IScreenCapturer
    {
        private Thread _thread;
        private volatile bool _running;
        private Stream _outputStream;
        private Exception _threadException;

        public void Start(int width, int height, Stream outputStream)
        {
            _outputStream = outputStream;
            _threadException = null;

            // 同步验证 DirectX 是否可用（在主线程中抛出异常）
            var (adapter, output) = FindWorkingAdapter();
            if (adapter == -1)
            {
                throw new NotSupportedException("No DXGI adapter found.");
            }

            // 尝试同步创建设备以确保真正可用
            try
            {
                TestDeviceCreation(adapter, output);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DirectX] Device test failed: {ex.Message}");
                throw new NotSupportedException("DirectX device creation failed", ex);
            }

            _running = true;
            _thread = new Thread(() => CaptureLoop(outputStream, width, height, adapter, output))
            {
                Priority = ThreadPriority.AboveNormal,
                IsBackground = true // 标记为后台线程，避免阻止进程退出
            };
            _thread.Start();

            // 等待一小段时间检查线程是否因异常立即退出
            Thread.Sleep(50);
            if (_threadException != null)
            {
                throw new NotSupportedException("DirectX capture failed to start", _threadException);
            }
        }

        public void Stop()
        {
            _running = false;

            // 关闭流以解除阻塞的写入操作
            try { _outputStream?.Close(); } catch { }

            // 等待线程结束，增加超时时间
            if (_thread != null && _thread.IsAlive)
            {
                if (!_thread.Join(500))
                {
                    Debug.WriteLine("[DirectX] Thread did not exit gracefully, aborting");
                    try { _thread.Abort(); } catch { }
                }
            }
        }

        private void TestDeviceCreation(int adapterIdx, int outputIdx)
        {
            var featureLevels = new[]
            {
                SharpDX.Direct3D.FeatureLevel.Level_11_0,
                SharpDX.Direct3D.FeatureLevel.Level_10_1,
                SharpDX.Direct3D.FeatureLevel.Level_10_0,
                SharpDX.Direct3D.FeatureLevel.Level_9_3
            };

            using (var factory = new Factory1())
            using (var adapter = factory.GetAdapter1(adapterIdx))
            using (var device = new SharpDX.Direct3D11.Device(adapter,
                SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport,
                featureLevels))
            using (var output = adapter.GetOutput(outputIdx))
            using (var output1 = output.QueryInterface<Output1>())
            using (var dup = output1.DuplicateOutput(device))
            {
                // 测试成功
            }
        }

        private (int, int) FindWorkingAdapter()
        {
            // 定义支持的 Feature Levels,从高到低尝试
            var featureLevels = new[]
            {
                SharpDX.Direct3D.FeatureLevel.Level_11_0,
                SharpDX.Direct3D.FeatureLevel.Level_10_1,
                SharpDX.Direct3D.FeatureLevel.Level_10_0,
                SharpDX.Direct3D.FeatureLevel.Level_9_3
            };

            using (var factory = new Factory1())
            {
                for (int i = 0; i < factory.Adapters1.Length; i++)
                {
                    using (var a = factory.GetAdapter1(i))
                    {
                        // 跳过软件渲染器
                        if (a.Description.Description.Contains("Basic Render") ||
                            a.Description.Description.Contains("Microsoft Basic"))
                            continue;

                        for (int j = 0; j < a.GetOutputCount(); j++)
                        {
                            try
                            {
                                // 使用 DriverType.Unknown 并指定 Adapter + Feature Levels
                                using (var d = new SharpDX.Direct3D11.Device(a,
                                    SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport,
                                    featureLevels))
                                using (var o = a.GetOutput(j))
                                using (var o1 = o.QueryInterface<Output1>())
                                using (var dup = o1.DuplicateOutput(d))
                                {
                                    Debug.WriteLine($"[DirectX] Using Adapter {i}, Output {j}: {a.Description.Description}");
                                    return (i, j);
                                }
                            }
                            catch (SharpDXException ex)
                            {
                                Debug.WriteLine($"[DirectX] Adapter {i} Output {j} failed: {ex.ResultCode} - {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[DirectX] Adapter {i} Output {j} error: {ex.GetType().Name}");
                            }
                        }
                    }
                }
            }
            Debug.WriteLine("[DirectX] No compatible DXGI adapter found, will use GDI fallback.");
            return (-1, -1);
        }

        private void CaptureLoop(Stream stream, int w, int h, int adapterIdx, int outputIdx)
        {
            var featureLevels = new[]
            {
                SharpDX.Direct3D.FeatureLevel.Level_11_0,
                SharpDX.Direct3D.FeatureLevel.Level_10_1,
                SharpDX.Direct3D.FeatureLevel.Level_10_0,
                SharpDX.Direct3D.FeatureLevel.Level_9_3
            };

            try
            {
                using (var factory = new Factory1())
                using (var adapter = factory.GetAdapter1(adapterIdx))
                using (var device = new SharpDX.Direct3D11.Device(adapter,
                    SharpDX.Direct3D11.DeviceCreationFlags.BgraSupport,
                    featureLevels))
                using (var output = adapter.GetOutput(outputIdx))
                using (var output1 = output.QueryInterface<Output1>())
                using (var duplicatedOutput = output1.DuplicateOutput(device))
                {
                    Texture2D stagingTexture = null;
                    byte[] lineBuff = new byte[w * 4];

                    while (_running)
                    {
                        try
                        {
                            var res = duplicatedOutput.TryAcquireNextFrame(20, out _, out var screenRes);
                            if (res.Success)
                            {
                                using (var screenTex = screenRes.QueryInterface<Texture2D>())
                                {
                                    if (stagingTexture == null)
                                    {
                                        var desc = screenTex.Description;
                                        desc.CpuAccessFlags = CpuAccessFlags.Read;
                                        desc.Usage = ResourceUsage.Staging;
                                        desc.BindFlags = BindFlags.None;
                                        desc.OptionFlags = ResourceOptionFlags.None;
                                        stagingTexture = new Texture2D(device, desc);
                                    }
                                    device.ImmediateContext.CopyResource(screenTex, stagingTexture);
                                }
                                screenRes.Dispose();
                                duplicatedOutput.ReleaseFrame();

                                var map = device.ImmediateContext.MapSubresource(stagingTexture, 0, MapMode.Read, MapFlags.None);
                                try
                                {
                                    if (!stream.CanWrite) break;
                                    IntPtr ptr = map.DataPointer;
                                    for (int y = 0; y < h; y++)
                                    {
                                        Marshal.Copy(ptr, lineBuff, 0, lineBuff.Length);
                                        stream.Write(lineBuff, 0, lineBuff.Length);
                                        ptr = IntPtr.Add(ptr, map.RowPitch);
                                    }
                                }
                                finally { device.ImmediateContext.UnmapSubresource(stagingTexture, 0); }
                            }
                        }
                        catch (SharpDXException e)
                        {
                            if (e.ResultCode == SharpDX.DXGI.ResultCode.AccessLost)
                            {
                                Debug.WriteLine("[DirectX] AccessLost - GPU reset detected");
                                break;
                            }
                        }
                        catch (IOException)
                        {
                            // 流已关闭，正常退出
                            break;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[DirectX] Frame capture error: {ex.Message}");
                            break;
                        }
                    }
                    stagingTexture?.Dispose();
                }
            }
            catch (Exception ex)
            {
                _threadException = ex;
                Debug.WriteLine($"[DirectX] CaptureLoop failed: {ex.GetType().Name} - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// GDI 桌面采集器 - 兼容性回退方案
    /// </summary>
    public class GdiScreenCapturer : IScreenCapturer
    {
        private Thread _thread;
        private volatile bool _running;
        private Stream _outputStream;

        public void Start(int width, int height, Stream outputStream)
        {
            _outputStream = outputStream;
            _running = true;
            _thread = new Thread(() => CaptureLoop(outputStream, width, height))
            {
                IsBackground = true
            };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;

            // 关闭流以解除阻塞的写入操作
            try { _outputStream?.Close(); } catch { }

            // 等待线程结束
            if (_thread != null && _thread.IsAlive)
            {
                if (!_thread.Join(500))
                {
                    Debug.WriteLine("[GDI] Thread did not exit gracefully, aborting");
                    try { _thread.Abort(); } catch { }
                }
            }
        }

        private void CaptureLoop(Stream stream, int targetW, int targetH)
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            bool needsScaling = (targetW != bounds.Width || targetH != bounds.Height);

            byte[] buffer = new byte[targetW * targetH * 4];
            var interval = TimeSpan.FromMilliseconds(33); // 30 FPS

            Debug.WriteLine($"[GDI] Capturing at {bounds.Width}x{bounds.Height}, output {targetW}x{targetH}, scaling: {needsScaling}");

            while (_running && stream.CanWrite)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    if (needsScaling)
                    {
                        // 需要缩放：先捕获全屏，再缩放
                        using (var fullBmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb))
                        using (var scaledBmp = new Bitmap(targetW, targetH, PixelFormat.Format32bppPArgb))
                        {
                            // 捕获全屏
                            using (var g1 = Graphics.FromImage(fullBmp))
                            {
                                g1.CopyFromScreen(0, 0, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                            }

                            // 缩放到目标大小
                            using (var g2 = Graphics.FromImage(scaledBmp))
                            {
                                g2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                                g2.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                                g2.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                                g2.DrawImage(fullBmp, 0, 0, targetW, targetH);
                            }

                            var data = scaledBmp.LockBits(new Rectangle(0, 0, targetW, targetH), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                            scaledBmp.UnlockBits(data);
                        }
                    }
                    else
                    {
                        // 不需要缩放：直接捕获
                        using (var bmp = new Bitmap(targetW, targetH, PixelFormat.Format32bppPArgb))
                        {
                            using (var g = Graphics.FromImage(bmp))
                            {
                                g.CopyFromScreen(0, 0, 0, 0, new Size(targetW, targetH), CopyPixelOperation.SourceCopy);
                            }

                            var data = bmp.LockBits(new Rectangle(0, 0, targetW, targetH), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                            bmp.UnlockBits(data);
                        }
                    }

                    stream.Write(buffer, 0, buffer.Length);
                }
                catch (IOException)
                {
                    // 流已关闭，正常退出
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[GDI] Capture error: {ex.Message}");
                    break;
                }

                if (sw.Elapsed < interval)
                {
                    try { Thread.Sleep(interval - sw.Elapsed); }
                    catch (ThreadAbortException) { break; }
                }
            }
        }
    }

    /// <summary>
    /// WASAPI 音频环回采集
    /// </summary>
    public class AudioLoopbackCapturer
    {
        private WasapiLoopbackCapture _capture;
        private Stream _output;
        private volatile bool _running;

        public void Start(Stream output)
        {
            _output = output;
            _running = true;

            try
            {
                _capture = new WasapiLoopbackCapture();
                _capture.DataAvailable += OnDataAvailable;
                _capture.RecordingStopped += (s, e) =>
                {
                    if (e.Exception != null)
                    {
                        Debug.WriteLine($"[Audio] Recording stopped with error: {e.Exception.Message}");
                    }
                };
                _capture.StartRecording();
                Debug.WriteLine("[Audio] WASAPI loopback capture started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Audio] Failed to start capture: {ex.Message}");
                throw;
            }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!_running || _output == null || !_output.CanWrite) return;

            try
            {
                // 将 Float32 转换为 PCM16 以匹配 FFmpeg 输入
                byte[] pcm16 = ConvertFloat32ToPcm16(e.Buffer, e.BytesRecorded);
                _output.Write(pcm16, 0, pcm16.Length);
            }
            catch (IOException)
            {
                // 流已关闭，正常停止
                _running = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Audio] Write error: {ex.Message}");
                _running = false;
            }
        }

        private byte[] ConvertFloat32ToPcm16(byte[] buffer, int bytes)
        {
            byte[] output = new byte[bytes / 2];
            int outIdx = 0;
            for (int i = 0; i < bytes; i += 4)
            {
                float sample = Math.Min(1.0f, Math.Max(-1.0f, BitConverter.ToSingle(buffer, i)));
                short val = (short)(sample * 32767);
                output[outIdx++] = (byte)(val & 0xFF);
                output[outIdx++] = (byte)((val >> 8) & 0xFF);
            }
            return output;
        }

        public void Stop()
        {
            _running = false;

            try
            {
                if (_capture != null)
                {
                    _capture.StopRecording();
                    _capture.Dispose();
                    _capture = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Audio] Error stopping capture: {ex.Message}");
            }

            try { _output?.Close(); } catch { }
            _output = null;
        }
    }
    #endregion

    #region Helpers & Utils
    public static class HardwareCapabilityDetector
    {
        private static bool? _nvencSupported;

        public static bool IsNVENCSupported()
        {
            if (_nvencSupported.HasValue) return _nvencSupported.Value;
            try
            {
                var psi = new ProcessStartInfo("ffmpeg", "-hide_banner -encoders") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using (var p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    _nvencSupported = output.Contains("h264_nvenc");
                }
            }
            catch { _nvencSupported = false; }
            return _nvencSupported.Value;
        }

        public static string BuildFFmpegArgs(string vPipe, string aPipe, int w, int h, bool hasAudio)
        {
            // 输入参数：降低延迟的关键配置
            string vInput = $"-probesize 32 -analyzeduration 0 -fflags nobuffer " +
                           $"-f rawvideo -pixel_format bgra -video_size {w}x{h} -framerate 30 -i \"{vPipe}\"";

            string aInput = hasAudio
                ? $"-probesize 32 -analyzeduration 0 -f s16le -ac 2 -ar 48000 -i \"{aPipe}\""
                : "";

            // 视频编码：极致低延迟优化
            string enc;
            if (IsNVENCSupported())
            {
                // NVIDIA 硬件编码：降低缓冲区和比特率
                enc = "-c:v h264_nvenc -preset p1 -tune ll -rc cbr -b:v 2000k -maxrate 2000k -bufsize 500k " +
                      "-zerolatency 1 -delay 0 -g 30 -bf 0 -forced-idr 1";
            }
            else
            {
                // CPU 编码：超低延迟配置
                enc = "-c:v libx264 -preset ultrafast -tune zerolatency -pix_fmt yuv420p " +
                      "-b:v 1500k -maxrate 1500k -bufsize 300k -g 30 -bf 0 -x264opts no-scenecut:rc-lookahead=0";
            }

            // 音频编码：低延迟 AAC
            string aEnc = hasAudio ? "-c:a aac -b:a 96k -ac 2 -ar 48000" : "";

            // 输出参数：MPEG-TS 低延迟封装
            string output = "-f mpegts -mpegts_flags +initial_discontinuity " +
                           "-flush_packets 1 -fflags +flush_packets+nobuffer " +
                           "-max_delay 0 -muxdelay 0.001 -";

            return $"-hide_banner -loglevel error {vInput} {aInput} " +
                   $"-map 0:v {(hasAudio ? "-map 1:a" : "")} " +
                   $"{enc} {aEnc} {output}";
        }
    }

    public static class NetworkHelper
    {
        public static string GetBestLocalIPAddress()
        {
            var ips = Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                .Select(x => x.ToString()).ToList();
            return ips.FirstOrDefault(i => i.StartsWith("192.168.")) ?? ips.FirstOrDefault() ?? "127.0.0.1";
        }

        public static int GetFreePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int p = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return p;
        }
    }

    public class DLNACommandSender
    {
        private readonly HttpClient _client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        public async Task SetAVTransportURIAsync(string url, string mediaUri, string metadata) =>
            await SendSoapAsync(url, "SetAVTransportURI",
                $"<InstanceID>0</InstanceID><CurrentURI>{mediaUri}</CurrentURI><CurrentURIMetaData>{metadata}</CurrentURIMetaData>");

        public async Task PlayAsync(string url) =>
            await SendSoapAsync(url, "Play", "<InstanceID>0</InstanceID><Speed>1</Speed>");

        public async Task StopAsync(string url) =>
            await SendSoapAsync(url, "Stop", "<InstanceID>0</InstanceID>");

        private async Task SendSoapAsync(string url, string action, string bodyInner)
        {
            string envelope = $"<?xml version=\"1.0\" encoding=\"utf-8\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\"><s:Body><u:{action} xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\">{bodyInner}</u:{action}></s:Body></s:Envelope>";
            var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
            content.Headers.TryAddWithoutValidation("SOAPAction", $"\"urn:schemas-upnp-org:service:AVTransport:1#{action}\"");
            await _client.PostAsync(url, content);
        }
    }

    public static class DLNAMetaBuilder
    {
        public static string BuildDidlLite(string url, string path)
        {
            string mime = MimeTypeHelper.Get(path);
            string cls = mime.StartsWith("audio") ? "object.item.audioItem" :
                         mime.StartsWith("image") ? "object.item.imageItem" : "object.item.videoItem";
            // 酷开/Skyworth 兼容性: protocolInfo 使用通配符
            string proto = $"http-get:*:{mime}:DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01700000000000000000000000000000";
            return BuildXml(Escape(Path.GetFileName(path)), cls, proto, Escape(url));
        }

        public static string BuildLiveDidlLite(string url)
        {
            string proto = "http-get:*:video/mpeg:DLNA.ORG_PN=MPEG_TS_SD_EU_ISO;DLNA.ORG_OP=10;DLNA.ORG_CI=1";
            return BuildXml("Windows Live", "object.item.videoItem.videoBroadcast", proto, Escape(url));
        }

        private static string BuildXml(string title, string cls, string proto, string url) =>
            $"<DIDL-Lite xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\"><item id=\"0\" parentID=\"0\" restricted=\"1\"><dc:title>{title}</dc:title><upnp:class>{cls}</upnp:class><res protocolInfo=\"{proto}\">{url}</res></item></DIDL-Lite>";

        public static string GetProfile(string mime) => mime.Contains("mp4") ? "AVC_MP4_MP_SD_AAC_MULT5" : "*";
        private static string Escape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }

    public static class DLNAUrlHelper
    {
        public static string Combine(Uri baseUri, string path) => path.StartsWith("http") ? path : $"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}{(path.StartsWith("/") ? "" : "/")}{path}";
    }

    public static class MimeTypeHelper
    {
        public static string Get(string path)
        {
            string extension = Path.GetExtension(path).ToLower();
            switch (extension)
            {
                case ".mp4":
                    return "video/mp4";
                case ".mkv":
                    return "video/x-matroska";
                case ".avi":
                    return "video/x-msvideo";
                case ".mp3":
                    return "audio/mpeg";
                case ".jpg":
                    return "image/jpeg";
                default:
                    return "application/octet-stream";
            }
        }
    }

    public class CastingStartedEventArgs : EventArgs { public string Url { get; } public CastingStartedEventArgs(string u) => Url = u; }
    public class CastingStoppedEventArgs : EventArgs { }
    public class CastingErrorEventArgs : EventArgs { public string Message { get; } public CastingErrorEventArgs(string m) => Message = m; }
    #endregion
}