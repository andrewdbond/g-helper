using HidSharp;
using GHelper.USB;

namespace GHelper.Input
{
    public class KeyboardListener
    {

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public KeyboardListener(Action<int> KeyHandler)
        {
            HidStream? input = AsusHid.FindHidStream(reportId: AsusHid.INPUT_ID);
            
            // Fallback
            if (input == null)
            {
                Aura.Init();
                Thread.Sleep(millisecondsTimeout: 1000);
                input = input = AsusHid.FindHidStream(reportId: AsusHid.INPUT_ID);
            }

            if (input == null)
            {
                Logger.WriteLine(logMessage: $"Input device not found");
                return;
            }

            Logger.WriteLine(logMessage: $"Input: {input.Device.DevicePath}");

            var task = Task.Run(action: () =>
            {
                try
                {
                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {

                        // Emergency break
                        if (input == null || !input.CanRead)
                        {
                            Logger.WriteLine(logMessage: "Listener terminated");
                            break;
                        }

                        input.ReadTimeout = int.MaxValue;

                        var data = input.Read();
                        if (data.Length > 1 && data[0] == AsusHid.INPUT_ID && data[1] > 0 && data[1] != 236)
                        {
                            Logger.WriteLine(logMessage: $"Key: {data[1]}");
                            KeyHandler(obj: data[1]);
                        }
                    }

                    Logger.WriteLine(logMessage: "Listener stopped");

                }
                catch (Exception ex)
                {
                    Logger.WriteLine(logMessage: ex.ToString());
                }
            });


        }

        public void Dispose()
        {
            cancellationTokenSource?.Cancel();
        }
    }
}
