using IOTLinkAPI.Configs;

namespace IOTLinkAddon.Common.Configs
{
    public class GeneralConfig
    {
        public bool Enabled { get; set; }
        public int CompareType { get; set; }
        public bool Discoverable { get; set; }
        public string DisplayName { get; set; }
        public bool Cacheable { get; set; }
        public int Interval { get; set; }
        public bool AdvancedMode { get; set; }

        public static GeneralConfig FromConfiguration(Configuration configuration)
        {
            return new GeneralConfig
            {
                Enabled = configuration.GetValue("enabled", false),
                CompareType = configuration.GetValue("compareType", 0),
                Discoverable = configuration.GetValue("discoverable", false),
                DisplayName = configuration.GetValue("displayName", null),
                Cacheable = configuration.GetValue("cacheable", false),
                Interval = configuration.GetValue("interval", 0),
                AdvancedMode = configuration.GetValue("advancedMode", false),
            };
        }
    }
}
