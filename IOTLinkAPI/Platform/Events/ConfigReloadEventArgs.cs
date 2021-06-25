using System;

namespace IOTLinkAPI.Platform.Events
{
    public class ConfigReloadEventArgs : EventArgs
    {
        public string FilePath { get; set; }

        public ConfigType ConfigType { get; set; }
    }
}
