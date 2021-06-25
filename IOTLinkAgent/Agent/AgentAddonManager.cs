using IOTLink.Platform.WebSocket;
using IOTLinkAgent.Agent.Loaders;
using IOTLinkAgent.Agent.WSClient;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace IOTLinkAgent.Agent
{
    public class AgentAddonManager : IAddonAgentManager
    {
        private static AgentAddonManager _instance;
        private Dictionary<string, AddonInfo> _addons = new Dictionary<string, AddonInfo>();

        public static AgentAddonManager GetInstance()
        {
            if (_instance == null)
                _instance = new AgentAddonManager();

            return _instance;
        }

        private AgentAddonManager()
        {
            LoggerHelper.Trace("AgentAddonManager instance created.");
        }

        /// <summary>
        /// Send a response to the websocket server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="addonData"></param>
        public void SendAgentResponse(AgentAddon sender, dynamic addonData)
        {
            if (sender == null || addonData == null)
                return;

            WebSocketClient webSocketClient = WebSocketClient.GetInstance();
            if (webSocketClient == null || !webSocketClient.IsConnected())
                return;

            dynamic data = new ExpandoObject();
            data.addonId = sender.GetAppInfo().AddonId;
            data.addonData = addonData;

            webSocketClient.SendResponse(ResponseTypeClient.RESPONSE_ADDON, data);
        }

        /// <summary>
        /// Check if any addon exists with the given ID.
        /// </summary>
        /// <param name="id">String containing the addon ID.</param>
        /// <returns>True if the addon exists, false otherwise.</returns>
        public bool AddonExists(string id)
        {
            if (id == null)
                return false;

            id = id.Trim().ToLowerInvariant();

            if (_addons.ContainsKey(id))
                return true;

            return false;
        }

        /// <summary>
        /// Get an <see cref="AddonInfo"/> by its Id.
        /// </summary>
        /// <param name="id">String containing the addon ID</param>
        /// <returns><see cref="AddonInfo"/> if found. Blank structure otherwise.</returns>
        public AddonInfo GetAddonById(string id)
        {
            if (id == null)
                return null;

            id = id.Trim().ToLowerInvariant();

            if (!AddonExists(id))
                return null;

            return _addons[id];
        }

        /// <summary>
        /// Get a list of all loaded addons.
        /// </summary>
        /// <returns>A list containing all loaded <see cref="AddonInfo">addons</see>.</returns>
        public List<AddonInfo> GetAddonsList()
        {
            return _addons.Values.ToList();
        }

        /// <summary>
        /// Add an <see cref="AddonInfo"/> to the current loaded list.
        /// </summary>
        /// <param name="id">String containing the addon ID.</param>
        /// <param name="AddonInfo"><see cref="AddonInfo"/> structure.</param>
        internal void AddAddon(string id, AddonInfo addonInfo)
        {
            if (id == null)
                return;

            id = id.Trim().ToLowerInvariant();
            LoggerHelper.Info("Loading addon: {0}", id);
            _addons.Add(id, addonInfo);
        }

        /// <summary>
        /// Load all available addons
        /// </summary>
        internal void LoadAddons()
        {
            _addons.Clear();
            LoadInternalAddons();

            if (ApplicationConfigHelper.GetEngineConfig().GetValue("addons:enabled", true) == true)
                LoadExternalAddons();
        }

        /// <summary>
        /// Load embedded addons
        /// </summary>
        private void LoadInternalAddons()
        {
            AgentAddon[] internalAddons = new AgentAddon[] { };
            if (internalAddons.Length == 0)
                return;

            LoggerHelper.Info("Loading {0} internal addons", internalAddons.Length);
            foreach (AgentAddon addon in internalAddons)
            {
                AddonInfo addonInfo = new AddonInfo
                {
                    AddonName = addon.GetType().Name,
                    AddonPath = PathHelper.BaseAppPath(),
                    AddonFile = string.Empty,
                    Internal = true,
                    AddonId = addon.GetType().Name,
                    AgentAddon = addon
                };

                if (addonInfo.AgentAddon != null)
                {
                    addonInfo.AgentAddon.SetAddonInfo(addonInfo);
                    addonInfo.AgentAddon.SetCurrentPath(addonInfo.AddonPath);
                    addonInfo.AgentAddon.Init(this);
                }

                AddAddon(addonInfo.AddonId, addonInfo);
            }
        }

        /// <summary>
        /// Load external addons
        /// </summary>
        private void LoadExternalAddons()
        {
            string addonsPath = PathHelper.AddonsPath();

            // Create addons directory.
            if (!Directory.Exists(addonsPath))
            {
                LoggerHelper.Warn("Addons directory doesn't exists. Creating {0}", addonsPath);
                Directory.CreateDirectory(addonsPath);
                return;
            }

            List<string> dirs = new List<string>(Directory.EnumerateDirectories(addonsPath));
            LoggerHelper.Debug("Loading {0} external addons", dirs.Count);
            foreach (var dir in dirs)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(dir);
                AddonInfo addonInfo = new AddonInfo();

                if (LoadAddonConfig(directoryInfo.Name, ref addonInfo) == true && addonInfo.Enabled == true)
                {
                    if (!AssemblyLoader.LoadAssemblyDLL(ref addonInfo))
                        continue;

                    try
                    {
                        if (addonInfo.AgentAddon != null)
                        {
                            addonInfo.AgentAddon.SetAddonInfo(addonInfo);
                            addonInfo.AgentAddon.SetCurrentPath(addonInfo.AddonPath);
                            addonInfo.AgentAddon.Init(this);
                        }

                        AddAddon(addonInfo.AddonId, addonInfo);
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Error("Error while loading addon {0}: {1}", addonInfo.AddonId, ex.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Load the addon informations
        /// </summary>
        /// <param name="DirName">Directory name containing the configuration file.</param>
        /// <param name="addonInfo"><see cref="AppInfo"/> structure to be loaded.</param>
        /// <returns>True if valid configuration file found, false otherwise.</returns>
        private bool LoadAddonConfig(string DirName, ref AddonInfo addonInfo)
        {
            AddonConfig config = AddonConfig.GetInstance(DirName);
            if (!config.Load())
                return false;

            return LoadAddonInfo(config, ref addonInfo);
        }

        /// <summary>
        /// Load the complete addon information from its configuration file.
        /// It also checks for invalid configuration entries and formats.
        /// </summary>
        /// <param name="config"><see cref="AddonConfig"/> object</param>
        /// <param name="addonInfo"><see cref="AddonInfo"/> object (ref)</param>
        /// <returns></returns>
        private bool LoadAddonInfo(AddonConfig config, ref AddonInfo addonInfo)
        {
            if (config == null)
                return false;

            addonInfo.Settings = config;
            addonInfo.AddonId = config.AddonId;
            addonInfo.Enabled = config.Enabled;
            addonInfo.AddonPath = config.AddonPath;
            addonInfo.AddonName = config.AddonName;
            addonInfo.AddonFile = config.AddonFile;

            /**
             * Check for a valid AddonId
             */
            if (string.IsNullOrWhiteSpace(addonInfo.AddonId) || !StringHelper.IsValidID(addonInfo.AddonId))
            {
                LoggerHelper.Error("Invalid App: {0}", addonInfo.AddonName);
                LoggerHelper.Error("App unique id is missing or is invalid (only a-zA-Z0-9_ characters allowed).");
                LoggerHelper.Error("App disabled.");
                addonInfo.Enabled = false;
            }

            /**
             * Check for a valid AddonName
             */
            if (string.IsNullOrWhiteSpace(addonInfo.AddonName))
            {
                LoggerHelper.Error("Invalid App: {0}", addonInfo.AddonName);
                LoggerHelper.Error("App name is missing.");
                LoggerHelper.Error("App disabled.");
                addonInfo.Enabled = false;
            }

            /**
             * Check app compatibility
             */
            if (!AssemblyHelper.CheckAssemblyVersion(config.MinApiVersion, config.MaxApiVersion))
            {
                LoggerHelper.Error("Incompatible App found: {0}", addonInfo.AddonName);
                LoggerHelper.Error("CurrentVersion: " + AssemblyHelper.GetCurrentVersion());
                LoggerHelper.Error("MinVersion: {0}, MaxVersion: {1}", config.MinApiVersion, config.MaxApiVersion);
                LoggerHelper.Error("App disabled.");
                addonInfo.Enabled = false;
            }

            LoggerHelper.Debug("Addon configuration loaded: {0}", addonInfo.AddonName);
            return true;
        }

        /// <summary>
        /// Broadcast system configuration change to all addons.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e"><see cref="ConfigReloadEventArgs"/></param>
        internal void Raise_OnConfigReloadHandler(object sender, ConfigReloadEventArgs e)
        {
            List<AddonInfo> addons = GetAddonsList();
            foreach (AddonInfo addonInfo in addons)
            {
                if (addonInfo.AgentAddon != null)
                    addonInfo.AgentAddon.Raise_OnConfigReloadHandler(sender, e);
            }
        }

        /// <summary>
        /// Dispatch Agent Response
        /// </summary>
        /// <param name="addonId"></param>
        /// <param name="addonData"></param>
        internal void Raise_OnAgentRequest(string addonId, dynamic addonData)
        {
            AddonInfo addonInfo = GetAddonById(addonId);
            if (addonInfo == null)
                return;

            addonInfo.AgentAddon.Raise_OnAgentResponse(this, new AgentAddonRequestEventArgs
            {
                Data = addonData
            });
        }
    }
}
