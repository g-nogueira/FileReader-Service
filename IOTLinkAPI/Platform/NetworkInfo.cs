namespace IOTLinkAPI.Platform
{
    public class NetworkInfo
    {
        public string IPv4Address { get; set; }
        public string IPv6Address { get; set; }
        public long Speed { get; set; }
        public bool Wired { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
    }
}
