using Addon_ReadFileService.Watcher;
using IOTLinkAddon.Common.Helpers;
using IOTLinkAPI.Configs;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Addon_ReadFileService.Configs
{
    public class FileConfig
    {
        public string Key { get; set; }
        public string FileName { get; set; }
        public string FileDirectory { get; set; }
        public Regex OnRegex { get; set; }
        public Regex OffRegex { get; set; }
        public FileWatcher Watcher { get; set; }
        public string CachedState { get; set; }

        public static FileConfig FromConfiguration(Configuration configuration)
        {
            var info = new FileInfo(configuration.GetValue("path", ""));
            var config = new FileConfig
            {
                Key = configuration.Key,
                OnRegex = new Regex(configuration.GetValue("onRegex", "")),
                OffRegex = new Regex(configuration.GetValue("offRegex", "")),
                FileDirectory = info.DirectoryName,
                FileName = info.Name
            };

            return config;
        }

        public bool TryGetStateValue(string path, out string state)
        {
            var lastline = File.ReadLines(path).Last();
            state = null;

            if (OnRegex.IsMatch(lastline))
            {
                state = "ON";
            }
            else if (OffRegex.IsMatch(lastline))
            {
                state = "OFF";
            }

            return !string.IsNullOrEmpty(state);

        }

    }
}
