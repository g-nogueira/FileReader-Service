using System;
using System.Runtime.InteropServices;

namespace IOTLinkAPI.Platform.Windows.Native.Internal
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WLanInterfaceInfo
    {
        public Guid InterfaceGuid;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strInterfaceDescription;

        public WLanInterfaceState isState;
    }
}
