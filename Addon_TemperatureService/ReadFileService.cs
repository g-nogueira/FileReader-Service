using Addon_ReadFileService.Configs;
using Addon_ReadFileService.Watcher;
using IOTLinkAddon.Common;
using IOTLinkAddon.Common.Configs;
using IOTLinkAddon.Common.Helpers;
using IOTLinkAddon.Common.Processes;
using IOTLinkAddon.Service;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.Events.Process;
using IOTLinkAPI.Platform.HomeAssistant;
using IOTLinkAPI.Platform.Windows;
using LibreHardwareMonitor.Hardware;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace Addon_ReadFileService
{
    public class ReadFileService : ServiceAddon
    {
        private string _configPath;
        private Configuration _config;

        private List<FileWatcher> _monitors = new List<FileWatcher>();

        private int tryAgainAmount = 0;

        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);

            LoggerHelper.Verbose("ReadFileService::Init() - Started");

            // Read the config.yaml file
            var cfgManager = ConfigurationManager.GetInstance();
            _configPath = Path.Combine(_currentPath, "config.yaml");
            _config = cfgManager.GetConfiguration(_configPath);

            // Set up handler for OnConfigReload
            //cfgManager.SetReloadHandler(_configPath, OnConfigReload);

            OnConfigReloadHandler += OnConfigReload;
            OnMQTTConnectedHandler += OnMQTTConnected;
            OnMQTTDisconnectedHandler += OnMQTTDisconnected;
            OnRefreshRequestedHandler += OnClearEvent;
            //OnAgentResponseHandler += OnAgentResponse;

            SetupMonitors();
            Restart();

            LoggerHelper.Verbose("ReadFileService::Init() - Completed");
        }

        /// <summary>
        /// Initializes each file monitor and its respective HA discovery config.
        /// </summary>
        private void SetupMonitors()
        {

            PublishCachedStates();
            ClearMonitors();

            List<Configuration> monitorConfigurations = _config.GetConfigurationList("files");
            if (monitorConfigurations == null || monitorConfigurations.Count == 0)
            {
                LoggerHelper.Info("ReadFileService::SetupMonitors() - Monitoring list is empty.");
                return;
            }

            foreach (Configuration fileConfiguration in monitorConfigurations)
            {
                try
                {
                    SetupMonitor(fileConfiguration);
                    SetupDiscovery(fileConfiguration);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Debug("ReadFileService::SetupMonitors({0}): Error - {1}", fileConfiguration.Key, ex);
                }
            }
        }

        private void PublishCachedStates()
        {
            foreach (var monitor in _monitors)
            {
                if (!string.IsNullOrEmpty(monitor.CachedState))
                {
                    GetManager().PublishMessage(this, $"Files/{monitor.Key}/Sensor", monitor.CachedState);
                }
            }
        }

        private bool IsAddonEnabled()
        {
            if (_config == null || !_config.GetValue("enabled", false))
            {
                return false;
            }

            return true;
        }

        private void Restart()
        {
            if (!IsAddonEnabled())
                return;

            SetupMonitors();
        }

        private void ClearMonitors()
        {
            _monitors.Clear();
        }

        /// <summary>
        /// Initializes a <see cref="FileSystemWatcher"/> for given <see cref="Configuration"/>
        /// </summary>
        /// <param name="monitorConfiguration"></param>
        private void SetupMonitor(Configuration monitorConfiguration)
        {
            FileConfig config = FileConfig.FromConfiguration(monitorConfiguration);
            config.Watcher = new FileWatcher(config.FileDirectory, config.FileName)
            {
                Key = config.Key,
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.LastWrite
            };


            bool hasMonitor = _monitors.Any(m => m.Key == config.Key);
            if (hasMonitor)
            {
                LoggerHelper.Warn("ReadFileService::SetupMonitor({0}) - Duplicated Key. Ignoring.", monitorConfiguration.Key);
                return;
            }

            //Start monitoring.
            config.Watcher.Changed += new FileSystemEventHandler((s, e) => OnChanged(e, config));
            config.Watcher.EnableRaisingEvents = true;


            _monitors.Add(config.Watcher);
        }

        /// <summary>
        /// Represents the method that will handle the <see cref="FileSystemWatcher.Changed"/> event 
        /// of a <see cref="FileSystemWatcher"/> class.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnChanged(FileSystemEventArgs e, FileConfig config)
        {
            try
            {
                if (config.TryGetStateValue(e.FullPath, out var state))
                {
                    config.Watcher.CachedState = state;
                    GetManager().PublishMessage(this, $"Files/{config.Key}/Sensor", state);
                }
            }
            catch (Exception ex)
            {

                if (tryAgainAmount <= 3)
                {
                    tryAgainAmount++;

                    LoggerHelper.Error($"ReadFileService::OnChanged() - Could not read file. Trying again in {tryAgainAmount} seconds.", ex);


                    var timer = new Timer(1000 * tryAgainAmount);
                    timer.Elapsed += new ElapsedEventHandler((s, t) => OnChanged(e, config));
                    timer.Start();
                }
                else
                {
                    LoggerHelper.Error("ReadFileService::OnChanged() - Could not read file. Will not try again.", ex);
                }

            }
        }

        /// <summary>
        /// Sets up the dicovery of the sensor for Home Assistant
        /// </summary>
        /// <param name="config"></param>
        private void SetupDiscovery(Configuration config)
        {
            var id = config.Key;
            var name = config.Key;

            HassDiscoveryOptions discoveryOptions = new HassDiscoveryOptions
            {
                Id = id,
                Name = name,
                DeviceClass = config.GetValue("deviceClass", ""),
                Component = HomeAssistantComponent.BinarySensor,
                Icon = config.GetValue("icon", "")
            };

            GetManager().PublishDiscoveryMessage(this, $"Files/{config.Key}/Sensor", "Files", discoveryOptions);
        }

        private void OnMQTTConnected(object sender, EventArgs e)
        {
            if (!IsAddonEnabled())
                return;

            SetupMonitors();
        }

        private void OnMQTTDisconnected(object sender, EventArgs e)
        {
            foreach (var monitor in _monitors)
            {
                monitor.EnableRaisingEvents = false;
            }
        }

        private void OnClearEvent(object sender, EventArgs e)
        {
            LoggerHelper.Verbose("ReadFileService::OnClearEvent() - Clearing cache and resending information.");

            // Do something
        }

        /// <summary>
        /// Handles the OnConfigReload event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConfigReload(object sender, ConfigReloadEventArgs e)
        {
            if (e.ConfigType != ConfigType.CONFIGURATION_ADDON)
                return;

            LoggerHelper.Verbose("ReadFileService::OnConfigReload() - Reloading configuration");

            _config = ConfigurationManager.GetInstance().GetConfiguration(_configPath);
            Restart();
        }

    }
}
