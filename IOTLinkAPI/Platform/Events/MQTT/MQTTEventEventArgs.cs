using System;

namespace IOTLinkAPI.Platform.Events.MQTT
{
    public class MQTTEventEventArgs : EventArgs
    {
        public MQTTEventType EventType { get; set; }
        public object EventArg { get; set; }

        public MQTTEventEventArgs(MQTTEventType type)
        {
            EventType = type;
        }

        public MQTTEventEventArgs(MQTTEventType type, object arg)
        {
            EventType = type;
            EventArg = arg;
        }

        public enum MQTTEventType
        {
            Connect,
            Disconnect,
            MessageReceived,
            MessageSentSuccess,
            MessageSentFailure
        }
    }
}
