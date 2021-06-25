using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IOTLinkAPI.Platform.Events.MQTT.MQTTHandlers;

namespace IOTLinkService.Service.Engine
{
    /// <summary>
    /// Handle Topic Subscription Informations
    /// </summary>
    class TopicSubscriptionInfo
    {
        /// <summary>
        /// Subscription Topic
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// Indicates if handler will accept global messages
        /// </summary>
        public bool AcceptGlobalMessages { get; set; }

        /// <summary>
        /// Event Handle
        /// </summary>
        public MQTTMessageEventHandler OnMessageReceived { get; set; }
    }
}
