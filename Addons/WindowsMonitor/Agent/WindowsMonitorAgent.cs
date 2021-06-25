using IOTLinkAddon.Common;
using IOTLinkAPI.Addons;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Events;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Dynamic;
using System.IO;
using System.Windows.Forms;

namespace IOTLinkAddon.Agent
{
    public class WindowsMonitorAgent : AgentAddon
    {
        public override void Init(IAddonManager addonManager)
        {
            base.Init(addonManager);

            OnConfigReloadHandler += OnConfigReload;
            OnAgentRequestHandler += OnAgentRequest;
        }

        private void OnConfigReload(object sender, ConfigReloadEventArgs e)
        {
            LoggerHelper.Verbose("WindowsMonitorAgent::OnConfigReload");
        }

        private void OnAgentRequest(object sender, AgentAddonRequestEventArgs e)
        {
            LoggerHelper.Verbose("WindowsMonitorAgent::OnAgentRequest");

            AddonRequestType requestType = e.Data.requestType;
            switch (requestType)
            {
                case AddonRequestType.REQUEST_IDLE_TIME:
                    SendIdleTime();
                    break;

                case AddonRequestType.REQUEST_DISPLAY_INFORMATION:
                    SendDisplayInfo();
                    break;

                case AddonRequestType.REQUEST_DISPLAY_SCREENSHOT:
                    SendDisplayScreenshot();
                    break;

                default: break;
            }
        }

        private void SendIdleTime()
        {
            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_IDLE_TIME;
            addonData.requestData = PlatformHelper.GetIdleTime();
            GetManager().SendAgentResponse(this, addonData);
        }

        private void SendDisplayInfo()
        {
            dynamic addonData = new ExpandoObject();
            addonData.requestType = AddonRequestType.REQUEST_DISPLAY_INFORMATION;
            addonData.requestData = PlatformHelper.GetDisplays();
            GetManager().SendAgentResponse(this, addonData);
        }

        private void SendDisplayScreenshot()
        {
            Screen[] screens = Screen.AllScreens;
            for (var i = 0; i < screens.Length; i++)
            {
                byte[] screenshot = GetScreenshot(screens[i]);
                if (screenshot == null || screenshot.Length == 0)
                    return;

                dynamic addonData = new ExpandoObject();
                addonData.requestType = AddonRequestType.REQUEST_DISPLAY_SCREENSHOT;
                addonData.requestData = new ExpandoObject();
                addonData.requestData.displayIndex = i;
                addonData.requestData.displayScreen = screenshot;
                GetManager().SendAgentResponse(this, addonData);
            }
        }

        private byte[] GetScreenshot(Screen screen)
        {
            try
            {
                using (Bitmap bmp = new Bitmap(screen.Bounds.Width, screen.Bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(screen.Bounds.X, screen.Bounds.Y, 0, 0, screen.Bounds.Size, CopyPixelOperation.SourceCopy);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, ImageFormat.Png);
                            return ms.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Debug("GetScreenshot - Exception: {0}", ex.ToString());
            }
            return null;
        }
    }
}
