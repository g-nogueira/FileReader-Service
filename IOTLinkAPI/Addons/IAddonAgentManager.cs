namespace IOTLinkAPI.Addons
{
    public interface IAddonAgentManager : IAddonManager
    {
        void SendAgentResponse(AgentAddon sender, dynamic addonData);
    }
}
