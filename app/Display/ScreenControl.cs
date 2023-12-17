using System.Diagnostics;

namespace GHelper.Display
{
    public class ScreenControl
    {

        public const int MAX_REFRESH = 1000;

        public void AutoScreen(bool force = false)
        {
            if (force || AppConfig.Is(name: "screen_auto"))
            {
                if (SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online)
                    SetScreen(frequency: MAX_REFRESH, overdrive: 1);
                else
                    SetScreen(frequency: 60, overdrive: 0);
            }
            else
            {
                SetScreen(overdrive: AppConfig.Get(name: "overdrive"));
            }
        }

        public void SetScreen(int frequency = -1, int overdrive = -1, int miniled = -1)
        {
            var laptopScreen = ScreenNative.FindLaptopScreen(log: true);

            if (laptopScreen is null) return;

            if (ScreenNative.GetRefreshRate(laptopScreen: laptopScreen) < 0) return;

            if (frequency >= MAX_REFRESH)
            {
                frequency = ScreenNative.GetMaxRefreshRate(laptopScreen: laptopScreen);
            }

            if (frequency > 0)
            {
                ScreenNative.SetRefreshRate(laptopScreen: laptopScreen, frequency: frequency);
            }

            if (overdrive >= 0)
            {
                if (AppConfig.Get(name: "no_overdrive") == 1)
                {
	                overdrive = 0;
                }

                Program.acpi.DeviceSet(DeviceID: AsusACPI.ScreenOverdrive, Status: overdrive, logName: "ScreenOverdrive");

            }

            if (miniled >= 0)
            {
                Program.acpi.DeviceSet(DeviceID: AsusACPI.ScreenMiniled, Status: miniled, logName: "Miniled");
                Debug.WriteLine(message: "Miniled " + miniled);
            }

            InitScreen();
        }


        public void ToogleMiniled()
        {
            int miniled = (Program.acpi.DeviceGet(DeviceID: AsusACPI.ScreenMiniled) == 1) ? 0 : 1;
            AppConfig.Set(name: "miniled", value: miniled);
            SetScreen(frequency: -1, overdrive: -1, miniled: miniled);
        }

        public void InitScreen()
        {
            var laptopScreen = ScreenNative.FindLaptopScreen();

            int frequency = ScreenNative.GetRefreshRate(laptopScreen: laptopScreen);
            int maxFrequency = ScreenNative.GetMaxRefreshRate(laptopScreen: laptopScreen);

            bool screenAuto = AppConfig.Is(name: "screen_auto");
            bool overdriveSetting = !AppConfig.Is(name: "no_overdrive");

            int overdrive = Program.acpi.DeviceGet(DeviceID: AsusACPI.ScreenOverdrive);
            int miniled = Program.acpi.DeviceGet(DeviceID: AsusACPI.ScreenMiniled);

            bool hdr = false;

            if (miniled >= 0)
            {
                AppConfig.Set(name: "miniled", value: miniled);
                hdr = ScreenCCD.GetHDRStatus();
            }

            bool screenEnabled = (frequency >= 0);

            AppConfig.Set(name: "frequency", value: frequency);
            AppConfig.Set(name: "overdrive", value: overdrive);

            Program.settingsForm.Invoke(method: delegate
            {
                Program.settingsForm.VisualiseScreen(
                    screenEnabled: screenEnabled,
                    screenAuto: screenAuto,
                    frequency: frequency,
                    maxFrequency: maxFrequency,
                    overdrive: overdrive,
                    overdriveSetting: overdriveSetting,
                    miniled: miniled,
                    hdr: hdr
                );
            });

        }
    }
}
