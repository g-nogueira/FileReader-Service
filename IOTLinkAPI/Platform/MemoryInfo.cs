namespace IOTLinkAPI.Platform
{
    public class MemoryInfo
    {
        public const int MEMORY_DIVISOR = 1024 * 1024;

        public uint MemoryLoad { get; set; }
        public ulong TotalPhysical { get; set; }
        public ulong AvailPhysical { get; set; }
        public ulong TotalPageFile { get; set; }
        public ulong AvailPageFile { get; set; }
        public ulong TotalVirtual { get; set; }
        public ulong AvailVirtual { get; set; }
        public ulong AvailExtendedVirtual { get; set; }
    }
}
