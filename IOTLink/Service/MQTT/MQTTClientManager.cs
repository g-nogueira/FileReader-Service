using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events.MQTT;
using IOTLinkAPI.Platform.HomeAssistant;
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Timers;
using static IOTLinkAPI.Platform.Events.MQTT.MQTTHandlers;

namespace IOTLinkService.Service.MQTT
{
    internal class MQTTClientManager : IDisposable
    {
        enum EventType
        {
            INITIAL = 0,
            DISCONNECTED = 1,
            CONNECTED = 2
        }

        private static readonly int MINIMUM_DELAY_VERIFY_CONNECTION = 5;

        private static readonly int REFRESH_TIMER_INTERVAL = 60 * 1000;

        private static readonly int VERIFY_TIMER_INITAL_INTERVAL = 60 * 1000;
        private static readonly int VERIFY_TIMER_NORMAL_INTERVAL = 10 * 1000;

        private static MQTTClientManager _instance;

        private MQTTClient _mqttClient;

        private Timer _verifyTimer;
        private Timer _refreshTimer;

        private DateTime lastVerifyConnection = new DateTime(0L);

        private readonly object connectionLock = new object();
        private readonly object verifyLock = new object();
        private readonly object publishLock = new object();

        private EventType lastFiredEvent = EventType.INITIAL;

        public event MQTTEventHandler OnMQTTConnected;
        public event MQTTEventHandler OnMQTTDisconnected;
        public event MQTTMessageEventHandler OnMQTTMessageReceived;
        public event MQTTRefreshMessageEventHandler OnMQTTRefreshMessageReceived;

        public static MQTTClientManager GetInstance()
        {
            if (_instance == null)
                _instance = new MQTTClientManager();

            return _instance;
        }

        private MQTTClientManager()
        {
            if (_verifyTimer == null)
                _verifyTimer = new Timer();

            _verifyTimer.Interval = VERIFY_TIMER_INITAL_INTERVAL;
            _verifyTimer.Elapsed += OnVerifyTimerElapsed;
            _verifyTimer.AutoReset = true;
            _verifyTimer.Enabled = true;

            if (_refreshTimer == null)
                _refreshTimer = new Timer();

            _refreshTimer.Interval = REFRESH_TIMER_INTERVAL;
            _refreshTimer.Elapsed += OnRefreshTimerElapsed;
            _refreshTimer.AutoReset = true;
            _refreshTimer.Enabled = true;

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        public async void Start()
        {
            LoggerHelper.Info("MQTTClientManager::Start() - Initilizing MQTT");
            await Task.Run(() => Connect());
        }

        public async void Stop()
        {
            LoggerHelper.Info("MQTTClientManager::Stop() - Finishing MQTT");
            await Task.Run(() => Disconnect());
        }

        public void Dispose()
        {
            LoggerHelper.Verbose("MQTTClientManager::Dispose()");
            if (_mqttClient != null)
                _mqttClient.Disconnect();

            _mqttClient = null;
        }

        internal void CleanEvents()
        {
            OnMQTTConnected = null;
            OnMQTTDisconnected = null;
            OnMQTTMessageReceived = null;
            OnMQTTRefreshMessageReceived = null;
        }

        internal void BindEvents()
        {
            if (_mqttClient == null)
                return;

            _mqttClient.OnMQTTConnected += OnMQTTConnectedHandler;
            _mqttClient.OnMQTTDisconnected += OnMQTTDisconnectedHandler;
            _mqttClient.OnMQTTMessageReceived += OnMQTTMessageReceivedHandler;
            _mqttClient.OnMQTTRefreshMessageReceived += OnMQTTRefreshMessageReceivedHandler;
        }

        internal void PublishMessage(string addonTopic, string message)
        {
            lock (publishLock)
            {
                LoggerHelper.Verbose("MQTTClientManager::PublishMessage('{0}', '{1}')", addonTopic, message);

                try
                {
                    VerifyConnection();

                    _mqttClient.PublishMessage(addonTopic, message);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error("MQTTClientManager::PublishMessage('{0}', '{1}') -> {2}", addonTopic, message, ex.Message);
                    Disconnect(true);
                }
            }
        }

        internal void PublishMessage(string topic, byte[] message)
        {
            lock (publishLock)
            {
                LoggerHelper.Verbose("MQTTClientManager::PublishMessage('{0}', '{1}')", topic, message);

                try
                {
                    VerifyConnection();

                    _mqttClient.PublishMessage(topic, message);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error("MQTTClientManager::PublishMessage('{0}', '{1}') -> {2}", topic, message, ex.Message);
                    Disconnect(true);
                }
            }
        }

        internal void PublishDiscoveryMessage(string addonTopic, string preffixName, HassDiscoveryOptions discoveryOptions)
        {
            lock (publishLock)
            {
                LoggerHelper.Verbose("MQTTClientManager::PublishDiscoveryMessage('{0}', '{1}', '{2}')", addonTopic, preffixName, discoveryOptions);

                try
                {
                    VerifyConnection();

                    _mqttClient.PublishDiscoveryMessage(addonTopic, preffixName, discoveryOptions);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error("MQTTClientManager::PublishDiscoveryMessage('{0}', '{1}', '{2}') -> {3}", addonTopic, preffixName, discoveryOptions, ex.Message);
                    Disconnect(true);
                }
            }
        }

        private void Connect()
        {
            lock (connectionLock)
            {
                LoggerHelper.Verbose("MQTTClientManager::Connect()");
                if (_mqttClient == null)
                {
                    _mqttClient = MQTTClient.GetInstance();
                    _mqttClient.Init();
                }

                lastFiredEvent = EventType.INITIAL;
                BindEvents();
                _mqttClient.Connect();
                _verifyTimer.Start();
            }
        }

        private void Disconnect(bool skipLastWill = false)
        {
            lock (connectionLock)
            {
                LoggerHelper.Verbose("MQTTClientManager::Disconnect({0})", skipLastWill);
                if (_mqttClient == null)
                    return;

                _verifyTimer.Stop();

                _mqttClient.CleanEvents();
                _mqttClient.Disconnect(skipLastWill);
                _mqttClient = null;
                lastFiredEvent = EventType.INITIAL;
            }
        }

        private void VerifyConnection()
        {
            lock (verifyLock)
            {
                if (_mqttClient == null)
                {
                    LoggerHelper.Warn("MQTTClientManager::VerifyConnection() - MQTT Client NOT Connected. Connecting.");
                    lastVerifyConnection = DateTime.UtcNow;
                    Connect();
                    return;
                }

                if (lastVerifyConnection.AddSeconds(MINIMUM_DELAY_VERIFY_CONNECTION) >= DateTime.UtcNow)
                {
                    LoggerHelper.Trace("MQTTClientManager::VerifyConnection() - Skipping verification [Delay].");
                    return;
                }

                lastVerifyConnection = DateTime.UtcNow;
                if (!_mqttClient.IsConnected())
                {
                    LoggerHelper.Warn("MQTTClientManager::VerifyConnection() - MQTT Connection Broken. Reconnecting.");

                    Disconnect(true);
                    Connect();
                }
            }
        }

        private void OnVerifyTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                LoggerHelper.Debug("MQTTClientManager::OnVerifyTimerElapsed() - Checking MQTT Connection.");

                _verifyTimer.Stop();
                VerifyConnection();
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::OnVerifyTimerElapsed() - Error: {0}", ex);
            }
            finally
            {
                _verifyTimer.Interval = VERIFY_TIMER_NORMAL_INTERVAL;
                _verifyTimer.Start();
            }
        }

        private void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                LoggerHelper.Debug("MQTTClientManager::OnRefreshTimerElapsed() - Refreshing LWT");

                _refreshTimer.Stop();
                if (_mqttClient != null)
                    _mqttClient.SendLWTConnect();
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::OnRefreshTimerElapsed() - Error: {0}", ex);
            }
            finally
            {
                _refreshTimer.Interval = REFRESH_TIMER_INTERVAL;
                _refreshTimer.Start();
            }
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    Stop();
                    break;

                case PowerModes.Resume:
                    Disconnect(true);
                    Connect();
                    break;

                default: break;
            }
        }

        private void OnMQTTConnectedHandler(object sender, MQTTEventEventArgs e)
        {
            try
            {
                if (EventType.CONNECTED.Equals(lastFiredEvent))
                {
                    LoggerHelper.Error("MQTTClientManager::OnMQTTConnectedHandler() - DUPLICATED Connected Event");
                    return;
                }

                lastFiredEvent = EventType.CONNECTED;
                OnMQTTConnected?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::OnMQTTConnectedHandler() - Error: {0}", ex.ToString());
            }
        }

        private void OnMQTTDisconnectedHandler(object sender, MQTTEventEventArgs e)
        {
            try
            {
                if (EventType.DISCONNECTED.Equals(lastFiredEvent))
                {
                    LoggerHelper.Error("MQTTClientManager::OnMQTTDisconnectedHandler() - DUPLICATED Disconnected Event");
                    return;
                }

                lastFiredEvent = EventType.DISCONNECTED;
                OnMQTTDisconnected?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::OnMQTTDisconnectedHandler() - Error: {0}", ex.ToString());
            }
        }

        private void OnMQTTMessageReceivedHandler(object sender, MQTTMessageEventEventArgs e)
        {
            try
            {
                OnMQTTMessageReceived?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::OnMQTTMessageReceivedHandler() - Error: {0}", ex.ToString());
            }
        }

        private void OnMQTTRefreshMessageReceivedHandler(object sender, EventArgs e)
        {
            try
            {
                OnMQTTRefreshMessageReceived?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("MQTTClientManager::OnMQTTRefreshMessageReceivedHandler() - Error: {0}", ex.ToString());
            }
        }
    }
}
