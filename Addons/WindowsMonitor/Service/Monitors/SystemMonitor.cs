using IOTLinkAddon.Common;
using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.HomeAssistant;
using System.Collections.Generic;

namespace IOTLinkAddon.Service.Monitors
{
    class SystemMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "SystemInfo";

        private static readonly Dictionary<string, AddonRequestType> AGENT_REQUESTS = new Dictionary<string, AddonRequestType>
        {
            { "IdleTime", AddonRequestType.REQUEST_IDLE_TIME }
        };

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override Dictionary<string, AddonRequestType> GetAgentRequests()
        {
            return AGENT_REQUESTS;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = MonitorItemType.TYPE_RAW,
                Topic = "Stats/System/CurrentUser",
                Value = PlatformHelper.GetCurrentUsername(),
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "CurrentUsername",
                    Name = "Current Username",
                    Icon = "mdi:account"
                }
            });

            return result;
        }

        public override List<MonitorItem> OnAgentResponse(Configuration config, AddonRequestType type, dynamic data, string username)
        {
            switch (type)
            {
                case AddonRequestType.REQUEST_IDLE_TIME:
                    return ParseIdleTimeInformation(config, data, username);

                default: break;
            }

            return null;
        }

        private List<MonitorItem> ParseIdleTimeInformation(Configuration config, dynamic data, string username)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            result.Add(new MonitorItem
            {
                ConfigKey = CONFIG_KEY,
                Type = config.GetValue("inSeconds", false) ? MonitorItemType.TYPE_RAW : MonitorItemType.TYPE_UPTIME,
                Topic = "Stats/System/IdleTime",
                Value = data.requestData,
                DiscoveryOptions = new HassDiscoveryOptions()
                {
                    Component = HomeAssistantComponent.Sensor,
                    Id = "IdleTime",
                    Name = "System Idle Time",
                    Unit = config.GetValue("inSeconds", false) ? "s" : null,
                    Icon = "mdi:timer-outline"
                }
            });

            return result;
        }
    }
}
