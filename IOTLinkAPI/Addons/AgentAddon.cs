using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using System;

namespace IOTLinkAPI.Addons
{
    /// <summary>
    /// Agent Addon Base.
    /// This class should be inherited by any agent addon that needs to run in background.
    /// This will be run on the IOT Link Agents.
    /// </summary>
    /// <seealso cref="AddonBase"/>
    public abstract class AgentAddon : AddonBase
    {
        public event ConfigReloadedHandler OnConfigReloadHandler;
        public event AgentRequestHandler OnAgentRequestHandler;

        public delegate void ConfigReloadedHandler(object sender, ConfigReloadEventArgs e);
        public delegate void AgentRequestHandler(object sender, AgentAddonRequestEventArgs e);

        public IAddonAgentManager GetManager()
        {
            return (IAddonAgentManager)_manager;
        }

        public void Raise_OnConfigReloadHandler(object sender, ConfigReloadEventArgs e)
        {
            try
            {
                OnConfigReloadHandler?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("AgentAddon::OnConfigReloadHandler - AddonId: {0} Error: {1}", _addonInfo.AddonId, ex.ToString());
            }
        }

        public void Raise_OnAgentResponse(object sender, AgentAddonRequestEventArgs e)
        {
            try
            {
                OnAgentRequestHandler?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("AgentAddon::OnAgentResponse - AddonId: {0} Error: {1}", _addonInfo.AddonId, ex.ToString());
            }
        }
    }
}
