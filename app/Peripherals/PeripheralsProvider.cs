using GHelper.Peripherals.Mouse;
using GHelper.Peripherals.Mouse.Models;
using System.Runtime.CompilerServices;

namespace GHelper.Peripherals
{
    public class PeripheralsProvider
    {
        private static readonly object _LOCK = new object();

        public static List<AsusMouse> ConnectedMice = new List<AsusMouse>();

        public static event EventHandler? DeviceChanged;

        private static System.Timers.Timer timer = new System.Timers.Timer(interval: 1000);

        static PeripheralsProvider()
        {
            timer.Elapsed += DeviceTimer_Elapsed;
        }


        private static long lastRefresh;

        public static bool IsMouseConnected()
        {
            lock (_LOCK)
            {
                return ConnectedMice.Count > 0;
            }
        }

        public static bool IsDeviceConnected(IPeripheral peripheral)
        {
            return AllPeripherals().Contains(item: peripheral);
        }

        //Expand if keyboards or other device get supported later.
        public static bool IsAnyPeripheralConnect()
        {
            return IsMouseConnected();
        }

        public static List<IPeripheral> AllPeripherals()
        {
            List<IPeripheral> l = new List<IPeripheral>();
            lock (_LOCK)
            {
                l.AddRange(collection: ConnectedMice);
            }
            return l;
        }

        public static void RefreshBatteryForAllDevices()
        {
            RefreshBatteryForAllDevices(force: false);
        }

        public static void RefreshBatteryForAllDevices(bool force)
        {
            //Polling the battery every 20s should be enough
            if (!force && Math.Abs(value: DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastRefresh) < 20_000) return;
            lastRefresh = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            List<IPeripheral> l = AllPeripherals();

            foreach (IPeripheral m in l)
            {
                if (!m.IsDeviceReady)
                {
                    //Try to sync the device if that hasn't been done yet
                    m.SynchronizeDevice();
                }
                else
                {
                    m.ReadBattery();
                }
            }
        }

        public static void Disconnect(AsusMouse am)
        {
            lock (_LOCK)
            {
                am.Disconnect -= Mouse_Disconnect;
                am.MouseReadyChanged -= MouseReadyChanged;
                am.BatteryUpdated -= BatteryUpdated;
                ConnectedMice.Remove(item: am);
            }
            if (DeviceChanged is not null)
            {
                DeviceChanged(sender: am, e: EventArgs.Empty);
            }
        }

        public static void Connect(AsusMouse am)
        {

            if (IsDeviceConnected(peripheral: am))
            {
                //Mouse already connected;
                return;
            }

            try
            {
                am.Connect();
            }
            catch (IOException e)
            {
                Logger.WriteLine(logMessage: am.GetDisplayName() + " failed to connect to device: " + e);
                return;
            }

            //The Mouse might needs a few ms to register all its subdevices or the sync will fail.
            //Retry 3 times. Do not call this on main thread! It would block the UI

            int tries = 0;
            while (!am.IsDeviceReady && tries < 3)
            {
                Thread.Sleep(millisecondsTimeout: 250);
                Logger.WriteLine(logMessage: am.GetDisplayName() + " synchronising. Try " + (tries + 1));
                am.SynchronizeDevice();
                ++tries;
            }

            lock (_LOCK)
            {
                ConnectedMice.Add(item: am);
            }
            Logger.WriteLine(logMessage: am.GetDisplayName() + " added to the list: " + ConnectedMice.Count + " device are conneted.");


            am.Disconnect += Mouse_Disconnect;
            am.MouseReadyChanged += MouseReadyChanged;
            am.BatteryUpdated += BatteryUpdated;
            if (DeviceChanged is not null)
            {
                DeviceChanged(sender: am, e: EventArgs.Empty);
            }
            UpdateSettingsView();
        }

        private static void BatteryUpdated(object? sender, EventArgs e)
        {
            UpdateSettingsView();
        }

        private static void MouseReadyChanged(object? sender, EventArgs e)
        {
            UpdateSettingsView();
        }

        private static void Mouse_Disconnect(object? sender, EventArgs e)
        {
            if (sender is null)
            {
                return;
            }

            AsusMouse am = (AsusMouse)sender;
            lock (_LOCK)
            {
                ConnectedMice.Remove(item: am);
            }

            Logger.WriteLine(logMessage: am.GetDisplayName() + " reported disconnect. " + ConnectedMice.Count + " device are conneted.");
            am.Dispose();

            UpdateSettingsView();
        }


        private static void UpdateSettingsView()
        {
            Program.settingsForm.Invoke(method: delegate
            {
                Program.settingsForm.VisualizePeripherals();
            });
        }

        [MethodImpl(methodImplOptions: MethodImplOptions.Synchronized)]
        public static void DetectAllAsusMice()
        {
            //Add one line for every supported mouse class here to support them.
            DetectMouse(am: new ChakramX());
            DetectMouse(am: new ChakramXWired());
            DetectMouse(am: new GladiusIIIAimpoint());
            DetectMouse(am: new GladiusIIIAimpointWired());
            DetectMouse(am: new ROGKerisWireless());
            DetectMouse(am: new ROGKerisWirelessWired());
            DetectMouse(am: new ROGKerisWirelessEvaEdition());
            DetectMouse(am: new ROGKerisWirelessEvaEditionWired());
            DetectMouse(am: new TUFM4Wirelss());
            DetectMouse(am: new StrixImpactIIWireless());
            DetectMouse(am: new StrixImpactIIWirelessWired());
            DetectMouse(am: new GladiusIII());
            DetectMouse(am: new GladiusIIIWired());
            DetectMouse(am: new HarpeAceAimLabEdition());
            DetectMouse(am: new HarpeAceAimLabEditionWired());
            DetectMouse(am: new HarpeAceAimLabEditionOmni());
            DetectMouse(am: new TUFM3());
            DetectMouse(am: new TUFM5());
            DetectMouse(am: new KerisWirelssAimpoint());
            DetectMouse(am: new KerisWirelssAimpointWired());
            DetectMouse(am: new PugioII());
            DetectMouse(am: new PugioIIWired());
            DetectMouse(am: new StrixImpactII());
            DetectMouse(am: new Chakram());
            DetectMouse(am: new ChakramWired());
            DetectMouse(am: new ChakramCore());
        }

        public static void DetectMouse(AsusMouse am)
        {
            if (am.IsDeviceConnected() && !IsDeviceConnected(peripheral: am))
            {
                Logger.WriteLine(logMessage: "Detected a new" + am.GetDisplayName() + " . Connecting...");
                Connect(am: am);
            }
        }

        public static void RegisterForDeviceEvents()
        {
            HidSharp.DeviceList.Local.Changed += Device_Changed;
        }

        public static void UnregisterForDeviceEvents()
        {
            HidSharp.DeviceList.Local.Changed -= Device_Changed;
        }

        private static void Device_Changed(object? sender, HidSharp.DeviceListChangedEventArgs e)
        {
            timer.Start();
        }

        private static void DeviceTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            Logger.WriteLine(logMessage: "HID Device Event: Checking for new ASUS Mice");
            DetectAllAsusMice();
            if (AppConfig.IsZ13()) Program.inputDispatcher.Init();
        }
    }
}
