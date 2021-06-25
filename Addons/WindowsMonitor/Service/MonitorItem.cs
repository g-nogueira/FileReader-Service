using IOTLinkAPI.Platform.HomeAssistant;

namespace IOTLinkAddon.Service
{
    class MonitorItem
    {
        public string Topic { get; set; }

        public string ConfigKey { get; set; }

        public MonitorItemType Type { get; set; }

        public object Value { get; set; }

        public HassDiscoveryOptions DiscoveryOptions { get; set; }
    }
}
