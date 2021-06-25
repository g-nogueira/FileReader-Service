using IOTLinkAddon.Common;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace IOTLinkAddon.Agent
{
    public class CommandsAgent : AgentAddon
    {
        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);

            OnConfigReloadHandler += OnConfigReload;
            OnAgentRequestHandler += OnAgentRequest;
        }

        private void OnConfigReload(object sender, ConfigReloadEventArgs e)
        {
            LoggerHelper.Verbose("CommandsAgent::OnConfigReload");
        }

        private void OnAgentRequest(object sender, AgentAddonRequestEventArgs e)
        {
            LoggerHelper.Verbose("CommandsAgent::OnAgentRequest");

            AddonRequestType requestType = e.Data.requestType;
            switch (requestType)
            {
                case AddonRequestType.REQUEST_DISPLAY_TURN_ON:
                    DisplayTurnOn();
                    break;

                case AddonRequestType.REQUEST_DISPLAY_TURN_OFF:
                    DisplayTurnOff();
                    break;

                case AddonRequestType.REQUEST_KEYS_SEND:
                    SendKeys(e.Data.requestData);
                    break;

                case AddonRequestType.REQUEST_KEYS_PRESS:
                    PressKey(e.Data.requestData);
                    break;

                default: break;
            }
        }

        private void DisplayTurnOn()
        {
            LoggerHelper.Verbose("CommandsAgent::DisplayTurnOn");
            PlatformHelper.TurnOnDisplays();
        }

        private void DisplayTurnOff()
        {
            LoggerHelper.Verbose("CommandsAgent::DisplayTurnOff");
            PlatformHelper.TurnOffDisplays();
        }

        private void SendKeys(dynamic data)
        {
            LoggerHelper.Verbose("CommandsAgent::SendKeys - {0}", data);
            JArray jArray = data;
            string[] keys = jArray.ToObject<string[]>();

            foreach (string key in keys)
            {
                System.Windows.Forms.SendKeys.SendWait(key);
                Thread.Sleep(100);
            }
        }

        private void PressKey(dynamic data)
        {
            LoggerHelper.Verbose("CommandsAgent::PressKey - {0}", data);
            PlatformHelper.PressKey((byte)data);
        }
    }
}
