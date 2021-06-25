using IOTLinkAPI.Configs;
using IOTLinkAPI.Platform.HomeAssistant;
using System.Collections.Generic;
using System.Windows.Forms;

namespace IOTLinkAddon.Service.Monitors
{
    class PowerMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "Power";

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            PowerStatus powerStatus = SystemInformation.PowerStatus;

            // Power Status
            bool powerLineStatus = powerStatus.PowerLineStatus == PowerLineStatus.Online;
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Power/Status",
                Value = powerLineStatus,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.BinarySensor,
                    Id = "PowerStatus",
                    Name = "Power Status",
                    PayloadOff = "False",
                    PayloadOn = "True",
                    DeviceClass = "plug"
                }
            });

            // Battery Status
            bool batteryStatus = powerStatus.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery && powerStatus.BatteryChargeStatus != BatteryChargeStatus.Unknown;
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Battery/Status",
                Value = batteryStatus,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.BinarySensor,
                    Id = "BatteryStatus",
                    Name = "Battery Status",
                    DeviceClass = "plug"
                }
            });

            // Battery Level
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Battery/Level",
                Value = powerStatus.BatteryChargeStatus,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "BatteryLevel",
                    Name = "Battery Level",
                    DeviceClass = "battery"
                }
            });

            // Battery Full Lifetime
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Battery/FullLifetime",
                Value = powerStatus.BatteryFullLifetime,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "FullLifetime",
                    Name = "Battery Full Lifetime",
                    Unit = "minutes",
                    ValueTemplate = "{{ ( value | float / 60 ) | int }}",
                    Icon = "mdi:timer-outline"
                }
            });

            // Battery Remaining Time
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Battery/RemainingTime",
                Value = powerStatus.BatteryLifeRemaining,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "RemainingTime",
                    Name = "Battery Remaining Time",
                    Unit = "minutes",
                    ValueTemplate = "{{ ( value | float / 60 ) | int }}",
                    Icon = "mdi:timer-outline"

                }
            });

            // Battery Remaining (%)
            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/Battery/RemainingPercent",
                Value = (powerStatus.BatteryLifePercent * 100),
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "Remaining",
                    Name = "Battery Remaining",
                    Unit = "%",
                    DeviceClass = "battery"
                }
            });

            return result;
        }
    }
}
