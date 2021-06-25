using IOTLinkAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IOTLinkAPI.Configs
{
    public class Configuration
    {
        public string Key { get; set; }

        public bool IsRoot
        {
            get
            {
                return string.IsNullOrWhiteSpace(Key);
            }
        }

        private Dictionary<object, object> config;

        private Configuration(Dictionary<object, object> config, string key = null)
        {
            Key = key;
            this.config = config;
        }

        public object ReadConfigKey(string keyString, object defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(keyString))
                return defaultValue;

            string[] keys = GetKeys(keyString);
            if (keys.Length == 0 || !config.ContainsKey(keys[0]))
                return defaultValue;

            var result = config[keys[0]];

            keys = keys.Skip(1).ToArray();
            foreach (string subKey in keys)
            {
                if ((result is Dictionary<object, object>) == false)
                    return defaultValue;

                var dict = (Dictionary<object, object>)result;
                if (!dict.ContainsKey(subKey))
                    return defaultValue;

                result = dict[subKey];
            }

            return result;
        }

        public Configuration GetValue(string keyString)
        {
            object result = ReadConfigKey(keyString);
            if (result == null || (result is Dictionary<object, object>) == false)
                return null;

            return BuildConfiguration(keyString, result);
        }

        public string GetValue(string keyString, string defaultValue = null)
        {
            object result = ReadConfigKey(keyString, defaultValue);
            if (result == null)
                return defaultValue;

            return result.ToString();
        }

        public bool GetValue(string keyString, bool defaultValue = false)
        {
            object result = ReadConfigKey(keyString, defaultValue);
            return MathHelper.ToBoolean(result, defaultValue);
        }

        public int GetValue(string keyString, int defaultValue = 0)
        {
            object result = ReadConfigKey(keyString, defaultValue);
            return MathHelper.ToInteger(result, defaultValue);
        }

        public long GetValue(string keyString, long defaultValue = 0L)
        {
            object result = ReadConfigKey(keyString, defaultValue);
            return MathHelper.ToLong(result, defaultValue);
        }

        public double GetValue(string keyString, double defaultValue = 0d)
        {
            object result = ReadConfigKey(keyString, defaultValue);
            return MathHelper.ToDouble(result, defaultValue);
        }

        public float GetValue(string keyString, float defaultValue = 0f)
        {
            object result = ReadConfigKey(keyString, defaultValue);
            return MathHelper.ToFloat(result, defaultValue);
        }

        public int GetListCount(string keyString)
        {
            var config = ReadConfigKey(keyString, null);
            if ((config is List<object>) == false)
                return 0;

            return ((List<object>)config).Count;
        }

        public List<T> GetList<T>(string keyString)
        {
            var config = ReadConfigKey(keyString, null);
            if ((config is List<object>) == false)
                return new List<T>();

            List<object> list = (List<object>)config;
            return list.Select(x => (T)x).ToList();
        }

        public List<Configuration> GetConfigurationList(string keyString)
        {
            var result = ReadConfigKey(keyString);

            if ((result is Dictionary<object, object>))
            {
                Dictionary<object, object> config = (Dictionary<object, object>)result;
                return ParseConfigurationToList(config);
            }

            if ((result is List<object>))
            {
                List<object> config = (List<object>)result;
                return ParseConfigurationToList(keyString, config);
            }

            return new List<Configuration>();
        }

        private List<Configuration> ParseConfigurationToList(Dictionary<object, object> configs)
        {
            List<Configuration> results = new List<Configuration>();
            foreach (var item in configs)
            {
                var config = BuildConfiguration(item.Key.ToString(), item.Value);
                if (config == null)
                    continue;

                results.Add(config);
            }

            return results;
        }

        private List<Configuration> ParseConfigurationToList(string keyString, List<object> configs)
        {
            List<Configuration> results = new List<Configuration>();
            foreach (var item in configs)
            {
                if ((item is Dictionary<object, object>) == false)
                    continue;

                results.Add(BuildConfiguration(keyString, item));
            }

            return results;
        }

        public Configuration GetConfigurationListItem(string keyString, int index)
        {
            var listObj = ReadConfigKey(keyString, null);
            if ((listObj is List<object>) == false)
                return null;

            List<object> myList = (List<object>)listObj;
            if (myList.Count <= index)
                return null;

            var item = myList[index];
            if ((item is Dictionary<object, object>) == false)
                return null;

            return BuildConfiguration(keyString, item);
        }

        public static Configuration BuildConfiguration(string keyString, object configObject)
        {
            if ((configObject is Dictionary<object, object>) == false)
                return null;

            Dictionary<object, object> config = (Dictionary<object, object>)configObject;
            return BuildConfiguration(keyString, config);
        }

        public static Configuration BuildConfiguration(string keyString, Dictionary<object, object> config)
        {
            return new Configuration(config, GetKey(keyString));
        }

        private static string[] GetKeys(string keyString)
        {
            if (string.IsNullOrWhiteSpace(keyString))
                return new string[0];

            return keyString.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetKey(string keyString)
        {
            string[] keys = GetKeys(keyString);
            if (keys.Length == 0)
                return null;

            return keys[keys.Length - 1];
        }
    }
}
