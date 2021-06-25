using IOTLinkAPI.Common.Yaml;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using YamlDotNet.Serialization;

namespace IOTLinkAPI.Configs
{
    public class ConfigurationManager
    {
        private static ConfigurationManager _instance;

        private static Dictionary<string, ConfigInfo> _configs = new Dictionary<string, ConfigInfo>();

        public delegate void ConfigReloadedHandler(object sender, ConfigReloadEventArgs e);

        /// <summary>
        /// Get object instance
        /// </summary>
        /// <returns></returns>
        public static ConfigurationManager GetInstance()
        {
            if (_instance == null)
                _instance = new ConfigurationManager();

            return _instance;
        }

        /// <summary>
        /// ConfigurationManager constructor
        /// </summary>
        private ConfigurationManager()
        {

        }

        /// <summary>
        /// Retrieve the configuration from out internal cache or load it if necessary.
        /// </summary>
        /// <param name="path">String containing the configuration file path</param>
        /// <returns>Configuration</returns>
        public Configuration GetConfiguration(string path)
        {
            ConfigInfo configInfo = GetConfiguration(path, false);
            if (configInfo == null)
                return null;

            return configInfo.Config;
        }

        /// <summary>
        /// Set the handler for handling the reload configuration event.
        /// </summary>
        /// <param name="path">String containing the configuration file path</param>
        /// <param name="configReloadedHandler">ConfigReloadedHandler delegate</param>
        public void SetReloadHandler(string path, ConfigReloadedHandler configReloadedHandler)
        {
            SetReloadHandler(path, configReloadedHandler, ConfigType.CONFIGURATION_ADDON);
        }

        /// <summary>
        /// Set the handler for handling the reload configuration event.
        /// </summary>
        /// <param name="path">String containing the configuration file path</param>
        /// <param name="configReloadedHandler">ConfigReloadedHandler delegate</param>
        /// <param name="configType">Configuration Type</param>
        internal void SetReloadHandler(string path, ConfigReloadedHandler configReloadedHandler, ConfigType configType)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                LoggerHelper.Warn("Trying to set a reload handler for empty path.");
                return;
            }

            path = Path.GetFullPath(path);
            if (!_configs.ContainsKey(path))
            {
                LoggerHelper.Warn("Trying to set a reload handler for a key which doesn't exists: {0}", path);
                return;
            }

            ConfigInfo configInfo = _configs[path];
            configInfo.ConfigType = configType;
            configInfo.OnConfigReloadHandler += configReloadedHandler;

            LoggerHelper.Verbose("Setting up file system watcher: {0}", path);
            if (configInfo.FileSystemWatcher == null)
            {
                LoggerHelper.Debug("Creating new watcher for {0}", path);

                configInfo.FileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
                configInfo.FileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
                configInfo.FileSystemWatcher.Changed += OnConfigChanged;
                configInfo.FileSystemWatcher.Created += OnConfigChanged;
                configInfo.FileSystemWatcher.EnableRaisingEvents = true;
            }
        }

        /// <summary>
        /// Retrieve the configuration from out internal cache or load it if necessary.
        /// </summary>
        /// <param name="path">String containing the configuration file path</param>
        /// <param name="reload">If set to true, it will reload the configuration file</param>
        /// <returns>Configuration</returns>
        private static ConfigInfo GetConfiguration(string path, bool reload = false)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            path = Path.GetFullPath(path);
            if (reload == false && _configs.ContainsKey(path) && _configs[path].Config != null)
                return _configs[path];

            object config = LoadConfigurationFromFile(path);
            if (config == null)
            {
                LoggerHelper.Error("Configuration file not found: {0}", path);
                return null;
            }

            if (!_configs.ContainsKey(path))
                _configs.Add(path, new ConfigInfo(config));
            else
                _configs[path].SetConfig(config);

            return _configs[path];
        }

        /// <summary>
        /// Load the configuration object from its file.
        /// </summary>
        /// <param name="path">String containing the configuration file path</param>
        /// <returns></returns>
        private static object LoadConfigurationFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            path = Path.GetFullPath(path);
            string configText = PathHelper.GetFileText(path);
            if (string.IsNullOrWhiteSpace(configText))
                return null;

            try
            {
                using (StringReader reader = new StringReader(configText))
                {
                    var builder = new DeserializerBuilder();

                    var includeNodeDeserializer = new YamlIncludeNodeDeserializer(new YamlIncludeNodeDeserializerOptions
                    {
                        DirectoryName = Path.GetDirectoryName(path),
                        Deserializer = builder.Build()
                    });

                    var deserializer = new DeserializerBuilder()
                        .WithTagMapping(YamlIncludeNodeDeserializer.TAG, typeof(IncludeRef))
                        .WithNodeDeserializer(includeNodeDeserializer, s => s.OnTop())
                        .Build();

                    return deserializer.Deserialize<object>(reader);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error while reading configuration file: {0}", ex);
            }

            return null;
        }

        /// <summary>
        /// Handle configuration file changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            string path = e.FullPath;
            if (!_configs.ContainsKey(path))
            {
                LoggerHelper.Error("File {0} is being watched, but there is no handler for it.", path);
                return;
            }

            try
            {
                // Disable Watcher
                _configs[path].FileSystemWatcher.EnableRaisingEvents = false;

                // This is needed on slow machines which file handlers can take longer to release.
                Thread.Sleep(3000);

                ConfigInfo configInfo = GetConfiguration(path, true);
                if (configInfo == null)
                {
                    LoggerHelper.Info("Configuration file {0} cannot be reloaded.", path);
                    return;
                }

                LoggerHelper.Debug("Firing events for reloaded configuration file {0}", path);
                configInfo.Raise_OnConfigReloadHandler(sender, new ConfigReloadEventArgs
                {
                    FilePath = path,
                    ConfigType = configInfo.ConfigType
                });
            }
            finally
            {
                // Enable Watcher
                _configs[path].FileSystemWatcher.EnableRaisingEvents = true;
            }
        }
    }
}
