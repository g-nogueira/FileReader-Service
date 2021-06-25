using System;
using System.ServiceProcess;

namespace IOTLinkAPI.Platform.Events
{
    public class SessionChangeEventArgs : EventArgs
    {
        public string Username { get; set; }

        public int SessionId { get; set; }

        public SessionChangeReason Reason { get; set; }
    }
}
