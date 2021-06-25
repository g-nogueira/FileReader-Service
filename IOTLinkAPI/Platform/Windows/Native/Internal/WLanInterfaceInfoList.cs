using System;
using System.Runtime.InteropServices;

namespace IOTLinkAPI.Platform.Windows.Native.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WLanInterfaceInfoList
    {
        public int dwNumberofItems;
        public int dwIndex;
        public WLanInterfaceInfo[] InterfaceInfo;

        public WLanInterfaceInfoList(IntPtr pList)
        {
            dwNumberofItems = Marshal.ReadInt32(pList, 0);

            dwIndex = Marshal.ReadInt32(pList, 4);

            InterfaceInfo = new WLanInterfaceInfo[dwNumberofItems];

            for (int i = 0; i < dwNumberofItems; i++)
            {
                IntPtr pItemList = new IntPtr(pList.ToInt32() + (i * 532) + 8);
                WLanInterfaceInfo wii = new WLanInterfaceInfo();
                wii = (WLanInterfaceInfo)Marshal.PtrToStructure(pItemList, typeof(WLanInterfaceInfo));
                InterfaceInfo[i] = wii;
            }
        }
    }
}
