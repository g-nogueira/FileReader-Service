using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IOTLinkAPI.Platform.HomeAssistant
{
    public class HassDiscoveryJsonClass
    {
        [JsonProperty("unit_of_measurement")]
        public string UnitOfMeasurement { get; set; }

        [JsonProperty("value_template")]
        public string ValueTemplate { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("availability_topic")]
        public string AvailabilityTopic { get; set; }

        [JsonProperty("state_topic")]
        public string StateTopic { get; set; }

        [JsonProperty("topic")]
        public string Topic { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("unique_id")]
        public string UniqueId { get; set; }

        [JsonProperty("device_class")]
        public string DeviceClass { get; set; }

        [JsonProperty("payload_on")]
        public string PayloadOn { get; set; }

        [JsonProperty("payload_off")]
        public string PayloadOff { get; set; }

        [JsonProperty("payload_available")]
        public string PayloadAvailable { get; set; }

        [JsonProperty("payload_not_available")]
        public string PayloadNotAvailable { get; set; }

        [JsonProperty("device")]
        public Device Device { get; set; }
    }

    public class Device
    {
        [JsonProperty("identifiers")]
        public string[] Identifiers { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("model")]
        public string Model { get; set; }
        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }
    }
}
