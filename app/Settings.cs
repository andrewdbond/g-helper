using GHelper.AnimeMatrix;
using GHelper.AutoUpdate;
using GHelper.Battery;
using GHelper.Display;
using GHelper.Fan;
using GHelper.Gpu;
using GHelper.Helpers;
using GHelper.Input;
using GHelper.Mode;
using GHelper.Peripherals;
using GHelper.Peripherals.Mouse;
using GHelper.UI;
using GHelper.USB;
using System.Diagnostics;
using System.Timers;

namespace GHelper
{
    public partial class SettingsForm : RForm
    {
        ContextMenuStrip contextMenuStrip = new CustomContextMenu();
        ToolStripMenuItem menuSilent, menuBalanced, menuTurbo, menuEco, menuStandard, menuUltimate, menuOptimized;

        public GPUModeControl gpuControl;
        ScreenControl screenControl = new ScreenControl();
        AutoUpdateControl updateControl;

        AsusMouseSettings? mouseSettings;

        public AniMatrixControl matrixControl;

        public static System.Timers.Timer sensorTimer = default!;

        public Matrix? matrixForm;
        public Fans? fansForm;
        public Extra? extraForm;
        public Updates? updatesForm;

        static long lastRefresh;
        static long lastBatteryRefresh;
        static long lastLostFocus;

        bool isGpuSection = true;

        bool batteryMouseOver = false;
        bool batteryFullMouseOver = false;

        public SettingsForm()
        {

            InitializeComponent();
            InitTheme(setDPI: true);

            gpuControl = new GPUModeControl(settingsForm: this);
            updateControl = new AutoUpdateControl(settingsForm: this);
            matrixControl = new AniMatrixControl(settingsForm: this);

            buttonSilent.Text = Properties.Strings.Silent;
            buttonBalanced.Text = Properties.Strings.Balanced;
            buttonTurbo.Text = Properties.Strings.Turbo;
            buttonFans.Text = Properties.Strings.FansPower;

            buttonEco.Text = Properties.Strings.EcoMode;
            buttonUltimate.Text = Properties.Strings.UltimateMode;
            buttonStandard.Text = Properties.Strings.StandardMode;
            buttonOptimized.Text = Properties.Strings.Optimized;
            buttonStopGPU.Text = Properties.Strings.StopGPUApps;

            buttonScreenAuto.Text = Properties.Strings.AutoMode;
            buttonMiniled.Text = Properties.Strings.Multizone;

            buttonKeyboardColor.Text = Properties.Strings.Color;
            buttonKeyboard.Text = Properties.Strings.Extra;

            labelPerf.Text = Properties.Strings.PerformanceMode;
            labelGPU.Text = Properties.Strings.GPUMode;
            labelSreen.Text = Properties.Strings.LaptopScreen;
            labelKeyboard.Text = Properties.Strings.LaptopKeyboard;
            labelMatrix.Text = Properties.Strings.AnimeMatrix;
            labelBatteryTitle.Text = Properties.Strings.BatteryChargeLimit;
            labelPeripherals.Text = Properties.Strings.Peripherals;

            checkMatrix.Text = Properties.Strings.TurnOffOnBattery;
            checkStartup.Text = Properties.Strings.RunOnStartup;

            buttonMatrix.Text = Properties.Strings.PictureGif;
            buttonQuit.Text = Properties.Strings.Quit;
            buttonUpdates.Text = Properties.Strings.Updates;

            FormClosing += SettingsForm_FormClosing;
            Deactivate += SettingsForm_LostFocus;

            buttonSilent.BorderColor = colorEco;
            buttonBalanced.BorderColor = colorStandard;
            buttonTurbo.BorderColor = colorTurbo;
            buttonFans.BorderColor = colorCustom;

            buttonEco.BorderColor = colorEco;
            buttonStandard.BorderColor = colorStandard;
            buttonUltimate.BorderColor = colorTurbo;
            buttonOptimized.BorderColor = colorEco;
            buttonXGM.BorderColor = colorTurbo;

            button60Hz.BorderColor = SystemColors.ActiveBorder;
            button120Hz.BorderColor = SystemColors.ActiveBorder;
            buttonScreenAuto.BorderColor = SystemColors.ActiveBorder;
            buttonMiniled.BorderColor = colorTurbo;

            buttonSilent.Click += ButtonSilent_Click;
            buttonBalanced.Click += ButtonBalanced_Click;
            buttonTurbo.Click += ButtonTurbo_Click;

            buttonEco.Click += ButtonEco_Click;
            buttonStandard.Click += ButtonStandard_Click;
            buttonUltimate.Click += ButtonUltimate_Click;
            buttonOptimized.Click += ButtonOptimized_Click;
            buttonStopGPU.Click += ButtonStopGPU_Click;

            VisibleChanged += SettingsForm_VisibleChanged;

            button60Hz.Click += Button60Hz_Click;
            button120Hz.Click += Button120Hz_Click;
            buttonScreenAuto.Click += ButtonScreenAuto_Click;
            buttonMiniled.Click += ButtonMiniled_Click;

            buttonQuit.Click += ButtonQuit_Click;

            buttonKeyboardColor.Click += ButtonKeyboardColor_Click;

            buttonFans.Click += ButtonFans_Click;
            buttonKeyboard.Click += ButtonKeyboard_Click;

            pictureColor.Click += PictureColor_Click;
            pictureColor2.Click += PictureColor2_Click;

            labelCPUFan.Click += LabelCPUFan_Click;
            labelGPUFan.Click += LabelCPUFan_Click;

            comboMatrix.DropDownStyle = ComboBoxStyle.DropDownList;
            comboMatrixRunning.DropDownStyle = ComboBoxStyle.DropDownList;

            comboMatrix.DropDownClosed += ComboMatrix_SelectedValueChanged;
            comboMatrixRunning.DropDownClosed += ComboMatrixRunning_SelectedValueChanged;

            buttonMatrix.Click += ButtonMatrix_Click;

            checkStartup.Checked = Startup.IsScheduled();
            checkStartup.CheckedChanged += CheckStartup_CheckedChanged;

            labelVersion.Click += LabelVersion_Click;
            labelVersion.ForeColor = Color.FromArgb(alpha: 128, baseColor: Color.Gray);

            buttonOptimized.MouseMove += ButtonOptimized_MouseHover;
            buttonOptimized.MouseLeave += ButtonGPU_MouseLeave;

            buttonEco.MouseMove += ButtonEco_MouseHover;
            buttonEco.MouseLeave += ButtonGPU_MouseLeave;

            buttonStandard.MouseMove += ButtonStandard_MouseHover;
            buttonStandard.MouseLeave += ButtonGPU_MouseLeave;

            buttonUltimate.MouseMove += ButtonUltimate_MouseHover;
            buttonUltimate.MouseLeave += ButtonGPU_MouseLeave;

            tableGPU.MouseMove += ButtonXGM_MouseMove;
            tableGPU.MouseLeave += ButtonGPU_MouseLeave;

            buttonXGM.Click += ButtonXGM_Click;

            buttonScreenAuto.MouseMove += ButtonScreenAuto_MouseHover;
            buttonScreenAuto.MouseLeave += ButtonScreen_MouseLeave;

            button60Hz.MouseMove += Button60Hz_MouseHover;
            button60Hz.MouseLeave += ButtonScreen_MouseLeave;

            button120Hz.MouseMove += Button120Hz_MouseHover;
            button120Hz.MouseLeave += ButtonScreen_MouseLeave;

            buttonUpdates.Click += ButtonUpdates_Click;

            sliderBattery.ValueChanged += SliderBattery_ValueChanged;
            Program.trayIcon.MouseMove += TrayIcon_MouseMove;

            sensorTimer = new System.Timers.Timer(interval: 1000);
            sensorTimer.Elapsed += OnTimedEvent;
            sensorTimer.Enabled = true;

            labelCharge.MouseEnter += PanelBattery_MouseEnter;
            labelCharge.MouseLeave += PanelBattery_MouseLeave;

            buttonPeripheral1.Click += ButtonPeripheral_Click;
            buttonPeripheral2.Click += ButtonPeripheral_Click;
            buttonPeripheral3.Click += ButtonPeripheral_Click;

            buttonPeripheral1.MouseEnter += ButtonPeripheral_MouseEnter;
            buttonPeripheral2.MouseEnter += ButtonPeripheral_MouseEnter;
            buttonPeripheral3.MouseEnter += ButtonPeripheral_MouseEnter;

            buttonBatteryFull.MouseEnter += ButtonBatteryFull_MouseEnter;
            buttonBatteryFull.MouseLeave += ButtonBatteryFull_MouseLeave;
            buttonBatteryFull.Click += ButtonBatteryFull_Click;

            Text = "G-Helper " + (ProcessHelper.IsUserAdministrator() ? "—" : "-") + " " + AppConfig.GetModelShort();
            TopMost = AppConfig.Is(name: "topmost");

            //This will auto position the window again when it resizes. Might mess with position if people drag the window somewhere else.
            this.Resize += SettingsForm_Resize;
            SetContextMenu();

            VisualiseFnLock();
            buttonFnLock.Click += ButtonFnLock_Click;

            panelPerformance.Focus();
        }

        private void SettingsForm_LostFocus(object? sender, EventArgs e)
        {
            lastLostFocus = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        private void ButtonBatteryFull_Click(object? sender, EventArgs e)
        {
            BatteryControl.ToggleBatteryLimitFull();
        }

        private void ButtonBatteryFull_MouseLeave(object? sender, EventArgs e)
        {
            batteryFullMouseOver = false;
            RefreshSensors(force: true);
        }

        private void ButtonBatteryFull_MouseEnter(object? sender, EventArgs e)
        {
            batteryFullMouseOver = true;
            labelCharge.Text = Properties.Strings.BatteryLimitFull;
        }

        private void SettingsForm_Resize(object? sender, EventArgs e)
        {
            Left = Screen.FromControl(control: this).WorkingArea.Width - 10 - Width;
            Top = Screen.FromControl(control: this).WorkingArea.Height - 10 - Height;
        }

        private void PanelBattery_MouseEnter(object? sender, EventArgs e)
        {
            batteryMouseOver = true;
            ShowBatteryWear();
        }

        private void PanelBattery_MouseLeave(object? sender, EventArgs e)
        {
            batteryMouseOver = false;
            RefreshSensors(force: true);
        }

        private void ShowBatteryWear()
        {
            //Refresh again only after 15 Minutes since the last refresh
            if (lastBatteryRefresh == 0 || Math.Abs(value: DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastBatteryRefresh) > 15 * 60_000)
            {
                lastBatteryRefresh = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                HardwareControl.RefreshBatteryHealth();
            }

            if (HardwareControl.batteryHealth != -1)
            {
                labelCharge.Text = Properties.Strings.BatteryHealth + ": " + Math.Round(d: HardwareControl.batteryHealth, decimals: 1) + "%";
            }
        }

        private void SettingsForm_VisibleChanged(object? sender, EventArgs e)
        {
            sensorTimer.Enabled = this.Visible;
            if (this.Visible)
            {
                screenControl.InitScreen();
                VisualizeXGM();

                Task.Run(action: (Action)RefreshPeripheralsBattery);
                updateControl.CheckForUpdates();
            }
        }

        private void RefreshPeripheralsBattery()
        {
            PeripheralsProvider.RefreshBatteryForAllDevices(force: true);
        }

        private void ButtonUpdates_Click(object? sender, EventArgs e)
        {
            if (updatesForm == null || updatesForm.Text == "")
            {
                updatesForm = new Updates();
                AddOwnedForm(ownedForm: updatesForm);
            }

            if (updatesForm.Visible)
            {
                updatesForm.Close();
            }
            else
            {
                updatesForm.Show();
            }
        }

        public void VisualiseMatrix(string image)
        {
            if (matrixForm == null || matrixForm.Text == "") return;
            matrixForm.VisualiseMatrix(fileName: image);
        }

        protected override void WndProc(ref Message m)
        {

            switch (m.Msg)
            {
                case NativeMethods.WM_POWERBROADCAST:
                    if (m.WParam == (IntPtr)NativeMethods.PBT_POWERSETTINGCHANGE)
                    {
                        var settings = (NativeMethods.POWERBROADCAST_SETTING)m.GetLParam(cls: typeof(NativeMethods.POWERBROADCAST_SETTING));
                        switch (settings.Data)
                        {
                            case 0:
                                Logger.WriteLine(logMessage: "Monitor Power Off");
                                Aura.ApplyBrightness(brightness: 0);
                                break;
                            case 1:
                                Logger.WriteLine(logMessage: "Monitor Power On");
                                Program.SetAutoModes();
                                break;
                            case 2:
                                Logger.WriteLine(logMessage: "Monitor Dimmed");
                                break;
                        }
                    }
                    m.Result = (IntPtr)1;
                    break;
            }

            try
            {
                base.WndProc(m: ref m);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(message: ex.ToString());
            }
        }

        public void SetContextMenu()
        {

            var mode = Modes.GetCurrent();

            contextMenuStrip.Items.Clear();
            Padding padding = new Padding(left: 15, top: 5, right: 5, bottom: 5);

            var title = new ToolStripMenuItem(text: Properties.Strings.PerformanceMode);
            title.Margin = padding;
            title.Enabled = false;
            contextMenuStrip.Items.Add(value: title);

            menuSilent = new ToolStripMenuItem(text: Properties.Strings.Silent);
            menuSilent.Click += ButtonSilent_Click;
            menuSilent.Margin = padding;
            menuSilent.Checked = (mode == AsusACPI.PerformanceSilent);
            contextMenuStrip.Items.Add(value: menuSilent);

            menuBalanced = new ToolStripMenuItem(text: Properties.Strings.Balanced);
            menuBalanced.Click += ButtonBalanced_Click;
            menuBalanced.Margin = padding;
            menuBalanced.Checked = (mode == AsusACPI.PerformanceBalanced);
            contextMenuStrip.Items.Add(value: menuBalanced);

            menuTurbo = new ToolStripMenuItem(text: Properties.Strings.Turbo);
            menuTurbo.Click += ButtonTurbo_Click;
            menuTurbo.Margin = padding;
            menuTurbo.Checked = (mode == AsusACPI.PerformanceTurbo);
            contextMenuStrip.Items.Add(value: menuTurbo);

            contextMenuStrip.Items.Add(text: "-");

            if (isGpuSection)
            {
                var titleGPU = new ToolStripMenuItem(text: Properties.Strings.GPUMode);
                titleGPU.Margin = padding;
                titleGPU.Enabled = false;
                contextMenuStrip.Items.Add(value: titleGPU);

                menuEco = new ToolStripMenuItem(text: Properties.Strings.EcoMode);
                menuEco.Click += ButtonEco_Click;
                menuEco.Margin = padding;
                contextMenuStrip.Items.Add(value: menuEco);

                menuStandard = new ToolStripMenuItem(text: Properties.Strings.StandardMode);
                menuStandard.Click += ButtonStandard_Click;
                menuStandard.Margin = padding;
                contextMenuStrip.Items.Add(value: menuStandard);

                menuUltimate = new ToolStripMenuItem(text: Properties.Strings.UltimateMode);
                menuUltimate.Click += ButtonUltimate_Click;
                menuUltimate.Margin = padding;
                contextMenuStrip.Items.Add(value: menuUltimate);

                menuOptimized = new ToolStripMenuItem(text: Properties.Strings.Optimized);
                menuOptimized.Click += ButtonOptimized_Click;
                menuOptimized.Margin = padding;
                contextMenuStrip.Items.Add(value: menuOptimized);

                contextMenuStrip.Items.Add(text: "-");
            }


            var quit = new ToolStripMenuItem(text: Properties.Strings.Quit);
            quit.Click += ButtonQuit_Click;
            quit.Margin = padding;
            contextMenuStrip.Items.Add(value: quit);

            //contextMenuStrip.ShowCheckMargin = true;
            contextMenuStrip.RenderMode = ToolStripRenderMode.System;

            if (darkTheme)
            {
                contextMenuStrip.BackColor = this.BackColor;
                contextMenuStrip.ForeColor = this.ForeColor;
            }

            Program.trayIcon.ContextMenuStrip = contextMenuStrip;


        }

        private void ButtonXGM_Click(object? sender, EventArgs e)
        {
            gpuControl.ToggleXGM();
        }

        private void SliderBattery_ValueChanged(object? sender, EventArgs e)
        {
            BatteryControl.SetBatteryChargeLimit(limit: sliderBattery.Value);
        }


        public void SetVersionLabel(string label, bool update = false)
        {
            Invoke(method: delegate
            {
                labelVersion.Text = label;
                if (update) labelVersion.ForeColor = colorTurbo;
            });
        }


        private void LabelVersion_Click(object? sender, EventArgs e)
        {
            updateControl.LoadReleases();
        }

        private static void TrayIcon_MouseMove(object? sender, MouseEventArgs e)
        {
            Program.settingsForm.RefreshSensors();
        }


        private static void OnTimedEvent(Object? source, ElapsedEventArgs? e)
        {
            Program.settingsForm.RefreshSensors();
        }

        private void Button120Hz_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = Properties.Strings.MaxRefreshTooltip;
        }

        private void Button60Hz_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = Properties.Strings.MinRefreshTooltip;
        }

        private void ButtonScreen_MouseLeave(object? sender, EventArgs e)
        {
            labelTipScreen.Text = "";
        }

        private void ButtonScreenAuto_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = Properties.Strings.AutoRefreshTooltip;
        }

        private void ButtonUltimate_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.UltimateGPUTooltip;
        }

        private void ButtonStandard_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.StandardGPUTooltip;
        }

        private void ButtonEco_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.EcoGPUTooltip;
        }

        private void ButtonOptimized_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.OptimizedGPUTooltip;
        }

        private void ButtonGPU_MouseLeave(object? sender, EventArgs e)
        {
            labelTipGPU.Text = "";
        }

        private void ButtonXGM_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is null) return;
            TableLayoutPanel table = (TableLayoutPanel)sender;

            if (!buttonXGM.Visible) return;

            labelTipGPU.Text = buttonXGM.Bounds.Contains(pt: table.PointToClient(p: Cursor.Position)) ?
                "XGMobile toggle works only in Standard mode" : "";

        }


        private void ButtonScreenAuto_Click(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "screen_auto", value: 1);
            screenControl.AutoScreen();
        }


        private void CheckStartup_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is null) return;
            CheckBox chk = (CheckBox)sender;

            if (chk.Checked)
                Startup.Schedule();
            else
                Startup.UnSchedule();
        }

        private void CheckMatrix_CheckedChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "matrix_auto", value: checkMatrix.Checked ? 1 : 0);
            matrixControl.SetMatrix();
        }



        private void ButtonMatrix_Click(object? sender, EventArgs e)
        {

            if (matrixForm == null || matrixForm.Text == "")
            {
                matrixForm = new Matrix();
                AddOwnedForm(ownedForm: matrixForm);
            }

            if (matrixForm.Visible)
            {
                matrixForm.Close();
            }
            else
            {
                matrixForm.FormPosition();
                matrixForm.Show();
            }

        }

        public void SetMatrixRunning(int mode)
        {
            Invoke(method: delegate
            {
                comboMatrixRunning.SelectedIndex = mode;
                if (comboMatrix.SelectedIndex == 0) comboMatrix.SelectedIndex = 3;
            });
        }

        private void ComboMatrixRunning_SelectedValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "matrix_running", value: comboMatrixRunning.SelectedIndex);
            matrixControl.SetMatrix();
        }


        private void ComboMatrix_SelectedValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "matrix_brightness", value: comboMatrix.SelectedIndex);
            matrixControl.SetMatrix();
        }


        private void LabelCPUFan_Click(object? sender, EventArgs e)
        {
            FanSensorControl.fanRpm = !FanSensorControl.fanRpm;
            RefreshSensors(force: true);
        }

        private void PictureColor2_Click(object? sender, EventArgs e)
        {

            ColorDialog colorDlg = new ColorDialog();
            colorDlg.AllowFullOpen = true;
            colorDlg.Color = pictureColor2.BackColor;

            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                AppConfig.Set(name: "aura_color2", value: colorDlg.Color.ToArgb());
                SetAura();
            }
        }

        private void PictureColor_Click(object? sender, EventArgs e)
        {
            buttonKeyboardColor.PerformClick();
        }

        private void ButtonKeyboard_Click(object? sender, EventArgs e)
        {
            if (extraForm == null || extraForm.Text == "")
            {
                extraForm = new Extra();
                AddOwnedForm(ownedForm: extraForm);
            }

            if (extraForm.Visible)
            {
                extraForm.Close();
            }
            else
            {
                extraForm.Show();
            }
        }

        public void FansInit()
        {
            Invoke(method: delegate
            {
                if (fansForm != null && fansForm.Text != "") fansForm.InitAll();
            });
        }

        public void GPUInit()
        {
            Invoke(method: delegate
            {
                if (fansForm != null && fansForm.Text != "") fansForm.InitGPU();
            });
        }

        public void FansToggle(int index = 0)
        {
            if (fansForm == null || fansForm.Text == "")
            {
                fansForm = new Fans();
                AddOwnedForm(ownedForm: fansForm);
            }

            if (fansForm.Visible)
            {
                fansForm.Close();
            }
            else
            {
                fansForm.FormPosition();
                fansForm.Show();
                fansForm.ToggleNavigation(index: index);
            }

        }

        private void ButtonFans_Click(object? sender, EventArgs e)
        {
            FansToggle();
        }

        private void ButtonKeyboardColor_Click(object? sender, EventArgs e)
        {

            ColorDialog colorDlg = new ColorDialog();
            colorDlg.AllowFullOpen = true;
            colorDlg.Color = pictureColor.BackColor;

            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                AppConfig.Set(name: "aura_color", value: colorDlg.Color.ToArgb());
                SetAura();
            }
        }

        public void InitAura()
        {
            Aura.Mode = (AuraMode)AppConfig.Get(name: "aura_mode");
            Aura.Speed = (AuraSpeed)AppConfig.Get(name: "aura_speed");
            Aura.SetColor(colorCode: AppConfig.Get(name: "aura_color"));
            Aura.SetColor2(colorCode: AppConfig.Get(name: "aura_color2"));

            comboKeyboard.DropDownStyle = ComboBoxStyle.DropDownList;
            comboKeyboard.DataSource = new BindingSource(dataSource: Aura.GetModes(), dataMember: null);
            comboKeyboard.DisplayMember = "Value";
            comboKeyboard.ValueMember = "Key";
            comboKeyboard.SelectedValue = Aura.Mode;
            comboKeyboard.SelectedValueChanged += ComboKeyboard_SelectedValueChanged;


            if (AppConfig.IsSingleColor())
            {
                panelColor.Visible = false;
            }

            if (AppConfig.NoAura())
            {
                comboKeyboard.Visible = false;
            }

            VisualiseAura();

        }

        public void SetAura()
        {
            Task.Run(action: () =>
            {
                Aura.ApplyAura();
                VisualiseAura();
            });
        }

        public void VisualiseAura()
        {
            Invoke(method: delegate
            {
                pictureColor.BackColor = Aura.Color1;
                pictureColor2.BackColor = Aura.Color2;
                pictureColor2.Visible = Aura.HasSecondColor();
            });
        }

        public void InitMatrix()
        {

            if (!matrixControl.IsValid)
            {
                panelMatrix.Visible = false;
                return;
            }

            comboMatrix.SelectedIndex = Math.Min(val1: AppConfig.Get(name: "matrix_brightness", empty: 0), val2: comboMatrix.Items.Count - 1);
            comboMatrixRunning.SelectedIndex = Math.Min(val1: AppConfig.Get(name: "matrix_running", empty: 0), val2: comboMatrixRunning.Items.Count - 1);

            checkMatrix.Checked = AppConfig.Is(name: "matrix_auto");
            checkMatrix.CheckedChanged += CheckMatrix_CheckedChanged;

        }


        public void CycleMatrix(int delta)
        {
            comboMatrix.SelectedIndex = Math.Min(val1: Math.Max(val1: 0, val2: comboMatrix.SelectedIndex + delta), val2: comboMatrix.Items.Count - 1);
            AppConfig.Set(name: "matrix_brightness", value: comboMatrix.SelectedIndex);
            matrixControl.SetMatrix();
            Program.toast.RunToast(text: comboMatrix.GetItemText(item: comboMatrix.SelectedItem), icon: delta > 0 ? ToastIcon.BacklightUp : ToastIcon.BacklightDown);
        }


        public void CycleAuraMode()
        {
            if (comboKeyboard.SelectedIndex < comboKeyboard.Items.Count - 1)
                comboKeyboard.SelectedIndex += 1;
            else
                comboKeyboard.SelectedIndex = 0;

            Program.toast.RunToast(text: comboKeyboard.GetItemText(item: comboKeyboard.SelectedItem), icon: ToastIcon.BacklightUp);
        }

        private void ComboKeyboard_SelectedValueChanged(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "aura_mode", value: (int)comboKeyboard.SelectedValue);
            SetAura();
        }


        private void Button120Hz_Click(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "screen_auto", value: 0);
            screenControl.SetScreen(frequency: ScreenControl.MAX_REFRESH, overdrive: 1);
        }

        private void Button60Hz_Click(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "screen_auto", value: 0);
            screenControl.SetScreen(frequency: 60, overdrive: 0);
        }


        private void ButtonMiniled_Click(object? sender, EventArgs e)
        {
            screenControl.ToogleMiniled();
        }



        public void VisualiseScreen(bool screenEnabled, bool screenAuto, int frequency, int maxFrequency, int overdrive, bool overdriveSetting, int miniled, bool hdr)
        {

            ButtonEnabled(but: button60Hz, enabled: screenEnabled);
            ButtonEnabled(but: button120Hz, enabled: screenEnabled);
            ButtonEnabled(but: buttonScreenAuto, enabled: screenEnabled);
            ButtonEnabled(but: buttonMiniled, enabled: screenEnabled);

            labelSreen.Text = screenEnabled
                ? Properties.Strings.LaptopScreen + ": " + frequency + "Hz" + ((overdrive == 1) ? " + " + Properties.Strings.Overdrive : "")
                : Properties.Strings.LaptopScreen + ": " + Properties.Strings.TurnedOff;

            button60Hz.Activated = false;
            button120Hz.Activated = false;
            buttonScreenAuto.Activated = false;

            if (screenAuto)
            {
                buttonScreenAuto.Activated = true;
            }
            else if (frequency == 60)
            {
                button60Hz.Activated = true;
            }
            else if (frequency > 60)
            {
                button120Hz.Activated = true;
            }

            if (maxFrequency > 60)
            {
                button120Hz.Text = maxFrequency.ToString() + "Hz" + (overdriveSetting ? " + OD" : "");
                panelScreen.Visible = true;
            }
            else if (maxFrequency > 0)
            {
                panelScreen.Visible = false;
            }

            if (miniled >= 0)
            {
                buttonMiniled.Activated = (miniled == 1) || hdr;
                buttonMiniled.Enabled = !hdr;
            }
            else
            {
                buttonMiniled.Visible = false;
            }

        }

        private void ButtonQuit_Click(object? sender, EventArgs e)
        {
            matrixControl.Dispose();
            Close();
            Program.trayIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// Closes all forms except the settings. Hides the settings
        /// </summary>
        public void HideAll()
        {
            this.Hide();
            if (fansForm != null && fansForm.Text != "") fansForm.Close();
            if (extraForm != null && extraForm.Text != "") extraForm.Close();
            if (updatesForm != null && updatesForm.Text != "") updatesForm.Close();
            if (matrixForm != null && matrixForm.Text != "") matrixForm.Close();
        }

        /// <summary>
        /// Brings all visible windows to the top, with settings being the focus
        /// </summary>
        public void ShowAll()
        {
            this.Activate();
        }

        /// <summary>
        /// Check if any of fans, keyboard, update, or itself has focus
        /// </summary>
        /// <returns>Focus state</returns>
        public bool HasAnyFocus(bool lostFocusCheck = false)
        {
            return (fansForm != null && fansForm.ContainsFocus) ||
                   (extraForm != null && extraForm.ContainsFocus) ||
                   (updatesForm != null && updatesForm.ContainsFocus) ||
                   (matrixForm != null && matrixForm.ContainsFocus) ||
                   this.ContainsFocus ||
                   (lostFocusCheck && Math.Abs(value: DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastLostFocus) < 300);
        }

        private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideAll();
            }
        }

        private void ButtonUltimate_Click(object? sender, EventArgs e)
        {
            gpuControl.SetGPUMode(GPUMode: AsusACPI.GPUModeUltimate);
        }

        private void ButtonStandard_Click(object? sender, EventArgs e)
        {
            gpuControl.SetGPUMode(GPUMode: AsusACPI.GPUModeStandard);
        }

        private void ButtonEco_Click(object? sender, EventArgs e)
        {
            gpuControl.SetGPUMode(GPUMode: AsusACPI.GPUModeEco);
        }


        private void ButtonOptimized_Click(object? sender, EventArgs e)
        {
            AppConfig.Set(name: "gpu_auto", value: (AppConfig.Get(name: "gpu_auto") == 1) ? 0 : 1);
            VisualiseGPUMode();
            gpuControl.AutoGPUMode(optimized: true);
        }

        private void ButtonStopGPU_Click(object? sender, EventArgs e)
        {
            gpuControl.KillGPUApps();
        }

        public async void RefreshSensors(bool force = false)
        {

            if (!force && Math.Abs(value: DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastRefresh) < 2000) return;
            lastRefresh = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            string cpuTemp = "";
            string gpuTemp = "";
            string battery = "";
            string charge = "";

            HardwareControl.ReadSensors();
            Task.Run(action: (Action)PeripheralsProvider.RefreshBatteryForAllDevices);

            if (HardwareControl.cpuTemp > 0)
                cpuTemp = ": " + Math.Round(d: (decimal)HardwareControl.cpuTemp).ToString() + "°C";

            if (HardwareControl.batteryCapacity > 0)
                charge = Properties.Strings.BatteryCharge + ": " + Math.Round(d: HardwareControl.batteryCapacity, decimals: 1) + "% ";

            if (HardwareControl.batteryRate < 0)
                battery = Properties.Strings.Discharging + ": " + Math.Round(d: -(decimal)HardwareControl.batteryRate, decimals: 1).ToString() + "W";
            else if (HardwareControl.batteryRate > 0)
                battery = Properties.Strings.Charging + ": " + Math.Round(d: (decimal)HardwareControl.batteryRate, decimals: 1).ToString() + "W";


            if (HardwareControl.gpuTemp > 0)
            {
                gpuTemp = $": {HardwareControl.gpuTemp}°C";
            }

            string trayTip = "CPU" + cpuTemp + " " + HardwareControl.cpuFan;
            if (gpuTemp.Length > 0) trayTip += "\nGPU" + gpuTemp + " " + HardwareControl.gpuFan;
            if (battery.Length > 0) trayTip += "\n" + battery;

            Program.settingsForm.BeginInvoke(method: delegate
            {
                labelCPUFan.Text = "CPU" + cpuTemp + " " + HardwareControl.cpuFan;
                labelGPUFan.Text = "GPU" + gpuTemp + " " + HardwareControl.gpuFan;
                if (HardwareControl.midFan is not null)
                    labelMidFan.Text = "Mid " + HardwareControl.midFan;

                labelBattery.Text = battery;
                if (!batteryMouseOver && !batteryFullMouseOver) labelCharge.Text = charge;

                //panelPerformance.AccessibleName = labelPerf.Text + " " + trayTip;
            });


            Program.trayIcon.Text = trayTip;

        }

        public void LabelFansResult(string text)
        {
            if (fansForm != null && fansForm.Text != "")
                fansForm.LabelFansResult(text: text);
        }

        public void ShowMode(int mode)
        {
            Invoke(method: delegate
            {
                buttonSilent.Activated = false;
                buttonBalanced.Activated = false;
                buttonTurbo.Activated = false;
                buttonFans.Activated = false;

                menuSilent.Checked = false;
                menuBalanced.Checked = false;
                menuTurbo.Checked = false;

                switch (mode)
                {
                    case AsusACPI.PerformanceSilent:
                        buttonSilent.Activated = true;
                        menuSilent.Checked = true;
                        break;
                    case AsusACPI.PerformanceTurbo:
                        buttonTurbo.Activated = true;
                        menuTurbo.Checked = true;
                        break;
                    case AsusACPI.PerformanceBalanced:
                        buttonBalanced.Activated = true;
                        menuBalanced.Checked = true;
                        break;
                    default:
                        buttonFans.Activated = true;
                        buttonFans.BorderColor = Modes.GetBase(mode: mode) switch
                        {
                            AsusACPI.PerformanceSilent => colorEco,
                            AsusACPI.PerformanceTurbo => colorTurbo,
                            _ => colorStandard,
                        };
                        break;
                }
            });
        }


        public void SetModeLabel(string modeText)
        {
            Invoke(method: delegate
            {
                labelPerf.Text = modeText;
                panelPerformance.AccessibleName = labelPerf.Text; // + ". " + Program.trayIcon.Text;
            });
        }


        public void AutoKeyboard()
        {

            if (!AppConfig.Is(name: "skip_aura"))
            {
                Aura.ApplyPower();
                Aura.ApplyAura();
            }

            InputDispatcher.SetBacklightAuto(init: true);

            if (Program.acpi.IsXGConnected())
                XGM.Light(status: AppConfig.Is(name: "xmg_light"));

            if (AppConfig.HasTabletMode()) InputDispatcher.TabletMode();

        }


        public void VisualizeXGM(int GPUMode = -1)
        {

            bool connected = Program.acpi.IsXGConnected();
            buttonXGM.Enabled = buttonXGM.Visible = connected;

            if (!connected) return;

            if (GPUMode != -1)
                ButtonEnabled(but: buttonXGM, enabled: AppConfig.IsNoGPUModes() || GPUMode != AsusACPI.GPUModeEco);


            int activated = Program.acpi.DeviceGet(DeviceID: AsusACPI.GPUXG);
            Logger.WriteLine(logMessage: "XGM Activated flag: " + activated);

            buttonXGM.Activated = activated == 1;

            if (activated == 1)
            {
                ButtonEnabled(but: buttonOptimized, enabled: false);
                ButtonEnabled(but: buttonEco, enabled: false);
                ButtonEnabled(but: buttonStandard, enabled: false);
                ButtonEnabled(but: buttonUltimate, enabled: false);
            }
            else
            {
                ButtonEnabled(but: buttonOptimized, enabled: true);
                ButtonEnabled(but: buttonEco, enabled: true);
                ButtonEnabled(but: buttonStandard, enabled: true);
                ButtonEnabled(but: buttonUltimate, enabled: true);
            }

        }

        public void VisualiseGPUButtons(bool eco = true, bool ultimate = true)
        {
            if (!eco)
            {
                menuEco.Visible = buttonEco.Visible = false;
                menuOptimized.Visible = buttonOptimized.Visible = false;
                buttonStopGPU.Visible = true;
                tableGPU.ColumnCount = 3;
                tableScreen.ColumnCount = 3;
            }
            else
            {
                buttonStopGPU.Visible = false;
            }

            if (!ultimate)
            {
                menuUltimate.Visible = buttonUltimate.Visible = false;
                tableGPU.ColumnCount = 3;
                tableScreen.ColumnCount = 3;
            }
        }

        public void HideGPUModes(bool gpuExists)
        {
            isGpuSection = false;

            buttonEco.Visible = false;
            buttonStandard.Visible = false;
            buttonUltimate.Visible = false;
            buttonOptimized.Visible = false;
            buttonStopGPU.Visible = true;

            tableGPU.ColumnCount = 0;

            SetContextMenu();

            panelGPU.Visible = gpuExists;

        }


        public void LockGPUModes(string text = null)
        {
            Invoke(method: delegate
            {
                if (text is null) text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUChanging + " ...";

                ButtonEnabled(but: buttonOptimized, enabled: false);
                ButtonEnabled(but: buttonEco, enabled: false);
                ButtonEnabled(but: buttonStandard, enabled: false);
                ButtonEnabled(but: buttonUltimate, enabled: false);
                ButtonEnabled(but: buttonXGM, enabled: false);

                labelGPU.Text = text;
            });
        }

        public void VisualiseGPUMode(int GPUMode = -1)
        {
            ButtonEnabled(but: buttonOptimized, enabled: true);
            ButtonEnabled(but: buttonEco, enabled: true);
            ButtonEnabled(but: buttonStandard, enabled: true);
            ButtonEnabled(but: buttonUltimate, enabled: true);

            if (GPUMode == -1)
                GPUMode = AppConfig.Get(name: "gpu_mode");

            bool GPUAuto = AppConfig.Is(name: "gpu_auto");

            buttonEco.Activated = false;
            buttonStandard.Activated = false;
            buttonUltimate.Activated = false;
            buttonOptimized.Activated = false;

            switch (GPUMode)
            {
                case AsusACPI.GPUModeEco:
                    buttonOptimized.BorderColor = colorEco;
                    buttonEco.Activated = !GPUAuto;
                    buttonOptimized.Activated = GPUAuto;
                    labelGPU.Text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUModeEco;
                    Program.trayIcon.Icon = Properties.Resources.eco;
                    IconHelper.SetIcon(form: this, icon: Properties.Resources.dot_eco);
                    break;
                case AsusACPI.GPUModeUltimate:
                    buttonUltimate.Activated = true;
                    labelGPU.Text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUModeUltimate;
                    Program.trayIcon.Icon = Properties.Resources.ultimate;
                    IconHelper.SetIcon(form: this, icon: Properties.Resources.dot_ultimate);
                    break;
                default:
                    buttonOptimized.BorderColor = colorStandard;
                    buttonStandard.Activated = !GPUAuto;
                    buttonOptimized.Activated = GPUAuto;
                    labelGPU.Text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUModeStandard;
                    Program.trayIcon.Icon = Properties.Resources.standard;
                    IconHelper.SetIcon(form: this, icon: Properties.Resources.dot_standard);
                    break;
            }

            VisualizeXGM(GPUMode: GPUMode);


            if (isGpuSection)
            {
                menuEco.Checked = buttonEco.Activated;
                menuStandard.Checked = buttonStandard.Activated;
                menuUltimate.Checked = buttonUltimate.Activated;
                menuOptimized.Checked = buttonOptimized.Activated;
            }

        }


        private void ButtonSilent_Click(object? sender, EventArgs e)
        {
            Program.modeControl.SetPerformanceMode(mode: AsusACPI.PerformanceSilent);
        }

        private void ButtonBalanced_Click(object? sender, EventArgs e)
        {
            Program.modeControl.SetPerformanceMode(mode: AsusACPI.PerformanceBalanced);
        }

        private void ButtonTurbo_Click(object? sender, EventArgs e)
        {
            Program.modeControl.SetPerformanceMode(mode: AsusACPI.PerformanceTurbo);
        }


        public void ButtonEnabled(RButton but, bool enabled)
        {
            but.Enabled = enabled;
            but.BackColor = but.Enabled ? Color.FromArgb(alpha: 255, baseColor: but.BackColor) : Color.FromArgb(alpha: 100, baseColor: but.BackColor);
        }

        public void VisualiseBattery(int limit)
        {
            labelBatteryTitle.Text = Properties.Strings.BatteryChargeLimit + ": " + limit.ToString() + "%";
            sliderBattery.Value = limit;
            VisualiseBatteryFull();
        }

        public void VisualiseBatteryFull()
        {
            if (AppConfig.Is(name: "charge_full"))
            {
                buttonBatteryFull.BackColor = colorStandard;
                buttonBatteryFull.ForeColor = SystemColors.ControlLightLight;
            }
            else
            {
                buttonBatteryFull.BackColor = buttonSecond;
                buttonBatteryFull.ForeColor = SystemColors.ControlDark;
            }

        }


        public void VisualizePeripherals()
        {
            if (!PeripheralsProvider.IsAnyPeripheralConnect())
            {
                panelPeripherals.Visible = false;
                return;
            }

            Button[] buttons = new Button[] { buttonPeripheral1, buttonPeripheral2, buttonPeripheral3 };

            //we only support 4 devces for now. Who has more than 4 mice connected to the same PC anyways....
            List<IPeripheral> lp = PeripheralsProvider.AllPeripherals();

            for (int i = 0; i < lp.Count && i < buttons.Length; ++i)
            {
                IPeripheral m = lp.ElementAt(index: i);
                Button b = buttons[i];

                if (m.IsDeviceReady)
                {
                    if (m.HasBattery())
                    {
                        b.Text = m.GetDisplayName() + "\n" + m.Battery + "%"
                                            + (m.Charging ? "(" + Properties.Strings.Charging + ")" : "");
                    }
                    else
                    {
                        b.Text = m.GetDisplayName();
                    }

                }
                else
                {
                    //Mouse is either not connected or in standby
                    b.Text = m.GetDisplayName() + "\n(" + Properties.Strings.NotConnected + ")";
                }

                switch (m.DeviceType())
                {
                    case PeripheralType.Mouse:
                        b.Image = ControlHelper.TintImage(image: Properties.Resources.icons8_maus_32, tintColor: b.ForeColor);
                        break;

                    case PeripheralType.Keyboard:
                        b.Image = ControlHelper.TintImage(image: Properties.Resources.icons8_keyboard_32, tintColor: b.ForeColor);
                        break;
                }

                b.Visible = true;
            }

            for (int i = lp.Count; i < buttons.Length; ++i)
            {
                buttons[i].Visible = false;
            }

            panelPeripherals.Visible = true;
        }

        private void ButtonPeripheral_MouseEnter(object? sender, EventArgs e)
        {
            int index = 0;
            if (sender == buttonPeripheral2) index = 1;
            if (sender == buttonPeripheral3) index = 2;
            IPeripheral iph = PeripheralsProvider.AllPeripherals().ElementAt(index: index);


            if (iph is null)
            {
                return;
            }

            if (!iph.IsDeviceReady)
            {
                //Refresh battery on hover if the device is marked as "Not Ready"
                iph.ReadBattery();
            }
        }

        private void ButtonPeripheral_Click(object? sender, EventArgs e)
        {
            if (mouseSettings is not null)
            {
                mouseSettings.Close();
                return;
            }

            int index = 0;
            if (sender == buttonPeripheral2) index = 1;
            if (sender == buttonPeripheral3) index = 2;

            IPeripheral iph = PeripheralsProvider.AllPeripherals().ElementAt(index: index);

            if (iph is null)
            {
                //Can only happen when the user hits the button in the exact moment a device is disconnected.
                return;
            }

            if (iph.DeviceType() == PeripheralType.Mouse)
            {
                AsusMouse? am = iph as AsusMouse;
                if (am is null || !am.IsDeviceReady)
                {
                    //Should not happen if all device classes are implemented correctly. But better safe than sorry.
                    return;
                }
                mouseSettings = new AsusMouseSettings(mouse: am);
                mouseSettings.TopMost = true;
                mouseSettings.FormClosed += MouseSettings_FormClosed;
                mouseSettings.Disposed += MouseSettings_Disposed;
                if (!mouseSettings.IsDisposed)
                {
                    mouseSettings.Show();
                }
                else
                {
                    mouseSettings = null;
                }

            }
        }

        private void MouseSettings_Disposed(object? sender, EventArgs e)
        {
            mouseSettings = null;
        }

        private void MouseSettings_FormClosed(object? sender, FormClosedEventArgs e)
        {
            mouseSettings = null;
        }

        public void VisualiseFnLock()
        {

            if (AppConfig.Is(name: "fn_lock"))
            {
                buttonFnLock.BackColor = colorStandard;
                buttonFnLock.ForeColor = SystemColors.ControlLightLight;
            }
            else
            {
                buttonFnLock.BackColor = buttonSecond;
                buttonFnLock.ForeColor = SystemColors.ControlDark;
            }
        }


        private void ButtonFnLock_Click(object? sender, EventArgs e)
        {
            InputDispatcher.ToggleFnLock();
        }

    }


}
