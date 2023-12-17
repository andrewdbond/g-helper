using GHelper;
using GHelper.Fan;
using GHelper.Gpu;
using GHelper.Gpu.NVidia;
using GHelper.Gpu.AMD;

using GHelper.Helpers;
using System.Diagnostics;
using System.Management;
using GHelper.Battery;

public static class HardwareControl
{

    public static IGpuControl? GpuControl;

    public static float? cpuTemp = -1;
    public static decimal? batteryRate = 0;
    public static decimal batteryHealth = -1;
    public static decimal batteryCapacity = -1;

    public static decimal? designCapacity;
    public static decimal? fullCapacity;
    public static decimal? chargeCapacity;


    public static int? gpuTemp = null;

    public static string? cpuFan;
    public static string? gpuFan;
    public static string? midFan;

    public static int? gpuUse;

    static long lastUpdate;

    private static int GetGpuUse()
    {
        try
        {
            int? gpuUse = GpuControl?.GetGpuUse();
            Logger.WriteLine(logMessage: "GPU usage: " + GpuControl?.FullName + " " + gpuUse + "%");
            if (gpuUse is not null) return (int)gpuUse;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(message: ex.ToString());
        }

        return 0;
    }


    public static void GetBatteryStatus()
    {

        batteryRate = 0;
        chargeCapacity = 0;

        try
        {
            ManagementScope scope = new ManagementScope(path: "root\\WMI");
            ObjectQuery query = new ObjectQuery(query: "SELECT * FROM BatteryStatus");

            using ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope: scope, query: query);
            foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
            {

                chargeCapacity = Convert.ToDecimal(value: obj[propertyName: "RemainingCapacity"]);

                decimal chargeRate = Convert.ToDecimal(value: obj[propertyName: "ChargeRate"]);
                decimal dischargeRate = Convert.ToDecimal(value: obj[propertyName: "DischargeRate"]);
                
                if (chargeRate > 0)
                {
	                batteryRate = chargeRate / 1000;
                }
                else
                {
	                batteryRate = -dischargeRate / 1000;
                }
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine(message: "Discharge Reading: " + ex.Message);
        }

    }
    public static void ReadFullChargeCapacity()
    {
        if (fullCapacity > 0) return;

        try
        {
            ManagementScope scope = new ManagementScope(path: "root\\WMI");
            ObjectQuery query = new ObjectQuery(query: "SELECT * FROM BatteryFullChargedCapacity");

            using ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope: scope, query: query);
            foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
            {
                fullCapacity = Convert.ToDecimal(value: obj[propertyName: "FullChargedCapacity"]);
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine(message: "Full Charge Reading: " + ex.Message);
        }

    }

    public static void ReadDesignCapacity()
    {
        if (designCapacity > 0) return;

        try
        {
            ManagementScope scope = new ManagementScope(path: "root\\WMI");
            ObjectQuery query = new ObjectQuery(query: "SELECT * FROM BatteryStaticData");

            using ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope: scope, query: query);
            foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
            {
                designCapacity = Convert.ToDecimal(value: obj[propertyName: "DesignedCapacity"]);
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine(message: "Design Capacity Reading: " + ex.Message);
        }
    }

    public static void RefreshBatteryHealth()
    {
        batteryHealth = GetBatteryHealth() * 100;
    }


    public static decimal GetBatteryHealth()
    {
        if (designCapacity is null)
        {
            ReadDesignCapacity();
        }
        ReadFullChargeCapacity();

        if (designCapacity is null || fullCapacity is null || designCapacity == 0 || fullCapacity == 0)
        {
            return -1;
        }

        decimal health = (decimal)fullCapacity / (decimal)designCapacity;
        Logger.WriteLine(logMessage: "Design Capacity: " + designCapacity + "mWh, Full Charge Capacity: " + fullCapacity + "mWh, Health: " + health + "%");

        return health;
    }

    public static float? GetCPUTemp() {

        var last = DateTimeOffset.Now.ToUnixTimeSeconds();
        if (Math.Abs(value: last - lastUpdate) < 2) return cpuTemp;
        lastUpdate = last;

        cpuTemp = Program.acpi.DeviceGet(DeviceID: AsusACPI.Temp_CPU);

        if (cpuTemp < 0) try
            {
                using (var ct = new PerformanceCounter(categoryName: "Thermal Zone Information", counterName: "Temperature", instanceName: @"\_TZ.THRM", readOnly: true))
                {
                    cpuTemp = ct.NextValue() - 273;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(message: "Failed reading CPU temp :" + ex.Message);
            }


        return cpuTemp;
    }


    public static void ReadSensors()
    {
        batteryRate = 0;
        gpuTemp = -1;
        gpuUse = -1;

        cpuFan = FanSensorControl.FormatFan(device: AsusFan.CPU, value: Program.acpi.GetFan(device: AsusFan.CPU));
        gpuFan = FanSensorControl.FormatFan(device: AsusFan.GPU, value: Program.acpi.GetFan(device: AsusFan.GPU));
        midFan = FanSensorControl.FormatFan(device: AsusFan.Mid, value: Program.acpi.GetFan(device: AsusFan.Mid));

        cpuTemp = GetCPUTemp();

        try
        {
            gpuTemp = GpuControl?.GetCurrentTemperature();

        }
        catch (Exception ex)
        {
            gpuTemp = -1;
            Debug.WriteLine(message: "Failed reading GPU temp :" + ex.Message);
        }

        if (gpuTemp is null || gpuTemp < 0)
        {
	        gpuTemp = Program.acpi.DeviceGet(DeviceID: AsusACPI.Temp_GPU);
        }

        ReadFullChargeCapacity();
        GetBatteryStatus();

        if (fullCapacity > 0 && chargeCapacity > 0)
        {
            batteryCapacity = Math.Min(val1: 100, val2: ((decimal)chargeCapacity / (decimal)fullCapacity) * 100);
            if (batteryCapacity > 99) BatteryControl.UnSetBatteryLimitFull();
        }


    }

    public static bool IsUsedGPU(int threshold = 10)
    {
        if (GetGpuUse() > threshold)
        {
            Thread.Sleep(millisecondsTimeout: 1000);
            return (GetGpuUse() > threshold);
        }
        return false;
    }


    public static NvidiaGpuControl? GetNvidiaGpuControl()
    {
        if ((bool)GpuControl?.IsNvidia)
            return (NvidiaGpuControl)GpuControl;
        else
            return null;
    }

    public static void RecreateGpuControlWithDelay(int delay = 5)
    {
        // Re-enabling the discrete GPU takes a bit of time,
        // so a simple workaround is to refresh again after that happens
        Task.Run(function: async () =>
        {
            await Task.Delay(delay: TimeSpan.FromSeconds(value: delay));
            RecreateGpuControl();
        });
    }

    public static void RecreateGpuControl()
    {
        try
        {
            GpuControl?.Dispose();

            IGpuControl _gpuControl = new NvidiaGpuControl();
            
            if (_gpuControl.IsValid)
            {
                GpuControl = _gpuControl;
                Logger.WriteLine(logMessage: GpuControl.FullName);
                return;
            }

            _gpuControl.Dispose();

            _gpuControl = new AmdGpuControl();
            if (_gpuControl.IsValid)
            {
                GpuControl = _gpuControl;
                if (GpuControl.FullName.Contains(value: "6850M")) AppConfig.Set(name: "xgm_special", value: 1);
                Logger.WriteLine(logMessage: GpuControl.FullName);
                return;
            }
            _gpuControl.Dispose();

            Logger.WriteLine(logMessage: "dGPU not found");
            GpuControl = null;


        }
        catch (Exception ex)
        {
            Debug.WriteLine(message: "Can't connect to GPU " + ex.ToString());
        }
    }


    public static void KillGPUApps()
    {

        List<string> tokill = new() { "EADesktop", "RadeonSoftware", "epicgameslauncher", "ASUSSmartDisplayControl" };
        foreach (string kill in tokill) ProcessHelper.KillByName(name: kill);

        if (AppConfig.Is(name: "kill_gpu_apps") && GpuControl is not null)
        {
            GpuControl.KillGPUApps();
        }
    }
}
