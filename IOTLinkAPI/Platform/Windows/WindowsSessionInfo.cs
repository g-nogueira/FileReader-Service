namespace IOTLinkAPI.Platform.Windows
{
    public class WindowsSessionInfo
    {
        public int SessionID { get; set; }

        public string Username { get; set; }

        public string StationName { get; set; }

        public bool IsActive { get; set; }

        public bool IsUser(string user)
        {
            return Username != null && user != null && string.Compare(Username.Trim().ToLowerInvariant(), user.Trim().ToLowerInvariant()) == 0;
        }
    }
}
