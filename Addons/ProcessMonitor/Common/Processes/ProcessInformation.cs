using IOTLinkAPI.Platform;
using System.Collections.Generic;

namespace IOTLinkAddon.Common.Processes
{
    public class ProcessInformation : ProcessInfo
    {
        public int MainWindowHandle { get; set; }
        public string MainWindowTitle { get; set; }
        public bool FullScreen { get; set; }
        public ProcessState Status { get; set; }
        public ProcessInformation Parent { get; set; }
        public List<string> Windows { get; set; }
        public List<string> ClassNames { get; set; }
    }
}
