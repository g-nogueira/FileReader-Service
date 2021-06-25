namespace IOTLinkService.Service.Engine
{
    /// <summary>
    /// Agent Information
    /// </summary>
    public class AgentInfo
    {
        /// <summary>
        /// OS SessionId
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// Agent PID
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// OS Username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Command-line used to run this agent
        /// </summary>
        public string CommandLine { get; set; }
    }
}
