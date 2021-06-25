using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using IOTLinkAPI.Helpers;
using IOTLinkService.Service.Engine;
using System.ServiceProcess;
using System.Threading;

namespace IOTLink
{
    public enum PowerEventType
    {
        QuerySuspend,
        QueryStandBy,
        QuerySuspendFailed,
        QueryStandByFailed,
        Suspend,
        StandBy,
        ResumeCritical,
        ResumeSuspend,
        ResumeStandBy,
        ResumeAutomatic
    }

    public partial class IOTLinkService : ServiceBase
    {
        private bool _ignoreOnSessionChangeEvent = false;
        private Thread _powerEventThread;
        private uint _powerEventThreadId;

        #region WndProc message constants

        private const int WM_QUIT = 0x0012;
        private const int WM_POWERBROADCAST = 0x0218;
        private const int PBT_APMQUERYSUSPEND = 0x0000;
        private const int PBT_APMQUERYSTANDBY = 0x0001;
        private const int PBT_APMQUERYSUSPENDFAILED = 0x0002;
        private const int PBT_APMQUERYSTANDBYFAILED = 0x0003;
        private const int PBT_APMSUSPEND = 0x0004;
        private const int PBT_APMSTANDBY = 0x0005;
        private const int PBT_APMRESUMECRITICAL = 0x0006;
        private const int PBT_APMRESUMESUSPEND = 0x0007;
        private const int PBT_APMRESUMESTANDBY = 0x0008;
        private const int PBT_APMRESUMEAUTOMATIC = 0x0012;
        private const int BROADCAST_QUERY_DENY = 0x424D5144;

        #endregion

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct WNDCLASS
        {
            public uint style;
            public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
        }

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public int message;
            public IntPtr wParam;
            public IntPtr lParam;
            public int time;
            public int pt_x;
            public int pt_y;
        }

        [DllImport("user32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern IntPtr DispatchMessageA([In] ref MSG msg);

        [DllImport("user32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern bool GetMessageA([In, Out] ref MSG msg, IntPtr hWnd, int uMsgFilterMin, int uMsgFilterMax);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern bool TranslateMessage([In, Out] ref MSG msg);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
            int x, int y,
            int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
            IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32", CharSet = CharSet.Auto)]
        private static extern int RegisterClass(ref WNDCLASS wndclass);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);


        private IntPtr PowerEventThreadWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_POWERBROADCAST)
            {
                int wParamInt = wParam.ToInt32();
                LoggerHelper.Debug("PowerEventThread received WM_POWERBROADCAST {0}", Convert.ToString(wParamInt));
                switch (wParamInt)
                {
                    case PBT_APMQUERYSUSPENDFAILED:
                        OnPowerEvent(PowerEventType.QuerySuspendFailed);
                        break;
                    case PBT_APMQUERYSTANDBYFAILED:
                        OnPowerEvent(PowerEventType.QueryStandByFailed);
                        break;
                    case PBT_APMQUERYSUSPEND:
                        if (!OnPowerEvent(PowerEventType.QuerySuspend))
                            return new IntPtr(BROADCAST_QUERY_DENY);
                        break;
                    case PBT_APMQUERYSTANDBY:
                        if (!OnPowerEvent(PowerEventType.QueryStandBy))
                            return new IntPtr(BROADCAST_QUERY_DENY);
                        break;
                    case PBT_APMSUSPEND:
                        OnPowerEvent(PowerEventType.Suspend);
                        break;
                    case PBT_APMSTANDBY:
                        OnPowerEvent(PowerEventType.StandBy);
                        break;
                    case PBT_APMRESUMECRITICAL:
                        OnPowerEvent(PowerEventType.ResumeCritical);
                        break;
                    case PBT_APMRESUMESUSPEND:
                        OnPowerEvent(PowerEventType.ResumeSuspend);
                        break;
                    case PBT_APMRESUMESTANDBY:
                        OnPowerEvent(PowerEventType.ResumeStandBy);
                        break;
                    case PBT_APMRESUMEAUTOMATIC:
                        OnPowerEvent(PowerEventType.ResumeAutomatic);
                        break;
                }
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// Handles the power event.
        /// </summary>
        /// <param name="powerStatus"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected bool OnPowerEvent(PowerEventType powerStatus)
        {
            LoggerHelper.Debug("OnPowerEvent: PowerStatus: {0}", powerStatus);

            
            if (powerStatus == PowerEventType.QuerySuspend ||
                powerStatus == PowerEventType.QueryStandBy ||
                powerStatus == PowerEventType.StandBy ||
                powerStatus == PowerEventType.Suspend)
            {
                _ignoreOnSessionChangeEvent = true;
                ServiceMain.GetInstance().SuspendApplication();
            }
            else if (powerStatus == PowerEventType.ResumeAutomatic ||
                     powerStatus == PowerEventType.ResumeCritical ||
                     powerStatus == PowerEventType.ResumeStandBy ||
                     powerStatus == PowerEventType.ResumeSuspend)
            {
                ServiceMain.GetInstance().ResumeApplication();
                _ignoreOnSessionChangeEvent = false;
            }

            return true;
        }


        private void PowerEventThread()
        {
            Thread.BeginThreadAffinity();
            try
            {
                _powerEventThreadId = GetCurrentThreadId();

                WNDCLASS wndclass;
                wndclass.style = 0;
                wndclass.lpfnWndProc = PowerEventThreadWndProc;
                wndclass.cbClsExtra = 0;
                wndclass.cbWndExtra = 0;
                wndclass.hInstance = Process.GetCurrentProcess().Handle;
                wndclass.hIcon = IntPtr.Zero;
                wndclass.hCursor = IntPtr.Zero;
                wndclass.hbrBackground = IntPtr.Zero;
                wndclass.lpszMenuName = null;
                wndclass.lpszClassName = "IOTLinkPowerEventThreadWndClass";

                RegisterClass(ref wndclass);

                IntPtr handle = CreateWindowEx(0x80, wndclass.lpszClassName, "", 0x80000000, 0, 0, 0, 0, IntPtr.Zero,
                    IntPtr.Zero, wndclass.hInstance, IntPtr.Zero);

                if (handle.Equals(IntPtr.Zero))
                {
                    LoggerHelper.Error("PowerEventThread cannot create window handle, exiting thread");
                    return;
                }

                // this thread needs an message loop
                LoggerHelper.Debug("PowerEventThread message loop is running");
                while (true)
                {
                    try
                    {
                        MSG msgApi = new MSG();

                        if (!GetMessageA(ref msgApi, IntPtr.Zero, 0, 0)) // returns false on WM_QUIT
                        {
                            return;
                        }

                        TranslateMessage(ref msgApi);
                        LoggerHelper.Debug("PowerEventThread {0}", msgApi.message);
                        DispatchMessageA(ref msgApi);
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.Error("PowerEventThread: Exception: {0}", ex.ToString());
                    }
                }
            }
            finally
            {
                Thread.EndThreadAffinity();
                LoggerHelper.Debug("PowerEventThread finished");
            }
        }



        public IOTLinkService()
        {
            InitializeComponent();
            CanHandleSessionChangeEvent = true;
        }

        public void OnDebug()
        {
#if DEBUG
            LoggerHelper.Info("DEBUG FLAG IS ACTIVATED.");
            OnStart(null);
#endif
        }

        protected override void OnStart(string[] args)
        {
            LoggerHelper.Info("Windows Service is started.");

            _powerEventThread = new Thread(PowerEventThread);
            _powerEventThread.Name = "PowerEventThread";
            _powerEventThread.IsBackground = true;
            _powerEventThread.Start();

            
            ServiceMain.GetInstance().StartApplication();
        }

        protected override void OnStop()
        {
            LoggerHelper.Info("Windows Service is stopped.");
            LoggerHelper.EmptyLine();

            if (_powerEventThreadId != 0)
            {
                PostThreadMessage(_powerEventThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
                _powerEventThread.Join();
            }
            _powerEventThreadId = 0;
            _powerEventThread = null;

            ServiceMain.GetInstance().StopApplication();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            if (!_ignoreOnSessionChangeEvent)
            {
                string username = PlatformHelper.GetUsername(changeDescription.SessionId);
                ServiceMain.GetInstance().OnSessionChange(username, changeDescription.SessionId, changeDescription.Reason);
            }
            else
            {
                LoggerHelper.Info("ignoring OnSessionChange event, as we are currently handling resume/standby activities.");
            }
            
        }


    }
}
