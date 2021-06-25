using System;

namespace IOTLinkAPI.Platform
{
    public class ProcessInfo
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public int SessionId { get; set; }
        public string ProcessName { get; set; }
        public DateTime StartDateTime { get; set; }
        public long MemoryUsed { get; set; }
        public double ProcessorUsage { get; set; }
    }
}
