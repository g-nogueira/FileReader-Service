using IOTLinkAPI.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;

namespace IOTLinkAPI.Configs
{
    /// <summary>
	/// Handle the configurations of an App.
	/// </summary>
    public class AddonConfig
    {
        /// <summary>
		/// Addon Unique ID
		/// </summary>
		public string AddonId;

        /// <summary>
        /// Addon Name
        /// </summary>
        public string AddonName;

        /// <summary>
        /// Addon DLL file.
        /// </summary>
        public string AddonFile;

        /// <summary>
        /// Addon Directory Path
        /// </summary>
        public string AddonPath;

        /// <summary>
        /// Minimum API version to run this addon.
        /// </summary>
        public string MinApiVersion;

        /// <summary>
        /// Maximum API version to run this addon.
        /// </summary>
        public string MaxApiVersion;

        /// <summary>
        /// Define if the addon is enabled or not.
        /// </summary>
        public bool Enabled;

        private static Dictionary<string, AddonConfig> _configs = new Dictionary<string, AddonConfig>();

        /// <summary>
        /// Get a valid instance of this class or create a new one if necessary.
        /// </summary>
        /// <param name="dirName">String containing the directory name of the Addon to be read.</param>
        /// <returns><see cref="AddonConfig">Configuration</see> instance</returns>
        public static AddonConfig GetInstance(string dirName)
        {
            if (!_configs.ContainsKey(dirName))
                _configs[dirName] = new AddonConfig(dirName);

            return _configs[dirName];
        }

        private AddonConfig(string dirName)
        {
            this.AddonPath = Path.Combine(PathHelper.AddonsPath(), dirName);
        }

        /// <summary>
        /// Load the configuration file if found.
        /// </summary>
        /// <returns>True when successful, false otherwise.</returns>
        public bool Load()
        {
            string filename = Path.Combine(this.AddonPath, "addon.yaml");
            if (!File.Exists(filename))
                return false;

            try
            {
                var input = new StringReader(File.ReadAllText(filename));
                var yaml = new YamlStream();
                yaml.Load(input);

                var root = (YamlMappingNode)yaml.Documents[0].RootNode;
                this.AddonId = ((YamlScalarNode)root.Children[new YamlScalarNode("id")]).Value;
                this.AddonName = ((YamlScalarNode)root.Children[new YamlScalarNode("name")]).Value;
                this.AddonFile = ((YamlScalarNode)root.Children[new YamlScalarNode("file")]).Value;
                this.Enabled = Convert.ToBoolean(((YamlScalarNode)root.Children[new YamlScalarNode("enabled")]).Value);
                this.MinApiVersion = ((YamlScalarNode)root.Children[new YamlScalarNode("minApiVersion")]).Value;
                this.MaxApiVersion = ((YamlScalarNode)root.Children[new YamlScalarNode("maxApiVersion")]).Value;
                return true;
            }
            catch (Exception) { }

            return false;
        }
    }
}
