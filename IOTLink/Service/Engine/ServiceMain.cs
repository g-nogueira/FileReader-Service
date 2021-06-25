using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.Events.MQTT;
using IOTLinkService.Service.MQTT;
using IOTLinkService.Service.WebSockets.Server;
using System;
using System.ServiceProcess;
using System.Threading;

namespace IOTLinkService.Service.Engine
{
    public class ServiceMain
    {
        private static ServiceMain _instance;
        private bool _addonsLoaded;

        private DateTime _lastConfigChange;

        private System.Timers.Timer _processMonitorTimer;

        public static ServiceMain GetInstance()
        {
            if (_instance == null)
                _instance = new ServiceMain();

            return _instance;
        }

        private ServiceMain()
        {
            LoggerHelper.Trace("ServiceMain::ServiceMain() - Instance created.");
            ApplicationConfigHelper.Init();
            ApplicationConfigHelper.SetEngineConfigReloadHandler(OnConfigChanged);
        }

        private void OnProcessMonitorTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _processMonitorTimer.Stop();
            AgentManager.GetInstance().StartAgents();
            _processMonitorTimer.Start();
        }

        public void StartApplication()
        {
            if (_processMonitorTimer == null)
            {
                _processMonitorTimer = new System.Timers.Timer();
                _processMonitorTimer.Interval = 5 * 1000;
                _processMonitorTimer.Elapsed += OnProcessMonitorTimerElapsed;
                _processMonitorTimer.Start();
            }

            RestartMQTTClient();
            SetupWebSocket();
            SetupAddons();
        }

        public void SuspendApplication()
        {
            StopApplication();
        }

        public void ResumeApplication()
        {
            StartApplication();
        }

        public void StopApplication()
        {
            if (_processMonitorTimer != null)
            {
                _processMonitorTimer.Stop();
                _processMonitorTimer = null;
            }

            AgentManager.GetInstance().StopAgents();
            StopMQTTClient();

            LoggerHelper.GetInstance().Flush();
        }

        private void SetupAddons()
        {
            ServiceAddonManager addonsManager = ServiceAddonManager.GetInstance();
            if (!_addonsLoaded)
            {
                addonsManager.LoadAddons();
                _addonsLoaded = true;
            }
        }

        private void SetupWebSocket()
        {
            WebSocketServerManager webSocketManager = WebSocketServerManager.GetInstance();
            webSocketManager.Init();
        }


        private void SetupMQTTHandlers()
        {
            MQTTClientManager client = MQTTClientManager.GetInstance();
            client.OnMQTTConnected += OnMQTTConnected;
            client.OnMQTTDisconnected += OnMQTTDisconnected;
            client.OnMQTTMessageReceived += OnMQTTMessageReceived;
            client.OnMQTTRefreshMessageReceived += OnMQTTRefreshMessageReceived;
            
        }

        private void RemoveMQTTHandlers()
        {
            MQTTClientManager client = MQTTClientManager.GetInstance();
            client.OnMQTTConnected -= OnMQTTConnected;
            client.OnMQTTDisconnected -= OnMQTTDisconnected;
            client.OnMQTTMessageReceived -= OnMQTTMessageReceived;
            client.OnMQTTRefreshMessageReceived -= OnMQTTRefreshMessageReceived;
        }

        private void StopMQTTClient()
        {
            MQTTClientManager client = MQTTClientManager.GetInstance();

            client.Stop();
            client.CleanEvents();
            RemoveMQTTHandlers();

            Thread.Sleep(1000);
        }


        private void RestartMQTTClient()
        {
            MQTTClientManager client = MQTTClientManager.GetInstance();

            client.Stop();
            client.CleanEvents();
            RemoveMQTTHandlers();
            SetupMQTTHandlers();
            client.Start();
            
            Thread.Sleep(1000);
        }

        private void OnConfigChanged(object sender, ConfigReloadEventArgs e)
        {
            if (_lastConfigChange == null || _lastConfigChange.AddSeconds(1) <= DateTime.Now)
            {
                LoggerHelper.Info("ServiceMain::OnConfigChanged() - Changes to configuration.yaml detected. Reloading.");
                ServiceAddonManager addonsManager = ServiceAddonManager.GetInstance();

                RestartMQTTClient();
                SetupWebSocket();
                addonsManager.Raise_OnConfigReloadHandler(this, e);

                _lastConfigChange = DateTime.Now;
            }
        }

        private void OnMQTTConnected(object sender, MQTTEventEventArgs e)
        {
            LoggerHelper.Verbose("ServiceMain::OnMQTTConnected() - MQTT Connected");

            ServiceAddonManager addonsManager = ServiceAddonManager.GetInstance();
            addonsManager.Raise_OnMQTTConnected(sender, e);
        }

        private void OnMQTTDisconnected(object sender, MQTTEventEventArgs e)
        {
            LoggerHelper.Verbose("ServiceMain::OnMQTTDisconnected() - MQTT Disconnected");
            ServiceAddonManager addonsManager = ServiceAddonManager.GetInstance();
            addonsManager.Raise_OnMQTTDisconnected(sender, e);
        }

        private void OnMQTTMessageReceived(object sender, MQTTMessageEventEventArgs e)
        {
            LoggerHelper.Debug("ServiceMain::OnMQTTMessageReceived() - MQTT Message Received");
            ServiceAddonManager addonsManager = ServiceAddonManager.GetInstance();
            addonsManager.Raise_OnMQTTMessageReceived(sender, e);
        }

        private void OnMQTTRefreshMessageReceived(object sender, EventArgs e)
        {
            LoggerHelper.Debug("ServiceMain::OnMQTTRefreshMessageReceived() - MQTT Refresh Message Received");
            ServiceAddonManager addonsManager = ServiceAddonManager.GetInstance();
            addonsManager.Raise_OnRefreshRequested(sender, e);
        }

        public void OnSessionChange(string username, int sessionId, SessionChangeReason reason)
        {
            LoggerHelper.Verbose("ServiceMain::OnSessionChange() - Session Changed");

            SessionChangeEventArgs args = new SessionChangeEventArgs
            {
                Username = username,
                SessionId = sessionId,
                Reason = reason
            };

            ServiceAddonManager addonsManager = ServiceAddonManager.GetInstance();
            addonsManager.Raise_OnSessionChange(this, args);
        }
    }
}
