using System;

namespace IOTLinkAPI.Platform.Events.Process
{
    public class ProcessEventArgs : EventArgs
    {
        public ProcessInfo ProcessInfo { get; set; }
    }
}
