using Addon_ReadFileService.Configs;
using IOTLinkAddon.Common.Helpers;
using IOTLinkAPI.Configs;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Addon_ReadFileService.Watcher
{
    public class FileWatcher : FileSystemWatcher
    {
        public FileWatcher(string path, string filter) : base(path, filter)
        {
        }

        public string Key { get; set; }
        public string CachedState { get; set; }
    }
}
