using System.Collections;
using System.Runtime.InteropServices;
using static GHelper.Display.ScreenInterrogatory;

namespace GHelper.Display
{

    class DeviceComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            uint displayX = ((DISPLAYCONFIG_TARGET_DEVICE_NAME)x).connectorInstance;
            uint displayY = ((DISPLAYCONFIG_TARGET_DEVICE_NAME)y).connectorInstance;

            if (displayX > displayY)
                return 1;
            if (displayX < displayY)
                return -1;
            else
                return 0;
        }
    }

    class ScreenComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            int displayX = Int32.Parse(s: ((Screen)x).DeviceName.Replace(oldValue: @"\\.\DISPLAY", newValue: ""));
            int displayY = Int32.Parse(s: ((Screen)y).DeviceName.Replace(oldValue: @"\\.\DISPLAY", newValue: ""));
            return (new CaseInsensitiveComparer()).Compare(a: displayX, b: displayY);
        }
    }
    internal class ScreenNative
    {
        [StructLayout(layoutKind: LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DEVMODE
        {
            [MarshalAs(unmanagedType: UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;

            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(unmanagedType: UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;

            public short dmLogPixels;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        };

        [Flags()]
        public enum DisplaySettingsFlags : int
        {
            CDS_UPDATEREGISTRY = 1,
            CDS_TEST = 2,
            CDS_FULLSCREEN = 4,
            CDS_GLOBAL = 8,
            CDS_SET_PRIMARY = 0x10,
            CDS_RESET = 0x40000000,
            CDS_NORESET = 0x10000000
        }

        // PInvoke declaration for EnumDisplaySettings Win32 API
        [DllImport(dllName: "user32.dll")]
        public static extern int EnumDisplaySettingsEx(
             string lpszDeviceName,
             int iModeNum,
             ref DEVMODE lpDevMode);

        // PInvoke declaration for ChangeDisplaySettings Win32 API
        [DllImport(dllName: "user32.dll")]
        public static extern int ChangeDisplaySettingsEx(
                string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd,
                DisplaySettingsFlags dwflags, IntPtr lParam);

        public static DEVMODE CreateDevmode()
        {
            DEVMODE dm = new DEVMODE();
            dm.dmDeviceName = new String(value: new char[32]);
            dm.dmFormName = new String(value: new char[32]);
            dm.dmSize = (short)Marshal.SizeOf(structure: dm);
            return dm;
        }

        public enum COLORPROFILETYPE
        {
            CPT_ICC,
            CPT_DMP,
            CPT_CAMP,
            CPT_GMMP
        }
        public enum COLORPROFILESUBTYPE
        {
            CPST_PERCEPTUAL,
            CPST_RELATIVE_COLORIMETRIC,
            CPST_SATURATION,
            CPST_ABSOLUTE_COLORIMETRIC,
            CPST_NONE,
            CPST_RGB_WORKING_SPACE,
            CPST_CUSTOM_WORKING_SPACE,
            CPST_STANDARD_DISPLAY_COLOR_MODE,
            CPST_EXTENDED_DISPLAY_COLOR_MODE
        }
        public enum WCS_PROFILE_MANAGEMENT_SCOPE
        {
            WCS_PROFILE_MANAGEMENT_SCOPE_SYSTEM_WIDE,
            WCS_PROFILE_MANAGEMENT_SCOPE_CURRENT_USER
        }

        [DllImport(dllName: "mscms.dll", CharSet = CharSet.Unicode)]
        public static extern bool WcsSetDefaultColorProfile(
            WCS_PROFILE_MANAGEMENT_SCOPE scope,
            string pDeviceName,
            COLORPROFILETYPE cptColorProfileType,
            COLORPROFILESUBTYPE cpstColorProfileSubType,
            uint dwProfileID,
            string pProfileName
        );


        public const int ENUM_CURRENT_SETTINGS = -1;
        public const string defaultDevice = @"\\.\DISPLAY1";


        private static string? FindInternalName(bool log = false)
        {
            try
            {
                var devices = GetAllDevices().ToArray();
                string internalName = AppConfig.GetString(name: "internal_display");

                foreach (var device in devices)
                {
                    if (device.outputTechnology == DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL ||
                        device.outputTechnology == DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED ||
                        device.monitorFriendlyDeviceName == internalName)
                    {
                        if (log) Logger.WriteLine(logMessage: device.monitorDevicePath + " " + device.outputTechnology);

                        AppConfig.Set(name: "internal_display", value: device.monitorFriendlyDeviceName);
                        var names = device.monitorDevicePath.Split(separator: "#");
                        
                        if (names.Length > 1) return names[1];
                        else return "";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: ex.ToString());
            }

            return null;
        }

        static string ExtractDisplay(string input)
        {
            int index = input.IndexOf(value: '\\', startIndex: 4); // Start searching from index 4 to skip ""

            if (index != -1)
            {
                string extracted = input.Substring(startIndex: 0, length: index);
                return extracted;
            }

            return input;
        }

        public static string? FindLaptopScreen(bool log = false)
        {
            string? laptopScreen = null;
            string? internalName = FindInternalName(log: log);

            if (internalName == null)
            {
                Logger.WriteLine(logMessage: "Internal screen off");
                return null;
            }

            try
            {
                var displays = GetDisplayDevices().ToArray();
                foreach (var display in displays)
                {
                    if (log) Logger.WriteLine(logMessage: display.DeviceID + " " + display.DeviceName);
                    if (display.DeviceID.Contains(value: internalName))
                    {
                        laptopScreen = ExtractDisplay(input: display.DeviceName);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: ex.ToString());
            }

            if (laptopScreen is null)
            {
                Logger.WriteLine(logMessage: "Default internal screen");
                laptopScreen = Screen.PrimaryScreen.DeviceName;
            }

            return laptopScreen;
        }


        public static int GetMaxRefreshRate(string? laptopScreen)
        {

            if (laptopScreen is null) return -1;

            DEVMODE dm = CreateDevmode();
            int frequency = -1;

            int i = 0;
            while (0 != EnumDisplaySettingsEx(lpszDeviceName: laptopScreen, iModeNum: i, lpDevMode: ref dm))
            {
                if (dm.dmDisplayFrequency > frequency)
                {
	                frequency = dm.dmDisplayFrequency;
                }

                i++;
            }

            if (frequency > 0) AppConfig.Set(name: "screen_max", value: frequency);
            else
            {
	            frequency = AppConfig.Get(name: "screen_max");
            }

            return frequency;

        }

        public static int GetRefreshRate(string? laptopScreen)
        {

            if (laptopScreen is null) return -1;

            DEVMODE dm = CreateDevmode();
            int frequency = -1;

            if (0 != EnumDisplaySettingsEx(lpszDeviceName: laptopScreen, iModeNum: ENUM_CURRENT_SETTINGS, lpDevMode: ref dm))
            {
                frequency = dm.dmDisplayFrequency;
            }

            return frequency;
        }

        public static int SetRefreshRate(string laptopScreen, int frequency = 120)
        {
            DEVMODE dm = CreateDevmode();

            if (0 != EnumDisplaySettingsEx(lpszDeviceName: laptopScreen, iModeNum: ENUM_CURRENT_SETTINGS, lpDevMode: ref dm))
            {
                dm.dmDisplayFrequency = frequency;
                int iRet = ChangeDisplaySettingsEx(lpszDeviceName: laptopScreen, lpDevMode: ref dm, hwnd: IntPtr.Zero, dwflags: DisplaySettingsFlags.CDS_UPDATEREGISTRY, lParam: IntPtr.Zero);
                Logger.WriteLine(logMessage: "Screen = " + frequency.ToString() + "Hz : " + (iRet == 0 ? "OK" : iRet));
                return iRet;
            }

            return 0;

        }
    }
}
