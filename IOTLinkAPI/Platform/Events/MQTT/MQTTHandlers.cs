using System;

namespace IOTLinkAPI.Platform.Events.MQTT
{
    public abstract class MQTTHandlers
    {
        public delegate void MQTTEventHandler(object sender, MQTTEventEventArgs e);
        public delegate void MQTTMessageEventHandler(object sender, MQTTMessageEventEventArgs e);
        public delegate void MQTTRefreshMessageEventHandler(object sender, EventArgs e);
    }
}
