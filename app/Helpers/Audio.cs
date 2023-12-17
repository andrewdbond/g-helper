using NAudio.CoreAudioApi;

namespace GHelper.Helpers
{
    internal class Audio
    {
        public static bool ToggleMute()
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                var commDevice = enumerator.GetDefaultAudioEndpoint(dataFlow: DataFlow.Capture, role: Role.Communications);
                var consoleDevice = enumerator.GetDefaultAudioEndpoint(dataFlow: DataFlow.Capture, role: Role.Console);
                var mmDevice = enumerator.GetDefaultAudioEndpoint(dataFlow: DataFlow.Capture, role: Role.Multimedia);

                bool status = !commDevice.AudioEndpointVolume.Mute;
                
                commDevice.AudioEndpointVolume.Mute = status;
                consoleDevice.AudioEndpointVolume.Mute = status;
                mmDevice.AudioEndpointVolume.Mute = status;

                Logger.WriteLine(logMessage: commDevice.ToString() + ":" + status);
                Logger.WriteLine(logMessage: consoleDevice.ToString() + ":" + status);
                Logger.WriteLine(logMessage: mmDevice.ToString() + ":" + status);

                return status;
            }
        }
    }
}
