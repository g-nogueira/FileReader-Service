using IOTLinkAPI.Platform.Windows.Native.Internal;
using System;
using System.Runtime.InteropServices;

namespace IOTLinkAPI.Platform.Windows.Native
{
#pragma warning disable 1591
    public class AdvApi32
    {
        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx", CharSet = CharSet.Auto)]
        public static extern bool DuplicateTokenEx(
            IntPtr ExistingTokenHandle,
            uint dwDesiredAccess,
            IntPtr lpThreadAttributes,
            int TokenType,
            int ImpersonationLevel,
            ref IntPtr DuplicateTokenHandle);

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation
            );
    }
}
