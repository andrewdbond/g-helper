// Reference : thanks to https://github.com/RomanYazvinsky/ for initial discovery of XGM payloads

using HidSharp;
using System.Text;

namespace GHelper.USB
{
    public static class XGM
    {
        const int XGM_ID = 0x1970;
        const int ASUS_ID = 0x0b05;

        public static void Write(byte[] data)
        {
            HidDeviceLoader loader = new HidDeviceLoader();
            try
            {
                HidDevice device = loader.GetDevices(vendorID: ASUS_ID, productID: XGM_ID).Where(predicate: device => device.CanOpen && device.GetMaxFeatureReportLength() >= 300).FirstOrDefault();

                if (device is null)
                {
                    Logger.WriteLine(logMessage: "XGM SUB device not found");
                    return;
                }

                using (HidStream hidStream = device.Open())
                {
                    var payload = new byte[300];
                    Array.Copy(sourceArray: data, destinationArray: payload, length: data.Length);

                    hidStream.SetFeature(buffer: payload);
                    Logger.WriteLine(logMessage: "XGM-" + device.ProductID + "|" + device.GetMaxFeatureReportLength() + ":" + BitConverter.ToString(value: data));

                    hidStream.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: $"Error accessing XGM device: {ex}");
            }

        }

        public static void Init()
        {
            Write(data: Encoding.ASCII.GetBytes(s: "^ASUS Tech.Inc."));
        }

        public static void Light(bool status)
        {
            Write(data: new byte[] { 0x5e, 0xc5, status ? (byte)0x50 : (byte)0 });
        }


        public static void Reset()
        {
            Write(data: new byte[] { 0x5e, 0xd1, 0x02 });
        }

        public static void SetFan(byte[] curve)
        {
            if (AsusACPI.IsInvalidCurve(curve: curve)) return;

            byte[] msg = new byte[19];
            Array.Copy(sourceArray: new byte[] { 0x5e, 0xd1, 0x01 }, destinationArray: msg, length: 3);
            Array.Copy(sourceArray: curve, sourceIndex: 0, destinationArray: msg, destinationIndex: 3, length: curve.Length);

            Write(data: msg);
        }
    }
}
