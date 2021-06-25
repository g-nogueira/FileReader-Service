using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events.MQTT;
using IOTLinkAPI.Platform.HomeAssistant;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static IOTLinkAPI.Platform.Events.MQTT.MQTTHandlers;

namespace IOTLinkService.Service.MQTT
{
    internal class MQTTClient
    {
        private static MQTTClient _instance;

        private MqttConfig _config;

        private IManagedMqttClient _client;
        private IMqttClientOptions _options;
        private IManagedMqttClientOptions _managedOptions;

        private readonly object connectionRequestLock = new object();
        private readonly object connectionEventLock = new object();

        public event MQTTEventHandler OnMQTTConnected;
        public event MQTTEventHandler OnMQTTDisconnected;
        public event MQTTMessageEventHandler OnMQTTMessageReceived;
        public event MQTTRefreshMessageEventHandler OnMQTTRefreshMessageReceived;

        public static MQTTClient GetInstance()
        {
            if (_instance == null)
                _instance = new MQTTClient();

            return _instance;
        }

        private MQTTClient()
        {
            LoggerHelper.Trace("MQTTClient::MQTTClient() - Instance created.");
        }

        /// <summary>
        /// Initialize the MQTT Client.
        /// </summary>
        /// <returns>Boolean</returns>
        internal bool Init()
        {
            _config = MqttConfig.FromConfiguration(ApplicationConfigHelper.GetEngineConfig().GetValue("mqtt"));

            // Configuration not found
            if (_config == null)
            {
                LoggerHelper.Warn("MQTTClient::Init() - MQTT is disabled or not configured yet.");
                return false;
            }

            // No broker information
            if ((_config.TCP == null || !_config.TCP.Enabled) && (_config.WebSocket == null || !_config.WebSocket.Enabled))
            {
                LoggerHelper.Error("MQTTClient::Init() - You need to configure TCP or WebSocket connection");
                return false;
            }

            // Ambiguous broker information
            if ((_config.TCP != null && _config.TCP.Enabled) && (_config.WebSocket != null && _config.WebSocket.Enabled))
            {
                LoggerHelper.Error("MQTTClient::Init() - You need to disable TCP or WebSocket connection. Cannot use both together.");
                return false;
            }

            var mqttOptionBuilder = new MqttClientOptionsBuilder();

            // Credentials
            if (_config.Credentials != null && !string.IsNullOrWhiteSpace(this._config.Credentials.Username))
                mqttOptionBuilder = mqttOptionBuilder.WithCredentials(this._config.Credentials.Username, this._config.Credentials.Password);

            // TCP Connection
            if (_config.TCP != null && _config.TCP.Enabled)
            {
                if (string.IsNullOrWhiteSpace(_config.TCP.Hostname))
                {
                    LoggerHelper.Warn("MQTTClient::Init() - MQTT TCP Hostname not configured yet.");
                    return false;
                }

                mqttOptionBuilder = mqttOptionBuilder.WithTcpServer(_config.TCP.Hostname, _config.TCP.Port);

                if (_config.TCP.Secure)
                    mqttOptionBuilder = mqttOptionBuilder.WithTls(GetTlsOptions(_config.TCP.TlsConfig));
            }

            // WebSocket Connection
            if (_config.WebSocket != null && _config.WebSocket.Enabled)
            {
                if (string.IsNullOrWhiteSpace(_config.WebSocket.URI))
                {
                    LoggerHelper.Warn("MQTTClient::Init() - MQTT WebSocket URI not configured yet.");
                    return false;
                }

                mqttOptionBuilder = mqttOptionBuilder.WithWebSocketServer(_config.WebSocket.URI);

                if (_config.WebSocket.Secure)
                    mqttOptionBuilder = mqttOptionBuilder.WithTls(GetTlsOptions(_config.TCP.TlsConfig));
            }

            // Client ID
            if (!string.IsNullOrEmpty(_config.ClientId))
                mqttOptionBuilder = mqttOptionBuilder.WithClientId(_config.ClientId);
            else
                mqttOptionBuilder = mqttOptionBuilder.WithClientId(Environment.MachineName);

            // Clean Session
            if (_config.CleanSession)
                mqttOptionBuilder = mqttOptionBuilder.WithCleanSession();

            // LWT
            if (_config.LWT != null && _config.LWT.Enabled)
            {
                if (!string.IsNullOrWhiteSpace(_config.LWT.DisconnectMessage))
                {
                    mqttOptionBuilder = mqttOptionBuilder.WithWillMessage(GetLWTMessage(_config.LWT.DisconnectMessage));
                }
                else
                {
                    LoggerHelper.Warn("MQTTClient::Init() - LWT Disabled - LWT disconnected message is empty or null. Fix your configuration.yaml");
                }
            }

            // Keep-Alive Period
            if (_config.KeepAlivePeriod > 0)
                mqttOptionBuilder = mqttOptionBuilder.WithKeepAlivePeriod(TimeSpan.FromSeconds(_config.KeepAlivePeriod));


            var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder();
            managedMqttClientOptions = managedMqttClientOptions.WithMaxPendingMessages(_config.MaxPendingMessages);
            managedMqttClientOptions = managedMqttClientOptions.WithPendingMessagesOverflowStrategy(MQTTnet.Server.MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage);

            if (_config.AutoReconnectDelay > 0)
                managedMqttClientOptions = managedMqttClientOptions.WithAutoReconnectDelay(TimeSpan.FromSeconds(_config.AutoReconnectDelay));

            // Build all options
            _options = mqttOptionBuilder.Build();
            _managedOptions = managedMqttClientOptions.WithClientOptions(_options).Build();

            LoggerHelper.Trace("MQTTClient::Init() - MQTT Init finished.");
            return true;
        }

        /// <summary>
        /// Try to connect to the configured broker.
        /// Every attempt a delay of (5 * attemps, max 60) seconds is executed.
        /// </summary>
        internal void Connect()
        {
            lock (connectionRequestLock)
            {
                LoggerHelper.Info("MQTTClient::Connect() - Trying to connect to broker: {0}.", GetBrokerInfo());

                _client = new MqttFactory().CreateManagedMqttClient();
                _client.UseConnectedHandler(OnConnectedHandler);
                _client.UseDisconnectedHandler(OnDisconnectedHandler);
                _client.UseApplicationMessageReceivedHandler(OnApplicationMessageReceivedHandler);

                _client.StartAsync(_managedOptions).GetAwaiter().GetResult();

                LoggerHelper.System("ALL YOUR MQTT TOPICS WILL START WITH {0}", MQTTHelper.GetFullTopicName(_config.Prefix));
            }
        }

        internal void CleanEvents()
        {
            OnMQTTConnected = null;
            OnMQTTDisconnected = null;
            OnMQTTMessageReceived = null;
            OnMQTTRefreshMessageReceived = null;
        }

        /// <summary>
        /// Disconnect from the broker
        /// </summary>
        internal void Disconnect(bool skipLastWill = false)
        {
            lock (connectionRequestLock)
            {
                LoggerHelper.Info("MQTTClient::Disconnect({0}) - Trying to disconnect from broker: {1}.", skipLastWill, GetBrokerInfo());
                if (_client == null)
                    return;

                if (_client.IsConnected && !skipLastWill)
                {
                    LoggerHelper.Verbose("MQTTClient::Disconnect({0}) - Sending LWT message before disconnecting.", skipLastWill);
                    SendLWTDisconnect();
                }

                try
                {
                    _client.StopAsync().GetAwaiter().GetResult();
                    _client.Dispose();
                }
                finally
                {
                    _client = null;
                }
            }
        }

        /// <summary>
        /// Return the current connection state
        /// </summary>
        /// <returns>Boolean</returns>
        internal bool IsConnected()
        {
            return _client != null && _client.IsConnected;
        }

        /// <summary>
        /// Return if LWT is enabled
        /// </summary>
        /// <returns></returns>
        internal bool IsLastWillEnabled()
        {
            return _config.LWT != null && _config.LWT.Enabled;
        }

        internal void SendLWTConnect()
        {
            if (!IsLastWillEnabled())
            {
                LoggerHelper.Verbose("MQTTClient::SendLWTConnect() - LWT is disabled.");
                return;
            }

            if (!_client.IsConnected)
            {
                LoggerHelper.Verbose("MQTTClient::SendLWTConnect() - MQTT is disconnected. Skipping LWT.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_config.LWT.ConnectMessage))
            {
                LoggerHelper.Verbose("MQTTClient::SendLWTConnect() - MQTT message is empty. Skipping LWT.");
                return;
            }

            LoggerHelper.Verbose("MQTTClient::SendLWTConnect() - Sending LWT Connected");
            _client.PublishAsync(GetLWTMessage(_config.LWT.ConnectMessage)).GetAwaiter().GetResult();
        }

        internal void SendLWTDisconnect()
        {
            if (_client == null || !_client.IsConnected)
                return;

            if (IsLastWillEnabled() && !string.IsNullOrWhiteSpace(_config.LWT.DisconnectMessage))
            {
                LoggerHelper.Verbose("MQTTClient::SendLWTDisconnect() - Sending LWT Disconnected");
                _client.PublishAsync(GetLWTMessage(_config.LWT.DisconnectMessage)).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Publish a message to the connected broker
        /// </summary>
        /// <param name="topic">String containg the topic</param>
        /// <param name="message">String containg the message</param>
        internal void PublishMessage(string topic, string message)
        {
            if (_client == null || !_client.IsConnected)
            {
                LoggerHelper.Verbose("MQTTClient::PublishMessage() - MQTT Client not connected. Skipping.");
                return;
            }

            if (string.IsNullOrWhiteSpace(topic))
            {
                LoggerHelper.Verbose("MQTTClient::PublishMessage() - Empty or invalid topic name. Skipping.");
                return;
            }

            topic = MQTTHelper.GetFullTopicName(_config.Prefix, topic);
            if (message == null)
                message = string.Empty;

            LoggerHelper.Trace("MQTTClient::PublishMessage() - Publishing to {0}: {1}", topic, message);

            MqttApplicationMessage mqttMsg = BuildMQTTMessage(topic, Encoding.UTF8.GetBytes(message), _config.Messages);
            _client.PublishAsync(mqttMsg).GetAwaiter().GetResult();
        }

        internal void PublishDiscoveryMessage(string stateTopic, string preffixName, HassDiscoveryOptions discoveryOptions)
        {
            if (_client == null || !_client.IsConnected)
            {
                LoggerHelper.Verbose("MQTTClient::PublishDiscoveryMessage() - MQTT Client not connected. Skipping.");
                return;
            }

            if (!_config.Discovery.Enabled)
            {
                LoggerHelper.Verbose("MQTTClient::PublishDiscoveryMessage() - MQTT Discovery Disabled");
                return;
            }

            var availabilityTopic = MQTTHelper.GetFullTopicName(_config.Prefix, "LWT");
            var topic = MQTTHelper.GetFullTopicName(_config.Prefix, stateTopic);
            var machineName = Environment.MachineName;
            var machineFullName = PlatformHelper.GetFullMachineName().Replace("\\", " ");

            if (!_config.Discovery.DomainPrefix)
                machineFullName = machineName;

            var machineId = machineName.Replace(" ", "_");
            var uniqueId = string.Format("{0}_{1}_{2}", machineFullName, preffixName, discoveryOptions.Id).Replace(" ", "_").ToLower();
            var discoveryJson = new HassDiscoveryJsonClass()
            {
                Name = string.Format("{0} {1}", machineName, discoveryOptions.Name),
                UniqueId = uniqueId,
                AvailabilityTopic = availabilityTopic,
                PayloadAvailable = "ON",
                PayloadNotAvailable = "OFF",
            };

            if (discoveryOptions.Component == HomeAssistantComponent.Camera)
                discoveryJson.Topic = topic;
            else
                discoveryJson.StateTopic = topic;

            if (!string.IsNullOrEmpty(discoveryOptions.Unit))
                discoveryJson.UnitOfMeasurement = discoveryOptions.Unit;

            if (!string.IsNullOrEmpty(discoveryOptions.ValueTemplate))
                discoveryJson.ValueTemplate = discoveryOptions.ValueTemplate;

            if (!string.IsNullOrEmpty(discoveryOptions.Icon))
                discoveryJson.Icon = discoveryOptions.Icon;

            if (!string.IsNullOrEmpty(discoveryOptions.DeviceClass))
                discoveryJson.DeviceClass = discoveryOptions.DeviceClass;

            if (!string.IsNullOrEmpty(discoveryOptions.PayloadOff))
                discoveryJson.PayloadOff = discoveryOptions.PayloadOff;

            if (!string.IsNullOrEmpty(discoveryOptions.PayloadOn))
                discoveryJson.PayloadOn = discoveryOptions.PayloadOn;

            discoveryJson.Device = new Device()
            {
                Identifiers = new string[1]
                {
                    string.Format("{0}_{1}", machineId, preffixName)
                },
                Manufacturer = "IOTLink " + AssemblyHelper.GetCurrentVersion(),
                Model = Environment.UserDomainName,
                Name = string.Format("{0} {1}", machineName, preffixName),
            };

            var componentTopic = discoveryOptions.Component.ToString().PascalToSnakeCase();
            var configTopic = string.Format("{0}/{1}/{2}/{3}/{4}", _config.Discovery.TopicPrefix, componentTopic, "iotlink", uniqueId, "config");
            var msgConfig = new MqttConfig.MsgConfig()
            {
                Retain = true
            };

            var jsonString = JsonConvert.SerializeObject(discoveryJson, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var mqttMsg = BuildMQTTMessage(configTopic, Encoding.UTF8.GetBytes(jsonString), msgConfig);

            _client.PublishAsync(mqttMsg).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Publish a message to the connected broker
        /// </summary>
        /// <param name="topic">String containg the topic</param>
        /// <param name="message">Message bytes[]</param>
        internal void PublishMessage(string topic, byte[] message)
        {
            if (_client == null || !_client.IsConnected)
            {
                LoggerHelper.Verbose("MQTTClient::PublishMessage() - MQTT Client not connected. Skipping.");
                return;
            }

            if (string.IsNullOrWhiteSpace(topic))
            {
                LoggerHelper.Verbose("MQTTClient::PublishMessage() - Empty or invalid topic name. Skipping.");
                return;
            }

            topic = MQTTHelper.GetFullTopicName(_config.Prefix, topic);
            if (message == null)
                message = new byte[] { };

            LoggerHelper.Trace("MQTTClient::PublishMessage() - Publishing to {0}: ({1} bytes)", topic, message?.Length);

            MqttApplicationMessage mqttMsg = BuildMQTTMessage(topic, message, _config.Messages);
            _client.PublishAsync(mqttMsg).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Handle broker connection
        /// </summary>
        /// <param name="arg"><see cref="MqttClientConnectedEventArgs"/> event</param>
        /// <returns></returns>
        private void OnConnectedHandler(MqttClientConnectedEventArgs arg)
        {
            lock (connectionEventLock)
            {
                LoggerHelper.Info("MQTTClient::OnConnectedHandler() - MQTT Connected");

                // Fire Connected Event
                MQTTEventEventArgs mqttEvent = new MQTTEventEventArgs(MQTTEventEventArgs.MQTTEventType.Connect, arg);
                OnMQTTConnected?.Invoke(this, mqttEvent);

                // Subscribe to ALL Messages
                SubscribeTopic(MQTTHelper.GetFullTopicName(_config.Prefix, "#"));

                if (!string.IsNullOrWhiteSpace(_config.GlobalPrefix))
                    SubscribeTopic(MQTTHelper.GetGlobalTopicName(_config.GlobalPrefix, "#"));

                SendLWTConnect();
            }
        }

        /// <summary>
        /// Handle broker disconnection.
        /// </summary>
        /// <param name="arg"><see cref="MqttClientDisconnectedEventArgs"/> event</param>
        /// <returns></returns>
        private void OnDisconnectedHandler(MqttClientDisconnectedEventArgs arg)
        {
            lock (connectionEventLock)
            {
                LoggerHelper.Info("MQTTClient::OnDisconnectedHandler() - MQTT Disconnected");

                MQTTEventEventArgs mqttEvent = new MQTTEventEventArgs(MQTTEventEventArgs.MQTTEventType.Disconnect, arg);
                OnMQTTDisconnected?.Invoke(this, mqttEvent);
            }
        }

        /// <summary>
        /// Handle received messages from the broker
        /// </summary>
        /// <param name="arg"><see cref="MqttApplicationMessageReceivedEventArgs"/> event</param>
        /// <returns></returns>
        private void OnApplicationMessageReceivedHandler(MqttApplicationMessageReceivedEventArgs arg)
        {
            LoggerHelper.Trace("MQTTClient::OnApplicationMessageReceivedHandler() - MQTT Message Received - Topic: {0}", arg.ApplicationMessage.Topic);

            // Fire event
            MQTTMessage message = GetMQTTMessage(arg);
            if (string.Compare(message.Topic, "refresh", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                SendRefresh();
            }
            else
            {
                MQTTMessageEventEventArgs mqttEvent = new MQTTMessageEventEventArgs(MQTTEventEventArgs.MQTTEventType.MessageReceived, message, arg);
                OnMQTTMessageReceived?.Invoke(this, mqttEvent);
            }
        }

        /// <summary>
        /// Send refresh request event
        /// </summary>
        private void SendRefresh()
        {
            OnMQTTRefreshMessageReceived?.Invoke(this, EventArgs.Empty);
            SendLWTConnect();
        }

        /// <summary>
        /// Subscribe to a topic
        /// </summary>
        /// <param name="topic">String containg the topic</param>
        private void SubscribeTopic(string topic)
        {
            LoggerHelper.Trace("MQTTClient::SubscribeTopic() - Subscribing to {0}", topic);
            _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build()).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Transform the <see cref="MqttApplicationMessageReceivedEventArgs"/> into <see cref="MQTTMessage"/>
        /// </summary>
        /// <param name="arg"><see cref="MqttApplicationMessageReceivedEventArgs"/> object</param>
        /// <returns><see cref="MQTTMessage"/> object</returns>
        private MQTTMessage GetMQTTMessage(MqttApplicationMessageReceivedEventArgs arg)
        {
            if (arg == null)
                return null;

            MqttApplicationMessage mqttMsg = arg.ApplicationMessage;
            MQTTMessage message = new MQTTMessage
            {
                FullTopic = mqttMsg.Topic,
                Topic = GetMessageTopic(mqttMsg.Topic),
                ContentType = mqttMsg.ContentType,
                Retain = mqttMsg.Retain,
                Payload = mqttMsg.Payload
            };

            return message;
        }

        /// <summary>
        /// Create the LWT message
        /// </summary>
        /// <returns>LWT message</returns>
        private MqttApplicationMessage GetLWTMessage(string message)
        {
            string topic = MQTTHelper.GetFullTopicName(_config.Prefix, "LWT");
            return BuildMQTTMessage(topic, Encoding.UTF8.GetBytes(message), _config.LWT);
        }

        /// <summary>
        /// Build MQTT message ready to be published based on user configurations
        /// </summary>
        /// <param name="topic">Full topic name</param>
        /// <param name="payload">Payload (bytes[])</param>
        /// <param name="msgConfig">Configuration to be used (optional)</param>
        /// <param name="builder">Builder instance to be used (optional)</param>
        /// <returns>MqttApplicationMessage instance ready to be sent</returns>
        private MqttApplicationMessage BuildMQTTMessage(string topic, byte[] payload, MqttConfig.MsgConfig msgConfig = null, MqttApplicationMessageBuilder builder = null)
        {
            if (builder == null)
                builder = new MqttApplicationMessageBuilder();

            // Topic and Payload
            builder = builder.WithTopic(topic);
            builder = builder.WithPayload(payload);

            if (msgConfig != null)
            {
                // Retain
                builder = builder.WithRetainFlag(msgConfig.Retain);

                // QoS
                switch (msgConfig.QoS)
                {
                    case 0:
                        builder = builder.WithAtMostOnceQoS();
                        break;
                    case 1:
                        builder = builder.WithAtLeastOnceQoS();
                        break;
                    case 2:
                        builder = builder.WithExactlyOnceQoS();
                        break;
                    default:
                        LoggerHelper.Warn("MQTTClient::BuildMQTTMessage() - Wrong LWT QoS configuration. Defaulting to 0.");
                        builder = builder.WithAtMostOnceQoS();
                        break;
                }
            }

            return builder.Build();
        }

        /// <summary>
        /// Return the broker information
        /// </summary>
        /// <returns>String containing the broker information</returns>
        private string GetBrokerInfo()
        {
            if (_config.WebSocket != null && _config.WebSocket.Enabled)
            {
                if (_config.WebSocket.Secure)
                    return string.Format("wss://{0}", _config.WebSocket.URI);
                else
                    return string.Format("ws://{0}", _config.WebSocket.URI);
            }

            if (_config.TCP != null && _config.TCP.Enabled)
            {
                if (_config.TCP.Secure)
                    return string.Format("tls://{0}:{1}", _config.TCP.Hostname, _config.TCP.Port);
                else
                    return string.Format("tcp://{0}:{1}", _config.TCP.Hostname, _config.TCP.Port);
            }

            return "Unknown";
        }

        private MqttClientOptionsBuilderTlsParameters GetTlsOptions(MqttConfig.TlsConfig tlsConfig)
        {
            return new MqttClientOptionsBuilderTlsParameters()
            {
                UseTls = true,
                AllowUntrustedCertificates = tlsConfig.AllowUntrustedCertificates,
                IgnoreCertificateChainErrors = tlsConfig.IgnoreCertificateChainErrors,
                IgnoreCertificateRevocationErrors = tlsConfig.IgnoreCertificateRevocationErrors,
                Certificates = GetCertificateList(tlsConfig)
            };
        }

        private List<X509Certificate> GetCertificateList(MqttConfig.TlsConfig tlsConfig)
        {
            List<X509Certificate> certList = new List<X509Certificate>();
            if (!string.IsNullOrWhiteSpace(tlsConfig.CACertificate))
            {
                X509Certificate caCert = X509Certificate.CreateFromCertFile(tlsConfig.CACertificate);
                certList.Add(caCert);
            }

            if (!string.IsNullOrWhiteSpace(tlsConfig.ClientCertificate))
            {
                X509Certificate clientCert;

                if (string.IsNullOrWhiteSpace(tlsConfig.ClientCertificatePassword))
                    clientCert = new X509Certificate2(tlsConfig.ClientCertificate);
                else
                    clientCert = new X509Certificate2(tlsConfig.ClientCertificate, tlsConfig.ClientCertificatePassword);

                certList.Add(clientCert);
            }

            return certList;
        }

        /// <summary>
        /// Return message topic string
        /// </summary>
        /// <param name="topic">Topic name</param>
        /// <returns>String containing the topic string</returns>
        private string GetMessageTopic(string topic)
        {
            if (topic == null)
                return string.Empty;

            return MQTTHelper
                .SanitizeTopic(topic)
                .Replace(MQTTHelper.GetFullTopicName(_config.Prefix), "")
                .Replace(MQTTHelper.GetGlobalTopicName(_config.GlobalPrefix), "");
        }
    }
}
