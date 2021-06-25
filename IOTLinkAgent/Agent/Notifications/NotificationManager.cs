using IOTLinkAgent.Agent.Loaders;
using IOTLinkAPI.Helpers;
using NotificationsExtensions.ToastContent;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace IOTLinkAgent.Agent.Notifications
{
    class NotificationManager
    {
        private const string APP_ID = "IOT Link";

        private static NotificationManager _instance;

        public static NotificationManager GetInstance()
        {
            if (_instance == null)
                _instance = new NotificationManager();

            return _instance;
        }

        private NotificationManager()
        {
            LoggerHelper.Trace("NotificationManager instance created.");
        }

        public void ShowNotification(string title, string message, string iconUrl = null, string launchParams = null)
        {
            LoggerHelper.Verbose("NotificationManager - Displaying Notification");

            var toast = ToastContentFactory.CreateToastImageAndText02();
            toast.TextHeading.Text = ParseTitle(title);
            toast.Image.Src = ParseIconUrl(iconUrl);
            toast.TextBodyWrap.Text = message;
            toast.Launch = launchParams;

            string xmlStr = toast.GetContent();
            LoggerHelper.DataDump("Title: {0} | Message: {1} | Icon: {2} | LaunchParams: {3}", toast.TextHeading.Text, message, toast.Image.Src, launchParams);
            LoggerHelper.DataDump("XML: {0}", xmlStr);

            var xml = new XmlDocument();
            xml.LoadXml(xmlStr);
            ShowToast(xml);
        }

        private string ParseTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return APP_ID;

            return title;
        }

        private string ParseIconUrl(string iconUrl)
        {
            if (string.IsNullOrWhiteSpace(iconUrl) || !iconUrl.StartsWith("http://") && !iconUrl.StartsWith("https://") && !iconUrl.StartsWith("file:///"))
                iconUrl = string.Format("file:///{0}", Path.Combine(PathHelper.IconsPath(), "application.ico"));

            if (iconUrl.StartsWith("http://") || !iconUrl.StartsWith("https://"))
            {
                try
                {
                    iconUrl = string.Format("file:///{0}", ImageLoader.DownloadFile(iconUrl));
                }
                catch (Exception ex)
                {
                    LoggerHelper.Warn("Cannot download {0} for displaying notification: {1}", iconUrl, ex.ToString());
                }
            }

            return iconUrl;
        }

        private void ShowToast(XmlDocument toastXml)
        {
            // Create the toast and attach event listeners
            ToastNotification toast = new ToastNotification(toastXml);
            toast.Activated += ToastActivated;

            // Show the toast.
            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
        }

        private void ToastActivated(ToastNotification sender, object e)
        {
            if (e.GetType() != typeof(ToastActivatedEventArgs))
                return;

            ToastActivatedEventArgs args = (ToastActivatedEventArgs)e;
            if (string.IsNullOrWhiteSpace(args.Arguments))
                return;

            LoggerHelper.Trace("Toast Arguments: {0}", args.Arguments);

            var regex = @"^toast://(?<command>[A-Za-z_]+)([/]{0,1})(?<arguments>.*)$";
            var toastLaunch = Regex.Match(args.Arguments, regex);
            var toastCommand = toastLaunch.Groups["command"];
            var toastArgs = toastLaunch.Groups["arguments"];
            if (!toastCommand.Success)
            {
                LoggerHelper.Warn("Invalid notification toast parameters: {0}", args.Arguments);
                return;
            }

            string command = toastCommand.Value.Trim().ToLowerInvariant();
            string[] commandArgs = toastArgs.Value.Split(new char[] { '#' }, options: StringSplitOptions.RemoveEmptyEntries);
            ParseToastCommand(command, commandArgs);
        }

        private void ParseToastCommand(string command, string[] args)
        {
            try
            {
                switch (command)
                {
                    case "open":
                        ParseOpenCommand(args);
                        break;

                    case "addon":
                        LoggerHelper.Warn("Addon type not implemented yet.");
                        break;

                    default:
                        LoggerHelper.Warn("Unknown toast command. Command: {0} Args: {1}", command, string.Join(" ", args));
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("Error while executing toast command {0}: {1}", command, ex.ToString());
            }
        }

        private void ParseOpenCommand(string[] args)
        {
            if (args.Length < 1)
                return;

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = args[0],
                Verb = "open",
                UseShellExecute = true
            };

            if (args.Length >= 2)
                psi.Arguments = args[1];

            if (args.Length >= 3)
                psi.WorkingDirectory = args[2];

            Process.Start(psi);
        }
    }
}
