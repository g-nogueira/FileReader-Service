using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.HomeAssistant;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IOTLinkAddon.Service.Monitors
{
    class UptimeMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "Uptime";

        private static PerformanceCounter _uptimePerformanceCounter;

        public override void Init()
        {
            if (_uptimePerformanceCounter == null)
                _uptimePerformanceCounter = new PerformanceCounter("System", "System Up Time");

            _uptimePerformanceCounter.NextValue();
            PlatformHelper.LastBootUpTime();
        }

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            // Boot Time
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_DATETIME,
                Topic = "Stats/System/BootTime",
                Value = PlatformHelper.LastBootUpTime(),
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "BootTime",
                    Name = "System Boot Time",
                    Icon = "mdi:timer-outline"
                }
            });

            // Uptime
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = config.GetValue("inSeconds", false) ? MonitorItemType.TYPE_RAW : MonitorItemType.TYPE_UPTIME,
                Topic = "Stats/System/Uptime",
                Value = Math.Round(_uptimePerformanceCounter.NextValue(), 0),
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "Uptime",
                    Name = "System Uptime",
                    Unit = config.GetValue("inSeconds", false) ? "s" : null,
                    Icon = "mdi:timer-outline"
                }
            });

            return result;
        }
    }
}
