using Microsoft.Win32;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace GHelper.Helpers
{
    public static class OptimizationService
    {

        static List<string> services = new() {
                "ArmouryCrateControlInterface",
                "ASUSOptimization",
                "AsusAppService",
                "ASUSLinkNear",
                "ASUSLinkRemote",
                "ASUSSoftwareManager",
                "ASUSSwitch",
                "ASUSSystemAnalysis",
                "ASUSSystemDiagnosis",
                "AsusCertService"
        };

        public static void SetChargeLimit(int newValue)
        {
            // Set the path to the .ini file
            string path = @"C:\ProgramData\ASUS\ASUS System Control Interface\ASUSOptimization\Customization.ini";


            // Make a backup copy of the INI file
            string backupPath = path + ".bak";
            File.Copy(sourceFileName: path, destFileName: backupPath, overwrite: true);

            string fileContents = File.ReadAllText(path: path, encoding: Encoding.Unicode);

            // Find the section [BatteryHealthCharging]
            string sectionPattern = @"\[BatteryHealthCharging\]\s*(version=\d+)?\s+value=(\d+)";
            Match sectionMatch = Regex.Match(input: fileContents, pattern: sectionPattern);
            if (sectionMatch.Success)
            {
                // Replace the value with the new value
                string oldValueString = sectionMatch.Groups[groupnum: 2].Value;
                int oldValue = int.Parse(s: oldValueString);
                string newSection = sectionMatch.Value.Replace(oldValue: $"value={oldValue}", newValue: $"value={newValue}");

                // Replace the section in the file contents
                fileContents = fileContents.Replace(oldValue: sectionMatch.Value, newValue: newSection);

                File.WriteAllText(path: path, contents: fileContents, encoding: Encoding.Unicode);
            }
        }

        public static bool IsRunning()
        {
            return Process.GetProcessesByName(processName: "AsusOptimization").Count() > 0;
        }

        public static bool IsOSDRunning()
        {
            return Process.GetProcessesByName(processName: "AsusOSD").Count() > 0;
        }


        public static int GetRunningCount()
        {
            int count = 0;
            foreach (string service in services)
            {
                if (Process.GetProcessesByName(processName: service).Count() > 0) count++;
            }
            return count;
        }


        public static void SetBacklightOffDelay(int value = 60)
        {
            try
            {
                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(name: @"SOFTWARE\ASUS\ASUS System Control Interface\AsusOptimization\ASUS Keyboard Hotkeys", writable: true);
                if (myKey != null)
                {
                    myKey.SetValue(name: "TurnOffKeybdLight", value: value, valueKind: RegistryValueKind.DWord);
                    myKey.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: ex.Message);
            }
        }



        public static void StopAsusServices()
        {
            foreach (string service in services)
            {
                ProcessHelper.StopDisableService(serviceName: service);
            }
        }

        public static void StartAsusServices()
        {
            foreach (string service in services)
            {
                ProcessHelper.StartEnableService(serviceName: service);
            }
        }

    }

}