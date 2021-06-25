using IOTLinkAPI.Configs;
using System.IO;
using static IOTLinkAPI.Configs.ConfigurationManager;

namespace IOTLinkAPI.Helpers
{
    public static class ApplicationConfigHelper
    {
        private const string ENGINE_CONF_FILE = "configuration.yaml";

        public static void Init()
        {
            Configuration configuration = GetEngineConfig();
            if (configuration == null)
                LoggerHelper.Error("Cannot initialize application engine configuration.");
        }

        public static Configuration GetEngineConfig()
        {
            string path = Path.Combine(PathHelper.ConfigPath(), ENGINE_CONF_FILE);
            return ConfigurationManager.GetInstance().GetConfiguration(path);
        }

        public static void SetEngineConfigReloadHandler(ConfigReloadedHandler configReloadedHandler)
        {
            Configuration configuration = GetEngineConfig();
            if (configuration == null)
                return;

            string path = Path.Combine(PathHelper.ConfigPath(), ENGINE_CONF_FILE);
            ConfigurationManager.GetInstance().SetReloadHandler(path, configReloadedHandler, Platform.ConfigType.CONFIGURATION_ENGINE);
        }
    }
}
