using IOTLinkAPI.Platform.Windows;
using System;

namespace IOTLinkAgent.Agent.Commands
{
    class ShowMessage : ICommand
    {
        private const string COMMAND_LINE = "showMessage";

        public string GetCommandLine()
        {
            return COMMAND_LINE;
        }

        public int ExecuteCommand(string[] args)
        {
            if (!Environment.UserInteractive)
                return -1;

            WindowsAPI.ShowMessage("IOT Link", string.Join(" ", args));
            return 0;
        }
    }
}
