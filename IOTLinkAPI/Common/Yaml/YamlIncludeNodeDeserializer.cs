using System;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace IOTLinkAPI.Common.Yaml
{
    public class YamlIncludeNodeDeserializer : INodeDeserializer
    {
        public static readonly string TAG = "!include";

        private readonly YamlIncludeNodeDeserializerOptions _options;

        public YamlIncludeNodeDeserializer(YamlIncludeNodeDeserializerOptions options)
        {
            _options = options;
        }

        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            if (reader.Accept(out Scalar scalar) && scalar.Tag == TAG)
            {
                string fileName = scalar.Value;
                string includePath = Path.Combine(_options.DirectoryName, fileName);

                using (var includedFile = File.OpenText(includePath))
                {
                    var includeRef = (IncludeRef)_options.Deserializer.Deserialize(new Parser(includedFile), expectedType);
                    includeRef.FileName = fileName;

                    reader.MoveNext();

                    value = includeRef;
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
