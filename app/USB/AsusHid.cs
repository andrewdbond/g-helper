using HidSharp;
using HidSharp.Reports;
using System.Diagnostics;

namespace GHelper.USB;
public static class AsusHid
{
    public const int ASUS_ID = 0x0b05;

    public const byte INPUT_ID = 0x5a;
    public const byte AURA_ID = 0x5d;

    static int[] deviceIds = { 0x1a30, 0x1854, 0x1869, 0x1866, 0x19b6, 0x1822, 0x1837, 0x1854, 0x184a, 0x183d, 0x8502, 0x1807, 0x17e0, 0x18c6, 0x1abe };

    static HidStream? auraStream;

    public static IEnumerable<HidDevice>? FindDevices(byte reportId)
    {
        HidDeviceLoader loader = new HidDeviceLoader();
        IEnumerable<HidDevice> deviceList;

        try
        {
            deviceList = loader.GetDevices(vendorID: ASUS_ID).Where(predicate: device => deviceIds.Contains(value: device.ProductID) && device.CanOpen && device.GetMaxFeatureReportLength() > 0);
        }
        catch (Exception ex)
        {
            Logger.WriteLine(logMessage: $"Error enumerating HID devices: {ex.Message}");
            yield break;
        }

        foreach (var device in deviceList)
            if (device.GetReportDescriptor().TryGetReport(type: ReportType.Feature, id: reportId, report: out _))
                yield return device;
    }

    public static HidStream? FindHidStream(byte reportId)
    {
        try
        {
            var devices = FindDevices(reportId: reportId);
            if (devices is null) return null;

            if (AppConfig.IsZ13())
            {
                var z13 = devices.Where(predicate: device => device.ProductID == 0x1a30).FirstOrDefault();
                if (z13 is not null) return z13.Open();
            }

            return devices.FirstOrDefault()?.Open();
        }
        catch (Exception ex)
        {
            Logger.WriteLine(logMessage: $"Error accessing HID device: {ex.Message}");
        }

        return null;
    }

    public static void WriteInput(byte[] data, string log = "USB")
    {
        foreach (var device in FindDevices(reportId: INPUT_ID))
        {
            try
            {
                using (var stream = device.Open())
                {
                    var payload = new byte[device.GetMaxFeatureReportLength()];
                    Array.Copy(sourceArray: data, destinationArray: payload, length: data.Length);
                    stream.SetFeature(buffer: payload);
                    Logger.WriteLine(logMessage: $"{log} Feature {device.ProductID.ToString(format: "X")}|{device.GetMaxFeatureReportLength()}: {BitConverter.ToString(value: data)}");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: $"Error setting feature {device.GetMaxFeatureReportLength()} {device.DevicePath}: {BitConverter.ToString(value: data)} {ex.Message}");

            }
        }
    }

    public static void Write(byte[] data, string log = "USB")
    {
        Write(dataList: new List<byte[]> { data }, log: log);
    }

    public static void Write(List<byte[]> dataList, string log = "USB")
    {
        var devices = FindDevices(reportId: AURA_ID);
        if (devices is null) return;

        foreach (var device in devices)
            using (var stream = device.Open())
                foreach (var data in dataList)
                    try
                    {
                        stream.Write(buffer: data);
                        Logger.WriteLine(logMessage: $"{log} {device.ProductID.ToString(format: "X")}: {BitConverter.ToString(value: data)}");
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine(logMessage: $"Error writing {log} {device.ProductID.ToString(format: "X")}: {ex.Message} {BitConverter.ToString(value: data)} ");
                    }

    }

    public static void WriteAura(byte[] data)
    {

        if (auraStream == null) auraStream = FindHidStream(reportId: AURA_ID);
        if (auraStream == null) return;

        try
        {
            auraStream.Write(buffer: data);
        }
        catch (Exception ex)
        {
            auraStream.Dispose();
            Debug.WriteLine(message: $"Error writing data to HID device: {ex.Message} {BitConverter.ToString(value: data)}");
        }
    }

}

