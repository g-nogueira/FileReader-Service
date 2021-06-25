using System.Text;

namespace IOTLinkAPI.Platform.Events.MQTT
{
    public class MQTTMessage
    {
        public string Topic { get; set; }
        public string FullTopic { get; set; }
        public byte[] Payload { get; set; }
        public bool Retain { get; set; }
        public string ContentType { get; set; }

        public string GetPayload()
        {
            if (Payload == null || Payload.Length == 0)
                return null;

            return Encoding.UTF8.GetString(Payload);
        }

        public void SetPayload(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                Payload = null;
            else
                Payload = Encoding.UTF8.GetBytes(message);
        }
    }
}
