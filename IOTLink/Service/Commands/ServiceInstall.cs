using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Windows;
using System;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

namespace IOTLink.Service.Commands
{
    class ServiceInstall : ICommand
    {
        private const string COMMAND_LINE = "install";

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
                    LoggerHelper.Verbose("Command {0} - Service is already installed. Skipping", COMMAND_LINE);
                    WindowsAPI.ShowMessage("Service Installer", "Service is already installed.");
                    return -1;
                }

                ManagedInstallerClass.InstallHelper(new string[] { "/LogFile=", "/LogToConsole=false", Assembly.GetExecutingAssembly().Location });
                WindowsAPI.ShowMessage("Service Installer", "Service is installed sucessfully.");

                LoggerHelper.Verbose("Command {0} - Service is installed sucessfully.", COMMAND_LINE);
                return 0;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Command {0} - Error while running command: {1}", COMMAND_LINE, ex.ToString());
                WindowsAPI.ShowMessage("Service Installer", "Install failed. Please, run as an administrator.");
            }

            return -1;
        }
    }
}
