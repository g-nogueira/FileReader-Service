using System;

namespace IOTLinkService.Service.WebSockets.Server
{
    class WebSocketClient
    {
        public string ClientId { get; set; }
        public string UserName { get; set; }
        public WebSocketClientState State { get; set; }
        public DateTime LastAck { get; set; }
    }
}
