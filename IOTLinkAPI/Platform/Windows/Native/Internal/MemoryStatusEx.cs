using System.Runtime.InteropServices;

namespace IOTLinkAPI.Platform.Windows.Native.Internal
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MemoryStatusEx
    {
        public static readonly uint SizeOf = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));

        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MemoryStatusEx()
        {
            dwLength = SizeOf;
        }
    }
}
