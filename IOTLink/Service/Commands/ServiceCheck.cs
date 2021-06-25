using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Windows;
using System;
using System.Linq;
using System.ServiceProcess;

namespace IOTLink.Service.Commands
{
    class ServiceCheck : ICommand
    {
        private const string COMMAND_LINE = "check";

        public string GetCommandLine()
        {
            return COMMAND_LINE;
        }

        public int ExecuteCommand(string[] args)
        {
            if (Program.IsService)
            {
                LoggerHelper.Verbose("Command {0} - Running on non-interactive mode. Skipping", COMMAND_LINE);
                return -1;
            }

            try
            {
                bool serviceExists = ServiceController.GetServices().Any(s => s.ServiceName == "IOTLink");
                if (serviceExists)
                {
                    LoggerHelper.Verbose("Command {0} - Service is installed.", COMMAND_LINE);
                    WindowsAPI.ShowMessage("Service Installer", "Service is installed.");
                }
                else
                {
                    LoggerHelper.Verbose("Command {0} - Service is NOT installed.", COMMAND_LINE);
                    WindowsAPI.ShowMessage("Service Installer", "Service is NOT installed.");
                }

                return 0;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Command {0} - Error while running command: {1}", COMMAND_LINE, ex.ToString());
                WindowsAPI.ShowMessage("Service Installer", "Please, run as an administrator.");
            }

            return -1;
        }
    }
}
