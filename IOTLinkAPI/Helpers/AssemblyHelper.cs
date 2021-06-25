using System;
using System.Reflection;

namespace IOTLinkAPI.Helpers
{
    public class AssemblyHelper
    {

        public static string GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public static bool CheckAssemblyVersion(string MinVersion, string MaxVersion)
        {
            int[] minVersion = new int[4];
            int[] maxVersion = new int[4];

            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            minVersion[0] = maxVersion[0] = currentVersion.Major;
            minVersion[1] = maxVersion[1] = currentVersion.Minor;
            minVersion[2] = maxVersion[2] = currentVersion.Build;
            minVersion[3] = maxVersion[3] = currentVersion.Revision;

            StringHelper.ParseVersion(ref minVersion, MinVersion);
            StringHelper.ParseVersion(ref maxVersion, MaxVersion);

            /**
			 * Min version compatibility checking
			 */
            if (currentVersion.Major < minVersion[0])
                return false;

            if (currentVersion.Minor < minVersion[1])
                return false;

            if (currentVersion.Build < minVersion[2])
                return false;

            if (currentVersion.Revision < minVersion[3])
                return false;

            /**
			 * Max version compatibility checking
			 */
            if (currentVersion.Major > maxVersion[0])
                return false;

            if (currentVersion.Minor > maxVersion[1])
                return false;

            if (currentVersion.Build > maxVersion[2])
                return false;

            if (currentVersion.Revision > maxVersion[3])
                return false;

            return true;
        }
    }
}
