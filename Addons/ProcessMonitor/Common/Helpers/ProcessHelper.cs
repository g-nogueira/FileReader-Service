using IOTLinkAddon.Common.Processes;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using static System.Management.ManagementObjectCollection;

namespace IOTLinkAddon.Common.Helpers
{
    public static class ProcessHelper
    {
        public static readonly string DEFAULT_SCOPE = @"\\.\root\CIMV2";
        public static readonly ManagementScope DEFAULT_SCOPE_OBJECT = new ManagementScope(DEFAULT_SCOPE);

        public static List<ProcessInformation> GetProcesses()
        {
            Process[] processes = Process.GetProcesses();

            return processes.Select(x => ParseProcess(x)).Where(x => x != null).ToList();
        }

        public static List<ProcessInformation> GetProcessesByName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return new List<ProcessInformation>();

            processName = CleanProcessName(processName);
            Process[] processes = Process.GetProcessesByName(processName);

            return processes.Select(x => ParseProcess(x)).Where(x => x != null).ToList();
        }

        public static List<int> GetProcessesIdsByName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return new List<int>();

            processName = CleanProcessName(processName);
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Select(x => x.Id).ToList();
        }

        public static ProcessInformation GetProcessInformation(int processId, bool fetchParent = true)
        {
            try
            {
                if (processId == 0)
                    return null;

                Process process = Process.GetProcessById(processId);
                if (process == null || process.Id == 0)
                    return null;

                return ParseProcess(process, fetchParent);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static ProcessInformation GetProcessParent(int processId)
        {
            try
            {
                var query = string.Format("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {0}", processId);
                ManagementObjectSearcher search = new ManagementObjectSearcher(DEFAULT_SCOPE, query);
                ManagementObjectCollection resultCollection = search.Get();

                if (resultCollection.Count == 0)
                    return null;

                ManagementObjectEnumerator results = resultCollection.GetEnumerator();
                if (!results.MoveNext())
                    return null;

                int parentProcessId = Convert.ToInt32((uint)results.Current["ParentProcessId"]);
                return GetProcessInformation(parentProcessId);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ProcessHelper::GetProcessParent({0}) - Error: {1}", processId, ex);
                return null;
            }
        }

        public static List<ProcessInformation> GetProcessChildren(int processId)
        {
            try
            {
                var query = string.Format("SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = {0}", processId);
                ManagementObjectSearcher search = new ManagementObjectSearcher(DEFAULT_SCOPE, query);
                ManagementObjectCollection resultCollection = search.Get();

                if (resultCollection.Count == 0)
                    return new List<ProcessInformation>();

                ManagementObjectEnumerator mgtResults = resultCollection.GetEnumerator();

                List<ProcessInformation> results = new List<ProcessInformation>();
                while (mgtResults.MoveNext())
                {
                    var pid = Convert.ToInt32((uint)mgtResults.Current["ProcessId"]);
                    var child = GetProcessInformation(pid, false);

                    if (child != null)
                    {
                        results.Add(child);
                        results.AddRange(GetProcessChildren(child.Id));
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ProcessHelper::GetProcessParent({0}) - Error: {1}", processId, ex);
                return null;
            }
        }

        public static string CleanProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return null;

            return processName.ToLowerInvariant().Replace(".exe", "").Trim();
        }

        private static ProcessInformation ParseProcess(Process process, bool fetchParent = true)
        {
            ProcessInfo processInfo = ProcessEventManager.GetInstance().GetProcessInfo(process.Id);
            if (processInfo == null)
                return null;

            ProcessInformation result = new ProcessInformation
            {
                Id = processInfo.Id,
                SessionId = processInfo.SessionId,
                ProcessName = processInfo.ProcessName,
                StartDateTime = processInfo.StartDateTime,
                MemoryUsed = processInfo.MemoryUsed,
                ProcessorUsage = processInfo.ProcessorUsage,
                Status = ParseState(process),
                Parent = fetchParent ? GetProcessParent(process.Id) : null
            };

            if (!process.HasExited)
            {
                result.MainWindowHandle = process.MainWindowHandle.ToInt32();
                result.MainWindowTitle = process.MainWindowTitle;
            }

            return result;
        }

        private static ProcessState ParseState(Process process)
        {
            return process.HasExited ? ProcessState.NotRunning : ProcessState.Running;
        }
    }
}
