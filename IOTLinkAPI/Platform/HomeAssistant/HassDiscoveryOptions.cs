namespace IOTLinkAPI.Platform.HomeAssistant
{
    public class HassDiscoveryOptions
    {
        public string Id { get; set; }
        public string Unit { get; set; }
        public string ValueTemplate { get; set; }
        public string Name { get; set; }
        public HomeAssistantComponent Component { get; set; }
        public string PayloadOn { get; set; }
        public string PayloadOff { get; set; }
        public string DeviceClass { get; set; }
        public string Icon { get; set; }
    }
}
