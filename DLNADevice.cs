using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TVCast
{
    #region Data Models

    public class DLNADevice
    {
        public string FriendlyName { get; set; }
        public string DeviceType { get; set; }
        public string Manufacturer { get; set; }
        public string ModelName { get; set; }
        public string UUID { get; set; }
        public string PresentationURL { get; set; }
        public Uri Location { get; set; }
        public XmlDocument DeviceDescriptionXml { get; set; }

        public Dictionary<string, DLNAService> Services { get; } = new Dictionary<string, DLNAService>();

        public override string ToString() => $"{FriendlyName} ({Manufacturer} {ModelName})";

        public bool HasService(string serviceType)
        {
            return !string.IsNullOrEmpty(serviceType) && Services.ContainsKey(serviceType);
        }

        public DLNAService GetService(string serviceType)
        {
            return Services.TryGetValue(serviceType, out var svc) ? svc : null;
        }
    }

    public class DLNAService
    {
        public string ServiceType { get; set; }
        public string ServiceId { get; set; }
        public string ControlURL { get; set; }
        public string EventSubURL { get; set; }
        public string SCPDURL { get; set; }
    }

    #endregion

    #region Event Args

    public class DeviceDiscoveredEventArgs : EventArgs
    {
        public DLNADevice Device { get; }
        public DeviceDiscoveredEventArgs(DLNADevice device) => Device = device;
    }

    public class SearchCompleteEventArgs : EventArgs
    {
        public List<DLNADevice> Devices { get; }
        public bool IsSuccessful { get; }

        public SearchCompleteEventArgs(List<DLNADevice> devices, bool isSuccessful)
        {
            Devices = devices;
            IsSuccessful = isSuccessful;
        }
    }

    #endregion

    #region Device Finder Logic

    public class DLNADeviceFinder : IDisposable
    {
        // SSDP 标准多播地址和端口
        private const string SSDP_ADDRESS = "239.255.255.250";
        private const int SSDP_PORT = 1900;
        private const string SEARCH_TARGET = "upnp:rootdevice";

        private const string MSEARCH_TEMPLATE =
            "M-SEARCH * HTTP/1.1\r\n" +
            "HOST: 239.255.255.250:1900\r\n" +
            "MAN: \"ssdp:discover\"\r\n" +
            "MX: 2\r\n" +
            "ST: {0}\r\n\r\n";

        private readonly List<DLNADevice> _devices = new List<DLNADevice>();
        private UdpClient _udp;
        private bool _running;
        private CancellationTokenSource _cts;

        public event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered;
        public event EventHandler<SearchCompleteEventArgs> SearchComplete;

        public List<DLNADevice> Devices => _devices;

        public async Task StartSearchAsync(TimeSpan? timeout = null)
        {
            if (_running) return;

            _running = true;
            _devices.Clear();
            _cts = new CancellationTokenSource();

            var searchDuration = timeout ?? TimeSpan.FromSeconds(2);

            try
            {
                InitializeUdpClient();
                await SendSearchPacketAsync();
                await ReceiveLoopAsync(searchDuration);
            }
            catch (Exception)
            {
                // 忽略网络错误，保证搜索流程不崩溃
            }
            finally
            {
                StopSearch();
                SearchComplete?.Invoke(this, new SearchCompleteEventArgs(new List<DLNADevice>(_devices), true));
            }
        }

        public void StopSearch()
        {
            _running = false;
            _cts?.Cancel();
            try { _udp?.Close(); } catch { }
            _udp = null;
        }

        private void InitializeUdpClient()
        {
            _udp = new UdpClient(AddressFamily.InterNetwork);
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, SSDP_PORT));
            _udp.JoinMulticastGroup(IPAddress.Parse(SSDP_ADDRESS));
            _udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
        }

        private async Task SendSearchPacketAsync()
        {
            var msg = string.Format(MSEARCH_TEMPLATE, SEARCH_TARGET);
            var data = Encoding.UTF8.GetBytes(msg);
            var target = new IPEndPoint(IPAddress.Parse(SSDP_ADDRESS), SSDP_PORT);

            if (_udp != null)
                await _udp.SendAsync(data, data.Length, target);
        }

        private async Task ReceiveLoopAsync(TimeSpan timeout)
        {
            var endTime = DateTime.UtcNow.Add(timeout);

            while (_running && DateTime.UtcNow < endTime)
            {
                // 使用 WhenAny 实现带超时的接收，因为 UdpClient.ReceiveAsync 不直接支持 CancellationToken
                var receiveTask = _udp.ReceiveAsync();
                var delayTask = Task.Delay(1000, _cts.Token);

                var completedTask = await Task.WhenAny(receiveTask, delayTask);
                if (completedTask == delayTask) continue;

                try
                {
                    var result = await receiveTask;
                    var responseText = Encoding.UTF8.GetString(result.Buffer);

                    // 异步解析，不阻塞接收循环
                    _ = ParseAndAddDeviceAsync(responseText);
                }
                catch { /* 忽略单个包解析错误 */ }
            }
        }

        private async Task ParseAndAddDeviceAsync(string rawResponse)
        {
            var device = await ParseResponseAsync(rawResponse);
            if (device != null)
            {
                lock (_devices)
                {
                    if (!_devices.Exists(d => d.UUID == device.UUID))
                    {
                        _devices.Add(device);
                        DeviceDiscovered?.Invoke(this, new DeviceDiscoveredEventArgs(device));
                    }
                }
            }
        }

        private async Task<DLNADevice> ParseResponseAsync(string raw)
        {
            var match = Regex.Match(raw, @"(?i)LOCATION:\s*(.+)");
            if (!match.Success) return null;

            if (!Uri.TryCreate(match.Groups[1].Value.Trim(), UriKind.Absolute, out var location))
                return null;

            var xmlDoc = await FetchDescriptionAsync(location);
            return xmlDoc != null ? ParseDeviceXml(xmlDoc, location) : null;
        }

        private async Task<XmlDocument> FetchDescriptionAsync(Uri url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "TVCast-DLNA/1.0");
                    client.Timeout = TimeSpan.FromSeconds(3); // 快速超时避免阻塞
                    var resp = await client.GetAsync(url);
                    resp.EnsureSuccessStatusCode();

                    var xmlString = await resp.Content.ReadAsStringAsync();
                    var doc = new XmlDocument();
                    doc.LoadXml(xmlString);
                    return doc;
                }
            }
            catch
            {
                return null;
            }
        }

        private DLNADevice ParseDeviceXml(XmlDocument doc, Uri location)
        {
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("d", "urn:schemas-upnp-org:device-1-0");

            var deviceNode = doc.SelectSingleNode("//d:device", ns);
            if (deviceNode == null) return null;

            var dev = new DLNADevice
            {
                Location = location,
                DeviceDescriptionXml = doc,
                FriendlyName = GetValue(deviceNode, "d:friendlyName", ns),
                DeviceType = GetValue(deviceNode, "d:deviceType", ns),
                Manufacturer = GetValue(deviceNode, "d:manufacturer", ns),
                ModelName = GetValue(deviceNode, "d:modelName", ns),
                PresentationURL = GetValue(deviceNode, "d:presentationURL", ns)
            };

            var udn = GetValue(deviceNode, "d:UDN", ns);
            if (udn != null) dev.UUID = udn.Replace("uuid:", "");

            // 解析服务列表
            var services = doc.SelectNodes("//d:service", ns);
            if (services != null)
            {
                foreach (XmlNode svcNode in services)
                {
                    var s = new DLNAService
                    {
                        ServiceType = GetValue(svcNode, "d:serviceType", ns),
                        ServiceId = GetValue(svcNode, "d:serviceId", ns),
                        ControlURL = GetValue(svcNode, "d:controlURL", ns),
                        EventSubURL = GetValue(svcNode, "d:eventSubURL", ns),
                        SCPDURL = GetValue(svcNode, "d:SCPDURL", ns)
                    };

                    if (!string.IsNullOrEmpty(s.ServiceType))
                        dev.Services[s.ServiceType] = s;
                }
            }

            return dev;
        }

        private string GetValue(XmlNode parent, string xpath, XmlNamespaceManager ns)
        {
            return parent.SelectSingleNode(xpath, ns)?.InnerText?.Trim();
        }

        public void Dispose()
        {
            StopSearch();
            _udp?.Dispose();
            _cts?.Dispose();
        }
    }

    #endregion
}