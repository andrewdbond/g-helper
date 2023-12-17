using GHelper.Display;
using GHelper.Gpu.NVidia;
using GHelper.Helpers;
using GHelper.USB;
using System.Diagnostics;

namespace GHelper.Gpu
{
    public class GPUModeControl
    {
        SettingsForm settings;
        ScreenControl screenControl = new ScreenControl();

        public static int gpuMode;
        public static bool? gpuExists = null;


        public GPUModeControl(SettingsForm settingsForm)
        {
            settings = settingsForm;
        }

        public void InitGPUMode()
        {
            int eco = Program.acpi.DeviceGet(DeviceID: AsusACPI.GPUEco);
            int mux = Program.acpi.DeviceGet(DeviceID: AsusACPI.GPUMux);

            if (mux < 0)
            {
	            mux = Program.acpi.DeviceGet(DeviceID: AsusACPI.GPUMuxVivo);
            }

            Logger.WriteLine(logMessage: "Eco flag : " + eco);
            Logger.WriteLine(logMessage: "Mux flag : " + mux);

            settings.VisualiseGPUButtons(eco: eco >= 0, ultimate: mux >= 0);

            if (mux == 0)
            {
                gpuMode = AsusACPI.GPUModeUltimate;
            }
            else
            {
                if (eco == 1)
                {
	                gpuMode = AsusACPI.GPUModeEco;
                }
                else
                {
	                gpuMode = AsusACPI.GPUModeStandard;
                }

                // GPU mode not supported
                if (eco < 0 && mux < 0)
                {
                    if (gpuExists is null)
                    {
	                    gpuExists = Program.acpi.GetFan(device: AsusFan.GPU) >= 0;
                    }

                    settings.HideGPUModes(gpuExists: (bool)gpuExists);
                }
            }

            AppConfig.Set(name: "gpu_mode", value: gpuMode);
            settings.VisualiseGPUMode(GPUMode: gpuMode);

            Aura.CustomRGB.ApplyGPUColor();

        }



        public void SetGPUMode(int GPUMode, int auto = 0)
        {

            int CurrentGPU = AppConfig.Get(name: "gpu_mode");
            AppConfig.Set(name: "gpu_auto", value: auto);

            if (CurrentGPU == GPUMode)
            {
                settings.VisualiseGPUMode();
                return;
            }

            var restart = false;
            var changed = false;

            int status;

            if (CurrentGPU == AsusACPI.GPUModeUltimate)
            {
                DialogResult dialogResult = MessageBox.Show(text: Properties.Strings.AlertUltimateOff, caption: Properties.Strings.AlertUltimateTitle, buttons: MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    status = Program.acpi.DeviceSet(DeviceID: AsusACPI.GPUMux, Status: 1, logName: "GPUMux");
                    if (status != 1) Program.acpi.DeviceSet(DeviceID: AsusACPI.GPUMuxVivo, Status: 1, logName: "GPUMuxVivo");
                    restart = true;
                    changed = true;
                }
            }
            else if (GPUMode == AsusACPI.GPUModeUltimate)
            {
                DialogResult dialogResult = MessageBox.Show(text: Properties.Strings.AlertUltimateOn, caption: Properties.Strings.AlertUltimateTitle, buttons: MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    if (AppConfig.NoAutoUltimate())
                    {
                        Program.acpi.SetGPUEco(eco: 0);
                        Thread.Sleep(millisecondsTimeout: 100);
                    }
                    status = Program.acpi.DeviceSet(DeviceID: AsusACPI.GPUMux, Status: 0, logName: "GPUMux");
                    if (status != 1) Program.acpi.DeviceSet(DeviceID: AsusACPI.GPUMuxVivo, Status: 0, logName: "GPUMuxVivo");
                    restart = true;
                    changed = true;
                }

            }
            else if (GPUMode == AsusACPI.GPUModeEco)
            {
                settings.VisualiseGPUMode(GPUMode: GPUMode);
                SetGPUEco(eco: 1, hardWay: true);
                changed = true;
            }
            else if (GPUMode == AsusACPI.GPUModeStandard)
            {
                settings.VisualiseGPUMode(GPUMode: GPUMode);
                SetGPUEco(eco: 0);
                changed = true;
            }

            if (changed)
            {
                AppConfig.Set(name: "gpu_mode", value: GPUMode);
            }

            if (restart)
            {
                settings.VisualiseGPUMode();
                Process.Start(fileName: "shutdown", arguments: "/r /t 1");
            }

        }



        public void SetGPUEco(int eco, bool hardWay = false)
        {

            settings.LockGPUModes();

            Task.Run(function: async () =>
            {

                int status = 1;

                if (eco == 1)
                {
                    /*
                    if (NvidiaSmi.GetDisplayActiveStatus())
                    {
                        DialogResult dialogResult = MessageBox.Show(Properties.Strings.EnableOptimusText, Properties.Strings.EnableOptimusTitle, MessageBoxButtons.YesNo);
                        if (dialogResult == DialogResult.No)
                        {
                            InitGPUMode();
                            return;
                        }
                    }
                    */

                    HardwareControl.KillGPUApps();
                }

                Logger.WriteLine(logMessage: $"Running eco command {eco}");

                status = Program.acpi.SetGPUEco(eco: eco);

                if (status == 0 && eco == 1 && hardWay) RestartGPU();

                await Task.Delay(delay: TimeSpan.FromMilliseconds(value: AppConfig.Get(name: "refresh_delay", empty: 500)));

                settings.Invoke(method: delegate
                {
                    InitGPUMode();
                    screenControl.AutoScreen();
                });

                if (eco == 0)
                {
                    await Task.Delay(delay: TimeSpan.FromMilliseconds(value: 3000));
                    HardwareControl.RecreateGpuControl();
                    Program.modeControl.SetGPUClocks(launchAsAdmin: false);
                }

                if (AppConfig.Is(name: "mode_reapply"))
                {
                    await Task.Delay(delay: TimeSpan.FromMilliseconds(value: 3000));
                    Program.modeControl.AutoPerformance();
                }

            });


        }

        public static bool IsPlugged()
        {
            if (SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Online) return false;
            if (!AppConfig.Is(name: "optimized_usbc")) return true;

            int chargerMode = Program.acpi.DeviceGet(DeviceID: AsusACPI.ChargerMode);
            Logger.WriteLine(logMessage: "ChargerStatus: " + chargerMode);

            if (chargerMode < 0) return true;
            return (chargerMode & AsusACPI.ChargerBarrel) > 0;

        }

        public bool AutoGPUMode(bool optimized = false)
        {

            bool GpuAuto = AppConfig.Is(name: "gpu_auto");
            bool ForceGPU = AppConfig.IsForceSetGPUMode();

            int GpuMode = AppConfig.Get(name: "gpu_mode");

            if (!GpuAuto && !ForceGPU) return false;

            int eco = Program.acpi.DeviceGet(DeviceID: AsusACPI.GPUEco);
            int mux = Program.acpi.DeviceGet(DeviceID: AsusACPI.GPUMux);

            if (mux == 0)
            {
                if (optimized) SetGPUMode(GPUMode: AsusACPI.GPUModeStandard, auto: 1);
                return false;
            }
            else
            {

                if (eco == 1)
                    if ((GpuAuto && IsPlugged()) || (ForceGPU && GpuMode == AsusACPI.GPUModeStandard))
                    {
                        SetGPUEco(eco: 0);
                        return true;
                    }
                if (eco == 0)
                    if ((GpuAuto && !IsPlugged()) || (ForceGPU && GpuMode == AsusACPI.GPUModeEco))
                    {

                        if (HardwareControl.IsUsedGPU())
                        {
                            DialogResult dialogResult = MessageBox.Show(text: Properties.Strings.AlertDGPU, caption: Properties.Strings.AlertDGPUTitle, buttons: MessageBoxButtons.YesNo);
                            if (dialogResult == DialogResult.No) return false;
                        }

                        SetGPUEco(eco: 1);
                        return true;
                    }
            }

            return false;

        }


        public void RestartGPU(bool confirm = true)
        {
            if (HardwareControl.GpuControl is null) return;
            if (!HardwareControl.GpuControl!.IsNvidia) return;

            if (confirm)
            {
                DialogResult dialogResult = MessageBox.Show(text: Properties.Strings.RestartGPU, caption: Properties.Strings.EcoMode, buttons: MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.No) return;
            }

            ProcessHelper.RunAsAdmin(param: "gpurestart");

            if (!ProcessHelper.IsUserAdministrator()) return;

            Logger.WriteLine(logMessage: "Trying to restart dGPU");

            Task.Run(function: async () =>
            {
                settings.LockGPUModes(text: "Restarting GPU ...");

                var nvControl = (NvidiaGpuControl)HardwareControl.GpuControl;
                bool status = nvControl.RestartGPU();

                settings.Invoke(method: delegate
                {
                    //labelTipGPU.Text = status ? "GPU Restarted, you can try Eco mode again" : "Failed to restart GPU"; TODO
                    InitGPUMode();
                });
            });

        }


        public void InitXGM()
        {
            if (Program.acpi.IsXGConnected())
            {
                //Program.acpi.DeviceSet(AsusACPI.GPUXGInit, 1, "XG Init");
                XGM.Init();
            }

        }

        public void ToggleXGM()
        {

            Task.Run(function: async () =>
            {
                settings.LockGPUModes();

                if (Program.acpi.DeviceGet(DeviceID: AsusACPI.GPUXG) == 1)
                {
                    XGM.Reset();
                    HardwareControl.KillGPUApps();

                    DialogResult dialogResult = MessageBox.Show(text: "Did you close all applications running on XG Mobile?", caption: "Disabling XG Mobile", buttons: MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.Yes)
                    {
                        Program.acpi.DeviceSet(DeviceID: AsusACPI.GPUXG, Status: 0, logName: "GPU XGM");
                        await Task.Delay(delay: TimeSpan.FromSeconds(value: 15));
                    }
                }
                else
                {

                    if (AppConfig.Is(name: "xgm_special"))
                        Program.acpi.DeviceSet(DeviceID: AsusACPI.GPUXG, Status: 0x101, logName: "GPU XGM");
                    else
                        Program.acpi.DeviceSet(DeviceID: AsusACPI.GPUXG, Status: 1, logName: "GPU XGM");

                    InitXGM();

                    XGM.Light(status: AppConfig.Is(name: "xmg_light"));

                    await Task.Delay(delay: TimeSpan.FromSeconds(value: 15));

                    if (AppConfig.IsMode(name: "auto_apply"))
                        XGM.SetFan(curve: AppConfig.GetFanConfig(device: AsusFan.XGM));

                    HardwareControl.RecreateGpuControl();

                }

                settings.Invoke(method: delegate
                {
                    InitGPUMode();
                });
            });
        }

        public void KillGPUApps()
        {
            if (HardwareControl.GpuControl is not null)
            {
                HardwareControl.GpuControl.KillGPUApps();
            }
        }

        // Manually forcing standard mode on shutdown/hibernate for some exotic cases
        // https://github.com/seerge/g-helper/pull/855 
        public void StandardModeFix()
        {
            if (!AppConfig.IsGPUFix()) return; // No config entry
            if (Program.acpi.DeviceGet(DeviceID: AsusACPI.GPUMux) == 0) return; // Ultimate mode

            Logger.WriteLine(logMessage: "Forcing Standard Mode on shutdown / hibernation");
            Program.acpi.SetGPUEco(eco: 0);
        }

    }
}
