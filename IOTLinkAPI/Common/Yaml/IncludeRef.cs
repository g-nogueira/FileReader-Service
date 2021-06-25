using System.Collections.Generic;

namespace IOTLinkAPI.Common.Yaml
{
    public class IncludeRef : Dictionary<object, object>
    {
        public string FileName { get; set; }
    }
}
