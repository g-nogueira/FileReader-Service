using IOTLinkAPI.Helpers;
using IOTLinkAPI.Platform.Windows.Native;
using IOTLinkAPI.Platform.Windows.Native.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace IOTLinkAPI.Platform.Windows
{
#pragma warning disable 1591
    public static class WindowsAPI
    {
        private static readonly IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;
        private static readonly int WTS_CURRENT_SESSION = -1;

        private static readonly uint WM_SYSCOMMAND = 0x0112;
        private static readonly uint SC_MONITORPOWER = 0xF170;

        private static readonly uint MOUSEEVENTF_MOVE = 0x0001;
        private static readonly uint MOUSEEVENTF_LEFTDOWN = 0x0001;
        private static readonly uint MOUSEEVENTF_LEFTUP = 0x0001;
        private static readonly uint MOUSEEVENTF_RIGHTDOWN = 0x0001;
        private static readonly uint MOUSEEVENTF_RIGHTUP = 0x0001;

        private static readonly uint KEYEVENTF_KEYDOWN = 0x0001;
        private static readonly uint KEYEVENTF_KEYUP = 0x0002;

        private static readonly uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        private static readonly uint CREATE_NO_WINDOW = 0x08000000;
        private static readonly uint CREATE_NEW_CONSOLE = 0x00000010;

        private static readonly uint INVALID_SESSION_ID = 0xFFFFFFFF;

        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);
        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

        public enum DialogStyle
        {
            // Buttons
            MB_OK = 0,
            MB_OKCANCEL = 1,
            MB_ABORTRETRYIGNORE = 2,
            MB_YESNOCANCEL = 3,
            MB_YESNO = 4,
            MB_RETRYCANCEL = 5,
            MB_CANCELTRYCONTINUE = 6,
            MB_HELP = 16384,
            // Icons
            MB_ICONERROR = 16,
            MB_ICONSTOP = 16,
            MB_ICONHAND = 16,
            MB_ICONQUESTION = 32,
            MB_ICONEXCLAMATION = 48,
            MB_ICONWARNING = 48,
            MB_ICONASTERISK = 64,
            MB_ICONINFORMATION = 64,
            MB_ICONMASK = 240
        }

        public static string GetUsername(int sessionId)
        {
            IntPtr server = WTS_CURRENT_SERVER_HANDLE;
            IntPtr buffer = IntPtr.Zero;
            string username = string.Empty;
            try
            {
                WtsApi32.WTSQuerySessionInformation(server, sessionId, WtsApi32.WtsInfoClass.WTSUserName, out buffer, out uint count);
                username = Marshal.PtrToStringAnsi(buffer).ToLowerInvariant().Trim();
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("WindowsAPI::GetUsername() - Error while trying to get username from sessionId {0}: {1}", sessionId, ex.Message);
                return "Unknown";
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    WtsApi32.WTSFreeMemory(buffer);

                if (server != IntPtr.Zero)
                    WtsApi32.WTSCloseServer(server);
            }
            return username;
        }

        public static string GetCurrentUsername()
        {
            IntPtr server = WTS_CURRENT_SERVER_HANDLE;
            try
            {
                WindowsSessionInfo sessionInfo = GetFirstActiveSession(server);
                if (sessionInfo == null)
                    return "SYSTEM";

                List<Process> processes = Process.GetProcesses().Where(p => p.ProcessName == "explorer" && p.SessionId == sessionInfo.SessionID).ToList();
                if (processes.Count == 0)
                    return "SYSTEM";

                return sessionInfo.Username;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("WindowsAPI::GetCurrentUsername() - Error while trying to get username: {0}", ex.Message);
                return "Unknown";
            }
            finally
            {
                if (server != IntPtr.Zero)
                    WtsApi32.WTSCloseServer(server);
            }
        }

        public static uint GetCurrentSessionId()
        {
            IntPtr server = WTS_CURRENT_SERVER_HANDLE;
            try
            {
                WindowsSessionInfo sessionInfo = GetFirstActiveSession(server);
                if (sessionInfo == null)
                    return INVALID_SESSION_ID;

                return (uint)sessionInfo.SessionID;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("WindowsAPI::GetCurrentSessionId() - Error while trying to get SessionId: {0}", ex.Message);
                return INVALID_SESSION_ID;
            }
            finally
            {
                if (server != IntPtr.Zero)
                    WtsApi32.WTSCloseServer(server);
            }
        }

        public static string GetDomainName(int sessionId)
        {
            IntPtr server = WTS_CURRENT_SERVER_HANDLE;
            IntPtr buffer = IntPtr.Zero;
            string domain = string.Empty;
            try
            {

                WtsApi32.WTSQuerySessionInformation(server, sessionId, WtsApi32.WtsInfoClass.WTSDomainName, out buffer, out uint count);
                domain = Marshal.PtrToStringAnsi(buffer).ToUpper().Trim();
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    WtsApi32.WTSFreeMemory(buffer);

                if (server != IntPtr.Zero)
                    WtsApi32.WTSCloseServer(server);
            }
            return domain;
        }

        public static void LockAll()
        {
            IntPtr server = WTS_CURRENT_SERVER_HANDLE;
            try
            {
                List<WindowsSessionInfo> sessionInfos = GetWindowsSessions(server);
                foreach (var sessionInfo in sessionInfos)
                {
                    WtsApi32.WTSDisconnectSession(server, sessionInfo.SessionID, true);
                }
            }
            finally
            {
                if (server != IntPtr.Zero)
                    WtsApi32.WTSCloseServer(server);
            }
        }

        public static bool LockUser(string username)
        {
            IntPtr server = WTS_CURRENT_SERVER_HANDLE;
            try
            {
                WindowsSessionInfo sessionInfo = GetWindowsSessions(server)
                    .Find(
                        p => p.Username != null &&
                        string.Compare(p.Username.Trim().ToLowerInvariant(), username.Trim().ToLowerInvariant()) == 0
                    );

                if (sessionInfo != null)
                    return WtsApi32.WTSDisconnectSession(server, sessionInfo.SessionID, true);

                return false;
            }
            finally
            {
                if (server != IntPtr.Zero)
                    WtsApi32.WTSCloseServer(server);
            }
        }

        public static void LogoffAll()
        {
            IntPtr server = WTS_CURRENT_SERVER_HANDLE;
            try
            {
                List<WindowsSessionInfo> sessionInfos = GetWindowsSessions(server);
                foreach (var sessionInfo in sessionInfos)
                {
                    WtsApi32.WTSLogoffSession(server, sessionInfo.SessionID, true);
                }
            }
            finally
            {
                if (server != IntPtr.Zero)
                    WtsApi32.WTSCloseServer(server);
            }
        }

        public static bool LogOffUser(string username)
        {
            IntPtr server = WTS_CURRENT_SERVER_HANDLE;
            try
            {
                WindowsSessionInfo sessionInfo = GetWindowsSessions(server)
                    .Find(
                        p => p.Username != null &&
                        string.Compare(p.Username.Trim().ToLowerInvariant(), username.Trim().ToLowerInvariant()) == 0
                    );

                if (sessionInfo != null)
                    return WtsApi32.WTSLogoffSession(server, sessionInfo.SessionID, true);

                return false;
            }
            finally
            {
                if (server != IntPtr.Zero)
                    WtsApi32.WTSCloseServer(server);
            }
        }

        public static bool Hibernate()
        {
            return PowrProf.SetSuspendState(true, true, true);
        }

        public static bool Suspend()
        {
            return PowrProf.SetSuspendState(false, true, true);
        }

        public static bool Run(RunInfo runInfo)
        {
            if (string.IsNullOrWhiteSpace(runInfo.Application))
            {
                LoggerHelper.Debug("WindowsAPI::Run() - Empty Application parameter. Returning.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(runInfo.CommandLine))
                runInfo.CommandLine = null;

            if (string.IsNullOrWhiteSpace(runInfo.WorkingDir))
                runInfo.WorkingDir = null;

            IntPtr server = WTS_CURRENT_SERVER_HANDLE;
            IntPtr hUserToken = IntPtr.Zero;
            IntPtr pEnv = IntPtr.Zero;

            try
            {
                WindowsSessionInfo sessionInfo = GetUserActiveSession(server, runInfo.Username);
                if (sessionInfo == null && (runInfo.Fallback || string.IsNullOrWhiteSpace(runInfo.Username)))
                {
                    LoggerHelper.Debug("WindowsAPI::Run() - User session not found, trying to get first active session.");
                    sessionInfo = GetFirstActiveSession(server);
                }

                if (sessionInfo == null)
                {
                    LoggerHelper.Warn("WindowsAPI::Run() - No User/Active session found. Returning.");
                    return false;
                }

                if (!GetSessionUserToken(server, sessionInfo, ref hUserToken))
                {
                    LoggerHelper.Error("WindowsAPI::Run() - Cannot get session user token.");
                    return false;
                }

                if (!UserEnv.CreateEnvironmentBlock(ref pEnv, hUserToken, false))
                {
                    LoggerHelper.Error("WindowsAPI::Run() - Cannot create environment block.");
                    return false;
                }

                // Launch the child process interactively using the token of the logged user. 
                ProcessInformation tProcessInfo;

                // Startup flags
                StartupInfo tStartUpInfo = new StartupInfo();
                tStartUpInfo.wShowWindow = (short)(runInfo.Visible ? SW.SW_SHOW : SW.SW_HIDE);
                tStartUpInfo.cb = StartupInfo.SizeOf;
                tStartUpInfo.lpDesktop = "winsta0\\default";

                // Creation Flags
                uint dwCreationFlags = CREATE_UNICODE_ENVIRONMENT | (runInfo.Visible ? CREATE_NEW_CONSOLE : CREATE_NO_WINDOW);

                bool childProcStarted = AdvApi32.CreateProcessAsUser(
                            hUserToken,                    // Token of the logged-on user. 
                            runInfo.Application,           // Name of the process to be started. 
                            runInfo.CommandLine,           // Any command line arguments to be passed. 
                            IntPtr.Zero,                   // Default Process' attributes. 
                            IntPtr.Zero,                   // Default Thread's attributes. 
                            false,                         // Does NOT inherit parent's handles. 
                            dwCreationFlags,               // No any specific creation flag. 
                            pEnv,                          // Default environment path. 
                            runInfo.WorkingDir,            // Default current directory. 
                            ref tStartUpInfo,              // Process Startup Info.  
                            out tProcessInfo               // Process information to be returned. 
                            );

                if (childProcStarted)
                {
                    LoggerHelper.Debug("WindowsAPI::Run() - Process seems to be started.");
                    Kernel32.CloseHandle(tProcessInfo.hThread);
                    Kernel32.CloseHandle(tProcessInfo.hProcess);
                }
                else
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    LoggerHelper.Error("WindowsAPI::Run() - CreateProcessAsUser failed. Error Code: {0}", errorCode);
                }

                return childProcStarted;
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("WindowsAPI::Run() - Exception: {0}", ex.ToString());
                return false;
            }
            finally
            {
                if (pEnv != IntPtr.Zero)
                    UserEnv.DestroyEnvironmentBlock(pEnv);

                if (hUserToken != IntPtr.Zero)
                    Kernel32.CloseHandle(hUserToken);

                if (server != IntPtr.Zero)
                    WtsApi32.WTSCloseServer(server);
            }
        }

        public static MemoryInfo GetMemoryInformation()
        {
            MemoryStatusEx memoryStatusEx = new MemoryStatusEx();
            if (Kernel32.GlobalMemoryStatusEx(memoryStatusEx))
            {
                MemoryInfo memoryInfo = new MemoryInfo
                {
                    MemoryLoad = memoryStatusEx.dwMemoryLoad,
                    AvailPhysical = memoryStatusEx.ullAvailPhys,
                    AvailVirtual = memoryStatusEx.ullAvailVirtual,
                    AvailExtendedVirtual = memoryStatusEx.ullAvailExtendedVirtual,
                    AvailPageFile = memoryStatusEx.ullAvailPageFile,
                    TotalPhysical = memoryStatusEx.ullTotalPhys,
                    TotalVirtual = memoryStatusEx.ullTotalVirtual,
                    TotalPageFile = memoryStatusEx.ullTotalPageFile,
                };

                return memoryInfo;
            }
            return null;
        }

        public static void ShowMessage(string title, string message)
        {
            uint sessionId = Kernel32.WTSGetActiveConsoleSessionId();
            if (sessionId == INVALID_SESSION_ID)
                return;

            IntPtr server = WTS_CURRENT_SERVER_HANDLE;
            try
            {
                int response = 0;
                int titleLen = title.Length * 2;
                int messageLen = message.Length * 2;
                int style = (int)(DialogStyle.MB_OK | DialogStyle.MB_ICONINFORMATION);

                WtsApi32.WTSSendMessage(server, (int)sessionId, title, titleLen, message, messageLen, style, 0, out response, false);
            }
            finally
            {
                if (server != IntPtr.Zero)
                    WtsApi32.WTSCloseServer(server);
            }
        }

        public static void TurnOffDisplays()
        {
            User32.PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)2);
        }

        public static void TurnOnDisplays()
        {
            // Simulate mouse movement of one pixel.
            User32.mouse_event(MOUSEEVENTF_MOVE, 0, 1, 0, 0);
            Thread.Sleep(100);
            User32.mouse_event(MOUSEEVENTF_MOVE, 0, -1, 0, 0);
            Thread.Sleep(100);

            // Simulate user click
            MousePoint mousePoint = GetCursorPosition();
            User32.mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, mousePoint.X, mousePoint.Y, 0, 0);
        }

        public static void PressKey(byte keyCode)
        {
            User32.keybd_event(keyCode, 0x45, KEYEVENTF_KEYDOWN, 0);
            User32.keybd_event(keyCode, 0x45, KEYEVENTF_KEYUP, 0);
        }

        public static string GetCurrentUser()
        {
            try
            {
                using (ManagementClass mc = new ManagementClass("Win32_ComputerSystem"))
                {
                    using (ManagementObjectCollection moc = mc.GetInstances())
                    {
                        foreach (ManagementObject mo in moc)
                        {
                            string value = mo.Properties["UserName"].Value.ToString();
                            return value.Substring(value.LastIndexOf('\\') + 1);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return "SYSTEM";
            }

            return "SYSTEM";
        }

        public static DateTimeOffset LastBootUpTime()
        {
            using (ManagementObject mo = new ManagementObject(@"\\.\root\cimv2:Win32_OperatingSystem=@"))
            {
                DateTime lastBootUpTime = DateTime.SpecifyKind(ManagementDateTimeConverter.ToDateTime(mo["LastBootUpTime"].ToString()), DateTimeKind.Unspecified);
                return new DateTimeOffset(lastBootUpTime, TimeZoneInfo.Local.BaseUtcOffset);
            }
        }

        public static long GetUptime()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            DateTimeOffset lastBootUpTime = PlatformHelper.LastBootUpTime();

            return (now.ToUnixTimeSeconds() - lastBootUpTime.ToUnixTimeSeconds());
        }

        public static uint GetIdleTime()
        {
            uint idleTime = 0;

            LastInputInfo lastInputInfo = new LastInputInfo();
            lastInputInfo.cbSize = LastInputInfo.SizeOf;
            lastInputInfo.dwTime = 0;

            uint envTicks = (uint)(Environment.TickCount & int.MaxValue);

            if (User32.GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime & int.MaxValue;
                idleTime = (envTicks - lastInputTick) & int.MaxValue;
            }

            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }

        public static List<DisplayInfo> GetDisplays()
        {
            List<DisplayInfo> displays = new List<DisplayInfo>();
            User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
            {
                MonitorInfoEx mi = new MonitorInfoEx();
                mi.Size = MonitorInfoEx.SizeOf;
                if (User32.GetMonitorInfo(hMonitor, ref mi))
                {
                    DisplayInfo di = new DisplayInfo();
                    di.ScreenWidth = (mi.Monitor.Right - mi.Monitor.Left);
                    di.ScreenHeight = (mi.Monitor.Bottom - mi.Monitor.Top);
                    di.Availability = mi.Flags.ToString();
                    displays.Add(di);
                }
                return true;
            }, IntPtr.Zero);

            return displays;
        }

        public static List<WindowsSessionInfo> GetWindowsSessions()
        {
            IntPtr server = WTS_CURRENT_SERVER_HANDLE;
            try
            {
                return GetWindowsSessions(server);
            }
            finally
            {
                if (server != IntPtr.Zero)
                    WtsApi32.WTSCloseServer(server);
            }
        }

        public static MousePoint GetCursorPosition()
        {
            MousePoint mousePoint;
            if (!User32.GetCursorPos(out mousePoint))
                mousePoint = new MousePoint(0, 0);

            return mousePoint;
        }

        public static List<IntPtr> GetChildWindows(IntPtr parent, int processId)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                User32.EnumWindowsProc childProc = new User32.EnumWindowsProc(EnumWindow);
                User32.EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }

            return result.Where(hWnd =>
            {
                if (hWnd == IntPtr.Zero)
                    return false;

                User32.GetWindowThreadProcessId(hWnd, out int hWndProcessId);
                if (hWndProcessId != processId)
                    return false;

                return User32.IsWindowVisible(hWnd);
            }).ToList();
        }

        public static bool IsFullScreen(IntPtr hWnd)
        {
            MonitorInfoEx mi = new MonitorInfoEx();
            mi.Size = MonitorInfoEx.SizeOf;

            IntPtr hMonitor = User32.MonitorFromWindow(hWnd, 1);
            if (!User32.GetMonitorInfoEx(hMonitor, ref mi))
                return false;

            if (!User32.GetWindowRect(hWnd, out Rect appBounds))
                return false;

            if (!User32.IsWindowVisible(hWnd))
                return false;

            int windowHeight = appBounds.Right - appBounds.Left;
            int windowWidth = appBounds.Bottom - appBounds.Top;

            int monitorHeight = mi.Monitor.Right - mi.Monitor.Left;
            int monitorWidth = mi.Monitor.Bottom - mi.Monitor.Top;

            bool result = (windowHeight == monitorHeight) && (windowWidth == monitorWidth);
            return result;
        }

        public static string GetWindowTitle(IntPtr hWnd)
        {
            int size = User32.GetWindowTextLength(hWnd);
            if (size > 0 && User32.IsWindowVisible(hWnd))
            {
                size++;
                var stringBuilder = new StringBuilder(size);
                User32.GetWindowText(hWnd, stringBuilder, size);

                if (stringBuilder.Length > 0)
                    return stringBuilder.ToString();
            }

            return null;
        }

        public static string GetWindowClassName(IntPtr hWnd)
        {
            if (!User32.IsWindowVisible(hWnd))
                return null;

            var stringBuilder = new StringBuilder(512);
            User32.GetClassName(hWnd, stringBuilder, stringBuilder.Capacity);
            if (stringBuilder.Length > 0)
                return stringBuilder.ToString();

            return null;
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            if (!(gch.Target is List<IntPtr> list))
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");

            list.Add(handle);
            return true;
        }

        private static bool GetSessionUserToken(IntPtr server, WindowsSessionInfo sessionInfo, ref IntPtr phUserToken)
        {
            var hImpersonationToken = IntPtr.Zero;
            bool bResult = false;
            if (WtsApi32.WTSQueryUserToken(sessionInfo.SessionID, out hImpersonationToken))
            {
                bResult = AdvApi32.DuplicateTokenEx(hImpersonationToken, 0, IntPtr.Zero, (int)SecurityImpersonationLevel.SecurityImpersonation, (int)TokenType.TokenPrimary, ref phUserToken);
                Kernel32.CloseHandle(hImpersonationToken);
            }

            return bResult;
        }

        private static List<WindowsSessionInfo> GetWindowsSessions(IntPtr server)
        {
            int sessionCount = 0;
            IntPtr pSessionInfo = IntPtr.Zero;
            List<WindowsSessionInfo> sessionInfos = new List<WindowsSessionInfo>();

            try
            {
                if (WtsApi32.WTSEnumerateSessions(server, 0, 1, ref pSessionInfo, ref sessionCount) != 0)
                {
                    int dataSize = Marshal.SizeOf(typeof(WtsApi32.WtsSessionInfo));
                    IntPtr current = pSessionInfo;
                    for (int i = 0; i < sessionCount; i++)
                    {
                        WtsApi32.WtsSessionInfo si = (WtsApi32.WtsSessionInfo)Marshal.PtrToStructure(current, typeof(WtsApi32.WtsSessionInfo));
                        current += dataSize;
                        WindowsSessionInfo sessionInfo = new WindowsSessionInfo();
                        sessionInfo.SessionID = si.SessionID;
                        sessionInfo.StationName = si.pWinStationName;
                        sessionInfo.IsActive = si.State == WtsApi32.WtsConnectStateClass.WTSActive;
                        sessionInfo.Username = GetUsername(sessionInfo.SessionID);

                        sessionInfos.Add(sessionInfo);
                    }
                }
            }
            finally
            {
                if (pSessionInfo != IntPtr.Zero)
                    WtsApi32.WTSFreeMemory(pSessionInfo);
            }

            return sessionInfos;
        }

        private static WindowsSessionInfo GetFirstActiveSession(IntPtr server)
        {
            int sessionCount = 0;
            IntPtr pSessionInfo = IntPtr.Zero;

            try
            {
                if (WtsApi32.WTSEnumerateSessions(server, 0, 1, ref pSessionInfo, ref sessionCount) != 0)
                {
                    int dataSize = Marshal.SizeOf(typeof(WtsApi32.WtsSessionInfo));
                    IntPtr current = pSessionInfo;

                    for (int i = 0; i < sessionCount; i++)
                    {
                        WtsApi32.WtsSessionInfo si = (WtsApi32.WtsSessionInfo)Marshal.PtrToStructure(current, typeof(WtsApi32.WtsSessionInfo));
                        current += dataSize;
                        if (si.State != WtsApi32.WtsConnectStateClass.WTSActive)
                            continue;

                        WindowsSessionInfo sessionInfo = new WindowsSessionInfo();
                        sessionInfo.SessionID = si.SessionID;
                        sessionInfo.StationName = si.pWinStationName;
                        sessionInfo.IsActive = si.State == WtsApi32.WtsConnectStateClass.WTSActive;
                        sessionInfo.Username = GetUsername(sessionInfo.SessionID);

                        return sessionInfo;
                    }
                }
            }
            finally
            {
                if (pSessionInfo != IntPtr.Zero)
                    WtsApi32.WTSFreeMemory(pSessionInfo);
            }

            return null;
        }

        private static WindowsSessionInfo GetUserActiveSession(IntPtr server, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            int sessionCount = 0;
            IntPtr pSessionInfo = IntPtr.Zero;

            try
            {
                if (WtsApi32.WTSEnumerateSessions(server, 0, 1, ref pSessionInfo, ref sessionCount) != 0)
                {
                    int dataSize = Marshal.SizeOf(typeof(WtsApi32.WtsSessionInfo));
                    IntPtr current = pSessionInfo;

                    for (int i = 0; i < sessionCount; i++)
                    {
                        WtsApi32.WtsSessionInfo si = (WtsApi32.WtsSessionInfo)Marshal.PtrToStructure(current, typeof(WtsApi32.WtsSessionInfo));
                        current += dataSize;
                        if (si.State != WtsApi32.WtsConnectStateClass.WTSActive)
                            continue;

                        string sessionUser = GetUsername(si.SessionID);
                        if (string.IsNullOrWhiteSpace(sessionUser) || string.Compare(username.Trim().ToLowerInvariant(), sessionUser.Trim().ToLowerInvariant()) != 0)
                            continue;

                        WindowsSessionInfo sessionInfo = new WindowsSessionInfo();
                        sessionInfo.SessionID = si.SessionID;
                        sessionInfo.StationName = si.pWinStationName;
                        sessionInfo.IsActive = si.State == WtsApi32.WtsConnectStateClass.WTSActive;
                        sessionInfo.Username = sessionUser;

                        return sessionInfo;
                    }
                }
            }
            finally
            {
                if (pSessionInfo != IntPtr.Zero)
                    WtsApi32.WTSFreeMemory(pSessionInfo);
            }

            return null;
        }
    }
}
