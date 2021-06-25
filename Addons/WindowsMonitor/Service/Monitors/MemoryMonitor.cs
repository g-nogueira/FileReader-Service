using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.HomeAssistant;
using System.Collections.Generic;

namespace IOTLinkAddon.Service.Monitors
{
    class MemoryMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "Memory";

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            MemoryInfo memoryInfo = PlatformHelper.GetMemoryInformation();

            // Memory Usage (%)
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Memory/Usage",
                Value = memoryInfo.MemoryLoad,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "Usage",
                    Name = "Memory Usage",
                    Unit = "%",
                    Icon = "mdi:memory"
                }
            });

            // Memory Available
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_MEMORY_SIZE,
                Topic = "Stats/Memory/Available",
                Value = memoryInfo.AvailPhysical,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "Available",
                    Name = "Memory Available",
                    Unit = "MB",
                    Icon = "mdi:memory"
                }
            });

            // Memory Used
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_MEMORY_SIZE,
                Topic = "Stats/Memory/Used",
                Value = (memoryInfo.TotalPhysical - memoryInfo.AvailPhysical),
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "Used",
                    Name = "Memory Used",
                    Unit = "MB",
                    Icon = "mdi:memory"
                }
            });

            // Memory Total
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_MEMORY_SIZE,
                Topic = "Stats/Memory/Total",
                Value = memoryInfo.TotalPhysical,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "Total",
                    Name = "Memory Total",
                    Unit = "MB",
                    Icon = "mdi:memory"
                }
            });

            return result;
        }
    }
}
