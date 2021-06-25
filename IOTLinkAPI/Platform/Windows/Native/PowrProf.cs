using System.Runtime.InteropServices;

namespace IOTLinkAPI.Platform.Windows.Native
{
#pragma warning disable 1591
    public class PowrProf
    {
        [DllImport("powrprof.dll")]
        static extern bool IsPwrHibernateAllowed();

        [DllImport("powrprof.dll")]
        static extern bool IsPwrShutdownAllowed();

        [DllImport("powrprof.dll")]
        static extern bool IsPwrSuspendAllowed();

        [DllImport("powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);
    }
}
