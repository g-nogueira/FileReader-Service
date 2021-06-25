using IOTLinkAddon.Common;
using IOTLinkAPI.Configs;
using System.Collections.Generic;

namespace IOTLinkAddon.Service.Monitors
{
    abstract class BaseMonitor : IMonitor
    {
        public abstract string GetConfigKey();
        public virtual List<MonitorItem> GetMonitorItems(Configuration config, int interval) { return null; }
        public virtual Dictionary<string, AddonRequestType> GetAgentRequests() { return null; }
        public virtual void Init() { }
        public virtual List<MonitorItem> OnAgentResponse(Configuration _config, AddonRequestType type, dynamic data, string username) { return null; }
    }
}
