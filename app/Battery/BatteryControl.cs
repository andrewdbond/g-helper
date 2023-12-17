namespace GHelper.Battery
{
    internal class BatteryControl
    {

        public static void ToggleBatteryLimitFull()
        {
            if (AppConfig.Is(name: "charge_full")) SetBatteryChargeLimit();
            else SetBatteryLimitFull();
        }

        public static void SetBatteryLimitFull()
        {
            AppConfig.Set(name: "charge_full", value: 1);
            Program.acpi.DeviceSet(DeviceID: AsusACPI.BatteryLimit, Status: 100, logName: "BatteryLimit");
            Program.settingsForm.VisualiseBatteryFull();
        }

        public static void UnSetBatteryLimitFull()
        {
            AppConfig.Set(name: "charge_full", value: 0);
            Program.settingsForm.VisualiseBatteryFull();
        }

        public static void AutoBattery(bool init = false)
        {
            if (AppConfig.Is(name: "charge_full") && !init) SetBatteryLimitFull();
            else SetBatteryChargeLimit();
        }

        public static void SetBatteryChargeLimit(int limit = -1)
        {

            if (limit < 0)
            {
	            limit = AppConfig.Get(name: "charge_limit");
            }

            if (limit < 40 || limit > 100) return;

            Program.acpi.DeviceSet(DeviceID: AsusACPI.BatteryLimit, Status: limit, logName: "BatteryLimit");

            AppConfig.Set(name: "charge_limit", value: limit);
            AppConfig.Set(name: "charge_full", value: 0);

            Program.settingsForm.VisualiseBattery(limit: limit);
        }

    }
}
