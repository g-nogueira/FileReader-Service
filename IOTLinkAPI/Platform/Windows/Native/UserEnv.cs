using System;
using System.Runtime.InteropServices;

namespace IOTLinkAPI.Platform.Windows.Native
{
#pragma warning disable 1591
    public class UserEnv
    {
        [DllImport("userenv.dll", CharSet = CharSet.Auto)]
        public static extern bool CreateEnvironmentBlock(ref IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("userenv.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);
    }
}
