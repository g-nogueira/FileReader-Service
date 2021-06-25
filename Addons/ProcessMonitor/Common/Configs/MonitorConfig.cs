using IOTLinkAddon.Common.Helpers;
using IOTLinkAPI.Configs;
using System.Collections.Generic;
using System.Linq;

namespace IOTLinkAddon.Common.Configs
{
    public class MonitorConfig
    {
        public string Key { get; set; }
        public List<string> ProcessNames { get; set; }
        public List<string> ProcessWindows { get; set; }
        public List<string> ProcessClassNames { get; set; }
        public GeneralConfig General { get; set; }

        public static MonitorConfig FromConfiguration(Configuration configuration)
        {
            return new MonitorConfig
            {
                Key = configuration.Key.ToLowerInvariant(),
                ProcessNames = configuration.GetList<string>("processes").Select(x => ProcessHelper.CleanProcessName(x)).ToList(),
                ProcessWindows = configuration.GetList<string>("windows"),
                ProcessClassNames = configuration.GetList<string>("classnames"),
                General = GeneralConfig.FromConfiguration(configuration.GetValue("configs"))
            };
        }
    }
}
