using IOTLink.Platform.WebSocket;
using IOTLinkAgent.Agent.Notifications;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.WebSocket;
using IOTLinkAPI.Platform.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using WebSocketSharp;

namespace IOTLinkAgent.Agent.WSClient
{
    internal class WebSocketClient
    {
        private static WebSocketClient _instance;

        private WebSocket _client;
        private string _webSocketUri;
        private bool _connecting;

        public static WebSocketClient GetInstance()
        {
            if (_instance == null)
                _instance = new WebSocketClient();

            return _instance;
        }

        private WebSocketClient()
        {
            LoggerHelper.Trace("WebSocketClient instance created.");
        }

        internal void Init(string uri)
        {
            if (_client != null)
            {
                LoggerHelper.Verbose("WebSocketClient instance found. Disconnecting.");
                Disconnect();
            }

            _webSocketUri = uri;
            Connect();
        }

        internal void Connect()
        {
            if (string.IsNullOrWhiteSpace(_webSocketUri) || _connecting)
            {
                LoggerHelper.Verbose("WebSocketURI is empty, null or client is already connecting. Skipping");
                return;
            }

            int tries = 0;
            _connecting = true;

            if (_client != null && _client.IsAlive)
            {
                LoggerHelper.Verbose("Previous session is alive! Closing it.");
                Disconnect();
            }

            _client = new WebSocket(_webSocketUri);
            _client.OnOpen += OnOpen;
            _client.OnClose += OnClose;
            _client.OnError += OnError;
            _client.OnMessage += OnMessageReceived;

            do
            {
                LoggerHelper.Verbose("Trying to connect to WebSocketServer: {0} (Try: {1})", _webSocketUri, (tries + 1));
                if (!_client.IsAlive)
                {
                    _client.Connect();

                    if (_client.IsAlive)
                        break;
                }

                LoggerHelper.Info("Waiting {0} seconds before trying again...", 5);
                Thread.Sleep(5000);
            } while (tries < 5);

            if (!_client.IsAlive)
            {
                LoggerHelper.Verbose("Connection not successful after 5 tries. Killing agent.");
                Environment.Exit(-1);
                return;
            }

            _connecting = false;
            LoggerHelper.Verbose("Connection successful");
        }

        internal void Disconnect()
        {
            if (_client != null && _client.IsAlive)
            {
                LoggerHelper.Verbose("Alive connection found. Closing it");
                _client.Close();
                Thread.Sleep(1000);
            }

            _client = null;
        }

        internal bool IsConnected()
        {
            return _client != null && _client.IsAlive;
        }

        internal void SendRequest(RequestTypeClient type, dynamic data = null)
        {
            SendMessage(MessageType.CLIENT_REQUEST, type, data);
        }

        internal void SendResponse(ResponseTypeClient type, dynamic data = null)
        {
            SendMessage(MessageType.CLIENT_RESPONSE, type, data);
        }

        private void OnOpen(object sender, EventArgs e)
        {
            LoggerHelper.Debug("WebSocketClient - Connection Opened.");

            Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();
            keyValuePairs.Add("username", Environment.UserName);

            SendRequest(RequestTypeClient.REQUEST_CONNECTED, keyValuePairs);
        }

        private void OnClose(object sender, CloseEventArgs arg)
        {
            LoggerHelper.Debug("WebSocketClient - Connection Closed (Clean: {0}).", arg.WasClean);
            Environment.Exit(arg.WasClean ? 0 : -1);
        }

        private void OnError(object sender, ErrorEventArgs arg)
        {
            LoggerHelper.Error("WebSocketClient - Error: {0}", arg.Message);
            Environment.Exit(-1);
        }

        private void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Type == Opcode.Close)
                return;

            if (e.RawData == null || e.RawData.Length == 0 || !e.IsText || string.IsNullOrWhiteSpace(e.Data))
            {
                LoggerHelper.Trace("OnMessageReceived - Invalid message content [1].");
                return;
            }

            string data = e.Data;
            LoggerHelper.Verbose("Message received from server");
            LoggerHelper.DataDump("Message Payload: {0}", data);
            try
            {
                dynamic json = JsonConvert.DeserializeObject<dynamic>(data);
                if (json == null || json.messageType == null || json.content == null)
                {
                    LoggerHelper.Trace("OnMessageReceived - Invalid message content [2].");
                    return;
                }

                MessageType messageType = json.messageType;
                switch (messageType)
                {
                    case MessageType.SERVER_REQUEST:
                        ParseServerRequest(json.content);
                        break;

                    case MessageType.SERVER_RESPONSE:
                        ParseServerResponse(json.content);
                        break;

                    default:
                        LoggerHelper.Warn("WebSocket server send an invalid message type ({0}) with data: {1}", json.messageType, json.data);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("OnMessageReceived - Exception: {0}", ex.ToString());
            }
        }

        private void ParseServerResponse(dynamic content)
        {
            if (content == null || content.type == null || content.data == null)
            {
                LoggerHelper.Trace("ParseServerResponse - Invalid message content.");
                return;
            }

            LoggerHelper.Trace("ParseServerResponse - Content: {0}", content);
        }

        private void ParseServerRequest(dynamic content)
        {
            if (content == null || content.type == null)
            {
                LoggerHelper.Trace("ParseServerRequest - Invalid message.");
                return;
            }

            RequestTypeServer type = content.type;
            if (content.data == null && type != RequestTypeServer.REQUEST_PING)
            {
                LoggerHelper.Trace("ParseServerRequest - Invalid message content.");
                return;
            }

            dynamic data = content.data;
            switch (type)
            {
                case RequestTypeServer.REQUEST_PING:
                    ParsePingRequest();
                    break;

                case RequestTypeServer.REQUEST_SHOW_MESSAGE:
                    ParseShowMessage(data);
                    break;

                case RequestTypeServer.REQUEST_SHOW_NOTIFICATION:
                    ParseShowNotification(data);
                    break;

                case RequestTypeServer.REQUEST_ADDON:
                    ParseAddonRequest(data);
                    break;

                default:
                    LoggerHelper.Warn("ParseServerRequest - Invalid Request Type: {0}", type);
                    break;
            }
        }

        private void ParsePingRequest()
        {
            LoggerHelper.Verbose("ParsePingRequest - Ping request received.");

            SendResponse(ResponseTypeClient.RESPONSE_PING);
        }

        private void ParseShowMessage(dynamic data)
        {
            string title = data.title;
            string message = data.message;

            LoggerHelper.Verbose("ParseShowMessage - Title: {0} Message: {1}", title, message);
            WindowsAPI.ShowMessage(title, message);
        }

        private void ParseShowNotification(dynamic data)
        {
            LoggerHelper.Trace("ParseShowNotification - Data: {0}", data);

            string title = data.title;
            string message = data.message;
            string iconUrl = data.iconUrl;
            string launchParams = data.launchParams;

            if (string.IsNullOrWhiteSpace(message))
                return;

            NotificationManager.GetInstance().ShowNotification(title, message, iconUrl, launchParams);
        }

        private void ParseAddonRequest(dynamic data)
        {
            string addonId = data.addonId;
            dynamic addonData = data.addonData;

            if (string.IsNullOrWhiteSpace(addonId))
                return;

            AgentAddonManager.GetInstance().Raise_OnAgentRequest(addonId, addonData);
        }

        private void SendMessage(MessageType messageType, dynamic contentType, dynamic data = null)
        {
            if (!IsConnected())
            {
                LoggerHelper.Verbose("Client not connected. Skipping.");
                return;
            }

            dynamic msg = new ExpandoObject();
            msg.messageType = messageType;
            msg.content = new ExpandoObject();
            msg.content.type = contentType;
            msg.content.data = data;

            string payload = JsonConvert.SerializeObject(msg, Formatting.None);

            LoggerHelper.Verbose("Sending message to server (Type: {0})", contentType.ToString());
            LoggerHelper.DataDump("Message Payload: {0}", payload);

            _client.Send(payload);
        }
    }
}
