using IOTLinkAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using static IOTLinkService.Service.WebSockets.Server.WebSocketHandlers;

namespace IOTLinkService.Service.WebSockets.Server
{
    internal class WebSocketServer
    {
        private const int BUFFER_LENGTH = 2048;
        private int _count = 0;
        private HttpListener _listener;
        private bool _disconnect;

        private readonly object clientsLock = new object();

        public event WebSocketMessageEventHandler OnMessageHandler;

        private Dictionary<WebSocket, string> _clients = new Dictionary<WebSocket, string>();

        public async void Start(string listenerPrefix)
        {
            // Try to create the HttpListener
            while (_listener == null || !_listener.IsListening)
            {
                try
                {
                    if (_listener != null)
                    {
                        if (_listener.IsListening)
                        {
                            _listener.Stop();
                            Thread.Sleep(2500);
                        }

                        _listener = null;
                    }

                    _disconnect = false;
                    _listener = new HttpListener();
                    _listener.Prefixes.Add(ParseURI(listenerPrefix));
                    _listener.Start();
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error("Error while trying to create a new WebSocketServer: {0}", ex.ToString());
                }
            }

            // Listen for incoming websocket connections
            while (!_disconnect && _listener != null && _listener.IsListening)
            {
                try
                {
                    HttpListenerContext listenerContext = await _listener.GetContextAsync();
                    if (listenerContext.Request.IsWebSocketRequest && !_disconnect)
                    {
                        ProcessRequest(listenerContext);
                    }
                    else
                    {
                        listenerContext.Response.StatusCode = 400;
                        listenerContext.Response.Close();
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public void Disconnect()
        {
            _disconnect = true;

            while (_listener != null && _listener.IsListening)
            {
                try
                {
                    _listener.Close();
                    Thread.Sleep(1000);

                    if (!_listener.IsListening)
                        _listener = null;
                }
                catch (Exception)
                {

                }
            }
        }

        internal void DisconnectClient(string clientId)
        {
            IEnumerable<KeyValuePair<WebSocket, string>> entries;

            lock (clientsLock)
            {
                entries = _clients.Where(x => x.Value.CompareTo(clientId) == 0);

                foreach (var entry in entries)
                {
                    WebSocket client = entry.Key;

                    try
                    {
                        client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Error("Error while trying to disconnect client: {0}", ex);
                    }
                    finally
                    {
                        client.Dispose();
                    }
                }

            }

            try
            {
                lock (clientsLock)
                {
                    var newClients = _clients.Except(entries).ToDictionary(x => x.Key, x => x.Value);
                    _clients = newClients;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error while trying to disconnect client: {0}", ex);
            }
        }

        internal void Broadcast(string payload)
        {
            lock (clientsLock)
            {
                foreach (WebSocket client in _clients.Keys)
                {
                    SendMessage(client, payload);
                }
            }
        }

        internal void SendMessage(string clientId, string payload)
        {
            lock (clientsLock)
            {
                var entries = _clients.Where(x => x.Value.CompareTo(clientId) == 0);
                foreach (var entry in entries)
                {
                    WebSocket client = entry.Key;
                    SendMessage(client, payload);
                }
            }
        }

        private string ParseURI(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return null;

            if (uri.StartsWith("wss://"))
                uri = string.Format("https://{0}", uri.Remove(0, 6));

            if (uri.StartsWith("ws://"))
                uri = string.Format("http://{0}", uri.Remove(0, 5));

            if (!uri.EndsWith("/"))
                uri = uri + "/";

            return uri;
        }

        private async void ProcessRequest(HttpListenerContext listenerContext)
        {

            WebSocketContext webSocketContext;
            try
            {
                webSocketContext = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
                Interlocked.Increment(ref _count);
            }
            catch (Exception)
            {
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                return;
            }

            WebSocket webSocket = webSocketContext.WebSocket;
            try
            {
                int offset = 0;
                byte[] buffer = new byte[BUFFER_LENGTH];
                int free = buffer.Length;
                string currentId = string.Empty;

                if (!_clients.ContainsKey(webSocket))
                {
                    currentId = Guid.NewGuid().ToString("N");
                    _clients.Add(webSocket, currentId);
                }
                else
                {
                    currentId = _clients[webSocket];
                }

                while (!_disconnect && webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, free), CancellationToken.None);
                    offset += receiveResult.Count;
                    free -= receiveResult.Count;

                    if (!receiveResult.EndOfMessage)
                    {
                        if (free == 0)
                        {
                            var newSize = buffer.Length + BUFFER_LENGTH;
                            var newBuffer = new byte[newSize];
                            Array.Copy(buffer, 0, newBuffer, 0, offset);
                            buffer = newBuffer;
                            free = buffer.Length - offset;
                        }
                        continue;
                    }

                    if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        string data = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                        data = data.Replace("\0", string.Empty);
                        OnMessageHandler?.Invoke(this, new WebSocketMessageEventArgs { ID = currentId, Message = data });
                    }
                    else
                    {
                        if (receiveResult.MessageType == WebSocketMessageType.Binary)
                            await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "This server only accept text frames", CancellationToken.None);
                        else
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }

                    // Reset buffer
                    offset = 0;
                    buffer = new byte[BUFFER_LENGTH];
                    free = buffer.Length;
                }
            }
            catch (WebSocketException ex)
            {
                if (ex.InnerException == null || ex.InnerException.GetType() != typeof(HttpListenerException))
                    LoggerHelper.Error("Exception while running WebSocketServer: {0}", ex.ToString());
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Exception while running WebSocketServer: {0}", ex.ToString());
            }
            finally
            {
                // Clean up by disposing the WebSocket once it is closed/aborted.
                if (webSocket != null)
                {
                    if (_clients.ContainsKey(webSocket))
                        _clients.Remove(webSocket);

                    webSocket.Dispose();
                }
            }
        }

        private async void SendMessage(WebSocket webSocket, string payload)
        {
            if (webSocket == null || string.IsNullOrWhiteSpace(payload))
                return;

            bool shouldRemoveClient = false;
            try
            {
                var encoded = System.Text.Encoding.UTF8.GetBytes(payload);
                var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
                await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error while sending message to client: {0}", ex.ToString());
                shouldRemoveClient = true;
            }

            if (shouldRemoveClient)
            {
                try
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                finally
                {
                    _clients.Remove(webSocket);
                    webSocket.Dispose();
                }
            }
        }
    }
}
