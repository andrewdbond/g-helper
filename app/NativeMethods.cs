using System.Runtime.InteropServices;


public class NativeMethods
{

    internal struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport(dllName: "User32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    public static TimeSpan GetIdleTime()
    {
        LASTINPUTINFO lastInPut = new LASTINPUTINFO();
        lastInPut.cbSize = (uint)Marshal.SizeOf(structure: lastInPut);
        GetLastInputInfo(plii: ref lastInPut);
        return TimeSpan.FromMilliseconds(value: (uint)Environment.TickCount - lastInPut.dwTime);

    }

    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MONITORPOWER = 0xF170;
    private const int MONITOR_OFF = 2;

    [DllImport(dllName: "user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport(dllName: "kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, out string lpBuffer, uint nSize, IntPtr Arguments);

    public static void TurnOffScreen()
    {
        IntPtr result = SendMessage(hWnd: -1, Msg: WM_SYSCOMMAND, wParam: (IntPtr)SC_MONITORPOWER, lParam: (IntPtr)MONITOR_OFF);
        if (result == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            string message = "";
            uint formatFlags = 0x00001000 | 0x00000200 | 0x00000100 | 0x00000080;
            uint formatResult = FormatMessage(dwFlags: formatFlags, lpSource: IntPtr.Zero, dwMessageId: (uint)error, dwLanguageId: 0, lpBuffer: out message, nSize: 0, Arguments: IntPtr.Zero);
            if (formatResult == 0)
            {
                message = "Unknown error.";
            }
            Logger.WriteLine(logMessage: $"Failed to turn off screen. Error code: {error}. {message}");
        }
    }

    // Monitor Power detection

    internal const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x0;
    internal const uint DEVICE_NOTIFY_SERVICE_HANDLE = 0x1;
    internal const int WM_POWERBROADCAST = 0x0218;
    internal const int PBT_POWERSETTINGCHANGE = 0x8013;

    [DllImport(dllName: "User32.dll", SetLastError = true)]
    internal static extern IntPtr RegisterPowerSettingNotification(IntPtr hWnd, [In] Guid PowerSettingGuid, uint Flags);

    [DllImport(dllName: "User32.dll", SetLastError = true)]
    internal static extern bool UnregisterPowerSettingNotification(IntPtr hWnd);

    [StructLayout(layoutKind: LayoutKind.Sequential, Pack = 4)]
    internal struct POWERBROADCAST_SETTING
    {
        public Guid PowerSetting;
        public uint DataLength;
        public byte Data;
    }

    public class PowerSettingGuid
    {
        // 0=Powered by AC, 1=Powered by Battery, 2=Powered by short-term source (UPC)
        public Guid AcdcPowerSource { get; } = new Guid(g: "5d3e9a59-e9D5-4b00-a6bd-ff34ff516548");
        // POWERBROADCAST_SETTING.Data = 1-100
        public Guid BatteryPercentageRemaining { get; } = new Guid(g: "a7ad8041-b45a-4cae-87a3-eecbb468a9e1");
        // Windows 8+: 0=Monitor Off, 1=Monitor On, 2=Monitor Dimmed
        public Guid ConsoleDisplayState { get; } = new Guid(g: "6fe69556-704a-47a0-8f24-c28d936fda47");
        // Windows 8+, Session 0 enabled: 0=User providing Input, 2=User Idle
        public Guid GlobalUserPresence { get; } = new Guid(g: "786E8A1D-B427-4344-9207-09E70BDCBEA9");
        // 0=Monitor Off, 1=Monitor On.
        public Guid MonitorPowerGuid { get; } = new Guid(g: "02731015-4510-4526-99e6-e5a17ebd1aea");
        // 0=Battery Saver Off, 1=Battery Saver On.
        public Guid PowerSavingStatus { get; } = new Guid(g: "E00958C0-C213-4ACE-AC77-FECCED2EEEA5");

        // Windows 8+: 0=Off, 1=On, 2=Dimmed
        public Guid SessionDisplayStatus { get; } = new Guid(g: "2B84C20E-AD23-4ddf-93DB-05FFBD7EFCA5");

        // Windows 8+, no Session 0: 0=User providing Input, 2=User Idle
        public Guid SessionUserPresence { get; } = new Guid(g: "3C0F4548-C03F-4c4d-B9F2-237EDE686376");
        // 0=Exiting away mode 1=Entering away mode
        public Guid SystemAwaymode { get; } = new Guid(g: "98a7f580-01f7-48aa-9c0f-44352c29e5C0");

        /* Windows 8+ */
        // POWERBROADCAST_SETTING.Data not used
        public Guid IdleBackgroundTask { get; } = new Guid(a: 0x515C31D8, b: 0xF734, c: 0x163D, d: 0xA0, e: 0xFD, f: 0x11, g: 0xA0, h: 0x8C, i: 0x91, j: 0xE8, k: 0xF1);

        public Guid PowerSchemePersonality { get; } = new Guid(a: 0x245D8541, b: 0x3943, c: 0x4422, d: 0xB0, e: 0x25, f: 0x13, g: 0xA7, h: 0x84, i: 0xF6, j: 0x79, k: 0xB7);

        // The Following 3 Guids are the POWERBROADCAST_SETTING.Data result of PowerSchemePersonality
        public Guid MinPowerSavings { get; } = new Guid(g: "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        public Guid MaxPowerSavings { get; } = new Guid(g: "a1841308-3541-4fab-bc81-f71556f20b4a");
        public Guid TypicalPowerSavings { get; } = new Guid(g: "381b4222-f694-41f0-9685-ff5bb260df2e");
    }



}
