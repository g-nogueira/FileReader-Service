using IOTLinkAPI.Configs;
using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.HomeAssistant;
using System;
using System.Collections.Generic;
using System.IO;

namespace IOTLinkAddon.Service.Monitors
{
    class StorageMonitor : BaseMonitor
    {
        private static readonly string CONFIG_KEY = "HardDrive";

        public override string GetConfigKey()
        {
            return CONFIG_KEY;
        }

        public override List<MonitorItem> GetMonitorItems(Configuration config, int interval)
        {
            List<MonitorItem> result = new List<MonitorItem>();

            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo == null || !driveInfo.IsReady || driveInfo.DriveType != DriveType.Fixed)
                    continue;

                try
                {
                    string drive = driveInfo.Name.Remove(1, 2);
                    string topic = string.Format("Stats/HardDrive/{0}", drive);

                    long usedSpace = driveInfo.TotalSize - driveInfo.TotalFreeSpace;
                    int driveUsage = (int)((100.0 / driveInfo.TotalSize) * usedSpace);

                    // Total Size
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_DISK_SIZE,
                        Topic = topic + "/TotalSize",
                        Value = driveInfo.TotalSize,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Id = $"{drive}_TotalStorage",
                            Name = $"Storage {drive} - Total Storage",
                            Unit = "GB",
                            Icon = "mdi:harddisk"
                        }
                    });

                    // Available Free Space
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_DISK_SIZE,
                        Topic = topic + "/AvailableFreeSpace",
                        Value = driveInfo.AvailableFreeSpace,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Id = $"{drive}_AvailableFreeSpace",
                            Name = $"Storage {drive} - Available Free Space",
                            Unit = "GB",
                            Icon = "mdi:harddisk"
                        }
                    });

                    // Total Free Space
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_DISK_SIZE,
                        Topic = topic + "/TotalFreeSpace",
                        Value = driveInfo.TotalFreeSpace,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Id = $"{drive}_TotalFreeSpace",
                            Name = $"Storage {drive} - Total Free Space",
                            Unit = "GB",
                            Icon = "mdi:harddisk"
                        }
                    });

                    // Used Space
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_DISK_SIZE,
                        Topic = topic + "/UsedSpace",
                        Value = usedSpace,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Id = $"{drive}_UsedSpace",
                            Name = $"Storage {drive} - Used Space",
                            Unit = "GB",
                            Icon = "mdi:harddisk"
                        }
                    });

                    // Drive Format
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_RAW,
                        Topic = topic + "/DriveFormat",
                        Value = driveInfo.DriveFormat,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Id = $"{drive}_DriveFormat",
                            Name = $"Storage {drive} - Format",
                            Icon = "mdi:harddisk"
                        }
                    });

                    // Drive Usage
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_RAW,
                        Topic = topic + "/DriveUsage",
                        Value = driveUsage,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Id = $"{drive}_DriveUsage",
                            Name = $"Storage {drive} - Usage",
                            Unit = "%",
                            Icon = "mdi:chart-donut"
                        }
                    });

                    // Drive Label
                    result.Add(new MonitorItem
                    {
                        ConfigKey = CONFIG_KEY,
                        Type = MonitorItemType.TYPE_RAW,
                        Topic = topic + "/VolumeLabel",
                        Value = driveInfo.VolumeLabel,
                        DiscoveryOptions = new HassDiscoveryOptions()
                        {
                            Component = HomeAssistantComponent.Sensor,
                            Id = $"{drive}_VolumeLabel",
                            Name = $"Storage {drive} - Label",
                            Icon = "mdi:harddisk"
                        }
                    });
                }
                catch (Exception ex)
                {
                    if (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
                        LoggerHelper.Error("Access to drives not allowed. Error: {0}", ex.ToString());
                    else if (ex is DriveNotFoundException)
                        LoggerHelper.Error("Drive not found. Error: {0}", ex.ToString());
                    else if (ex is IOException)
                        LoggerHelper.Error("Drive inaccessible. Error: {0}", ex.ToString());
                    else
                        LoggerHelper.Error("Error while getting drive information: {0}", ex.ToString());
                }
            }
            return result;
        }
    }
}
