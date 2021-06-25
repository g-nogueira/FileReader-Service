using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events;
using System.Collections.Generic;
using System.IO;
using static IOTLinkAPI.Configs.ConfigurationManager;

namespace IOTLinkAPI.Configs
{
    public class ConfigInfo
    {
        public Configuration Config { get; set; }

        public FileSystemWatcher FileSystemWatcher { get; set; }

        public ConfigType ConfigType { get; set; }

        public event ConfigReloadedHandler OnConfigReloadHandler;

        public void Raise_OnConfigReloadHandler(object sender, ConfigReloadEventArgs e)
        {
            OnConfigReloadHandler?.Invoke(sender, e);
        }

        public ConfigInfo(object config)
        {
            SetConfig(config);
        }

        public void SetConfig(object config)
        {
            Config = Configuration.BuildConfiguration(null, config);
        }
    }
}
