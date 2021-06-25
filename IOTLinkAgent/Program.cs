using IOTLinkAgent.Agent;
using IOTLinkAgent.Agent.Commands;
using IOTLinkAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IOTLinkAgent
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static int Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            LoggerHelper.Info("Agent Initialized");

            // Service Run
            if (!Environment.UserInteractive)
            {
                LoggerHelper.Error("Agent has been initialized in a non-interactive environment. Finishing.");
                return -1;
            }

            new Task(() => Run(args)).Start();

            Application.Run();
            new ManualResetEvent(false).WaitOne();
            return 0;
        }

        private static void Run(string[] args)
        {
            Dictionary<string, List<string>> commandLine = ParseCommandLine(args);
            if (commandLine.ContainsKey("agent"))
            {
                AgentMain.GetInstance().Init(commandLine["agent"]);
            }
            else
            {
                LoggerHelper.Debug("Searching for internal commands");

                int result = 0;
                Dictionary<string, ICommand> commands = GetCommands();

                foreach (KeyValuePair<string, ICommand> command in commands)
                {
                    if (commandLine.ContainsKey(command.Key))
                    {
                        LoggerHelper.Verbose("Command found: {0}", command.Key);
                        try
                        {
                            result = command.Value.ExecuteCommand(commandLine[command.Key].ToArray());
                            if (result != 0)
                                break;
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Error("Error while executing command {0}: {1}", command, ex.ToString());
                            result = -1;
                            break;
                        }
                    }
                }

                LoggerHelper.Info("Agent finishing with result: {0}", result);
                LoggerHelper.GetInstance().Flush();

                Environment.Exit(result);
            }
        }

        private static Dictionary<string, List<string>> ParseCommandLine(string[] args)
        {
            Dictionary<string, List<string>> commands = new Dictionary<string, List<string>>();
            if (args == null || args.Length == 0)
                return commands;

            // Run through all arguments to find runnable commands
            Queue<string> argsQueue = new Queue<string>(args);
            while (argsQueue.Count > 0)
            {
                // Ignore all command line arguments until we find an argument
                // which starts with '--' characters
                while (argsQueue.Count > 0 && !argsQueue.Peek().StartsWith("--"))
                    argsQueue.Dequeue();

                string command = argsQueue.Dequeue().Trim().ToLowerInvariant().Remove(0, 2);

                // Parse command line to get all commands arguments
                List<string> commandArgs = new List<string>();
                while (argsQueue.Count > 0 && !argsQueue.Peek().StartsWith("--"))
                    commandArgs.Add(argsQueue.Dequeue());

                commands.Add(command, commandArgs);
            }

            return commands;
        }

        private static Dictionary<string, ICommand> GetCommands()
        {
            var commands = new Dictionary<string, ICommand>();

            var interfaceType = typeof(ICommand);
            var interfaces = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract && !p.IsInterface);

            foreach (Type type in interfaces)
            {
                ICommand command = (ICommand)Activator.CreateInstance(type);
                string key = command.GetCommandLine().Trim().ToLowerInvariant();
                commands.Add(key, command);
            }

            return commands;
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LoggerHelper.Critical("Critical Unhandled Exception: " + e.ExceptionObject.ToString());
            LoggerHelper.GetInstance().Flush();
        }
    }
}
