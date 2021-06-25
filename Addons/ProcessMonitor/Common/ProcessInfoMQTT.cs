using System.Collections.Generic;

namespace IOTLinkAddon.Common
{
    class ProcessInfoMQTT
    {
        public string Id { get; set; }
        public string SessionId { get; set; }
        public string ProcessName { get; set; }
        public string StartDateTime { get; set; }
        public string Uptime { get; set; }
        public string MemoryUsed { get; set; }
        public string ProcessorUsage { get; set; }
        public string MainWindowTitle { get; set; }
        public string FullScreen { get; set; }
        public string Status { get; set; }
        public List<string> Windows { get; set; }
        public List<string> ClassNames { get; set; }
    }
}
