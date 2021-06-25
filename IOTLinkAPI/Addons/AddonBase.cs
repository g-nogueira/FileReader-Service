namespace IOTLinkAPI.Addons
{
    /// <summary>
	/// Base application class used by both application interface and scripts.
	/// </summary>
    public abstract class AddonBase
    {
        /// <summary>
		/// Current addon directory.
		/// </summary>
		protected string _currentPath;

        /// <summary>
        /// Current set of informations about this addon.
        /// </summary>
        protected AddonInfo _addonInfo;

        /// <summary>
        /// AddonManager instance
        /// </summary>
        protected IAddonManager _manager;

        /// <summary>
		/// Addon constructor
		/// </summary>
		public AddonBase()
        {

        }

        /// <summary>
        /// Initialize the Addon
        /// </summary>
        public virtual void Init(IAddonManager addonManager)
        {
            _manager = addonManager;
        }

        /// <summary>
		/// Get the current addon directory
		/// </summary>
		/// <returns>string containing the addon directory</returns>
		public string GetCurrentPath()
        {
            return _currentPath;
        }

        /// <summary>
		/// Set the current addon directory. Used internally.
		/// </summary>
		/// <param name="path">String containing the addon directory</param>
		public void SetCurrentPath(string path)
        {
            _currentPath = path;
        }

        /// <summary>
		/// Get the current <see cref="AddonInfo"/>
		/// </summary>
		public AddonInfo GetAppInfo()
        {
            return _addonInfo;
        }

        /// <summary>
		/// Set the current <see cref="AddonInfo"/>.
		/// </summary>
		/// <param name="addonInfo"></param>
		public void SetAddonInfo(AddonInfo addonInfo)
        {
            _addonInfo = addonInfo;
        }
    }
}
