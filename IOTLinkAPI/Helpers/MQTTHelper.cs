using System.Text;

namespace IOTLinkAPI.Helpers
{
    public static class MQTTHelper
    {
        /// <summary>
        /// Return the full topic name (including domain/machine)
        /// </summary>
        /// <param name="prefix">prefix string</param>
        /// <param name="name">message topic string</param>
        /// <returns>String containing the full topic name</returns>
        public static string GetFullTopicName(string prefix, string name = "")
        {
            if (name == null)
                name = string.Empty;

            string machineName = PlatformHelper.GetFullMachineName().Replace("\\", "/");
            string topic = string.Format("{0}/{1}/{2}", prefix, machineName, name);
            return MQTTHelper.SanitizeTopic(topic);
        }

        /// <summary>
        /// Return the global topic name
        /// </summary>
        /// <param name="prefix">prefix string</param>
        /// <param name="name">message topic string</param>
        /// <returns>String containing the global topic name</returns>
        public static string GetGlobalTopicName(string prefix, string name = "")
        {
            if (name == null)
                name = string.Empty;

            string topic = string.Format("{0}/{1}", prefix, name);
            return MQTTHelper.SanitizeTopic(topic);
        }

        /// <summary>
        /// Remove non-wanted characters from the node name
        /// </summary>
        /// <param name="name">String containing the source name</param>
        /// <returns>String containing the sanitized name</returns>
        public static string SanitizeNodeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            return StringHelper.RemoveDiacritics(name)
                .Replace(" ", "_")
                .Replace("\\", "")
                .Trim();
        }

        /// <summary>
        /// Remove non-wanted characters from the MQTT topic path
        /// </summary>
        /// <param name="name">String containing the MQTT topic path</param>
        /// <returns>String containing the MQTT topic path</returns>
        public static string SanitizeTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return string.Empty;

            topic = topic.Replace("\\\\", "\\").Trim();
            string[] nodes = topic.Split('/');

            StringBuilder sb = new StringBuilder();
            foreach (string node in nodes)
            {
                sb.Append(SanitizeNodeName(node));
                sb.Append('/');
            }

            sb.Length--;
            return sb.ToString().ToLowerInvariant();
        }
    }
}
