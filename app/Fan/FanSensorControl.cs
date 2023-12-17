using GHelper.Mode;

namespace GHelper.Fan
{
    public class FanSensorControl
    {
        public const int DEFAULT_FAN_MIN = 18;
        public const int DEFAULT_FAN_MAX = 58;

        public const int XGM_FAN_MAX = 72;

        public const int INADEQUATE_MAX = 92;

        const int FAN_COUNT = 3;

        Fans fansForm;
        ModeControl modeControl = Program.modeControl;

        static int[] measuredMax;
        static int sameCount = 0;

        static System.Timers.Timer timer = default!;

        static int[] _fanMax = InitFanMax();
        static bool _fanRpm = AppConfig.IsNotFalse(name: "fan_rpm");

        public FanSensorControl(Fans fansForm)
        {
            this.fansForm = fansForm;
            timer = new System.Timers.Timer(interval: 1000);
            timer.Elapsed += Timer_Elapsed;
        }

        static int[] InitFanMax()
        {
            int[] defaultMax = GetDefaultMax();

            return new int[3] {
                AppConfig.Get(name: "fan_max_" + (int)AsusFan.CPU, empty: defaultMax[(int)AsusFan.CPU]),
                AppConfig.Get(name: "fan_max_" + (int)AsusFan.GPU, empty: defaultMax[(int)AsusFan.GPU]),
                AppConfig.Get(name: "fan_max_" + (int)AsusFan.Mid, empty: defaultMax[(int)AsusFan.Mid])
            };
        }


        static int[] GetDefaultMax()
        {
            if (AppConfig.ContainsModel(contains: "GA401I")) return new int[3] { 78, 76, DEFAULT_FAN_MAX };
            if (AppConfig.ContainsModel(contains: "GA401")) return new int[3] { 71, 73, DEFAULT_FAN_MAX };
            if (AppConfig.ContainsModel(contains: "GA402")) return new int[3] { 55, 56, DEFAULT_FAN_MAX };

            if (AppConfig.ContainsModel(contains: "G513R")) return new int[3] { 58, 60, DEFAULT_FAN_MAX };
            if (AppConfig.ContainsModel(contains: "G513Q")) return new int[3] { 69, 69, DEFAULT_FAN_MAX };
            if (AppConfig.ContainsModel(contains: "GA503")) return new int[3] { 64, 64, DEFAULT_FAN_MAX };

            if (AppConfig.ContainsModel(contains: "GU603")) return new int[3] { 62, 64, DEFAULT_FAN_MAX };

            if (AppConfig.ContainsModel(contains: "FA507R")) return new int[3] { 63, 57, DEFAULT_FAN_MAX };
            if (AppConfig.ContainsModel(contains: "FA507X")) return new int[3] { 63, 68, DEFAULT_FAN_MAX };

            if (AppConfig.ContainsModel(contains: "GX650")) return new int[3] { 62, 62, DEFAULT_FAN_MAX };

            if (AppConfig.ContainsModel(contains: "G732")) return new int[3] { 61, 60, DEFAULT_FAN_MAX };
            if (AppConfig.ContainsModel(contains: "G713")) return new int[3] { 56, 60, DEFAULT_FAN_MAX };

            if (AppConfig.ContainsModel(contains: "Z301")) return new int[3] { 72, 64, DEFAULT_FAN_MAX };

            if (AppConfig.ContainsModel(contains: "GV601")) return new int[3] { 78, 59, 85 };

            return new int[3] { DEFAULT_FAN_MAX, DEFAULT_FAN_MAX, DEFAULT_FAN_MAX };
        }

        public static int GetFanMax(AsusFan device)
        {
            if (device == AsusFan.XGM) return XGM_FAN_MAX;

            if (_fanMax[(int)device] < 0 || _fanMax[(int)device] > INADEQUATE_MAX)
                SetFanMax(device: device, value: DEFAULT_FAN_MAX);

            return _fanMax[(int)device];
        }

        public static void SetFanMax(AsusFan device, int value)
        {
            _fanMax[(int)device] = value;
            AppConfig.Set(name: "fan_max_" + (int)device, value: value);
        }

        public static bool fanRpm
        {
            get
            {
                return _fanRpm;
            }
            set
            {
                AppConfig.Set(name: "fan_rpm", value: value ? 1 : 0);
                _fanRpm = value;
            }
        }

        public static string FormatFan(AsusFan device, int value)
        {
            if (value < 0) return null;

            if (value > GetFanMax(device: device) && value <= INADEQUATE_MAX) SetFanMax(device: device, value: value);

            if (fanRpm)
                return Properties.Strings.FanSpeed + ": " + (value * 100).ToString() + "RPM";
            else
                return Properties.Strings.FanSpeed + ": " + Math.Min(val1: Math.Round(a: (float)value / GetFanMax(device: device) * 100), val2: 100).ToString() + "%"; // relatively to max RPM
        }

        public void StartCalibration()
        {

            measuredMax = new int[] { 0, 0, 0 };
            timer.Enabled = true;

            for (int i = 0; i < FAN_COUNT; i++)
                AppConfig.Remove(name: "fan_max_" + i);

            Program.acpi.DeviceSet(DeviceID: AsusACPI.PerformanceMode, Status: AsusACPI.PerformanceTurbo, logName: "ModeCalibration");

            for (int i = 0; i < FAN_COUNT; i++)
                Program.acpi.SetFanCurve(device: (AsusFan)i, curve: new byte[] { 20, 30, 40, 50, 60, 70, 80, 90, 100, 100, 100, 100, 100, 100, 100, 100 });

        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            int fan;
            bool same = true;

            for (int i = 0; i < FAN_COUNT; i++)
            {
                fan = Program.acpi.GetFan(device: (AsusFan)i);
                if (fan > measuredMax[i])
                {
                    measuredMax[i] = fan;
                    same = false;
                }
            }

            if (same) sameCount++;
            else sameCount = 0;

            string label = "Measuring Max Speed - CPU: " + measuredMax[(int)AsusFan.CPU] * 100 + ", GPU: " + measuredMax[(int)AsusFan.GPU] * 100;
            if (measuredMax[(int)AsusFan.Mid] > 10) label = label + ", Mid: " + measuredMax[(int)AsusFan.Mid] * 100;
            label = label + " (" + sameCount + "s)";

            fansForm.LabelFansResult(text: label);

            if (sameCount >= 15)
            {
                for (int i = 0; i < FAN_COUNT; i++)
                {
                    if (measuredMax[i] > 30 && measuredMax[i] < INADEQUATE_MAX) SetFanMax(device: (AsusFan)i, value: measuredMax[i]);
                }

                sameCount = 0;
                FinishCalibration();
            }

        }

        private void FinishCalibration()
        {

            timer.Enabled = false;
            modeControl.SetPerformanceMode();

            string label = "Measured - CPU: " + AppConfig.Get(name: "fan_max_" + (int)AsusFan.CPU) * 100;

            if (AppConfig.Get(name: "fan_max_" + (int)AsusFan.GPU) > 0)
                label = label + ", GPU: " + AppConfig.Get(name: "fan_max_" + (int)AsusFan.GPU) * 100;

            if (AppConfig.Get(name: "fan_max_" + (int)AsusFan.Mid) > 0)
                label = label + ", Mid: " + AppConfig.Get(name: "fan_max_" + (int)AsusFan.Mid) * 100;

            fansForm.LabelFansResult(text: label);
            fansForm.InitAxis();
        }
    }
}
