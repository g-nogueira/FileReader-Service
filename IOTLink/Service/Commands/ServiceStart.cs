using IOTLinkAPI.Helpers;
using System;

namespace IOTLink.Service.Commands
{
    class ServiceStart : ICommand
    {
        private const string COMMAND_LINE = "start";

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

#if DEBUG
            IOTLinkService service = new IOTLinkService();
            service.OnDebug();
#endif
            return 0;
        }
    }
}
