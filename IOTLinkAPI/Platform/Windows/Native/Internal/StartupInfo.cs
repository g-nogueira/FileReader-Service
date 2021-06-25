using System;
using System.Runtime.InteropServices;

namespace IOTLinkAPI.Platform.Windows.Native.Internal
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct StartupInfo
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(StartupInfo));

        public Int32 cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public Int32 dwX;
        public Int32 dwY;
        public Int32 dwXSize;
        public Int32 dwYSize;
        public Int32 dwXCountChars;
        public Int32 dwYCountChars;
        public Int32 dwFillAttribute;
        public Int32 dwFlags;
        public Int16 wShowWindow;
        public Int16 cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }
}
