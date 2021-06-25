namespace IOTLinkAPI.Platform.Events.MQTT
{
    public class MQTTMessageEventEventArgs : MQTTEventEventArgs
    {
        public MQTTMessage Message { get; set; }

        public MQTTMessageEventEventArgs(MQTTEventType type, MQTTMessage msg, object arg) : base(type, arg)
        {
            Message = msg;
        }
    }
}
