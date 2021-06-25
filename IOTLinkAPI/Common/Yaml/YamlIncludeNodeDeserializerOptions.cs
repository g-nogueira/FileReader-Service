using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace IOTLinkAPI.Common.Yaml
{
    public class YamlIncludeNodeDeserializerOptions
    {
        public IDeserializer Deserializer { get; set; }

        public string DirectoryName { get; set; }

        public IList<IncludeRef> includeRefs { get; set; }
    }
}