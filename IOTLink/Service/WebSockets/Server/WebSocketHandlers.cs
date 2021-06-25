namespace IOTLinkService.Service.WebSockets.Server
{
    internal abstract class WebSocketHandlers
    {
        public delegate void WebSocketMessageEventHandler(object sender, WebSocketMessageEventArgs e);
    }
}
