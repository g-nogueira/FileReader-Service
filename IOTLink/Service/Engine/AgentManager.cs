using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform;
using IOTLinkAPI.Platform.Windows;
using IOTLinkService.Service.WebSockets.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace IOTLinkService.Service.Engine
{
    public class AgentManager
    {
        private static AgentManager _instance;

        private readonly object getAgentsLock = new object();
        private readonly object startAgentsLock = new object();
        private readonly object startAgentLock = new object();
        private readonly object stopAgentsLock = new object();

        public static AgentManager GetInstance()
        {
            if (_instance == null)
                _instance = new AgentManager();

            return _instance;
        }

        private AgentManager()
        {
            LoggerHelper.Trace("AgentManager instance created.");
        }

        public List<AgentInfo> GetAgents()
        {
            lock (getAgentsLock)
            {
                LoggerHelper.Trace("GetAgents() - Initialized");

                List<AgentInfo> agents = new List<AgentInfo>();

                Process[] processes = Process.GetProcessesByName(PathHelper.APP_AGENT_NAME);
                foreach (Process process in processes)
                {
                    int sessionId = Convert.ToInt32(process.SessionId);
                    int processId = Convert.ToInt32(process.Id);
                    string commandLine = process.StartInfo?.Arguments;
                    string username = PlatformHelper.GetUsername(sessionId);

                    agents.Add(new AgentInfo
                    {
                        SessionId = sessionId,
                        ProcessId = processId,
                        CommandLine = commandLine,
                        Username = username
                    });
                }

                return agents;
            }
        }

        public void StartAgents()
        {
            lock (startAgentsLock)
            {
                LoggerHelper.Trace("StartAgents() - Initialized");
                try
                {
                    List<WindowsSessionInfo> winSessions = WindowsAPI.GetWindowsSessions().FindAll(s => s.IsActive);
                    foreach (WindowsSessionInfo sessionInfo in winSessions)
                    {
                        LoggerHelper.Trace(
                            "StartAgents() - Windows Session Found. SessionId: {0} StationName: {1} Username: {2} IsActive: {3}",
                            sessionInfo.SessionID, sessionInfo.StationName, sessionInfo.Username, sessionInfo.IsActive
                        );
                        StartAgent(sessionInfo.SessionID, sessionInfo.Username);
                    }
                }
                catch (Exception ex)
                {
                    LoggerHelper.Error("StartAgents() - Exception Handled: {0}", ex.Message);
                }
            }
        }

        public void StartAgent(int sessionId, string username = null)
        {
            lock (startAgentLock)
            {
                LoggerHelper.Trace("StartAgent() - Initialized. SessionId: {0} Username: {1}", sessionId, username);
                List<AgentInfo> agents = GetAgents();

                foreach (AgentInfo agentInfo in agents)
                {
                    LoggerHelper.Trace("StartAgent() - Agent Found. SessionId: {0} PID: {1} Username: {2} CommandLine: {3}", agentInfo.SessionId, agentInfo.ProcessId, agentInfo.Username, agentInfo.CommandLine);
                    if (agentInfo.SessionId == sessionId)
                    {
                        LoggerHelper.Trace("StartAgent() - Agent instance is already running for this user. Skipping.");
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(username))
                    username = PlatformHelper.GetUsername(sessionId);

                RunInfo runInfo = new RunInfo
                {
                    Application = Path.Combine(PathHelper.BaseAppPath(), PathHelper.APP_AGENT_NAME + ".exe"),
                    CommandLine = "--agent " + WebSocketServerManager.WEBSOCKET_URI,
                    WorkingDir = PathHelper.BaseAppPath(),
                    Username = username,
                    Visible = false,
                    Fallback = false
                };

                LoggerHelper.Debug(
                    "StartAgent() - Executing Run. Application: {0} CommandLine: {1} WorkingDir: {2} Username: {3} Visible: {4} Fallback: {5}",
                    runInfo.Application, runInfo.CommandLine, runInfo.WorkingDir, runInfo.Username, runInfo.Visible, runInfo.Fallback
                );
                PlatformHelper.Run(runInfo);
            }
        }

        public void StopAgents()
        {
            lock (stopAgentsLock)
            {
                LoggerHelper.Trace("StopAgents() - Initialized");
                List<AgentInfo> agents = GetAgents();
                foreach (AgentInfo agentInfo in agents)
                {
                    try
                    {
                        LoggerHelper.Trace("StopAgents() - Agent Found. SessionId: {0} PID: {1} Username: {2} CommandLine: {3}", agentInfo.SessionId, agentInfo.ProcessId, agentInfo.Username, agentInfo.CommandLine);
                        Process process = Process.GetProcessById(agentInfo.ProcessId);
                        if (process != null)
                        {
                            LoggerHelper.Debug("StopAgents() - Process {0} found. Killing it.", process.Id);
                            process.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Error("StopAgents() - Failed to Kill PID {0}: {1}", agentInfo.ProcessId, ex.ToString());
                    }
                }
            }
        }
    }
}
