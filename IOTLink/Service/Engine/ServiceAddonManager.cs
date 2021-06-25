using IOTLinkAPI.Addons;
using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.Events.MQTT;
using IOTLinkService.Service.Loaders;
using IOTLinkService.Service.WebSockets.Server;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using IOTLinkAPI.Platform.HomeAssistant;
using IOTLinkService.Service.MQTT;

using static IOTLinkAPI.Platform.Events.MQTT.MQTTHandlers;
using Addon_ReadFileService;

namespace IOTLinkService.Service.Engine
{
    public class ServiceAddonManager : IAddonServiceManager
    {
        private static ServiceAddonManager _instance;
        private SortedDictionary<string, AddonInfo> _addons = new SortedDictionary<string, AddonInfo>();
        private SortedDictionary<string, TopicSubscriptionInfo> _topics = new SortedDictionary<string, TopicSubscriptionInfo>();

        public static ServiceAddonManager GetInstance()
        {
            if (_instance == null)
                _instance = new ServiceAddonManager();

            return _instance;
        }

        private ServiceAddonManager()
        {
            LoggerHelper.Trace("ServiceAddonManager instance created.");
        }

        /// <summary>
        /// Add a subscription to a specific topic
        /// </summary>
        /// <param name="sender">Origin <see cref="AddonInfo">Addon</see></param>
        /// <param name="topic">MQTT Topic</param>
        /// <param name="msgHandler"><see cref="MQTTMessageEventHandler">Event Handler</see></param>
        /// <param name="acceptGlobals">Indicates if the subscription will handle global messages</param>
        public void SubscribeTopic(ServiceAddon sender, string topic, MQTTMessageEventHandler msgHandler, bool acceptGlobals = true)
        {
            if (sender == null || string.IsNullOrWhiteSpace(topic) || HasSubscription(sender, topic))
                return;

            string addonTopic = BuildTopicName(sender, topic);

            _topics.Add(addonTopic, new TopicSubscriptionInfo
            {
                Topic = addonTopic,
                AcceptGlobalMessages = acceptGlobals,
                OnMessageReceived = msgHandler
            });

            LoggerHelper.Info("Addon {0} has subscribed to topic {1} (Accepts Global: {2})", sender.GetAppInfo().AddonId, addonTopic, acceptGlobals);
        }

        /// <summary>
        /// Checks if an addon has a subscription to a specific topic
        /// </summary>
        /// <param name="sender">Origin <see cref="AddonInfo">Addon</see></param>
        /// <param name="topic">MQTT Topic</param>
        public bool HasSubscription(ServiceAddon sender, string topic)
        {
            if (sender == null)
                return false;

            string addonTopic = BuildTopicName(sender, topic);
            return _topics.ContainsKey(addonTopic);
        }

        /// <summary>
        /// Remove an addon subscription to a specific topic
        /// </summary>
        /// <param name="sender">Origin <see cref="AddonInfo">Addon</see></param>
        /// <param name="topic">MQTT Topic</param>
        public void RemoveSubscription(ServiceAddon sender, string topic)
        {
            if (sender == null || !HasSubscription(sender, topic))
                return;

            string addonTopic = BuildTopicName(sender, topic);
            _topics.Remove(addonTopic);

            LoggerHelper.Info("Addon {0} has removed subscription to topic {1}", sender.GetAppInfo().AddonId, addonTopic);
        }

        /// <summary>
        /// Publish a message using the origin addon information.
        /// </summary>
        /// <param name="sender">Origin <see cref="AddonInfo">Addon</see></param>
        /// <param name="topic">MQTT Topic</param>
        /// <param name="message">String containing the message</param>
        public void PublishMessage(ServiceAddon sender, string topic, string message)
        {
            if (sender == null || string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(message))
                return;

            string addonTopic = BuildTopicName(sender, topic);
            MQTTClientManager.GetInstance().PublishMessage(addonTopic, message);
        }

        public void PublishDiscoveryMessage(ServiceAddon sender, string topic, string preffixName, HassDiscoveryOptions discoveryOptions)
        {
            if (sender == null || string.IsNullOrWhiteSpace(discoveryOptions.Name))
                return;

            string addonTopic = BuildTopicName(sender, topic);
            MQTTClientManager.GetInstance().PublishDiscoveryMessage(addonTopic, preffixName, discoveryOptions);
        }

        /// <summary>
        /// Publish a message using the origin addon information.
        /// </summary>
        /// <param name="sender">Origin <see cref="AddonInfo">Addon</see></param>
        /// <param name="topic">MQTT Topic</param>
        /// <param name="message">Message (byte[])</param>
        public void PublishMessage(ServiceAddon sender, string topic, byte[] message)
        {
            if (sender == null || string.IsNullOrWhiteSpace(topic))
                return;

            string addonTopic = BuildTopicName(sender, topic);
            MQTTClientManager.GetInstance().PublishMessage(addonTopic, message);
        }

        /// <summary>
        /// Send a request to the agent(s) connected.
        /// If username is null, then the message will be broadcasted to all available agents.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="addonData"></param>
        /// <param name="username"></param>
        public void SendAgentRequest(ServiceAddon sender, dynamic addonData, string username = null)
        {
            if (sender == null || addonData == null)
                return;

            WebSocketServerManager webSocketServerManager = WebSocketServerManager.GetInstance();
            if (webSocketServerManager == null || !webSocketServerManager.IsConnected())
                return;

            if (username != null && string.Compare(username, "SYSTEM") == 0)
                return;

            dynamic data = new ExpandoObject();
            data.addonId = sender.GetAppInfo().AddonId;
            data.addonData = addonData;

            webSocketServerManager.SendRequest(IOTLink.Platform.WebSocket.RequestTypeServer.REQUEST_ADDON, data, username);
        }

        public void ShowNotification(ServiceAddon sender, string title, string message, string iconUrl = null, string launchParams = null)
        {
            if (sender == null || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
                return;

            WebSocketServerManager webSocketServerManager = WebSocketServerManager.GetInstance();
            if (webSocketServerManager == null || !webSocketServerManager.IsConnected())
                return;

            dynamic data = new ExpandoObject();
            data.title = title;
            data.message = message;
            data.iconUrl = iconUrl;
            data.launchParams = launchParams;

            webSocketServerManager.SendRequest(IOTLink.Platform.WebSocket.RequestTypeServer.REQUEST_SHOW_NOTIFICATION, data);
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
            ServiceAddon[] internalAddons = new ServiceAddon[] { new ReadFileService() };
            //ServiceAddon[] internalAddons = new ServiceAddon[] { };
            if (internalAddons.Length == 0)
                return;

            LoggerHelper.Info("Loading {0} internal addons", internalAddons.Length);
            foreach (ServiceAddon addon in internalAddons)
            {
                AddonInfo addonInfo = new AddonInfo
                {
                    AddonName = addon.GetType().Name,
                    AddonPath = PathHelper.BaseAppPath(),
                    AddonFile = string.Empty,
                    Internal = true,
                    AddonId = addon.GetType().Name,
                    ServiceAddon = addon
                };

                if (addonInfo.ServiceAddon != null)
                {
                    addonInfo.ServiceAddon.SetAddonInfo(addonInfo);
                    addonInfo.ServiceAddon.SetCurrentPath(addonInfo.AddonPath);
                    addonInfo.ServiceAddon.Init(this);
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
                        if (addonInfo.ServiceAddon != null)
                        {
                            addonInfo.ServiceAddon.SetAddonInfo(addonInfo);
                            addonInfo.ServiceAddon.SetCurrentPath(addonInfo.AddonPath);
                            addonInfo.ServiceAddon.Init(this);
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
        /// Build the topic name including the origin addon information.
        /// </summary>
        /// <param name="addon">Origin <see cref="ServiceAddon"/> object</param>
        /// <param name="topic">String containing the desired topic</param>
        /// <returns></returns>
        internal string BuildTopicName(ServiceAddon addon, string topic)
        {
            if (addon == null)
                return string.Empty;

            topic = topic.Split('/').Select(x => StringHelper.PascalToKebabCase(x)).Aggregate((x, y) => x + '/' + y);
            string addonId = StringHelper.PascalToKebabCase(addon.GetAppInfo().AddonId);
            return MQTTHelper.SanitizeTopic(string.Format("{0}/{1}", addonId, topic));
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
                if (addonInfo.ServiceAddon != null)
                    addonInfo.ServiceAddon.Raise_OnConfigReloadHandler(sender, e);
            }
        }

        /// <summary>
        /// Broadcast system session change to all addons.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e"><see cref="SessionChangeEventArgs"/> object</param>
        internal void Raise_OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            List<AddonInfo> addons = GetAddonsList();
            foreach (AddonInfo addonInfo in addons)
            {
                if (addonInfo.ServiceAddon != null)
                    addonInfo.ServiceAddon.Raise_OnSessionChange(sender, e);
            }
        }

        /// <summary>
        /// Broadcast MQTT Client connection to all addons.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e"><see cref="MQTTEventEventArgs"/> object</param>
        internal void Raise_OnMQTTConnected(object sender, MQTTEventEventArgs e)
        {
            List<AddonInfo> addons = GetAddonsList();
            foreach (AddonInfo addonInfo in addons)
            {
                if (addonInfo.ServiceAddon != null)
                    addonInfo.ServiceAddon.Raise_OnMQTTConnected(sender, e);
            }
        }

        /// <summary>
        /// Broadcast MQTT Client disconnection to all addons.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e"><see cref="MQTTEventEventArgs"/> object</param>
        internal void Raise_OnMQTTDisconnected(object sender, MQTTEventEventArgs e)
        {
            List<AddonInfo> addons = GetAddonsList();
            foreach (AddonInfo addonInfo in addons)
            {
                if (addonInfo.ServiceAddon != null)
                    addonInfo.ServiceAddon.Raise_OnMQTTDisconnected(sender, e);
            }
        }

        /// <summary>
        /// Broadcast Refresh Request
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e"><see cref="EventArgs"/> object</param>
        internal void Raise_OnRefreshRequested(object sender, EventArgs e)
        {
            List<AddonInfo> addons = GetAddonsList();
            foreach (AddonInfo addonInfo in addons)
            {
                if (addonInfo.ServiceAddon != null)
                    addonInfo.ServiceAddon.Raise_OnRefreshRequested(sender, e);
            }
        }

        /// <summary>
        /// Dispatch MQTT Message Received event to all addons 
        /// which has been subscribed to the related topic
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e"><see cref="MQTTMessageEventEventArgs"/> object</param>
        internal void Raise_OnMQTTMessageReceived(object sender, MQTTMessageEventEventArgs e)
        {
            if (_topics.ContainsKey(e.Message.Topic))
            {
                TopicSubscriptionInfo subscriptionInfo = _topics[e.Message.Topic];
                subscriptionInfo.OnMessageReceived(sender, e);
            }
        }

        /// <summary>
        /// Dispatch Agent Response
        /// </summary>
        /// <param name="username"></param>
        /// <param name="addonId"></param>
        /// <param name="addonData"></param>
        internal void Raise_OnAgentResponse(string username, string addonId, dynamic addonData)
        {
            AddonInfo addonInfo = GetAddonById(addonId);
            if (addonInfo == null)
            {
                LoggerHelper.Debug("Raise_OnAgentResponse - AddonId {0} not found", addonId);
                return;
            }

            LoggerHelper.Debug("Raise_OnAgentResponse - AddonId {0} receiving response from agent", addonId);
            addonInfo.ServiceAddon.Raise_OnAgentResponse(this, new AgentAddonResponseEventArgs
            {
                Username = username,
                Data = addonData
            });
        }
    }
}
