using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using IOTLinkAPI.Platform.Events.MQTT;
using System;
using static IOTLinkAPI.Platform.Events.MQTT.MQTTHandlers;

namespace IOTLinkAPI.Addons
{
    /// <summary>
    /// Service Addon Base.
    /// This class should be inherited by any service addon that needs to run in background.
    /// This will be run on the Windows Service.
    /// </summary>
    /// <seealso cref="AddonBase"/>
    public abstract class ServiceAddon : AddonBase
    {
        public event MQTTEventHandler OnMQTTConnectedHandler;
        public event MQTTEventHandler OnMQTTDisconnectedHandler;
        public event MQTTMessageEventHandler OnMQTTMessageReceivedHandler;
        public event SessionChangeHandler OnSessionChangeHandler;
        public event ConfigReloadedHandler OnConfigReloadHandler;
        public event AgentResponseHandler OnAgentResponseHandler;
        public event RefreshRequestedHandler OnRefreshRequestedHandler;

        public delegate void SessionChangeHandler(object sender, SessionChangeEventArgs e);
        public delegate void ConfigReloadedHandler(object sender, ConfigReloadEventArgs e);
        public delegate void AgentResponseHandler(object sender, AgentAddonResponseEventArgs e);
        public delegate void RefreshRequestedHandler(object sender, EventArgs e);

        public IAddonServiceManager GetManager()
        {
            return (IAddonServiceManager)_manager;
        }

        public void Raise_OnSessionChange(object sender, SessionChangeEventArgs e)
        {
            try
            {
                OnSessionChangeHandler?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ServiceAddon::OnSessionChange - AddonId: {0} Error: {1}", _addonInfo.AddonId, ex.ToString());
            }
        }

        public void Raise_OnMQTTConnected(object sender, MQTTEventEventArgs e)
        {
            try
            {
                OnMQTTConnectedHandler?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ServiceAddon::OnMQTTConnected - AddonId: {0} Error: {1}", _addonInfo.AddonId, ex.ToString());
            }
        }

        public void Raise_OnMQTTDisconnected(object sender, MQTTEventEventArgs e)
        {
            try
            {
                OnMQTTDisconnectedHandler?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ServiceAddon::OnMQTTDisconnected - AddonId: {0} Error: {1}", _addonInfo.AddonId, ex.ToString());
            }
        }

        public void Raise_OnMQTTMessageReceived(object sender, MQTTMessageEventEventArgs e)
        {
            try
            {
                OnMQTTMessageReceivedHandler?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ServiceAddon::OnMQTTMessageReceived - AddonId: {0} Error: {1}", _addonInfo.AddonId, ex.ToString());
            }
        }

        public void Raise_OnConfigReloadHandler(object sender, ConfigReloadEventArgs e)
        {
            try
            {
                OnConfigReloadHandler?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ServiceAddon::OnConfigReloadHandler - AddonId: {0} Error: {1}", _addonInfo.AddonId, ex.ToString());
            }
        }

        public void Raise_OnAgentResponse(object sender, AgentAddonResponseEventArgs e)
        {
            try
            {
                OnAgentResponseHandler?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ServiceAddon::OnAgentResponse - AddonId: {0} Error: {1}", _addonInfo.AddonId, ex.ToString());
            }
        }

        public void Raise_OnRefreshRequested(object sender, EventArgs e)
        {
            try
            {
                OnRefreshRequestedHandler?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("ServiceAddon::OnRefreshRequested - AddonId: {0} Error: {1}", _addonInfo.AddonId, ex.ToString());
            }
        }
    }
}
