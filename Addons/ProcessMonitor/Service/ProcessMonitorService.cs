using IOTLinkAddon.Common;
using IOTLinkAddon.Common.Configs;
using IOTLinkAddon.Common.Helpers;
using IOTLinkAddon.Common.Processes;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.Events.Process;
using IOTLinkAPI.Platform.HomeAssistant;
using IOTLinkAPI.Platform.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Timers;

namespace IOTLinkAddon.Service
{
    public class ProcessMonitorService : ServiceAddon
    {
        private static readonly string PAYLOAD_ON = "ON";
        private static readonly string PAYLOAD_OFF = "OFF";

        private string _configPath;
        private Configuration _config;

        private List<ProcessMonitor> _monitors = new List<ProcessMonitor>();

        private Timer _monitorTimer;
        private uint _monitorCounter = 0;

        private Dictionary<string, string> _cache = new Dictionary<string, string>();

        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);

            LoggerHelper.Verbose("ProcessMonitorService::Init() - Started");

            var cfgManager = ConfigurationManager.GetInstance();
            _configPath = Path.Combine(_currentPath, "config.yaml");
            _config = cfgManager.GetConfiguration(_configPath);
            cfgManager.SetReloadHandler(_configPath, OnConfigReload);

            OnConfigReloadHandler += OnConfigReload;
            OnMQTTConnectedHandler += OnMQTTConnected;
            OnMQTTDisconnectedHandler += OnMQTTDisconnected;
            OnRefreshRequestedHandler += OnClearEvent;
            OnAgentResponseHandler += OnAgentResponse;

            SetupEventMonitor();
            Restart();

            LoggerHelper.Verbose("ProcessMonitorService::Init() - Completed");
        }

        private bool IsAddonEnabled()
        {
            if (_config == null || !_config.GetValue("enabled", false))
            {
                CleanTimers();
                return false;
            }

            return true;
        }

        private void Restart()
        {
            if (!IsAddonEnabled())
                return;

            SetupMonitors();
            StartTimers();
        }

        private void ClearMonitors()
        {
            _cache.Clear();
            _monitors.Clear();
        }

        private void SetupEventMonitor()
        {
            var eventMonitor = ProcessEventManager.GetInstance();

            eventMonitor.OnProcessStarted += OnProcessStarted;
            eventMonitor.OnProcessStopped += OnProcessStopped;
        }

        private void SetupMonitors()
        {
            ClearMonitors();

            List<Configuration> monitorConfigurations = _config.GetConfigurationList("monitors");
            if (monitorConfigurations == null || monitorConfigurations.Count == 0)
            {
                LoggerHelper.Info("ProcessMonitorService::SetupMonitors() - Monitoring list is empty.");
                return;
            }

            foreach (Configuration monitorConfiguration in monitorConfigurations)
            {
                try
                {
                    SetupMonitor(monitorConfiguration);
                    SetupDiscovery(monitorConfiguration.Key);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Debug("ProcessMonitorService::SetupMonitors({0}): Error - {1}", monitorConfiguration.Key, ex);
                }
            }
        }

        private void SetupMonitor(Configuration monitorConfiguration)
        {
            MonitorConfig config = MonitorConfig.FromConfiguration(monitorConfiguration);
            if (!config.General.Enabled)
            {
                LoggerHelper.Debug("ProcessMonitorService::SetupMonitor({0}) - Monitoring is disabled.", monitorConfiguration.Key);
                return;
            }

            ProcessMonitor monitor = new ProcessMonitor { Name = config.Key, Config = config };
            bool hasMonitor = MonitorHelper.GetProcessMonitorByName(_monitors, monitor.Name) != null;
            if (hasMonitor)
            {
                LoggerHelper.Warn("ProcessMonitorService::SetupMonitor({0}) - Duplicated Key. Ignoring.", monitorConfiguration.Key);
                return;
            }

            _monitors.Add(monitor);
        }

        private void SetupDiscovery(string monitorName)
        {
            ProcessMonitor monitor = MonitorHelper.GetProcessMonitorByName(_monitors, monitorName);

            if (monitor == null || !monitor.Config.General.Enabled)
            {
                LoggerHelper.Debug("ProcessMonitorService::SetupMonitor({0}) - Monitoring not found or disabled.", monitorName);
                return;
            }

            if (!monitor.Config.General.Discoverable)
            {
                LoggerHelper.Debug("ProcessMonitorService::SetupDiscovery({0}) - Discovery is disabled.", monitorName);
                return;
            }

            SetupProcessDiscoveryState(monitor);
            if (monitor.Config.General.AdvancedMode)
            {
                SetupProcessDiscoveryMemory(monitor);
                SetupProcessDiscoveryProcessor(monitor);
                SetupProcessDiscoveryUptime(monitor);
                SetupProcessDiscoveryFullscreen(monitor);
            }
        }

        private void SetupProcessDiscoveryState(ProcessMonitor monitor)
        {
            if (monitor == null)
                return;

            var id = monitor.Name;
            var name = monitor.Config.General.DisplayName;

            HassDiscoveryOptions discoveryOptions = new HassDiscoveryOptions
            {
                Id = id,
                Name = name,
                DeviceClass = "moving",
                Component = HomeAssistantComponent.BinarySensor
            };

            GetManager().PublishDiscoveryMessage(this, GetStateTopic(monitor), "Processes", discoveryOptions);
        }

        private void SetupProcessDiscoveryMemory(ProcessMonitor monitor)
        {
            if (monitor == null)
                return;

            var id = string.Format("{0}_{1}", monitor.Name, "MemoryUsed");
            var name = string.Format("{0} {1}", monitor.Config.General.DisplayName, "Memory Used");

            HassDiscoveryOptions discoveryOptions = new HassDiscoveryOptions
            {
                Id = id,
                Name = name,
                Icon = "mdi:memory",
                Unit = "MB",
                ValueTemplate = "{{ value_json.MemoryUsed }}",
                Component = HomeAssistantComponent.Sensor
            };

            GetManager().PublishDiscoveryMessage(this, GetSensorTopic(monitor), "Processes", discoveryOptions);
        }

        private void SetupProcessDiscoveryProcessor(ProcessMonitor monitor)
        {
            if (monitor == null)
                return;

            var id = string.Format("{0}_{1}", monitor.Name, "ProcessorUsage");
            var name = string.Format("{0} {1}", monitor.Config.General.DisplayName, "Processor Usage");

            HassDiscoveryOptions discoveryOptions = new HassDiscoveryOptions
            {
                Id = id,
                Name = name,
                Icon = "mdi:gauge",
                Unit = "%",
                ValueTemplate = "{{ value_json.ProcessorUsage }}",
                Component = HomeAssistantComponent.Sensor
            };

            GetManager().PublishDiscoveryMessage(this, GetSensorTopic(monitor), "Processes", discoveryOptions);
        }

        private void SetupProcessDiscoveryUptime(ProcessMonitor monitor)
        {
            if (monitor == null)
                return;

            var id = string.Format("{0}_{1}", monitor.Name, "Uptime");
            var name = string.Format("{0} {1}", monitor.Config.General.DisplayName, "Uptime");
            var inSeconds = _config.GetValue("formats:uptimeInSeconds", false);

            HassDiscoveryOptions discoveryOptions = new HassDiscoveryOptions
            {
                Id = id,
                Name = name,
                Icon = "mdi:timer-outline",
                ValueTemplate = "{{ value_json.Uptime }}",
                Unit = inSeconds ? "s" : "",
                Component = HomeAssistantComponent.Sensor
            };

            GetManager().PublishDiscoveryMessage(this, GetSensorTopic(monitor), "Processes", discoveryOptions);
        }

        private void SetupProcessDiscoveryFullscreen(ProcessMonitor monitor)
        {
            if (monitor == null)
                return;

            var id = string.Format("{0}_{1}", monitor.Name, "FullScreen");
            var name = string.Format("{0} {1}", monitor.Config.General.DisplayName, "FullScreen");

            HassDiscoveryOptions discoveryOptions = new HassDiscoveryOptions
            {
                Id = id,
                Name = name,
                ValueTemplate = "{{ value_json.FullScreen }}",
                PayloadOn = "True",
                PayloadOff = "False",
                Component = HomeAssistantComponent.BinarySensor
            };

            GetManager().PublishDiscoveryMessage(this, GetSensorTopic(monitor), "Processes", discoveryOptions);
        }

        private void CleanTimers()
        {
            if (_monitorTimer != null)
            {
                _monitorTimer.Stop();
                _monitorTimer = null;
            }
        }

        private void StartTimers()
        {
            if (!IsAddonEnabled())
                return;

            CleanTimers();

            _monitorTimer = new Timer();
            _monitorTimer.Elapsed += new ElapsedEventHandler(OnMonitorTimerElapsed);
            _monitorTimer.Interval = 1000;
            _monitorTimer.Start();
        }

        private void RestartTimers()
        {
            if (_monitorTimer == null)
                return;

            _monitorTimer.Stop();
            _monitorTimer.Start();
        }

        private void StopTimers()
        {
            if (_monitorTimer == null)
                return;

            _monitorTimer.Stop();
        }

        private void OnMQTTConnected(object sender, EventArgs e)
        {
            if (!IsAddonEnabled())
                return;

            SetupMonitors();
            StartTimers();
        }

        private void OnMQTTDisconnected(object sender, EventArgs e)
        {
            StopTimers();
        }

        private void OnClearEvent(object sender, EventArgs e)
        {
            LoggerHelper.Verbose("ProcessMonitorService::OnClearEvent() - Clearing cache and resending information.");

            _cache.Clear();
            ExecuteMonitors();
        }

        private void OnConfigReload(object sender, ConfigReloadEventArgs e)
        {
            if (e.ConfigType != ConfigType.CONFIGURATION_ADDON)
                return;

            LoggerHelper.Verbose("ProcessMonitorService::OnConfigReload() - Reloading configuration");

            _config = ConfigurationManager.GetInstance().GetConfiguration(_configPath);
            Restart();
        }

        private void OnMonitorTimerElapsed(object source, ElapsedEventArgs e)
        {
            LoggerHelper.Verbose("ProcessMonitorService::OnMonitorTimerElapsed() - Started");

            ExecuteMonitors();

            if (_monitorCounter++ == uint.MaxValue)
                _monitorCounter = 0;

            LoggerHelper.Verbose("ProcessMonitorService::OnMonitorTimerElapsed() - Completed");
        }

        private void ExecuteMonitors()
        {
            StopTimers();

            LoggerHelper.Verbose("ProcessMonitorService::ExecuteMonitors() - Started");
            foreach (ProcessMonitor monitor in _monitors)
            {
                try
                {
                    var interval = monitor.Config.General.Interval;
                    if ((_monitorCounter % interval) != 0)
                        continue;

                    ExecuteMonitor(monitor);
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error("ProcessMonitorService::ExecuteMonitors({0}) - Exception: {1}", monitor.Name, ex);
                }
            }
            LoggerHelper.Verbose("ProcessMonitorService::ExecuteMonitors() - Completed");

            RestartTimers();
        }

        private void ExecuteMonitor(ProcessMonitor monitor)
        {
            LoggerHelper.Verbose("ProcessMonitorService::ExecuteMonitor({0}) - Started", monitor.Name);

            var config = monitor.Config;
            var processes = new List<ProcessInformation>();
            foreach (string processName in config.ProcessNames)
            {
                processes.AddRange(ProcessHelper.GetProcessesByName(processName));
            }

            LoggerHelper.Verbose("ProcessMonitorService::ExecuteMonitor({0}) - {1} Processes Found", monitor.Name, processes.Count);
            if (processes.Count == 0)
            {
                LoggerHelper.Debug("ProcessMonitorService::ExecuteMonitor({0}) - Not Running", monitor.Name);
                ProcessSingleProcess(monitor);
                return;
            }

            if (processes.Count > 0)
                ProcessGroup(monitor, processes);
            else
                ProcessSingleProcess(monitor, processes[0]);

            LoggerHelper.Verbose("ProcessMonitorService::ExecuteMonitor({0}) - Completed", monitor.Name);
        }

        private void ProcessGroup(ProcessMonitor monitor, List<ProcessInformation> processes)
        {
            LoggerHelper.Verbose("ProcessMonitorService::ProcessGroup({0}) - Started", monitor.Name);

            List<ProcessInformation> mainProcesses = new List<ProcessInformation>();
            foreach (ProcessInformation item in processes)
            {
                ProcessInformation pi = item;
                while (pi.Parent != null && IsChildProcess(pi))
                {
                    LoggerHelper.Debug("ProcessMonitorService::ProcessMonitor({0}) - Iterating over child process to find parent: {1}", monitor.Name, JsonConvert.SerializeObject(pi));
                    pi = pi.Parent;
                }

                if (!mainProcesses.Any(x => x.Id == pi.Id))
                    mainProcesses.Add(pi);
            }

            if (mainProcesses.Count == 0)
                return;

            ProcessSingleProcess(monitor, mainProcesses[0]);

            LoggerHelper.Verbose("ProcessMonitorService::ProcessGroup({0}) - Completed", monitor.Name);
        }

        private void ProcessSingleProcess(ProcessMonitor monitor, ProcessInformation processInfo = null)
        {
            LoggerHelper.Verbose("ProcessMonitorService::ProcessSingleProcess({0}) - Started", monitor.Name);

            if (processInfo == null)
            {
                LoggerHelper.Debug("ProcessMonitorService::ProcessSingleProcess({0}) - Setting empty process", monitor.Name);
                processInfo = new ProcessInformation { ProcessName = monitor.Name, Status = ProcessState.NotRunning };
            }

            if (processInfo.Status == ProcessState.NotRunning)
            {
                LoggerHelper.Debug("ProcessMonitorService::ProcessSingleProcess({0}) - Process not running.", monitor.Name);
                SendMonitorValue(monitor, GetStateTopic(monitor), PAYLOAD_OFF);
                SendMonitorValue(monitor, GetSensorTopic(monitor), PAYLOAD_ON);
                return;
            }

            if (processInfo.Status == ProcessState.Running && !MonitorHelper.HasConditions(monitor))
            {
                LoggerHelper.Debug("ProcessMonitorService::ProcessSingleProcess({0}) - Process Running - Monitor without rules.", monitor.Name);
                SendMonitorValue(monitor, GetStateTopic(monitor), PAYLOAD_ON);
            }

            RequestAgentData(processInfo.Id);
            LoggerHelper.Verbose("ProcessMonitorService::ProcessSingleProcess({0}) - Completed", monitor.Name);
        }

        private void RequestAgentData(int processId)
        {
            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_PROCESS_INFORMATION;
            addonData.processId = processId;

            GetManager().SendAgentRequest(this, addonData);
        }

        private void OnAgentResponse(object sender, AgentAddonResponseEventArgs e)
        {
            AddonRequestType requestType = (AddonRequestType)e.Data.requestType;
            if (requestType != AddonRequestType.REQUEST_PROCESS_INFORMATION)
                return;

            ProcessInformation processInfo = e.Data.requestData.ToObject<ProcessInformation>();
            OnProcessInformationReceived(processInfo);
        }

        private void OnProcessInformationReceived(ProcessInformation processInfo)
        {
            if (processInfo == null)
                return;

            List<ProcessMonitor> monitors = MonitorHelper.GetProcessMonitorsByProcessInfo(_monitors, processInfo);

            LoggerHelper.Debug("ProcessMonitorService::OnProcessInformationReceived({0}) - {1} Monitors found.", processInfo.Id, monitors.Count);
            if (monitors.Count == 0)
            {
                LoggerHelper.Debug("ProcessMonitorService::OnProcessInformationReceived({0}) - Monitoring not found", processInfo.Id);
                return;
            }

            foreach (ProcessMonitor monitor in monitors)
            {
                LoggerHelper.Debug("ProcessMonitorService::OnProcessInformationReceived({0}) - Monitor: {1}", processInfo.Id, monitor.Name);
                SendProcessInformation(monitor, processInfo);
            }
        }

        private void SendProcessInformation(ProcessMonitor monitor, ProcessInformation processInfo)
        {
            if (processInfo == null)
                return;

            LoggerHelper.Debug("ProcessMonitorService::SendProcessInformation({0}) - {1}", monitor.Name, JsonConvert.SerializeObject(processInfo));

            ProcessInfoMQTT mqttObject = null;
            var state = PAYLOAD_OFF;

            if (processInfo.Status == ProcessState.Running && MonitorHelper.CheckMonitorFilters(monitor, processInfo))
            {
                mqttObject = CreateProcessInfoMQTT(monitor, processInfo);
                state = PAYLOAD_ON;
            }

            var json = JsonConvert.SerializeObject(mqttObject, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            SendMonitorValue(monitor, GetStateTopic(monitor), state);
            SendMonitorValue(monitor, GetSensorTopic(monitor), json);
        }

        private void OnProcessStarted(object sender, ProcessEventArgs e)
        {
            string processName = ProcessHelper.CleanProcessName(e.ProcessInfo.ProcessName);

            List<ProcessMonitor> monitors = MonitorHelper.GetProcessMonitorsByProcessName(_monitors, processName);

            LoggerHelper.Debug("ProcessMonitorService::OnProcessStarted({0}) - {1} Monitors found.", processName, monitors.Count);
            if (monitors.Count == 0)
            {
                LoggerHelper.Debug("ProcessMonitorService::OnProcessStarted({0}) - Monitoring NOT FOUND (PID: {1} - Parent: {2})", processName, e.ProcessInfo.Id, e.ProcessInfo.ParentId);
                return;
            }

            ProcessInformation pi = ProcessHelper.GetProcessInformation(e.ProcessInfo.Id);
            if (pi == null)
            {
                LoggerHelper.Debug("ProcessMonitorService::OnProcessStarted({0}) - Unable to get information from PID {1}", processName, e.ProcessInfo.Id);
                return;
            }

            if (pi.Parent != null && IsChildProcess(pi))
            {
                LoggerHelper.Debug("ProcessMonitorService::OnProcessStarted({0}) - Spawning child {1} from {0}", processName, pi.Id, pi.Parent.Id);
                return;
            }

            LoggerHelper.Debug("ProcessMonitorService::OnProcessStarted({0}) - Process Detected (PID: {1} - Parent: {2})", processName, e.ProcessInfo.Id, e.ProcessInfo.ParentId);

            foreach (ProcessMonitor monitor in monitors)
                ProcessSingleProcess(monitor, pi);
        }

        private void OnProcessStopped(object sender, ProcessEventArgs e)
        {
            string processName = ProcessHelper.CleanProcessName(e.ProcessInfo.ProcessName);

            List<ProcessMonitor> monitors = MonitorHelper.GetProcessMonitorsByProcessName(_monitors, processName);

            LoggerHelper.Debug("ProcessMonitorService::OnProcessStarted({0}) - {1} Monitors found.", processName, monitors.Count);
            foreach (ProcessMonitor monitor in monitors)
                ExecuteMonitor(monitor);
        }

        private bool IsChildProcess(ProcessInformation processInfo)
        {
            return (processInfo.Parent != null) && (string.Compare(processInfo.ProcessName, processInfo.Parent.ProcessName, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private ProcessInfoMQTT CreateProcessInfoMQTT(ProcessMonitor monitor, ProcessInformation processInfo)
        {
            if (processInfo == null)
                return null;

            var uptime = (int)(DateTime.Now - processInfo.StartDateTime).TotalSeconds;

            ProcessInfoMQTT pi = new ProcessInfoMQTT
            {
                Id = processInfo.Id.ToString(),
                SessionId = processInfo.SessionId.ToString(),
                ProcessName = processInfo.ProcessName,
                MainWindowTitle = processInfo.MainWindowTitle,
                FullScreen = processInfo.FullScreen.ToString(),
                Windows = processInfo.Windows,
                ClassNames = processInfo.ClassNames,
                ProcessorUsage = processInfo.ProcessorUsage.ToString(),
                MemoryUsed = FormatSizeObject(processInfo.MemoryUsed),
            };

            pi.Status = processInfo.Status.ToString();
            pi.StartDateTime = FormatDateObject(processInfo.StartDateTime);
            pi.Uptime = FormatUptime(uptime);

            return pi;
        }

        private string FormatSizeObject(object value)
        {
            long sizeInBytes = MathHelper.ToLong(value, 0L);
            string formatStr = _config.GetValue("formats:memoryFormat", "MB");

            string format = formatStr.Contains(":") ? formatStr.Split(':')[0] : formatStr;
            int roundDigits = formatStr.Contains(":") ? MathHelper.ToInteger(formatStr.Split(':')[1]) : 0;

            double size = UnitsHelper.ConvertSize(sizeInBytes, format);
            return Math.Round(size, roundDigits).ToString(CultureInfo.InvariantCulture);
        }

        private string FormatDateObject(object value)
        {
            var format = _config.GetValue("formats:dateTimeFormat", "yyyy-MM-dd HH:mm:ss");
            if (value is DateTime)
                return ((DateTime)value).ToString(format, CultureInfo.InvariantCulture);

            return value.ToString();
        }

        private string FormatUptime(int value)
        {
            var inSeconds = _config.GetValue("formats:uptimeInSeconds", false);
            if (inSeconds)
                return value.ToString();

            TimeSpan uptime = TimeSpan.FromSeconds(MathHelper.ToDouble(value));
            return uptime.ToString(@"dd\:hh\:mm\:ss", CultureInfo.InvariantCulture);
        }

        private string GetStateTopic(ProcessMonitor monitor)
        {
            return $"Processes/{monitor.Name}/State";
        }

        private string GetSensorTopic(ProcessMonitor monitor)
        {
            return $"Processes/{monitor.Name}/Sensor";
        }

        private void SendMonitorValue(ProcessMonitor monitor, string topic, string value)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return;

            if (monitor.Config.General.Cacheable)
            {
                if (_cache.ContainsKey(topic) && _cache[topic].CompareTo(value) == 0)
                    return;

                _cache[topic] = value;
            }

            GetManager().PublishMessage(this, topic, value);
        }
    }
}