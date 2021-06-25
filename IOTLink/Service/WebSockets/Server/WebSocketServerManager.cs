using IOTLink.Platform.WebSocket;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.WebSocket;
using IOTLinkService.Service.Engine;
using IOTLinkService.Service.MQTT;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Timers;

namespace IOTLinkService.Service.WebSockets.Server
{
    internal class WebSocketServerManager
    {
        public const string WEBSOCKET_URI = "ws://localhost:9799";
        private static WebSocketServerManager _instance;

        private WebSocketServer _server;

        private List<WebSocketClient> _clients = new List<WebSocketClient>();

        private Timer _pingTimer;

        public static WebSocketServerManager GetInstance()
        {
            if (_instance == null)
                _instance = new WebSocketServerManager();

            return _instance;
        }

        private WebSocketServerManager()
        {
            LoggerHelper.Trace("WebSocketServerManager instance created.");
        }

        internal void Init()
        {
            if (_server != null)
            {
                LoggerHelper.Verbose("WebSocketServer instance found. Disconnecting.");
                Disconnect();
            }

            _server = new WebSocketServer();
            _server.OnMessageHandler += OnMessage;
            _server.Start(WEBSOCKET_URI);

            SetupTimer();
        }

        internal void Disconnect()
        {
            if (_server == null)
                return;

            _server.Disconnect();
            _server = null;
        }

        internal bool IsConnected()
        {
            return _server != null;
        }

        protected void SetupTimer()
        {
            if (_pingTimer == null)
            {
                _pingTimer = new Timer();
                _pingTimer.Elapsed += new ElapsedEventHandler(OnPingTimerElapsed);
            }

            _pingTimer.Stop();
            _pingTimer.Interval = 10000;
            _pingTimer.Start();
        }

        private void OnPingTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsConnected())
                return;

            try
            {
                _pingTimer.Stop();

                // Send ping request for all clients
                SendRequest(RequestTypeServer.REQUEST_PING);

                // Handle all clients which doesn't respond for about 30 seconds.
                List<WebSocketClient> timedOutClients = _clients.FindAll(x => (DateTime.Now - x.LastAck).TotalSeconds > 30);
                if (timedOutClients != null && timedOutClients.Count > 0)
                {
                    foreach (WebSocketClient client in timedOutClients)
                    {
                        LoggerHelper.Verbose("Disconnecting {0} - Ping timeout", client.ClientId);
                        _server.DisconnectClient(client.ClientId);
                    }

                    // New list without the removed clients
                    _clients = _clients.Except(timedOutClients).ToList();
                }
            }
            finally
            {
                _pingTimer.Start();
            }
        }

        protected bool HasClient(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return false;

            return _clients.Any(x => string.Compare(x.ClientId, clientId) == 0);
        }

        protected WebSocketClient GetClientById(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return null;

            return _clients.FirstOrDefault(x => string.Compare(x.ClientId, clientId) == 0);
        }

        protected WebSocketClient GetClientByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            return _clients.FirstOrDefault(x => string.Compare(x.UserName, username) == 0);
        }

        protected void OnMessage(object sender, WebSocketMessageEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(e.Message))
                {
                    LoggerHelper.Trace("OnMessage - Empty message received.");
                    return;
                }

                LoggerHelper.Trace("Message received from client {0}", e.ID);
                LoggerHelper.DataDump("Message Payload: {0}", e.Message);

                dynamic json = JsonConvert.DeserializeObject<dynamic>(e.Message);
                if (json == null || json.messageType == null || json.content == null)
                {
                    LoggerHelper.Trace("OnMessage - Invalid message content.");
                    return;
                }

                MessageType messageType = json.messageType;
                switch (messageType)
                {
                    case MessageType.CLIENT_REQUEST:
                        ParseClientRequest(e.ID, json.content);
                        break;

                    case MessageType.CLIENT_RESPONSE:
                        ParseClientResponse(e.ID, json.content);
                        break;

                    case MessageType.API_MESSAGE:
                        ParseAPIMessage(e.ID, json.content);
                        break;

                    default:
                        LoggerHelper.Warn("WebSocket client send an invalid message type ({0}) with data: {1}", json.messageType, json.data);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("OnMessage - Exception: {0}", ex.ToString());
            }
        }

        internal void ParseClientRequest(string clientId, dynamic content)
        {
            if (content == null || content.type == null || content.data == null)
            {
                LoggerHelper.Trace("ParseClientRequest - Invalid message content.");
                return;
            }

            RequestTypeClient type = content.type;
            dynamic data = content.data;
            switch (type)
            {
                case RequestTypeClient.REQUEST_CONNECTED:
                    ParseClientConnected(clientId, data);
                    break;

                case RequestTypeClient.REQUEST_PUBLISH_MESSAGE:
                    ParsePublishMessage(data);
                    break;

                default:
                    LoggerHelper.Warn("ParseClientRequest - Invalid Request Type: {0}", type);
                    break;
            }
        }

        internal void ParseClientConnected(string clientId, dynamic data)
        {
            string username = data.username;
            if (string.IsNullOrWhiteSpace(username) || HasClient(clientId))
                return;

            username = username.Trim().ToLowerInvariant();
            WebSocketClient client = new WebSocketClient
            {
                ClientId = clientId,
                UserName = username,
                State = WebSocketClientState.STATE_CONNECTED,
                LastAck = DateTime.Now
            };

            _clients.Add(client);
        }

        internal void ParsePublishMessage(dynamic data)
        {
            string topic = data.topic;
            byte[] payload = data.payload;

            if (string.IsNullOrWhiteSpace(topic) || payload == null || payload.Length == 0)
                return;

            MQTTClientManager.GetInstance().PublishMessage(topic, payload);
        }

        internal void ParseClientResponse(string clientId, dynamic content)
        {
            if (content == null || content.type == null)
            {
                LoggerHelper.Trace("ParseClientResponse - Invalid message.");
                return;
            }

            ResponseTypeClient type = content.type;
            if (content.data == null && type != ResponseTypeClient.RESPONSE_PING)
            {
                LoggerHelper.Trace("ParseClientResponse - Invalid message content.");
                return;
            }

            dynamic data = content.data;
            switch (type)
            {
                case ResponseTypeClient.RESPONSE_PING:
                    ParsePingResponse(clientId);
                    break;

                case ResponseTypeClient.RESPONSE_ADDON:
                    ParseAddonResponse(clientId, data);
                    break;

                default:
                    LoggerHelper.Warn("ParseClientResponse - Invalid Request Type: {0}", type);
                    break;
            }
        }

        private void ParsePingResponse(string clientId)
        {
            if (!HasClient(clientId))
                return;

            WebSocketClient client = GetClientById(clientId);
            client.LastAck = DateTime.Now;
        }

        private void ParseAddonResponse(string clientId, dynamic data)
        {
            string addonId = data.addonId;
            dynamic addonData = data.addonData;

            if (string.IsNullOrWhiteSpace(addonId))
                return;

            if (!HasClient(clientId))
                return;

            string username = GetClientById(clientId).UserName;
            ServiceAddonManager.GetInstance().Raise_OnAgentResponse(username, addonId, addonData);
        }

        internal void ParseAPIMessage(string clientId, dynamic content)
        {
            LoggerHelper.Debug("ParseAPIMessage: {0}", content);
        }

        internal void SendRequest(RequestTypeServer type, dynamic data = null, string username = null)
        {
            if (!IsConnected())
                return;

            if (string.IsNullOrWhiteSpace(username))
                username = null;
            else
                username = username.Trim().ToLowerInvariant();

            dynamic msg = new ExpandoObject();
            msg.messageType = MessageType.SERVER_REQUEST;
            msg.content = new ExpandoObject();
            msg.content.type = type;
            msg.content.data = data;

            string payload = JsonConvert.SerializeObject(msg, Formatting.None);
            LoggerHelper.Verbose("Sending message to clients (Type: {0})", type.ToString());
            LoggerHelper.DataDump("Message Payload: {0}", payload);

            if (username == null)
            {
                _server.Broadcast(payload);
                return;
            }

            WebSocketClient client = GetClientByUsername(username);
            if (client == null)
            {
                LoggerHelper.Warn("WebSocketServer - Agent from {0} not found.", username);
                return;
            }

            _server.SendMessage(client.ClientId, payload);
        }
    }
}
