using IOTLinkAddon.Common.Configs;
using IOTLinkAddon.Common.Processes;
using IOTLinkAddon.Service;
using IOTLinkAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IOTLinkAddon.Common.Helpers
{
    public static class MonitorHelper
    {
        public static List<ProcessMonitor> GetProcessMonitorsByName(List<ProcessMonitor> monitors, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return monitors.Where(x => string.Compare(x.Name, name, StringComparison.OrdinalIgnoreCase) == 0).ToList();
        }

        public static ProcessMonitor GetProcessMonitorByName(List<ProcessMonitor> monitors, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return monitors.FirstOrDefault(x => string.Compare(x.Name, name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public static List<ProcessMonitor> GetProcessMonitorsByProcessName(List<ProcessMonitor> monitors, string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return null;

            processName = ProcessHelper.CleanProcessName(processName);
            return monitors.Where(x => x.Config.ProcessNames.Contains(processName, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        public static ProcessMonitor GetProcessMonitorByProcessName(List<ProcessMonitor> monitors, string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return null;

            processName = ProcessHelper.CleanProcessName(processName);
            return monitors.FirstOrDefault(x => x.Config.ProcessNames.Contains(processName, StringComparer.OrdinalIgnoreCase));
        }

        public static List<ProcessMonitor> GetProcessMonitorsByProcessInfo(List<ProcessMonitor> monitors, ProcessInformation processInfo)
        {
            if (processInfo == null)
                return new List<ProcessMonitor>();

            return GetProcessMonitorsByProcessName(monitors, processInfo.ProcessName);
        }

        public static bool HasConditions(ProcessMonitor monitor)
        {
            return (monitor.Config.ProcessWindows.Count != 0 || monitor.Config.ProcessClassNames.Count != 0);
        }

        public static bool CheckMonitorFilters(ProcessMonitor monitor, ProcessInformation processInfo)
        {
            if (processInfo == null)
            {
                LoggerHelper.Debug("MonitorHelper::CheckMonitorFilters({0}) - Process Information NULL", monitor.Name);
                return false;
            }

            MonitorConfig config = monitor.Config;
            if (!config.ProcessNames.Contains(processInfo.ProcessName, StringComparer.OrdinalIgnoreCase))
            {
                LoggerHelper.Debug("MonitorHelper::CheckMonitorFilters({0}, {1}) - Process Name not found", monitor.Name, processInfo.ProcessName);
                return false;
            }

            if (!HasConditions(monitor))
            {
                LoggerHelper.Debug("MonitorHelper::CheckMonitorFilters({0}, {1}) - NO additional rules", monitor.Name, processInfo.ProcessName);
                return true;
            }

            var processWindows = config.ProcessWindows;
            var processClasses = config.ProcessClassNames;
            var compareType = config.General.CompareType;
            var windowMatches = 0;
            var classMatches = 0;

            if (processInfo.Windows != null)
            {
                foreach (string windowName in processWindows)
                {
                    bool hasFound = processInfo.Windows.Any(w => Regex.IsMatch(w, windowName));
                    if (hasFound)
                        windowMatches++;
                }
            }

            if (processInfo.ClassNames != null)
            {
                foreach (string className in processClasses)
                {
                    bool hasFound = processInfo.ClassNames.Any(w => Regex.IsMatch(w, className));
                    if (hasFound)
                        classMatches++;
                }
            }

            var result = false;
            switch (compareType)
            {
                default:
                    result = (processWindows.Count == windowMatches) && (processClasses.Count == classMatches);
                    break;

                case 1:
                    result = (processWindows.Count == windowMatches) || (processClasses.Count == classMatches);
                    break;

                case 2:
                    result = (processWindows.Count == windowMatches) && (classMatches > 0);
                    break;

                case 3:
                    result = (processClasses.Count == classMatches) && (windowMatches > 0);
                    break;

                case 4:
                    result = (windowMatches > 0) || (classMatches > 0);
                    break;
            }

            LoggerHelper.Debug("MonitorHelper::CheckMonitorFilters({0}, {1}) - Compare Type: {2} - Result: {3}", monitor.Name, processInfo.ProcessName, compareType, result);
            return result;
        }
    }
}
