using System.ComponentModel;
using HidSharp;

namespace GHelper.AnimeMatrix.Communication.Platform
{
    internal class WindowsUsbProvider : UsbProvider
    {
        protected HidDevice HidDevice { get; }
        protected HidStream HidStream { get; }

        public WindowsUsbProvider(ushort vendorId, ushort productId, string path, int timeout = 500) : base(vendorId: vendorId, productId: productId)
        {
            try
            {
                HidDevice = DeviceList.Local.GetHidDevices(vendorID: vendorId, productID: productId)
                   .First(predicate: x => x.DevicePath.Contains(value: path));
            }
            catch
            {
                throw new IOException(message: "HID device was not found on your machine.");
            }

            var config = new OpenConfiguration();
            config.SetOption(option: OpenOption.Interruptible, value: true);
            config.SetOption(option: OpenOption.Exclusive, value: false);
            config.SetOption(option: OpenOption.Priority, value: 10);
            HidStream = HidDevice.Open(openConfig: config);
            HidStream.ReadTimeout = timeout;
            HidStream.WriteTimeout = timeout;
        }

        public WindowsUsbProvider(ushort vendorId, ushort productId, int maxFeatureReportLength)
            : base(vendorId: vendorId, productId: productId)
        {
            try
            {
                HidDevice = DeviceList.Local
                    .GetHidDevices(vendorID: vendorId, productID: productId)
                    .First(predicate: x => x.GetMaxFeatureReportLength() == maxFeatureReportLength);
            }
            catch
            {
                throw new IOException(message: "AniMe Matrix control device was not found on your machine.");
            }

            var config = new OpenConfiguration();
            config.SetOption(option: OpenOption.Interruptible, value: true);
            config.SetOption(option: OpenOption.Exclusive, value: false);
            config.SetOption(option: OpenOption.Priority, value: 10);

            HidStream = HidDevice.Open(openConfig: config);
        }

        public override void Set(byte[] data)
        {
            WrapException(action: () =>
            {
                HidStream.SetFeature(buffer: data);
                HidStream.Flush();
            });
        }

        public override byte[] Get(byte[] data)
        {
            var outData = new byte[data.Length];
            Array.Copy(sourceArray: data, destinationArray: outData, length: data.Length);

            WrapException(action: () =>
            {
                HidStream.GetFeature(buffer: outData);
                HidStream.Flush();
            });

            return data;
        }

        public override void Read(byte[] data)
        {
            WrapException(action: () =>
            {
                HidStream.Read(buffer: data);
            });
        }

        public override void Write(byte[] data)
        {
            WrapException(action: () =>
            {
                HidStream.Write(buffer: data);
                HidStream.Flush();
            });
        }

        public override void Dispose()
        {
            HidStream.Dispose();
        }

        private void WrapException(Action action)
        {
            try
            {
                action();
            }
            catch (IOException e)
            {
                if (e.InnerException is Win32Exception w32e)
                {
                    if (w32e.NativeErrorCode != 0)
                    {
                        throw;
                    }
                }
            }
        }
    }
}