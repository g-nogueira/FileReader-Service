using IOTLinkAPI.Configs;
using IOTLinkAPI.Platform.HomeAssistant;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IOTLinkAddon.Service.Monitors
{
    class CPUMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "CPU";

        private static PerformanceCounter _cpuPerformanceCounter;

        public override void Init()
        {
            if (_cpuPerformanceCounter == null)
                _cpuPerformanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            _cpuPerformanceCounter.NextValue();
        }

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            // CPU Usage
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/CPU/Usage",
                Value = Math.Round(_cpuPerformanceCounter.NextValue(), 0),
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Id = "Usage",
                    Unit = "%",
                    Name = "CPU Usage",
                    Component = HomeAssistantComponent.Sensor,
                    Icon = "mdi:speedometer"
                }
            });

            return result;
        }
    }
}
