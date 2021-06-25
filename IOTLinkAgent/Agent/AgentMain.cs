using IOTLinkAgent.Agent.WSClient;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using System;
using System.Collections.Generic;

namespace IOTLinkAgent.Agent
{
    internal class AgentMain
    {
        private static readonly int MINIMUM_DELAY_CONFIG_CHANGE = 1;

        private static AgentMain _instance;

        private DateTime _lastConfigChange;
        private string _webSocketUri;

        public static AgentMain GetInstance()
        {
            if (_instance == null)
                _instance = new AgentMain();

            return _instance;
        }

        private AgentMain()
        {
            LoggerHelper.Trace("AgentMain instance created.");
            ApplicationConfigHelper.Init();
            ApplicationConfigHelper.SetEngineConfigReloadHandler(OnConfigChanged);
        }

        internal void Init(List<string> args)
        {
            LoggerHelper.Trace("AgentMain::Init() - Initialized.");

            string uri = string.Concat(args);
            if (!string.IsNullOrWhiteSpace(uri))
            {
                LoggerHelper.Verbose("Initializing WebSocketClient - Server URI: {0}", uri);

                _webSocketUri = uri;
                WebSocketClient.GetInstance().Init(_webSocketUri);

                SetupAgent();
            }
        }

        private void OnConfigChanged(object sender, ConfigReloadEventArgs e)
        {
            if (_lastConfigChange == null || _lastConfigChange.AddSeconds(MINIMUM_DELAY_CONFIG_CHANGE) <= DateTime.Now)
            {
                LoggerHelper.Info("Changes to configuration.yaml detected. Reloading.");
                AgentAddonManager addonManager = AgentAddonManager.GetInstance();

                SetupAgent();
                addonManager.Raise_OnConfigReloadHandler(this, e);

                _lastConfigChange = DateTime.Now;
            }
        }

        private void SetupAgent()
        {
            LoggerHelper.Debug("Initializing AgentAddonManager");

            AgentAddonManager addonManager = AgentAddonManager.GetInstance();
            addonManager.LoadAddons();
        }
    }
}
