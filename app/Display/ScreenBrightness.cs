namespace GHelper.Display
{
    using System;
    using System.Diagnostics;
    using System.Management;

    public static class ScreenBrightness
    {
        public static int Get()
        {
            using var mclass = new ManagementClass(path: "WmiMonitorBrightness")
            {
                Scope = new ManagementScope(path: @"\\.\root\wmi")
            };
            using var instances = mclass.GetInstances();
            foreach (ManagementObject instance in instances)
            {
                return (byte)instance.GetPropertyValue(propertyName: "CurrentBrightness");
            }
            return 0;
        }

        public static void Set(int brightness)
        {
            using var mclass = new ManagementClass(path: "WmiMonitorBrightnessMethods")
            {
                Scope = new ManagementScope(path: @"\\.\root\wmi")
            };
            using var instances = mclass.GetInstances();
            var args = new object[] { 1, brightness };
            foreach (ManagementObject instance in instances)
            {
                instance.InvokeMethod(methodName: "WmiSetBrightness", args: args);
            }
        }

        public static int Adjust(int delta)
        {
            int brightness = Get();
            Debug.WriteLine(value: brightness);
            brightness = Math.Min(val1: 100, val2: Math.Max(val1: 0, val2: brightness + delta));
            Set(brightness: brightness);
            return brightness;
        }

    }
}
