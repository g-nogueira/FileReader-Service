using System;

namespace IOTLinkAPI.Platform.Events
{
    public class AgentAddonResponseEventArgs : EventArgs
    {
        public string Username { get; set; }
        public dynamic Data { get; set; }
    }
}
