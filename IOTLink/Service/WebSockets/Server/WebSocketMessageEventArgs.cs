using System;
using System.Net.WebSockets;

namespace IOTLinkService.Service.WebSockets.Server
{
    internal class WebSocketMessageEventArgs : EventArgs
    {
        public string ID { get; set; }
        public string Message { get; set; }
    }
}
