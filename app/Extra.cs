using GHelper.Display;
using GHelper.Gpu.AMD;
using GHelper.Helpers;
using GHelper.Input;
using GHelper.Mode;
using GHelper.UI;
using GHelper.USB;
using System.Diagnostics;

namespace GHelper
{
    public partial class Extra : RForm
    {

        ScreenControl screenControl = new ScreenControl();
        ModeControl modeControl = new ModeControl();
        ClamshellModeControl clamshellControl = new ClamshellModeControl();

        const string EMPTY = "--------------";


        private void SetKeyCombo(ComboBox combo, TextBox txbox, string name)
        {

            Dictionary<string, string> customActions = new Dictionary<string, string>
            {
              {"", EMPTY},
              {"mute", Properties.Strings.VolumeMute},
              {"screenshot", Properties.Strings.PrintScreen},
              {"play", Properties.Strings.PlayPause},
              {"aura", Properties.Strings.ToggleAura},
              {"performance", Properties.Strings.PerformanceMode},
              {"screen", Properties.Strings.ToggleScreen},
              {"miniled", Properties.Strings.ToggleMiniled},
              {"fnlock", Properties.Strings.ToggleFnLock},
              {"brightness_down", Properties.Strings.BrightnessDown},
              {"brightness_up", Properties.Strings.BrightnessUp},
              {"ghelper", Properties.Strings.OpenGHelper},
              {"custom", Properties.Strings.Custom}
            };

            if (AppConfig.IsDUO())
            {
                customActions.Add(key: "screenpad_down", value: Properties.Strings.ScreenPadDown);
                customActions.Add(key: "screenpad_up", value: Properties.Strings.ScreenPadUp);
            }

            switch (name)
            {
                case "m1":
                    customActions[key: ""] = Properties.Strings.VolumeDown;
                    break;
                case "m2":
                    customActions[key: ""] = Properties.Strings.VolumeUp;
                    break;
                case "m3":
                    customActions[key: ""] = Properties.Strings.MuteMic;
                    break;
                case "m4":
                    customActions[key: ""] = Properties.Strings.OpenGHelper;
                    customActions.Remove(key: "ghelper");
                    break;
                case "fnf4":
                    customActions[key: ""] = Properties.Strings.ToggleAura;
                    customActions.Remove(key: "aura");
                    break;
                case "fnc":
                    customActions[key: ""] = Properties.Strings.ToggleFnLock;
                    customActions.Remove(key: "fnlock");
                    break;
                case "fne":
                    customActions[key: ""] = "Calculator";
                    break;
                case "paddle":
                    customActions[key: ""] = EMPTY;
                    break;
                case "cc":
                    customActions[key: ""] = EMPTY;
                    break;
            }

            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.DataSource = new BindingSource(dataSource: customActions, dataMember: null);
            combo.DisplayMember = "Value";
            combo.ValueMember = "Key";

            string action = AppConfig.GetString(name: name);

            combo.SelectedValue = (action is not null) ? action : "";
            if (combo.SelectedValue is null) combo.SelectedValue = "";

            combo.SelectedValueChanged += delegate
            {
                if (combo.SelectedValue is not null)
                    AppConfig.Set(name: name, value: combo.SelectedValue.ToString());

                if (name == "m1" || name == "m2")
                    Program.inputDispatcher.RegisterKeys();

            };

            txbox.Text = AppConfig.GetString(name: name + "_custom");
            txbox.TextChanged += delegate
            {
                AppConfig.Set(name: name + "_custom", value: txbox.Text);
            };
        }

        public Extra()
        {
            InitializeComponent();

            labelBindings.Text = Properties.Strings.KeyBindings;
            labelBacklightTitle.Text = Properties.Strings.LaptopBacklight;
            labelSettings.Text = Properties.Strings.Other;

            checkAwake.Text = Properties.Strings.Awake;
            checkSleep.Text = Properties.Strings.Sleep;
            checkBoot.Text = Properties.Strings.Boot;
            checkShutdown.Text = Properties.Strings.Shutdown;
            checkBootSound.Text = Properties.Strings.BootSound;

            labelSpeed.Text = Properties.Strings.AnimationSpeed;
            //labelBrightness.Text = Properties.Strings.Brightness;

            labelBacklightTimeout.Text = Properties.Strings.BacklightTimeout;
            //labelBacklightTimeoutPlugged.Text = Properties.Strings.BacklightTimeoutPlugged;

            checkNoOverdrive.Text = Properties.Strings.DisableOverdrive;
            checkTopmost.Text = Properties.Strings.WindowTop;
            checkUSBC.Text = Properties.Strings.OptimizedUSBC;
            checkAutoToggleClamshellMode.Text = Properties.Strings.ToggleClamshellMode;

            labelBacklightKeyboard.Text = Properties.Strings.Keyboard;
            labelBacklightBar.Text = Properties.Strings.Lightbar;
            labelBacklightLid.Text = Properties.Strings.Lid;
            labelBacklightLogo.Text = Properties.Strings.Logo;

            checkGpuApps.Text = Properties.Strings.KillGpuApps;
            labelHibernateAfter.Text = Properties.Strings.HibernateAfter;

            labelAPUMem.Text = Properties.Strings.APUMemory;

            Text = Properties.Strings.ExtraSettings;

            if (AppConfig.IsARCNM())
            {
                labelM3.Text = "FN+F6";
                labelM1.Visible = comboM1.Visible = textM1.Visible = false;
                labelM2.Visible = comboM2.Visible = textM2.Visible = false;
                labelM4.Visible = comboM4.Visible = textM4.Visible = false;
                labelFNF4.Visible = comboFNF4.Visible = textFNF4.Visible = false;
            }

            if (AppConfig.NoMKeys())
            {
                labelM1.Text = "FN+F2";
                labelM2.Text = "FN+F3";
                labelM3.Text = "FN+F4";
                labelM4.Visible = comboM4.Visible = textM4.Visible = AppConfig.IsDUO();
                labelFNF4.Visible = comboFNF4.Visible = textFNF4.Visible = false;
            }

            if (AppConfig.NoAura())
            {
                labelFNF4.Visible = comboFNF4.Visible = textFNF4.Visible = false;
            }

            if (!AppConfig.IsTUF())
            {
                labelFNE.Visible = comboFNE.Visible = textFNE.Visible = false;
            }

            if (Program.acpi.DeviceGet(DeviceID: AsusACPI.GPUEco) < 0)
            {
                checkGpuApps.Visible = false;
                checkUSBC.Visible = false;
            }

            // Change text and hide irrelevant options on the ROG Ally,
            // which is a bit of a special case piece of hardware.
            if (AppConfig.IsAlly())
            {
                labelM1.Visible = comboM1.Visible = textM1.Visible = false;
                labelM2.Visible = comboM2.Visible = textM2.Visible = false;

                // Re-label M3 and M4 and FNF4 to match the front labels.
                labelM3.Text = "Ctrl Center";
                labelM4.Text = "ROG";
                labelFNF4.Text = "Back Paddles";

                // Hide all of the FN options, as the Ally has no special keyboard FN key.
                labelFNC.Visible = false;
                comboFNC.Visible = false;
                textFNC.Visible = false;

                SetKeyCombo(combo: comboM3, txbox: textM3, name: "cc");
                SetKeyCombo(combo: comboM4, txbox: textM4, name: "m4");
                SetKeyCombo(combo: comboFNF4, txbox: textFNF4, name: "paddle");


                int apuMem = Program.acpi.GetAPUMem();
                if (apuMem >= 0)
                {
                    panelAPU.Visible = true;
                    comboAPU.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboAPU.SelectedIndex = apuMem;
                }

                comboAPU.SelectedIndexChanged += ComboAPU_SelectedIndexChanged;

            }
            else
            {
                SetKeyCombo(combo: comboM1, txbox: textM1, name: "m1");
                SetKeyCombo(combo: comboM2, txbox: textM2, name: "m2");

                SetKeyCombo(combo: comboM3, txbox: textM3, name: "m3");
                SetKeyCombo(combo: comboM4, txbox: textM4, name: "m4");
                SetKeyCombo(combo: comboFNF4, txbox: textFNF4, name: "fnf4");

                SetKeyCombo(combo: comboFNC, txbox: textFNC, name: "fnc");
                SetKeyCombo(combo: comboFNE, txbox: textFNE, name: "fne");
            }

            if (AppConfig.IsStrix())
            {
                labelM4.Text = "M5/ROG";
            }


            InitTheme();
            Shown += Keyboard_Shown;

            comboKeyboardSpeed.DropDownStyle = ComboBoxStyle.DropDownList;
            comboKeyboardSpeed.DataSource = new BindingSource(dataSource: Aura.GetSpeeds(), dataMember: null);
            comboKeyboardSpeed.DisplayMember = "Value";
            comboKeyboardSpeed.ValueMember = "Key";
            comboKeyboardSpeed.SelectedValue = Aura.Speed;
            comboKeyboardSpeed.SelectedValueChanged += ComboKeyboardSpeed_SelectedValueChanged;

            // Keyboard
            checkAwake.Checked = AppConfig.IsNotFalse(name: "keyboard_awake");
            checkBoot.Checked = AppConfig.IsNotFalse(name: "keyboard_boot");
            checkSleep.Checked = AppConfig.IsNotFalse(name: "keyboard_sleep");
            checkShutdown.Checked = AppConfig.IsNotFalse(name: "keyboard_shutdown");

            // Lightbar
            checkAwakeBar.Checked = AppConfig.IsNotFalse(name: "keyboard_awake_bar");
            checkBootBar.Checked = AppConfig.IsNotFalse(name: "keyboard_boot_bar");
            checkSleepBar.Checked = AppConfig.IsNotFalse(name: "keyboard_sleep_bar");
            checkShutdownBar.Checked = AppConfig.IsNotFalse(name: "keyboard_shutdown_bar");

            // Lid
            checkAwakeLid.Checked = AppConfig.IsNotFalse(name: "keyboard_awake_lid");
            checkBootLid.Checked = AppConfig.IsNotFalse(name: "keyboard_boot_lid");
            checkSleepLid.Checked = AppConfig.IsNotFalse(name: "keyboard_sleep_lid");
            checkShutdownLid.Checked = AppConfig.IsNotFalse(name: "keyboard_shutdown_lid");

            // Logo
            checkAwakeLogo.Checked = AppConfig.IsNotFalse(name: "keyboard_awake_logo");
            checkBootLogo.Checked = AppConfig.IsNotFalse(name: "keyboard_boot_logo");
            checkSleepLogo.Checked = AppConfig.IsNotFalse(name: "keyboard_sleep_logo");
            checkShutdownLogo.Checked = AppConfig.IsNotFalse(name: "keyboard_shutdown_logo");

            checkAwake.CheckedChanged += CheckPower_CheckedChanged;
            checkBoot.CheckedChanged += CheckPower_CheckedChanged;
            checkSleep.CheckedChanged += CheckPower_CheckedChanged;
            checkShutdown.CheckedChanged += CheckPower_CheckedChanged;

            checkAwakeBar.CheckedChanged += CheckPower_CheckedChanged;
            checkBootBar.CheckedChanged += CheckPower_CheckedChanged;
            checkSleepBar.CheckedChanged += CheckPower_CheckedChanged;
            checkShutdownBar.CheckedChanged += CheckPower_CheckedChanged;

            checkAwakeLid.CheckedChanged += CheckPower_CheckedChanged;
            checkBootLid.CheckedChanged += CheckPower_CheckedChanged;
            checkSleepLid.CheckedChanged += CheckPower_CheckedChanged;
            checkShutdownLid.CheckedChanged += CheckPower_CheckedChanged;

            checkAwakeLogo.CheckedChanged += CheckPower_CheckedChanged;
            checkBootLogo.CheckedChanged += CheckPower_CheckedChanged;
            checkSleepLogo.CheckedChanged += CheckPower_CheckedChanged;
            checkShutdownLogo.CheckedChanged += CheckPower_CheckedChanged;

            if (!AppConfig.IsStrix())
            {
                labelBacklightBar.Visible = false;
                checkAwakeBar.Visible = false;
                checkBootBar.Visible = false;
                checkSleepBar.Visible = false;
                checkShutdownBar.Visible = false;

            }

            if ((!AppConfig.IsStrix() && !AppConfig.IsZ13()) || AppConfig.IsStrixLimitedRGB() || AppConfig.IsARCNM())
            {
                labelBacklightLid.Visible = false;
                checkAwakeLid.Visible = false;
                checkBootLid.Visible = false;
                checkSleepLid.Visible = false;
                checkShutdownLid.Visible = false;

                labelBacklightLogo.Visible = false;
                checkAwakeLogo.Visible = false;
                checkBootLogo.Visible = false;
                checkSleepLogo.Visible = false;
                checkShutdownLogo.Visible = false;

            }

            if (!AppConfig.IsStrix() && !AppConfig.IsZ13())
            {
                labelBacklightKeyboard.Visible = false;
            }

            //checkAutoToggleClamshellMode.Visible = clamshellControl.IsExternalDisplayConnected();
            checkAutoToggleClamshellMode.Checked = AppConfig.Is(name: "toggle_clamshell_mode");
            checkAutoToggleClamshellMode.CheckedChanged += checkAutoToggleClamshellMode_CheckedChanged;

            checkTopmost.Checked = AppConfig.Is(name: "topmost");
            checkTopmost.CheckedChanged += CheckTopmost_CheckedChanged; ;

            checkNoOverdrive.Checked = AppConfig.Is(name: "no_overdrive");
            checkNoOverdrive.CheckedChanged += CheckNoOverdrive_CheckedChanged;

            checkUSBC.Checked = AppConfig.Is(name: "optimized_usbc");
            checkUSBC.CheckedChanged += CheckUSBC_CheckedChanged;

            sliderBrightness.Value = InputDispatcher.GetBacklight();
            sliderBrightness.ValueChanged += SliderBrightness_ValueChanged;

            panelXMG.Visible = (Program.acpi.DeviceGet(DeviceID: AsusACPI.GPUXGConnected) == 1);
            checkXMG.Checked = !(AppConfig.Get(name: "xmg_light") == 0);
            checkXMG.CheckedChanged += CheckXMG_CheckedChanged;

            numericBacklightTime.Value = AppConfig.Get(name: "keyboard_timeout", empty: 60);
            numericBacklightPluggedTime.Value = AppConfig.Get(name: "keyboard_ac_timeout", empty: 0);

            numericBacklightTime.ValueChanged += NumericBacklightTime_ValueChanged;
            numericBacklightPluggedTime.ValueChanged += NumericBacklightTime_ValueChanged;

            checkGpuApps.Checked = AppConfig.Is(name: "kill_gpu_apps");
            checkGpuApps.CheckedChanged += CheckGpuApps_CheckedChanged;

            checkBootSound.Checked = (Program.acpi.DeviceGet(DeviceID: AsusACPI.BootSound) == 1);
            checkBootSound.CheckedChanged += CheckBootSound_CheckedChanged;

            pictureHelp.Click += PictureHelp_Click;
            buttonServices.Click += ButtonServices_Click;

            pictureLog.Click += PictureLog_Click;

            checkGPUFix.Visible = AppConfig.IsGPUFixNeeded();
            checkGPUFix.Checked = AppConfig.IsGPUFix();
            checkGPUFix.CheckedChanged += CheckGPUFix_CheckedChanged;

            toolTip.SetToolTip(control: checkAutoToggleClamshellMode, caption: "Disable sleep on lid close when plugged in and external monitor is connected");

            InitVariBright();
            InitServices();
            InitHibernate();
        }

        private void ComboAPU_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int mem = comboAPU.SelectedIndex;
            Program.acpi.SetAPUMem(memory: mem);

            DialogResult dialogResult = MessageBox.Show(text: Properties.Strings.AlertAPUMemoryRestart, caption: Properties.Strings.AlertAPUMemoryRestartTitle, buttons: MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                Process.Start(fileName: "shutdown", arguments: "/r /t 1");
            }

        }

        private void CheckBootSound_CheckedChanged(object? sender, EventArgs e)
        {
            Program.acpi.DeviceSet(DeviceID: AsusACPI.BootSound, Status: (checkBootSound.Checked ? 1 : 0), logName: "BootSound");
        }

        private void CheckGPUFix_CheckedChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "gpu_fix", value: (checkGPUFix.Checked ? 1 : 0));
        }

        private void InitHibernate()
        {
            try
            {
                int hibernate = PowerNative.GetHibernateAfter();
                if (hibernate < 0 || hibernate > numericHibernateAfter.Maximum) hibernate = 0;
                numericHibernateAfter.Value = hibernate;
                numericHibernateAfter.ValueChanged += NumericHibernateAfter_ValueChanged;

            }
            catch (Exception ex)
            {
                panelPower.Visible = false;
                Logger.WriteLine(logMessage: ex.ToString());
            }

        }

        private void NumericHibernateAfter_ValueChanged(object? sender, EventArgs e)
        {
            PowerNative.SetHibernateAfter(minutes: (int)numericHibernateAfter.Value);
        }

        private void PictureLog_Click(object? sender, EventArgs e)
        {
            new Process
            {
                StartInfo = new ProcessStartInfo(fileName: Logger.logFile)
                {
                    UseShellExecute = true
                }
            }.Start();
        }

        private void SliderBrightness_ValueChanged(object? sender, EventArgs e)
        {
            bool onBattery = SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Online;

            if (onBattery)
                AppConfig.Set(name: "keyboard_brightness_ac", value: sliderBrightness.Value);
            else
                AppConfig.Set(name: "keyboard_brightness", value: sliderBrightness.Value);

            Aura.ApplyBrightness(brightness: sliderBrightness.Value, log: "Slider");
        }

        private void InitServices()
        {

            int servicesCount = OptimizationService.GetRunningCount();

            if (servicesCount > 0)
            {
                buttonServices.Text = Properties.Strings.Stop;
                labelServices.ForeColor = colorTurbo;
            }
            else
            {
                buttonServices.Text = Properties.Strings.Start;
                labelServices.ForeColor = colorStandard;
            }

            labelServices.Text = Properties.Strings.AsusServicesRunning + ":  " + servicesCount;
            buttonServices.Enabled = true;

        }

        public void ServiesToggle()
        {
            buttonServices.Enabled = false;

            if (OptimizationService.GetRunningCount() > 0)
            {
                labelServices.Text = Properties.Strings.StoppingServices + " ...";
                Task.Run(action: () =>
                {
                    OptimizationService.StopAsusServices();
                    BeginInvoke(method: delegate
                    {
                        InitServices();
                    });
                    Program.inputDispatcher.Init();
                });
            }
            else
            {
                labelServices.Text = Properties.Strings.StartingServices + " ...";
                Task.Run(action: () =>
                {
                    OptimizationService.StartAsusServices();
                    BeginInvoke(method: delegate
                    {
                        InitServices();
                    });
                });
            }
        }

        private void ButtonServices_Click(object? sender, EventArgs e)
        {
            if (ProcessHelper.IsUserAdministrator())
                ServiesToggle();
            else
                ProcessHelper.RunAsAdmin(param: "services");
        }

        private void InitVariBright()
        {
            try
            {

                using (var amdControl = new AmdGpuControl())
                {
                    int variBrightSupported = 0, VariBrightEnabled;
                    if (amdControl.GetVariBright(supported: out variBrightSupported, enabled: out VariBrightEnabled))
                    {
                        Logger.WriteLine(logMessage: "Varibright: " + variBrightSupported + "," + VariBrightEnabled);
                        checkVariBright.Checked = (VariBrightEnabled == 3);
                    }

                    checkVariBright.Visible = (variBrightSupported > 0);
                    checkVariBright.CheckedChanged += CheckVariBright_CheckedChanged;
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(message: ex.ToString());
                checkVariBright.Visible = false;
            }


        }

        private void CheckVariBright_CheckedChanged(object? sender, EventArgs e)
        {
            using (var amdControl = new AmdGpuControl())
            {
                amdControl.SetVariBright(enabled: checkVariBright.Checked ? 1 : 0);
                ProcessHelper.KillByName(name: "RadeonSoftware");
            }
        }

        private void CheckGpuApps_CheckedChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "kill_gpu_apps", value: (checkGpuApps.Checked ? 1 : 0));
        }

        private void NumericBacklightTime_ValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "keyboard_timeout", value: (int)numericBacklightTime.Value);
            AppConfig.Set(name: "keyboard_ac_timeout", value: (int)numericBacklightPluggedTime.Value);
            Program.inputDispatcher.InitBacklightTimer();
        }

        private void CheckXMG_CheckedChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "xmg_light", value: (checkXMG.Checked ? 1 : 0));
            XGM.Light(status: checkXMG.Checked);
        }

        private void CheckUSBC_CheckedChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "optimized_usbc", value: (checkUSBC.Checked ? 1 : 0));
        }

        private void PictureHelp_Click(object? sender, EventArgs e)
        {
            Process.Start(startInfo: new ProcessStartInfo(fileName: "https://github.com/seerge/g-helper#custom-hotkey-actions") { UseShellExecute = true });
        }

        private void CheckNoOverdrive_CheckedChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "no_overdrive", value: (checkNoOverdrive.Checked ? 1 : 0));
            screenControl.AutoScreen(force: true);
        }


        private void CheckTopmost_CheckedChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "topmost", value: (checkTopmost.Checked ? 1 : 0));
            Program.settingsForm.TopMost = checkTopmost.Checked;
        }

        private void CheckPower_CheckedChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "keyboard_awake", value: (checkAwake.Checked ? 1 : 0));
            AppConfig.Set(name: "keyboard_boot", value: (checkBoot.Checked ? 1 : 0));
            AppConfig.Set(name: "keyboard_sleep", value: (checkSleep.Checked ? 1 : 0));
            AppConfig.Set(name: "keyboard_shutdown", value: (checkShutdown.Checked ? 1 : 0));

            AppConfig.Set(name: "keyboard_awake_bar", value: (checkAwakeBar.Checked ? 1 : 0));
            AppConfig.Set(name: "keyboard_boot_bar", value: (checkBootBar.Checked ? 1 : 0));
            AppConfig.Set(name: "keyboard_sleep_bar", value: (checkSleepBar.Checked ? 1 : 0));
            AppConfig.Set(name: "keyboard_shutdown_bar", value: (checkShutdownBar.Checked ? 1 : 0));

            AppConfig.Set(name: "keyboard_awake_lid", value: (checkAwakeLid.Checked ? 1 : 0));
            AppConfig.Set(name: "keyboard_boot_lid", value: (checkBootLid.Checked ? 1 : 0));
            AppConfig.Set(name: "keyboard_sleep_lid", value: (checkSleepLid.Checked ? 1 : 0));
            AppConfig.Set(name: "keyboard_shutdown_lid", value: (checkShutdownLid.Checked ? 1 : 0));

            AppConfig.Set(name: "keyboard_awake_logo", value: (checkAwakeLogo.Checked ? 1 : 0));
            AppConfig.Set(name: "keyboard_boot_logo", value: (checkBootLogo.Checked ? 1 : 0));
            AppConfig.Set(name: "keyboard_sleep_logo", value: (checkSleepLogo.Checked ? 1 : 0));
            AppConfig.Set(name: "keyboard_shutdown_logo", value: (checkShutdownLogo.Checked ? 1 : 0));

            Aura.ApplyPower();

        }

        private void ComboKeyboardSpeed_SelectedValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "aura_speed", value: (int)comboKeyboardSpeed.SelectedValue);
            Aura.ApplyAura();
        }


        private void Keyboard_Shown(object? sender, EventArgs e)
        {
            if (Height > Program.settingsForm.Height)
            {
                Top = Program.settingsForm.Top + Program.settingsForm.Height - Height;
            }
            else
            {
                Top = Program.settingsForm.Top;
            }

            Left = Program.settingsForm.Left - Width - 5;
        }


        private void checkAutoToggleClamshellMode_CheckedChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "toggle_clamshell_mode", value: checkAutoToggleClamshellMode.Checked ? 1 : 0);

            if (checkAutoToggleClamshellMode.Checked)
            {
                clamshellControl.ToggleLidAction();
            }
            else
            {
                ClamshellModeControl.DisableClamshellMode();
            }

        }

        private void panelAPU_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
