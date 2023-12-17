using System.Runtime.InteropServices;

namespace GHelper.Mode
{
    internal class PowerNative
    {
        [DllImport(dllName: "PowrProf.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 PowerWriteDCValueIndex(IntPtr RootPowerKey,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid SchemeGuid,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid PowerSettingGuid,
            int AcValueIndex);

        [DllImport(dllName: "PowrProf.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 PowerWriteACValueIndex(IntPtr RootPowerKey,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid SchemeGuid,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid PowerSettingGuid,
            int AcValueIndex);

        [DllImport(dllName: "PowrProf.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 PowerReadACValueIndex(IntPtr RootPowerKey,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid SchemeGuid,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid PowerSettingGuid,
            out IntPtr AcValueIndex
            );

        [DllImport(dllName: "PowrProf.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 PowerReadDCValueIndex(IntPtr RootPowerKey,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid SchemeGuid,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid PowerSettingGuid,
            out IntPtr AcValueIndex
            );


        [DllImport(dllName: "powrprof.dll")]
        static extern uint PowerReadACValue(
            IntPtr RootPowerKey,
            Guid SchemeGuid,
            Guid SubGroupOfPowerSettingGuid,
            Guid PowerSettingGuid,
            ref int Type,
            ref IntPtr Buffer,
            ref uint BufferSize
            );


        [DllImport(dllName: "PowrProf.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 PowerSetActiveScheme(IntPtr RootPowerKey,
            [MarshalAs(unmanagedType: UnmanagedType.LPStruct)] Guid SchemeGuid);

        [DllImport(dllName: "PowrProf.dll", CharSet = CharSet.Unicode)]
        static extern UInt32 PowerGetActiveScheme(IntPtr UserPowerKey, out IntPtr ActivePolicyGuid);

        static readonly Guid GUID_CPU = new Guid(g: "54533251-82be-4824-96c1-47b60b740d00");
        static readonly Guid GUID_BOOST = new Guid(g: "be337238-0d82-4146-a960-4f3749d470c7");

        private static Guid GUID_SLEEP_SUBGROUP = new Guid(g: "238c9fa8-0aad-41ed-83f4-97be242c8f20");
        private static Guid GUID_HIBERNATEIDLE = new Guid(g: "9d7815a6-7ee4-497e-8888-515a05f02364");

        private static Guid GUID_SYSTEM_BUTTON_SUBGROUP = new Guid(g: "4f971e89-eebd-4455-a8de-9e59040e7347");
        private static Guid GUID_LIDACTION = new Guid(g: "5CA83367-6E45-459F-A27B-476B1D01C936");

        [DllImportAttribute(dllName: "powrprof.dll", EntryPoint = "PowerGetActualOverlayScheme")]
        public static extern uint PowerGetActualOverlayScheme(out Guid ActualOverlayGuid);

        [DllImportAttribute(dllName: "powrprof.dll", EntryPoint = "PowerGetEffectiveOverlayScheme")]
        public static extern uint PowerGetEffectiveOverlayScheme(out Guid EffectiveOverlayGuid);

        [DllImportAttribute(dllName: "powrprof.dll", EntryPoint = "PowerSetActiveOverlayScheme")]
        public static extern uint PowerSetActiveOverlayScheme(Guid OverlaySchemeGuid);

        const string POWER_SILENT = "961cc777-2547-4f9d-8174-7d86181b8a7a";
        const string POWER_BALANCED = "00000000-0000-0000-0000-000000000000";
        const string POWER_TURBO = "ded574b5-45a0-4f42-8737-46345c09c238";
        const string POWER_BETTERPERFORMANCE = "ded574b5-45a0-4f42-8737-46345c09c238";

        static List<string> overlays = new() {
                POWER_BALANCED,
                POWER_TURBO,
                POWER_SILENT,
                POWER_BETTERPERFORMANCE
            };

        public static Dictionary<string, string> powerModes = new Dictionary<string, string>
            {
                { POWER_SILENT, "Best Power Efficiency" },
                { POWER_BALANCED, "Balanced" },
                { POWER_TURBO, "Best Performance" },
            };
        static Guid GetActiveScheme()
        {
            IntPtr pActiveSchemeGuid;
            var hr = PowerGetActiveScheme(UserPowerKey: IntPtr.Zero, ActivePolicyGuid: out pActiveSchemeGuid);
            Guid activeSchemeGuid = (Guid)Marshal.PtrToStructure(ptr: pActiveSchemeGuid, structureType: typeof(Guid));
            return activeSchemeGuid;
        }

        public static int GetCPUBoost()
        {
            IntPtr AcValueIndex;
            Guid activeSchemeGuid = GetActiveScheme();

            UInt32 value = PowerReadACValueIndex(RootPowerKey: IntPtr.Zero,
                 SchemeGuid: activeSchemeGuid,
                 SubGroupOfPowerSettingsGuid: GUID_CPU,
                 PowerSettingGuid: GUID_BOOST, AcValueIndex: out AcValueIndex);

            return AcValueIndex.ToInt32();

        }

        public static void SetCPUBoost(int boost = 0)
        {
            Guid activeSchemeGuid = GetActiveScheme();

            if (boost == GetCPUBoost()) return;

            var hrAC = PowerWriteACValueIndex(
                 RootPowerKey: IntPtr.Zero,
                 SchemeGuid: activeSchemeGuid,
                 SubGroupOfPowerSettingsGuid: GUID_CPU,
                 PowerSettingGuid: GUID_BOOST,
                 AcValueIndex: boost);

            PowerSetActiveScheme(RootPowerKey: IntPtr.Zero, SchemeGuid: activeSchemeGuid);

            var hrDC = PowerWriteDCValueIndex(
                 RootPowerKey: IntPtr.Zero,
                 SchemeGuid: activeSchemeGuid,
                 SubGroupOfPowerSettingsGuid: GUID_CPU,
                 PowerSettingGuid: GUID_BOOST,
                 AcValueIndex: boost);

            PowerSetActiveScheme(RootPowerKey: IntPtr.Zero, SchemeGuid: activeSchemeGuid);

            Logger.WriteLine(logMessage: "Boost " + boost);
        }

        public static string GetPowerMode()
        {
            PowerGetEffectiveOverlayScheme(EffectiveOverlayGuid: out Guid activeScheme);
            return activeScheme.ToString();
        }

        public static void SetPowerMode(string scheme)
        {

            if (!overlays.Contains(item: scheme)) return;

            Guid guidScheme = new Guid(g: scheme);

            uint status = PowerGetEffectiveOverlayScheme(EffectiveOverlayGuid: out Guid activeScheme);

            if (GetBatterySaverStatus())
            {
                Logger.WriteLine(logMessage: "Battery Saver detected");
                return;
            }

            if (status != 0 || activeScheme != guidScheme)
            {
                status = PowerSetActiveOverlayScheme(OverlaySchemeGuid: guidScheme);
                Logger.WriteLine(logMessage: "Power Mode " + scheme + ":" + (status == 0 ? "OK" : status));
            }

        }

        public static void SetBalancedPowerPlan()
        {
            Guid activeSchemeGuid = GetActiveScheme();
            string balanced = "381b4222-f694-41f0-9685-ff5bb260df2e";

            if (activeSchemeGuid.ToString() != balanced && !AppConfig.Is(name: "skip_power_plan"))
            {
                SetPowerPlan(scheme: balanced);
            }
        }

        public static void SetPowerPlan(string scheme)
        {
            // Skipping power modes
            if (overlays.Contains(item: scheme)) return;

            Guid guidScheme = new Guid(g: scheme);
            uint status = PowerSetActiveScheme(RootPowerKey: IntPtr.Zero, SchemeGuid: guidScheme);
            Logger.WriteLine(logMessage: "Power Plan " + scheme + ":" + (status == 0 ? "OK" : status));
        }

        public static string GetDefaultPowerMode(int mode)
        {
            switch (mode)
            {
                case 1: // turbo
                    return POWER_TURBO;
                case 2: //silent
                    return POWER_SILENT;
                default: // balanced
                    return POWER_BALANCED;
            }
        }

        public static void SetPowerMode(int mode)
        {
            SetPowerMode(scheme: GetDefaultPowerMode(mode: mode));
        }

        public static int GetLidAction(bool ac)
        {
            Guid activeSchemeGuid = GetActiveScheme();

            IntPtr activeIndex;
            if (ac)
                PowerReadACValueIndex(RootPowerKey: IntPtr.Zero,
                     SchemeGuid: activeSchemeGuid,
                     SubGroupOfPowerSettingsGuid: GUID_SYSTEM_BUTTON_SUBGROUP,
                     PowerSettingGuid: GUID_LIDACTION, AcValueIndex: out activeIndex);

            else
                PowerReadDCValueIndex(RootPowerKey: IntPtr.Zero,
                    SchemeGuid: activeSchemeGuid,
                    SubGroupOfPowerSettingsGuid: GUID_SYSTEM_BUTTON_SUBGROUP,
                    PowerSettingGuid: GUID_LIDACTION, AcValueIndex: out activeIndex);


            return activeIndex.ToInt32();
        }


        public static void SetLidAction(int action, bool acOnly = false)
        {
            /**
             * 1: Do nothing
             * 2: Seelp
             * 3: Hibernate
             * 4: Shutdown
             */

            Guid activeSchemeGuid = GetActiveScheme();

            var hrAC = PowerWriteACValueIndex(
                RootPowerKey: IntPtr.Zero,
                SchemeGuid: activeSchemeGuid,
                SubGroupOfPowerSettingsGuid: GUID_SYSTEM_BUTTON_SUBGROUP,
                PowerSettingGuid: GUID_LIDACTION,
                AcValueIndex: action);

            PowerSetActiveScheme(RootPowerKey: IntPtr.Zero, SchemeGuid: activeSchemeGuid);

            if (!acOnly)
            {
                var hrDC = PowerWriteDCValueIndex(
                  RootPowerKey: IntPtr.Zero,
                  SchemeGuid: activeSchemeGuid,
                  SubGroupOfPowerSettingsGuid: GUID_SYSTEM_BUTTON_SUBGROUP,
                  PowerSettingGuid: GUID_LIDACTION,
                  AcValueIndex: action);

                PowerSetActiveScheme(RootPowerKey: IntPtr.Zero, SchemeGuid: activeSchemeGuid);
            }

            Logger.WriteLine(logMessage: "Changed Lid Action to " + action);
        }

        public static int GetHibernateAfter()
        {
            Guid activeSchemeGuid = GetActiveScheme();
            IntPtr seconds;
            PowerReadDCValueIndex(RootPowerKey: IntPtr.Zero,
                    SchemeGuid: activeSchemeGuid,
                    SubGroupOfPowerSettingsGuid: GUID_SLEEP_SUBGROUP,
                    PowerSettingGuid: GUID_HIBERNATEIDLE, AcValueIndex: out seconds);

            Logger.WriteLine(logMessage: "Hibernate after " + seconds);
            return (seconds.ToInt32() / 60);
        }


        public static void SetHibernateAfter(int minutes)
        {
            int seconds = minutes * 60;

            Guid activeSchemeGuid = GetActiveScheme();
            var hrAC = PowerWriteDCValueIndex(
                RootPowerKey: IntPtr.Zero,
                SchemeGuid: activeSchemeGuid,
                SubGroupOfPowerSettingsGuid: GUID_SLEEP_SUBGROUP,
                PowerSettingGuid: GUID_HIBERNATEIDLE,
                AcValueIndex: seconds);

            PowerSetActiveScheme(RootPowerKey: IntPtr.Zero, SchemeGuid: activeSchemeGuid);

            Logger.WriteLine(logMessage: "Setting Hibernate after " + seconds + ": " + (hrAC == 0 ? "OK" : hrAC));
        }

        [DllImport(dllName: "Kernel32")]
        private static extern bool GetSystemPowerStatus(SystemPowerStatus sps);
        public enum ACLineStatus : byte
        {
            Offline = 0, Online = 1, Unknown = 255
        }

        public enum BatteryFlag : byte
        {
            High = 1,
            Low = 2,
            Critical = 4,
            Charging = 8,
            NoSystemBattery = 128,
            Unknown = 255
        }

        // Fields must mirror their unmanaged counterparts, in order
        [StructLayout(layoutKind: LayoutKind.Sequential)]
        public class SystemPowerStatus
        {
            public ACLineStatus ACLineStatus;
            public BatteryFlag BatteryFlag;
            public Byte BatteryLifePercent;
            public Byte SystemStatusFlag;
            public Int32 BatteryLifeTime;
            public Int32 BatteryFullLifeTime;
        }

        public static bool GetBatterySaverStatus()
        {
            SystemPowerStatus sps = new SystemPowerStatus();
            try
            {
                GetSystemPowerStatus(sps: sps);
                return (sps.SystemStatusFlag > 0);
            } catch (Exception ex)
            {
                return false;
            }

        }

    }
}
