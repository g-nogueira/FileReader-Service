using IOTLinkAPI.Configs;

namespace IOTLinkAPI.Addons
{
    /// <summary>
	/// Object containing all informations about an addon. Used by the <see cref="AddonManager"/>.
	/// </summary>
	/// <seealso cref="AddonManager"/>
	public class AddonInfo
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
        /// Addon Directory
        /// </summary>
        public string AddonPath;

        /// <summary>
        /// Addon DLL resource (filename).
        /// </summary>
        public string AddonFile;

        /// <summary>
        /// Define if the addon is enabled or not.
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// Define if the addon is an internal version (embedded).
        /// </summary>
        public bool Internal;

        /// <summary>
        /// Addon <see cref="AddonConfig">Settings</see>.
        /// </summary>
        public AddonConfig Settings;

        public ServiceAddon ServiceAddon { get; set; }

        public AgentAddon AgentAddon { get; set; }
    }
}
