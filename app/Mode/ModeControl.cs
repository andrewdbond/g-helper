using GHelper.Gpu.NVidia;
using GHelper.Helpers;
using GHelper.USB;
using Ryzen;

namespace GHelper.Mode
{
    public class ModeControl
    {

        static SettingsForm settings = Program.settingsForm;

        private static bool customFans = false;
        private static int customPower = 0;

        private int _cpuUV = 0;
        private int _igpuUV = 0;

        static System.Timers.Timer reapplyTimer = default!;

        public ModeControl()
        {
            reapplyTimer = new System.Timers.Timer(interval: AppConfig.GetMode(name: "reapply_time", empty: 30) * 1000);
            reapplyTimer.Elapsed += ReapplyTimer_Elapsed;
            reapplyTimer.Enabled = false;
        }

        private void ReapplyTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            SetCPUTemp(cpuTemp: AppConfig.GetMode(name: "cpu_temp"), log: false);
        }

        public void AutoPerformance(bool powerChanged = false)
        {
            var Plugged = SystemInformation.PowerStatus.PowerLineStatus;

            int mode = AppConfig.Get(name: "performance_" + (int)Plugged);

            if (mode != -1)
                SetPerformanceMode(mode: mode, notify: powerChanged);
            else
                SetPerformanceMode(mode: Modes.GetCurrent());
        }


        public void ResetPerformanceMode()
        {
            ResetRyzen();

            Program.acpi.DeviceSet(DeviceID: AsusACPI.PerformanceMode, Status: Modes.GetCurrentBase(), logName: "Mode");

            // Default power mode
            AppConfig.RemoveMode(name: "powermode");
            PowerNative.SetPowerMode(mode: Modes.GetCurrentBase());
        }

        public void SetPerformanceMode(int mode = -1, bool notify = false)
        {

            int oldMode = Modes.GetCurrent();
            if (mode < 0) mode = oldMode;

            if (!Modes.Exists(mode: mode)) mode = 0;

            customFans = false;
            customPower = 0;

            settings.ShowMode(mode: mode);
            SetModeLabel();

            Modes.SetCurrent(mode: mode);

            int status = Program.acpi.DeviceSet(DeviceID: AsusACPI.PerformanceMode, Status: AppConfig.IsManualModeRequired() ? AsusACPI.PerformanceManual : Modes.GetBase(mode: mode), logName: "Mode");

            // Vivobook fallback
            if (status != 1)
            {
                int vivoMode = Modes.GetBase(mode: mode);
                if (vivoMode == 1) vivoMode = 2;
                else if (vivoMode == 2) vivoMode = 1;
                Program.acpi.DeviceSet(DeviceID: AsusACPI.VivoBookMode, Status: vivoMode, logName: "VivoMode");
            }

            if (AppConfig.Is(name: "xgm_fan") && Program.acpi.IsXGConnected()) XGM.Reset();

            if (notify)
                Program.toast.RunToast(text: Modes.GetCurrentName(), icon: SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online ? ToastIcon.Charger : ToastIcon.Battery);

            SetGPUClocks();
            AutoFans();
            AutoPower(delay: 1000);

            // Power plan from config or defaulting to balanced
            if (AppConfig.GetModeString(name: "scheme") is not null)
                PowerNative.SetPowerPlan(scheme: AppConfig.GetModeString(name: "scheme"));
            else
                PowerNative.SetBalancedPowerPlan();

            // Windows power mode
            if (AppConfig.GetModeString(name: "powermode") is not null)
                PowerNative.SetPowerMode(scheme: AppConfig.GetModeString(name: "powermode"));
            else
                PowerNative.SetPowerMode(mode: Modes.GetBase(mode: mode));

            // CPU Boost setting override
            if (AppConfig.GetMode(name: "auto_boost") != -1)
                PowerNative.SetCPUBoost(boost: AppConfig.GetMode(name: "auto_boost"));

            //BatteryControl.SetBatteryChargeLimit();

            /*
            if (NativeMethods.PowerGetEffectiveOverlayScheme(out Guid activeScheme) == 0)
            {
                Debug.WriteLine("Effective :" + activeScheme);
            }
            */

            settings.FansInit();
        }


        public void CyclePerformanceMode(bool back = false)
        {
            SetPerformanceMode(mode: Modes.GetNext(back: back), notify: true);
        }

        public void AutoFans(bool force = false)
        {
            customFans = false;

            if (AppConfig.IsMode(name: "auto_apply") || force)
            {

                bool xgmFan = false;
                if (AppConfig.Is(name: "xgm_fan") && Program.acpi.IsXGConnected())
                {
                    XGM.SetFan(curve: AppConfig.GetFanConfig(device: AsusFan.XGM));
                    xgmFan = true;
                }

                int cpuResult = Program.acpi.SetFanCurve(device: AsusFan.CPU, curve: AppConfig.GetFanConfig(device: AsusFan.CPU));
                int gpuResult = Program.acpi.SetFanCurve(device: AsusFan.GPU, curve: AppConfig.GetFanConfig(device: AsusFan.GPU));

                if (AppConfig.Is(name: "mid_fan"))
                    Program.acpi.SetFanCurve(device: AsusFan.Mid, curve: AppConfig.GetFanConfig(device: AsusFan.Mid));


                // something went wrong, resetting to default profile
                if (cpuResult != 1 || gpuResult != 1)
                {
                    cpuResult = Program.acpi.SetFanRange(device: AsusFan.CPU, curve: AppConfig.GetFanConfig(device: AsusFan.CPU));
                    gpuResult = Program.acpi.SetFanRange(device: AsusFan.GPU, curve: AppConfig.GetFanConfig(device: AsusFan.GPU));

                    if (cpuResult != 1 || gpuResult != 1)
                    {
                        int mode = Modes.GetCurrentBase();
                        Logger.WriteLine(logMessage: "ASUS BIOS rejected fan curve, resetting mode to " + mode);
                        Program.acpi.DeviceSet(DeviceID: AsusACPI.PerformanceMode, Status: mode, logName: "Reset Mode");
                        settings.LabelFansResult(text: "ASUS BIOS rejected fan curve");
                    }
                }
                else
                {
                    settings.LabelFansResult(text: "");
                    customFans = true;
                }

                // force set PPTs for missbehaving bios on FX507/517 series
                if ((AppConfig.IsPowerRequired() || xgmFan) && !AppConfig.IsMode(name: "auto_apply_power"))
                {
                    Task.Run(function: async () =>
                    {
                        await Task.Delay(delay: TimeSpan.FromSeconds(value: 1));
                        Program.acpi.DeviceSet(DeviceID: AsusACPI.PPT_TotalA0, Status: 80, logName: "PowerLimit Fix A0");
                        Program.acpi.DeviceSet(DeviceID: AsusACPI.PPT_APUA3, Status: 80, logName: "PowerLimit Fix A3");
                    });
                }

            }

            SetModeLabel();

        }

        public void AutoPower(int delay = 0)
        {

            customPower = 0;

            bool applyPower = AppConfig.IsMode(name: "auto_apply_power");
            bool applyFans = AppConfig.IsMode(name: "auto_apply");
            //bool applyGPU = true;

            if (applyPower && !applyFans)
            {
                // force fan curve for misbehaving bios PPTs on some models
                if (AppConfig.IsFanRequired())
                {
                    delay = 500;
                    AutoFans(force: true);
                }

                // Fix for models that don't support PPT settings in all modes, setting a "manual" mode for them
                if (AppConfig.IsManualModeRequired())
                {
                    AutoFans(force: true);
                }
            }

            if (delay > 0)
            {
                var timer = new System.Timers.Timer(interval: delay);
                timer.Elapsed += delegate
                {
                    timer.Stop();
                    timer.Dispose();

                    if (applyPower) SetPower();
                    Thread.Sleep(millisecondsTimeout: 500);
                    SetGPUPower();
                    AutoRyzen();
                };
                timer.Start();
            }
            else
            {
                if (applyPower) SetPower(launchAsAdmin: true);
                SetGPUPower();
                AutoRyzen();
            }

        }

        public void SetModeLabel()
        {
            settings.SetModeLabel(modeText: Properties.Strings.PerformanceMode + ": " + Modes.GetCurrentName() + (customFans ? "+" : "") + ((customPower > 0) ? " " + customPower + "W" : ""));
        }

        public void SetPower(bool launchAsAdmin = false)
        {

            bool allAMD = Program.acpi.IsAllAmdPPT();

            int limit_total = AppConfig.GetMode(name: "limit_total");
            int limit_cpu = AppConfig.GetMode(name: "limit_cpu");
            int limit_slow = AppConfig.GetMode(name: "limit_slow");
            int limit_fast = AppConfig.GetMode(name: "limit_fast");

            if (limit_slow < 0 || allAMD) limit_slow = limit_total;

            if (limit_total > AsusACPI.MaxTotal) return;
            if (limit_total < AsusACPI.MinTotal) return;

            if (limit_cpu > AsusACPI.MaxCPU) return;
            if (limit_cpu < AsusACPI.MinCPU) return;

            if (limit_fast > AsusACPI.MaxTotal) return;
            if (limit_fast < AsusACPI.MinTotal) return;

            if (limit_slow > AsusACPI.MaxTotal) return;
            if (limit_slow < AsusACPI.MinTotal) return;

            // SPL and SPPT
            if (Program.acpi.DeviceGet(DeviceID: AsusACPI.PPT_TotalA0) >= 0)
            {
                Program.acpi.DeviceSet(DeviceID: AsusACPI.PPT_TotalA0, Status: limit_total, logName: "PowerLimit A0");
                Program.acpi.DeviceSet(DeviceID: AsusACPI.PPT_APUA3, Status: limit_slow, logName: "PowerLimit A3");
                customPower = limit_total;
            }
            else if (RyzenControl.IsAMD())
            {

                if (ProcessHelper.IsUserAdministrator())
                {
                    var stapmResult = SendCommand.set_stapm_limit(value: (uint)limit_total * 1000);
                    Logger.WriteLine(logMessage: $"STAPM: {limit_total} {stapmResult}");

                    var stapmResult2 = SendCommand.set_stapm2_limit(value: (uint)limit_total * 1000);
                    Logger.WriteLine(logMessage: $"STAPM2: {limit_total} {stapmResult2}");

                    var slowResult = SendCommand.set_slow_limit(value: (uint)limit_total * 1000);
                    Logger.WriteLine(logMessage: $"SLOW: {limit_total} {slowResult}");

                    var fastResult = SendCommand.set_fast_limit(value: (uint)limit_total * 1000);
                    Logger.WriteLine(logMessage: $"FAST: {limit_total} {fastResult}");

                    customPower = limit_total;
                }
                else if (launchAsAdmin)
                {
                    ProcessHelper.RunAsAdmin(param: "cpu");
                    return;
                }
            }

            if (Program.acpi.IsAllAmdPPT()) // CPU limit all amd models
            {
                Program.acpi.DeviceSet(DeviceID: AsusACPI.PPT_CPUB0, Status: limit_cpu, logName: "PowerLimit B0");
                customPower = limit_cpu;
            }
            else if (Program.acpi.DeviceGet(DeviceID: AsusACPI.PPT_APUC1) >= 0) // FPPT boost for non all-amd models
            {
                Program.acpi.DeviceSet(DeviceID: AsusACPI.PPT_APUC1, Status: limit_fast, logName: "PowerLimit C1");
                customPower = limit_fast;
            }


            SetModeLabel();

        }

        public void SetGPUClocks(bool launchAsAdmin = true)
        {
            Task.Run(action: () =>
            {

                int core = AppConfig.GetMode(name: "gpu_core");
                int memory = AppConfig.GetMode(name: "gpu_memory");
                int clock_limit = AppConfig.GetMode(name: "gpu_clock_limit");

                if (core == -1 && memory == -1 && clock_limit == -1) return;

                //if ((gpu_core > -5 && gpu_core < 5) && (gpu_memory > -5 && gpu_memory < 5)) launchAsAdmin = false;

                if (Program.acpi.DeviceGet(DeviceID: AsusACPI.GPUEco) == 1) { Logger.WriteLine(logMessage: "Clocks: Eco"); return; }
                if (HardwareControl.GpuControl is null) { Logger.WriteLine(logMessage: "Clocks: NoGPUControl"); return; }
                if (!HardwareControl.GpuControl!.IsNvidia) { Logger.WriteLine(logMessage: "Clocks: NotNvidia"); return; }

                using NvidiaGpuControl nvControl = (NvidiaGpuControl)HardwareControl.GpuControl;
                try
                {
                    int statusLimit = nvControl.SetMaxGPUClock(clock: clock_limit);
                    int statusClocks = nvControl.SetClocks(core: core, memory: memory);
                    if ((statusLimit != 0 || statusClocks != 0) && launchAsAdmin) ProcessHelper.RunAsAdmin(param: "gpu");
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(logMessage: "Clocks Error:" + ex.ToString());
                }

                settings.GPUInit();
            });
        }

        public void SetGPUPower()
        {

            int gpu_boost = AppConfig.GetMode(name: "gpu_boost");
            int gpu_temp = AppConfig.GetMode(name: "gpu_temp");


            if (gpu_boost < AsusACPI.MinGPUBoost || gpu_boost > AsusACPI.MaxGPUBoost) return;
            if (gpu_temp < AsusACPI.MinGPUTemp || gpu_temp > AsusACPI.MaxGPUTemp) return;

            if (Program.acpi.DeviceGet(DeviceID: AsusACPI.PPT_GPUC0) >= 0)
                Program.acpi.DeviceSet(DeviceID: AsusACPI.PPT_GPUC0, Status: gpu_boost, logName: "PowerLimit C0");

            if (Program.acpi.DeviceGet(DeviceID: AsusACPI.PPT_GPUC2) >= 0)
                Program.acpi.DeviceSet(DeviceID: AsusACPI.PPT_GPUC2, Status: gpu_temp, logName: "PowerLimit C2");

        }

        public void SetCPUTemp(int? cpuTemp, bool log = true)
        {
            if (cpuTemp >= RyzenControl.MinTemp && cpuTemp < RyzenControl.MaxTemp)
            {
                var resultCPU = SendCommand.set_tctl_temp(value: (uint)cpuTemp);
                if (log) Logger.WriteLine(logMessage: $"CPU Temp: {cpuTemp} {resultCPU}");

                var restultAPU = SendCommand.set_apu_skin_temp_limit(value: (uint)cpuTemp);
                if (log) Logger.WriteLine(logMessage: $"APU Temp: {cpuTemp} {restultAPU}");

                reapplyTimer.Enabled = AppConfig.IsMode(name: "auto_uv");

            }
            else
            {
                reapplyTimer.Enabled = false;
            }
        }

        public void SetUV(int cpuUV)
        {
            if (!RyzenControl.IsSupportedUV()) return;

            if (cpuUV >= RyzenControl.MinCPUUV && cpuUV <= RyzenControl.MaxCPUUV)
            {
                var uvResult = SendCommand.set_coall(value: cpuUV);
                Logger.WriteLine(logMessage: $"UV: {cpuUV} {uvResult}");
                if (uvResult == Smu.Status.OK) _cpuUV = cpuUV;
            }
        }

        public void SetUViGPU(int igpuUV)
        {
            if (!RyzenControl.IsSupportedUViGPU()) return;

            if (igpuUV >= RyzenControl.MinIGPUUV && igpuUV <= RyzenControl.MaxIGPUUV)
            {
                var iGPUResult = SendCommand.set_cogfx(value: igpuUV);
                Logger.WriteLine(logMessage: $"iGPU UV: {igpuUV} {iGPUResult}");
                if (iGPUResult == Smu.Status.OK) _igpuUV = igpuUV;
            }
        }


        public void SetRyzen(bool launchAsAdmin = false)
        {
            if (!ProcessHelper.IsUserAdministrator())
            {
                if (launchAsAdmin) ProcessHelper.RunAsAdmin(param: "uv");
                return;
            }

            try
            {
                SetUV(cpuUV: AppConfig.GetMode(name: "cpu_uv", empty: 0));
                SetUViGPU(igpuUV: AppConfig.GetMode(name: "igpu_uv", empty: 0));
                SetCPUTemp(cpuTemp: AppConfig.GetMode(name: "cpu_temp"));
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: "UV Error: " + ex.ToString());
            }
        }

        public void ResetRyzen()
        {
            if (_cpuUV != 0) SetUV(cpuUV: 0);
            if (_igpuUV != 0) SetUViGPU(igpuUV: 0);
        }

        public void AutoRyzen()
        {
            if (!RyzenControl.IsAMD()) return;

            if (AppConfig.IsMode(name: "auto_uv")) SetRyzen();
            else ResetRyzen();
        }

    }
}
