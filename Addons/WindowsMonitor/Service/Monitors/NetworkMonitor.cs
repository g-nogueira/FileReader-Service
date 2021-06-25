using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.HomeAssistant;
using System.Collections.Generic;

namespace IOTLinkAddon.Service.Monitors
{
    class NetworkMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "NetworkInfo";

        // Store how much was transferred last time, initialized to prevent null reference exception
        private long[] _lastBytesSent = new long[0];
        private long[] _lastBytesReceived = new long[0];

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            List<NetworkInfo> networks = PlatformHelper.GetNetworkInfos();

            //Make sure the array for the lastBytes values are as big as numbers of networks
            //By being checked every time, we should be able to handle like a wifi dongle installed while running
            if (_lastBytesReceived.Length != networks.Count)
            {
                _lastBytesReceived = new long[networks.Count];
                _lastBytesSent = new long[networks.Count];
            }

            for (var i = 0; i < networks.Count; i++)
            {
                NetworkInfo networkInfo = networks[i];
                if (networkInfo == null)
                    continue; // Shouldn't happen, but...

                var bytesSentPerSecond = CalculateBytesPerSecond(networkInfo.BytesSent, ref _lastBytesSent[i], interval);
                var bytesReceivedPerSecond = CalculateBytesPerSecond(networkInfo.BytesReceived, ref _lastBytesReceived[i], interval);

                var topic = $"Stats/Network/{i}";

                // IPv4
                if (!string.IsNullOrWhiteSpace(networkInfo.IPv4Address))
                {
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_RAW,
                        Topic = topic + "/IPv4",
                        Value = networkInfo.IPv4Address,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Id = $"{i}_IPv4Adress",
                            Name = $"Network #{i} - IPv4",
                            Icon = "mdi:lan"
                        }
                    });
                }

                // IPv6
                if (!string.IsNullOrWhiteSpace(networkInfo.IPv6Address))
                {
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_RAW,
                        Topic = topic + "/IPv6",
                        Value = networkInfo.IPv6Address,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Id = $"{i}_IPv6Adress",
                            Name = $"Network #{i} - IPv6",
                            Icon = "mdi:lan"
                        }
                    });
                }

                // Network Speed
                result.Add(new MonitorItem
                {
                    ConfigKey = CONFIG_KEY,
                    Type = MonitorItemType.TYPE_RAW,
                    Topic = topic + "/Speed",
                    Value = networkInfo.Speed,
                    DiscoveryOptions = new HassDiscoveryOptions()
                    {
                        Component = HomeAssistantComponent.Sensor,
                        Id = $"{i}_Speed",
                        Name = $"Network #{i} - Speed",
                        Unit = "Mbps",
                        Icon = "mdi:speedometer"
                    }
                });

                // Network Wired
                result.Add(new MonitorItem
                {
                    ConfigKey = CONFIG_KEY,
                    Type = MonitorItemType.TYPE_RAW,
                    Topic = topic + "/Wired",
                    Value = networkInfo.Wired,
                    DiscoveryOptions = new HassDiscoveryOptions()
                    {
                        Component = HomeAssistantComponent.BinarySensor,
                        Id = $"{i}_Wired",
                        Name = $"Network #{i} - Wired",
                        PayloadOff = "False",
                        PayloadOn = "True"
                    }
                });

                // Network Bytes Sent
                result.Add(new MonitorItem
                {
                    ConfigKey = CONFIG_KEY,
                    Type = MonitorItemType.TYPE_NETWORK_SIZE,
                    Topic = topic + "/BytesSent",
                    Value = networkInfo.BytesSent,
                    DiscoveryOptions = new HassDiscoveryOptions()
                    {
                        Component = HomeAssistantComponent.Sensor,
                        Id = $"{i}_BytesSent",
                        Name = $"Network #{i} - Bytes Sent",
                        Unit = "B"
                    }
                });

                // Network Bytes Received
                result.Add(new MonitorItem
                {
                    ConfigKey = CONFIG_KEY,
                    Type = MonitorItemType.TYPE_NETWORK_SIZE,
                    Topic = topic + "/BytesReceived",
                    Value = networkInfo.BytesReceived,
                    DiscoveryOptions = new HassDiscoveryOptions()
                    {
                        Component = HomeAssistantComponent.Sensor,
                        Id = $"{i}_BytesReceived",
                        Name = $"Network #{i} - Bytes Received",
                        Unit = "B"
                    }
                });

                // Bytes Sent per second
                if (bytesSentPerSecond >= 0)
                {
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_NETWORK_SPEED,
                        Topic = topic + "/BytesSentPerSecond",
                        Value = bytesSentPerSecond,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Id = $"{i}_BytesSentPerSecond",
                            Name = $"Network #{i} - BPS Sent",
                            Unit = "Bps"
                        }
                    });
                }

                // Bytes Received per second
                if (bytesSentPerSecond >= 0)
                {
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_NETWORK_SPEED,
                        Topic = topic + "/BytesReceivedPerSecond",
                        Value = bytesReceivedPerSecond,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Id = $"{i}_BytesReceivedPerSecond",
                            Name = $"Network #{i} - BPS Received",
                            Unit = "Bps"
                        }
                    });
                }
            }
            return result;
        }

        private long CalculateBytesPerSecond(long bytesReceived, ref long lastBytes, int interval)
        {
            var bytesPerSecond = -1L;

            if (lastBytes != 0)
                bytesPerSecond = (bytesReceived - lastBytes) / interval;

            lastBytes = bytesReceived;
            return bytesPerSecond;
        }
    }
}
