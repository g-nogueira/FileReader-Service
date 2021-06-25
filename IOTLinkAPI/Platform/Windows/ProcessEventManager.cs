using IOTLinkAPI.Common;
using IOTLinkAPI.Platform.Events.Process;
using System;
using System.Diagnostics;
using System.Management;

using static IOTLinkAPI.Platform.Events.Process.ProcessHandlers;

namespace IOTLinkAPI.Platform.Windows
{
    public class ProcessEventManager
    {
        private static readonly string WMI_SCOPE = "root\\CIMV2";

        private static readonly int WMI_INTERVAL_PROCESS_PERF = 60;
        private static readonly string WMI_QUERY_PROCESS_PERF = "SELECT * FROM __InstanceModificationEvent WITHIN {0} WHERE TargetInstance Isa 'Win32_PerfFormattedData_PerfProc_Process' AND PreviousInstance.PercentProcessorTime != TargetInstance.PercentProcessorTime";

        private static readonly int WMI_INTERVAL_PROCESS_STATE = 1;
        private static readonly string WMI_QUERY_PROCESS_STATE = "SELECT * FROM __InstanceOperationEvent WITHIN {0} WHERE TargetInstance ISA 'Win32_Process'";

        private static ProcessEventManager _instance;

        private ManagementEventWatcher _processPerformanceWatcher;
        private ManagementEventWatcher _processStateWatcher;

        private CacheContainer<int, ProcessInfoEntry> _cache = new CacheContainer<int, ProcessInfoEntry>();

        public event ProcessStartedEventHandler OnProcessStarted;
        public event ProcessStoppedEventHandler OnProcessStopped;
        public event ProcessUpdatedEventHandler OnProcessUpdated;

        public static ProcessEventManager GetInstance()
        {
            if (_instance == null)
                _instance = new ProcessEventManager();

            return _instance;
        }

        private ProcessEventManager()
        {
            Init();
        }

        public ProcessInfo GetProcessInfo(int processId)
        {
            var entry = _cache.GetItem(processId);
            if (entry == null)
                return null;

            return entry.ProcessInfo;
        }

        private void Init()
        {
            if (_processPerformanceWatcher == null)
            {
                _processPerformanceWatcher = new ManagementEventWatcher(WMI_SCOPE, string.Format(WMI_QUERY_PROCESS_PERF, WMI_INTERVAL_PROCESS_PERF));
                _processPerformanceWatcher.EventArrived += new EventArrivedEventHandler(OnProcessPerformanceWatcherEvent);
            }

            if (_processStateWatcher == null)
            {
                _processStateWatcher = new ManagementEventWatcher(WMI_SCOPE, string.Format(WMI_QUERY_PROCESS_STATE, WMI_INTERVAL_PROCESS_STATE));
                _processStateWatcher.EventArrived += new EventArrivedEventHandler(OnProcessStateWatcherEvent);
            }

            _processPerformanceWatcher.Start();
            _processStateWatcher.Start();
        }

        private void OnProcessPerformanceWatcherEvent(object sender, EventArrivedEventArgs e)
        {
            if (e.NewEvent.Properties.Count == 0)
                return;

            var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            var processId = Convert.ToInt32((uint)targetInstance["IDProcess"]);

            var processEntry = _cache.GetItem(processId);
            if (processEntry != null)
            {
                var processName = (string)targetInstance["Name"];
                processEntry.ProcessInfo.ProcessorUsage = GetProcessorUsage(Convert.ToInt32(targetInstance["PercentProcessorTime"]));
                processEntry.ProcessInfo.MemoryUsed = GetMemoryUsed(processName);

                var eventArgs = new ProcessEventArgs { ProcessInfo = processEntry.ProcessInfo };
                if (!processEntry.Initialized)
                    OnProcessStarted?.Invoke(this, eventArgs);
                else
                    OnProcessUpdated?.Invoke(this, eventArgs);

                processEntry.Initialized = true;
                _cache.UpdateItem(processId, processEntry);
            }
        }

        private void OnProcessStateWatcherEvent(object sender, EventArrivedEventArgs e)
        {
            if (e.NewEvent.Properties.Count == 0)
                return;

            var className = e.NewEvent.ClassPath.ClassName;
            var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];

            if (className.Contains("InstanceCreationEvent"))
                OnProcessStartEvent(targetInstance);
            else if (className.Contains("InstanceDeletionEvent"))
                OnProcessStopEvent(targetInstance);
            else
                OnProcessUpdateEvent(targetInstance);
        }

        private void OnProcessStartEvent(ManagementBaseObject targetInstance)
        {
            var processId = Convert.ToInt32((uint)targetInstance["ProcessId"]);

            _cache.UpdateItem(processId, new ProcessInfoEntry { ProcessInfo = ParseProcessInformation(targetInstance) });
        }

        private void OnProcessStopEvent(ManagementBaseObject targetInstance)
        {
            var processInfo = ParseProcessInformation(targetInstance);

            _cache.RemoveKey(processInfo.Id);

            var eventArgs = new ProcessEventArgs { ProcessInfo = processInfo };
            OnProcessStopped?.Invoke(this, eventArgs);
        }

        private void OnProcessUpdateEvent(ManagementBaseObject targetInstance)
        {
            var processId = Convert.ToInt32((uint)targetInstance["ProcessId"]);

            _cache.GetItem(processId, () => new ProcessInfoEntry { ProcessInfo = ParseProcessInformation(targetInstance) });
        }

        private ProcessInfo ParseProcessInformation(ManagementBaseObject targetInstance)
        {
            return new ProcessInfo
            {
                Id = Convert.ToInt32((uint)targetInstance["ProcessId"]),
                ParentId = Convert.ToInt32((uint)targetInstance["ParentProcessId"]),
                SessionId = Convert.ToInt32((uint)targetInstance["SessionId"]),
                StartDateTime = ManagementDateTimeConverter.ToDateTime((string)targetInstance["CreationDate"]),
                ProcessName = ParseProcessName((string)targetInstance["Name"]),
            };
        }

        private long GetMemoryUsed(string processName)
        {
            long memoryUsed = 0;
            using (PerformanceCounter PC = new PerformanceCounter("Process", "Working Set - Private", processName))
            {
                memoryUsed = Convert.ToInt64(PC.NextValue());
                PC.Close();
            }

            return memoryUsed;
        }

        private double GetProcessorUsage(int processorTime)
        {
            return Math.Round((processorTime / (double)Environment.ProcessorCount), 2);
        }

        private string ParseProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return null;

            return processName.ToLowerInvariant().Replace(".exe", "").Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        }
    }
}
