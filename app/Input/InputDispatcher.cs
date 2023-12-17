using GHelper.Display;
using GHelper.Helpers;
using GHelper.Mode;
using GHelper.USB;
using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace GHelper.Input
{

    public class InputDispatcher
    {
        System.Timers.Timer timer = new System.Timers.Timer(interval: 1000);
        public static bool backlightActivity = true;

        public static Keys keyProfile = Keys.F5;
        public static Keys keyApp = Keys.F12;

        static ModeControl modeControl = Program.modeControl;
        static ScreenControl screenControl = new ScreenControl();

        static bool isTUF = AppConfig.IsTUF();

        KeyboardListener listener;
        KeyboardHook hook = new KeyboardHook();

        public InputDispatcher()
        {

            byte[] result = Program.acpi.DeviceInit();
            Debug.WriteLine(message: $"Init: {BitConverter.ToString(value: result)}");

            Program.acpi.SubscribeToEvents(EventHandler: WatcherEventArrived);
            //Task.Run(Program.acpi.RunListener);

            hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(KeyPressed);

            RegisterKeys();

            timer.Elapsed += Timer_Elapsed;

        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (GetBacklight() == 0) return;

            TimeSpan iddle = NativeMethods.GetIdleTime();
            int kb_timeout;

            if (SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online)
            {
	            kb_timeout = AppConfig.Get(name: "keyboard_ac_timeout", empty: 0);
            }
            else
            {
	            kb_timeout = AppConfig.Get(name: "keyboard_timeout", empty: 60);
            }

            if (kb_timeout == 0) return;

            if (backlightActivity && iddle.TotalSeconds > kb_timeout)
            {
                backlightActivity = false;
                Aura.ApplyBrightness(brightness: 0, log: "Timeout");
            }

            if (!backlightActivity && iddle.TotalSeconds < kb_timeout)
            {
                backlightActivity = true;
                SetBacklightAuto();
            }

            //Logger.WriteLine("Iddle: " + iddle.TotalSeconds);
        }

        public void Init()
        {
            if (listener is not null) listener.Dispose();

            Program.acpi.DeviceInit();

            if (!OptimizationService.IsRunning())
            {
	            this.listener = new KeyboardListener(KeyHandler: HandleEvent);
            }
            else
                Logger.WriteLine(logMessage: "Optimization service is running");

            InitBacklightTimer();

            if (AppConfig.ContainsModel(contains: "VivoBook")) Program.acpi.DeviceSet(DeviceID: AsusACPI.FnLock, Status: AppConfig.Is(name: "fn_lock") ? 1 : 0, logName: "FnLock");

        }

        public void InitBacklightTimer()
        {
            timer.Enabled = AppConfig.Get(name: "keyboard_timeout") > 0 && SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Online ||
                            AppConfig.Get(name: "keyboard_ac_timeout") > 0 && SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
        }



        public void RegisterKeys()
        {
            hook.UnregisterAll();

            // CTRL + SHIFT + F5 to cycle profiles
            if (AppConfig.Get(name: "keybind_profile") != -1)
            {
	            keyProfile = (Keys)AppConfig.Get(name: "keybind_profile");
            }

            if (AppConfig.Get(name: "keybind_app") != -1)
            {
	            keyApp = (Keys)AppConfig.Get(name: "keybind_app");
            }

            string actionM1 = AppConfig.GetString(name: "m1");
            string actionM2 = AppConfig.GetString(name: "m2");

            if (keyProfile != Keys.None)
            {
                hook.RegisterHotKey(modifier: ModifierKeys.Shift | ModifierKeys.Control, key: keyProfile);
                hook.RegisterHotKey(modifier: ModifierKeys.Shift | ModifierKeys.Control | ModifierKeys.Alt, key: keyProfile);
            }

            if (keyApp != Keys.None) hook.RegisterHotKey(modifier: ModifierKeys.Shift | ModifierKeys.Control, key: keyApp);

            hook.RegisterHotKey(modifier: ModifierKeys.Shift | ModifierKeys.Control | ModifierKeys.Alt, key: Keys.F14);
            hook.RegisterHotKey(modifier: ModifierKeys.Shift | ModifierKeys.Control | ModifierKeys.Alt, key: Keys.F15);

            if (!AppConfig.Is(name: "skip_hotkeys"))
            {
                hook.RegisterHotKey(modifier: ModifierKeys.Control, key: Keys.VolumeDown);
                hook.RegisterHotKey(modifier: ModifierKeys.Control, key: Keys.VolumeUp);
                hook.RegisterHotKey(modifier: ModifierKeys.Shift, key: Keys.VolumeDown);
                hook.RegisterHotKey(modifier: ModifierKeys.Shift, key: Keys.VolumeUp);
                hook.RegisterHotKey(modifier: ModifierKeys.Shift | ModifierKeys.Control, key: Keys.F20);
            }

            if (!AppConfig.IsZ13() && !AppConfig.IsAlly())
            {
                if (actionM1 is not null && actionM1.Length > 0) hook.RegisterHotKey(modifier: ModifierKeys.None, key: Keys.VolumeDown);
                if (actionM2 is not null && actionM2.Length > 0) hook.RegisterHotKey(modifier: ModifierKeys.None, key: Keys.VolumeUp);
            }

            // FN-Lock group

            if (AppConfig.Is(name: "fn_lock") && !AppConfig.ContainsModel(contains: "VivoBook"))
                for (Keys i = Keys.F1; i <= Keys.F11; i++) hook.RegisterHotKey(modifier: ModifierKeys.None, key: i);

            // Arrow-lock group
            if (AppConfig.Is(name: "arrow_lock") && AppConfig.IsDUO())
            {
                hook.RegisterHotKey(modifier: ModifierKeys.None, key: Keys.Left);
                hook.RegisterHotKey(modifier: ModifierKeys.None, key: Keys.Right);
                hook.RegisterHotKey(modifier: ModifierKeys.None, key: Keys.Up);
                hook.RegisterHotKey(modifier: ModifierKeys.None, key: Keys.Down);
            }

        }


        public static int[] ParseHexValues(string input)
        {
            string pattern = @"\b(0x[0-9A-Fa-f]{1,2}|[0-9A-Fa-f]{1,2})\b";

            if (!Regex.IsMatch(input: input, pattern: $"^{pattern}(\\s+{pattern})*$")) return new int[0];

            MatchCollection matches = Regex.Matches(input: input, pattern: pattern);

            int[] hexValues = new int[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                string hexValueStr = matches[i: i].Value;
                int hexValue = int.Parse(s: hexValueStr.StartsWith(value: "0x", comparisonType: StringComparison.OrdinalIgnoreCase)
                    ? hexValueStr.Substring(startIndex: 2)
                    : hexValueStr, style: System.Globalization.NumberStyles.HexNumber);

                hexValues[i] = hexValue;
            }

            return hexValues;
        }


        static void CustomKey(string configKey = "m3")
        {
            string command = AppConfig.GetString(name: configKey + "_custom");
            int[] hexKeys = new int[0];

            try
            {
                hexKeys = ParseHexValues(input: command);
            }
            catch
            {
            }

            switch (hexKeys.Length)
            {
                case 1:
                    KeyboardHook.KeyPress(key: (Keys)hexKeys[0]);
                    break;
                case 2:
                    KeyboardHook.KeyKeyPress(key: (Keys)hexKeys[0], key2: (Keys)hexKeys[1]);
                    break;
                case 3:
                    KeyboardHook.KeyKeyKeyPress(key: (Keys)hexKeys[0], key2: (Keys)hexKeys[1], key3: (Keys)hexKeys[2]);
                    break;
                default:
                    LaunchProcess(command: command);
                    break;
            }

        }


        static void SetBrightness(int delta)
        {
            int brightness = -1;

            if (isTUF)
            {
	            brightness = ScreenBrightness.Get();
            }

            if (AppConfig.SwappedBrightness())
            {
	            delta = -delta;
            }

            Program.acpi.DeviceSet(DeviceID: AsusACPI.UniversalControl, Status: delta > 0 ? AsusACPI.Brightness_Up : AsusACPI.Brightness_Down, logName: "Brightness");

            if (isTUF)
            {
                if (AppConfig.SwappedBrightness()) return;
                if (delta < 0 && brightness <= 0) return;
                if (delta > 0 && brightness >= 100) return;

                Thread.Sleep(millisecondsTimeout: 100);
                if (brightness == ScreenBrightness.Get())
                    Program.toast.RunToast(text: ScreenBrightness.Adjust(delta: delta) + "%", icon: (delta < 0) ? ToastIcon.BrightnessDown : ToastIcon.BrightnessUp);
            }

        }

        public void KeyPressed(object sender, KeyPressedEventArgs e)
        {

            if (e.Modifier == ModifierKeys.None)
            {
                Logger.WriteLine(logMessage: e.Key.ToString());

                if (AppConfig.NoMKeys())
                {
                    switch (e.Key)
                    {
                        case Keys.F2:
                            KeyboardHook.KeyPress(key: Keys.VolumeDown);
                            return;
                        case Keys.F3:
                            KeyboardHook.KeyPress(key: Keys.VolumeUp);
                            return;
                        case Keys.F4:
                            KeyProcess(name: "m3");
                            return;
                    }
                }

                if (AppConfig.IsZ13() || AppConfig.IsDUO())
                {
                    switch (e.Key)
                    {
                        case Keys.F11:
                            HandleEvent(EventID: 199);
                            return;
                    }
                }

                if (AppConfig.NoAura())
                {
                    switch (e.Key)
                    {
                        case Keys.F2:
                            KeyboardHook.KeyPress(key: Keys.MediaPreviousTrack);
                            return;
                        case Keys.F3:
                            KeyboardHook.KeyPress(key: Keys.MediaPlayPause);
                            return;
                        case Keys.F4:
                            KeyboardHook.KeyPress(key: Keys.MediaNextTrack);
                            return;
                    }
                }


                switch (e.Key)
                {
                    case Keys.F1:
                        KeyboardHook.KeyPress(key: Keys.VolumeMute);
                        break;
                    case Keys.F2:
                        SetBacklight(delta: -1, force: true);
                        break;
                    case Keys.F3:
                        SetBacklight(delta: 1, force: true);
                        break;
                    case Keys.F4:
                        KeyProcess(name: "fnf4");
                        break;
                    case Keys.F5:
                        KeyProcess(name: "fnf5");
                        break;
                    case Keys.F6:
                        KeyboardHook.KeyPress(key: Keys.Snapshot);
                        break;
                    case Keys.F7:
                        SetBrightness(delta: -10);
                        break;
                    case Keys.F8:
                        SetBrightness(delta: +10);
                        break;
                    case Keys.F9:
                        KeyboardHook.KeyKeyPress(key: Keys.LWin, key2: Keys.P);
                        break;
                    case Keys.F10:
                        ToggleTouchpadEvent(hotkey: true);
                        break;
                    case Keys.F11:
                        SleepEvent();
                        break;
                    case Keys.VolumeDown:
                        KeyProcess(name: "m1");
                        break;
                    case Keys.VolumeUp:
                        KeyProcess(name: "m2");
                        break;
                    case Keys.Left:
                        KeyboardHook.KeyPress(key: Keys.Home);
                        break;
                    case Keys.Right:
                        KeyboardHook.KeyPress(key: Keys.End);
                        break;
                    case Keys.Up:
                        KeyboardHook.KeyPress(key: Keys.PageUp);
                        break;
                    case Keys.Down:
                        KeyboardHook.KeyPress(key: Keys.PageDown);
                        break;
                    default:
                        break;
                }

            }

            if (e.Modifier == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                if (e.Key == keyProfile) modeControl.CyclePerformanceMode();
                if (e.Key == keyApp) Program.SettingsToggle();
                if (e.Key == Keys.F20) KeyProcess(name: "m3");
            }

            if (e.Modifier == (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt))
            {
                if (e.Key == keyProfile) modeControl.CyclePerformanceMode(back: true);

                switch (e.Key)
                {
                    case Keys.F14:
                        Program.settingsForm.gpuControl.SetGPUMode(GPUMode: AsusACPI.GPUModeEco);
                        break;
                    case Keys.F15:
                        Program.settingsForm.gpuControl.SetGPUMode(GPUMode: AsusACPI.GPUModeStandard);
                        break;
                }
            }


            if (e.Modifier == (ModifierKeys.Control))
            {
                switch (e.Key)
                {
                    case Keys.VolumeDown:
                        // Screen brightness down on CTRL+VolDown
                        SetBrightness(delta: -10);
                        break;
                    case Keys.VolumeUp:
                        // Screen brightness up on CTRL+VolUp
                        SetBrightness(delta: +10);
                        break;
                }
            }

            if (e.Modifier == (ModifierKeys.Shift))
            {
                switch (e.Key)
                {
                    case Keys.VolumeDown:
                        // Keyboard backlight down on SHIFT+VolDown
                        SetBacklight(delta: -1);
                        break;
                    case Keys.VolumeUp:
                        // Keyboard backlight up on SHIFT+VolUp
                        SetBacklight(delta: 1);
                        break;
                }
            }
        }


        public static void KeyProcess(string name = "m3")
        {
            string action = AppConfig.GetString(name: name);

            if (action is null || action.Length <= 1)
            {
                if (name == "m4")
                {
	                action = "ghelper";
                }

                if (name == "fnf4")
                {
	                action = "aura";
                }

                if (name == "fnf5")
                {
	                action = "performance";
                }

                if (name == "m3" && !OptimizationService.IsRunning())
                {
	                action = "micmute";
                }

                if (name == "fnc")
                {
	                action = "fnlock";
                }

                if (name == "fne")
                {
	                action = "calculator";
                }
            }

            switch (action)
            {
                case "mute":
                    KeyboardHook.KeyPress(key: Keys.VolumeMute);
                    break;
                case "play":
                    KeyboardHook.KeyPress(key: Keys.MediaPlayPause);
                    break;
                case "screenshot":
                    KeyboardHook.KeyPress(key: Keys.Snapshot);
                    break;
                case "screen":
                    Logger.WriteLine(logMessage: "Screen off toggle");
                    NativeMethods.TurnOffScreen();
                    break;
                case "miniled":
                    screenControl.ToogleMiniled();
                    break;
                case "aura":
                    Program.settingsForm.BeginInvoke(method: Program.settingsForm.CycleAuraMode);
                    break;
                case "performance":
                    modeControl.CyclePerformanceMode(back: Control.ModifierKeys == Keys.Shift);
                    break;
                case "ghelper":
                    try
                    {
                        Program.settingsForm.BeginInvoke(method: delegate
                        {
                            Program.SettingsToggle();
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(value: ex);
                    }
                    break;
                case "fnlock":
                    ToggleFnLock();
                    break;
                case "micmute":
                    bool muteStatus = Audio.ToggleMute();
                    Program.toast.RunToast(text: muteStatus ? "Muted" : "Unmuted", icon: muteStatus ? ToastIcon.MicrophoneMute : ToastIcon.Microphone);
                    if (AppConfig.IsVivobook()) Program.acpi.DeviceSet(DeviceID: AsusACPI.MICMUTE_LED, Status: muteStatus ? 1 : 0, logName: "MicmuteLed");
                    break;
                case "brightness_up":
                    SetBrightness(delta: +10);
                    break;
                case "brightness_down":
                    SetBrightness(delta: -10);
                    break;
                case "screenpad_up":
                    SetScreenpad(delta: 10);
                    break;
                case "screenpad_down":
                    SetScreenpad(delta: -10);
                    break;
                case "custom":
                    CustomKey(configKey: name);
                    break;
                case "calculator":
                    LaunchProcess(command: "calc");
                    break;
                default:
                    break;
            }
        }

        static bool GetTouchpadState()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(name: @"SOFTWARE\Microsoft\Windows\CurrentVersion\PrecisionTouchPad\Status", writable: false))
            {
                Logger.WriteLine(logMessage: "Touchpad status:" + key?.GetValue(name: "Enabled")?.ToString());
                return key?.GetValue(name: "Enabled")?.ToString() == "1";
            }
        }

        static void ToggleTouchpadEvent(bool hotkey = false)
        {
            if (hotkey || !AppConfig.IsHardwareTouchpadToggle()) ToggleTouchpad();
            Thread.Sleep(millisecondsTimeout: 200);
            Program.toast.RunToast(text: GetTouchpadState() ? "On" : "Off", icon: ToastIcon.Touchpad);
        }

        static void ToggleTouchpad()
        {
            KeyboardHook.KeyKeyKeyPress(key: Keys.LWin, key2: Keys.LControlKey, key3: Keys.F24, sleep: 50);
        }

        static void SleepEvent()
        {
            Program.acpi.DeviceSet(DeviceID: AsusACPI.UniversalControl, Status: AsusACPI.KB_Sleep, logName: "Sleep");
        }

        public static void ToggleArrowLock()
        {
            int arLock = AppConfig.Is(name: "arrow_lock") ? 0 : 1;
            AppConfig.Set(name: "arrow_lock", value: arLock);

            Program.settingsForm.BeginInvoke(method: Program.inputDispatcher.RegisterKeys);
            Program.toast.RunToast(text: "Arrow-Lock " + (arLock == 1 ? "On" : "Off"), icon: ToastIcon.FnLock);
        }

        public static void ToggleFnLock()
        {
            int fnLock = AppConfig.Is(name: "fn_lock") ? 0 : 1;
            AppConfig.Set(name: "fn_lock", value: fnLock);

            if (AppConfig.ContainsModel(contains: "VivoBook"))
                Program.acpi.DeviceSet(DeviceID: AsusACPI.FnLock, Status: fnLock == 1 ? 1 : 0, logName: "FnLock");
            else
                Program.settingsForm.BeginInvoke(method: Program.inputDispatcher.RegisterKeys);

            Program.settingsForm.BeginInvoke(method: Program.settingsForm.VisualiseFnLock);

            Program.toast.RunToast(text: "Fn-Lock " + (fnLock == 1 ? "On" : "Off"), icon: ToastIcon.FnLock);
        }

        public static void TabletMode()
        {
            if (AppConfig.Is(name: "disable_tablet")) return;

            bool touchpadState = GetTouchpadState();
            bool tabletState = Program.acpi.DeviceGet(DeviceID: AsusACPI.TabletState) > 0;

            Logger.WriteLine(logMessage: "Tablet: " + tabletState + " Touchpad: " + touchpadState);

            if (tabletState && touchpadState || !tabletState && !touchpadState) ToggleTouchpad();

        }

        static void HandleEvent(int EventID)
        {
            // The ROG Ally uses different M-key codes.
            // We'll special-case the translation of those.
            if (AppConfig.IsAlly())
            {
                switch (EventID)
                {

                    // This is both the M1 and M2 keys.
                    // There's a way to differentiate, apparently, but it isn't over USB or any other obvious protocol.
                    case 165:
                        KeyProcess(name: "paddle");
                        return;
                    // The Command Center ("play-looking") button below the select key.
                    case 166:
                        KeyProcess(name: "cc");
                        return;
                    // The M4/ROG key.
                    case 56:
                        KeyProcess(name: "m4");
                        return;

                }
            }
            // All other devices seem to use the same HID key-codes,
            // so we can process them all the same.
            else
            {
                switch (EventID)
                {
                    case 124:    // M3
                        KeyProcess(name: "m3");
                        return;
                    case 56:    // M4 / Rog button
                        KeyProcess(name: "m4");
                        return;
                    case 55:    // Arconym
                        KeyProcess(name: "m6");
                        return;
                    case 181:    // FN + Numpad Enter
                        KeyProcess(name: "fne");
                        return;
                    case 174:   // FN+F5
                        modeControl.CyclePerformanceMode(back: Control.ModifierKeys == Keys.Shift);
                        return;
                    case 179:   // FN+F4
                    case 178:   // FN+F4
                        KeyProcess(name: "fnf4");
                        return;
                    case 158:   // Fn + C
                        KeyProcess(name: "fnc");
                        return;
                    case 78:    // Fn + ESC
                        ToggleFnLock();
                        return;
                    case 75:    // Fn + ESC
                        ToggleArrowLock();
                        return;
                    case 189: // Tablet mode
                        TabletMode();
                        return;
                    case 197: // FN+F2
                        SetBacklight(delta: -1);
                        return;
                    case 196: // FN+F3
                        SetBacklight(delta: 1);
                        return;
                    case 199: // ON Z13 - FN+F11 - cycles backlight
                        SetBacklight(delta: 4);
                        return;
                    case 51:    // Fn+F6 on old TUFs
                    case 53:    // Fn+F6 on GA-502DU model
                        NativeMethods.TurnOffScreen();
                        return;
                }
            }

            if (!OptimizationService.IsRunning())
                HandleOptimizationEvent(EventID: EventID);

        }

        // Asus Optimization service Events 
        static void HandleOptimizationEvent(int EventID)
        {
            switch (EventID)
            {
                case 16: // FN+F7
                    if (Control.ModifierKeys == Keys.Shift)
                    {
                        if (AppConfig.IsDUO()) SetScreenpad(delta: -10);
                        else Program.settingsForm.BeginInvoke(method: Program.settingsForm.CycleMatrix, -1);
                    }
                    else
                        Program.acpi.DeviceSet(DeviceID: AsusACPI.UniversalControl, Status: AsusACPI.Brightness_Down, logName: "Brightness");
                    break;
                case 32: // FN+F8
                    if (Control.ModifierKeys == Keys.Shift)
                    {
                        if (AppConfig.IsDUO()) SetScreenpad(delta: 10);
                        else Program.settingsForm.BeginInvoke(method: Program.settingsForm.CycleMatrix, 1);
                    }
                    else
                        Program.acpi.DeviceSet(DeviceID: AsusACPI.UniversalControl, Status: AsusACPI.Brightness_Up, logName: "Brightness");
                    break;
                case 107: // FN+F10
                    ToggleTouchpadEvent();
                    break;
                case 108: // FN+F11
                    SleepEvent();
                    break;
                case 106: // Screenpad button on DUO
                    if (Control.ModifierKeys == Keys.Shift)
                        ToggleScreenpad();
                    else
                        SetScreenpad(delta: 100);
                    break;


            }
        }


        public static int GetBacklight()
        {
            int backlight_power = AppConfig.Get(name: "keyboard_brightness", empty: 1);
            int backlight_battery = AppConfig.Get(name: "keyboard_brightness_ac", empty: 1);
            bool onBattery = SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Online;

            int backlight;

            //backlight = onBattery ? Math.Min(backlight_battery, backlight_power) : Math.Max(backlight_battery, backlight_power);
            backlight = onBattery ? backlight_battery : backlight_power;

            return Math.Max(val1: Math.Min(val1: 3, val2: backlight), val2: 0);
        }

        public static void SetBacklightAuto(bool init = false)
        {
            if (init) Aura.Init();
            Aura.ApplyBrightness(brightness: GetBacklight(), log: "Auto", delay: init);
        }

        public static void SetBacklight(int delta, bool force = false)
        {
            int backlight_power = AppConfig.Get(name: "keyboard_brightness", empty: 1);
            int backlight_battery = AppConfig.Get(name: "keyboard_brightness_ac", empty: 1);
            bool onBattery = SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Online;

            int backlight = onBattery ? backlight_battery : backlight_power;

            if (delta >= 4)
            {
	            backlight = ++backlight % 4;
            }
            else
            {
	            backlight = Math.Max(val1: Math.Min(val1: 3, val2: backlight + delta), val2: 0);
            }

            if (onBattery)
                AppConfig.Set(name: "keyboard_brightness_ac", value: backlight);
            else
                AppConfig.Set(name: "keyboard_brightness", value: backlight);

            if (force || !OptimizationService.IsRunning())
            {
                Aura.ApplyBrightness(brightness: backlight, log: "HotKey");
            }

            if (!OptimizationService.IsOSDRunning())
            {
                string[] backlightNames = new string[] { "Off", "Low", "Mid", "Max" };
                Program.toast.RunToast(text: backlightNames[backlight], icon: delta > 0 ? ToastIcon.BacklightUp : ToastIcon.BacklightDown);
            }

        }

        public static void ToggleScreenpad()
        {
            int toggle = AppConfig.Is(name: "screenpad_toggle") ? 0 : 1;

            Program.acpi.DeviceSet(DeviceID: AsusACPI.ScreenPadToggle, Status: toggle, logName: "ScreenpadToggle");
            AppConfig.Set(name: "screenpad_toggle", value: toggle);
            Program.toast.RunToast(text: $"Screen Pad " + (toggle == 1 ? "On" : "Off"), icon: toggle > 0 ? ToastIcon.BrightnessUp : ToastIcon.BrightnessDown);
        }


        public static void SetScreenpad(int delta)
        {
            int brightness = AppConfig.Get(name: "screenpad", empty: 100);

            if (delta == 100)
            {
                if (brightness < 0)
                {
	                brightness = 100;
                }
                else if (brightness >= 100)
                {
	                brightness = 0;
                }
                else
                {
	                brightness = -10;
                }
            }
            else
            {
                brightness = Math.Max(val1: Math.Min(val1: 100, val2: brightness + delta), val2: -10);
            }

            AppConfig.Set(name: "screenpad", value: brightness);

            if (brightness >= 0) Program.acpi.DeviceSet(DeviceID: AsusACPI.ScreenPadToggle, Status: 1, logName: "ScreenpadOn");

            Program.acpi.DeviceSet(DeviceID: AsusACPI.ScreenPadBrightness, Status: Math.Max(val1: brightness * 255 / 100, val2: 0), logName: "Screenpad");

            if (brightness < 0) Program.acpi.DeviceSet(DeviceID: AsusACPI.ScreenPadToggle, Status: 0, logName: "ScreenpadOff");

            string toast;

            if (brightness < 0)
            {
	            toast = "Off";
            }
            else if (brightness == 0)
            {
	            toast = "Hidden";
            }
            else
            {
	            toast = brightness.ToString() + "%";
            }

            Program.toast.RunToast(text: $"Screen Pad {toast}", icon: delta > 0 ? ToastIcon.BrightnessUp : ToastIcon.BrightnessDown);

        }


        static void LaunchProcess(string command = "")
        {

            try
            {

                //string executable = command.Split(' ')[0];
                //string arguments = command.Substring(executable.Length).Trim();
                ProcessStartInfo startInfo = new ProcessStartInfo(fileName: "cmd", arguments: "/C " + command);

                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                //startInfo.Arguments = arguments;
                Process proc = Process.Start(startInfo: startInfo);
            }
            catch
            {
                Logger.WriteLine(logMessage: "Failed to run  " + command);
            }


        }



        static void WatcherEventArrived(object sender, EventArrivedEventArgs e)
        {
            if (e.NewEvent is null) return;
            int EventID = int.Parse(s: e.NewEvent[propertyName: "EventID"].ToString());
            Logger.WriteLine(logMessage: "WMI event " + EventID);
            HandleEvent(EventID: EventID);
        }
    }
}
