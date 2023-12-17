using NAudio.CoreAudioApi;
using NAudio.Wave;
using Starlight.AnimeMatrix;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Timers;

namespace GHelper.AnimeMatrix
{

    public class AniMatrixControl
    {

        SettingsForm settings;

        System.Timers.Timer matrixTimer = default!;
        public AnimeMatrixDevice? device;

        double[]? AudioValues;
        WasapiCapture? AudioDevice;

        public bool IsValid => device != null;

        private long lastPresent;
        private List<double> maxes = new List<double>();

        public AniMatrixControl(SettingsForm settingsForm)
        {
            settings = settingsForm;

            try
            {
                device = new AnimeMatrixDevice();
                Task.Run(action: device.WakeUp);
                matrixTimer = new System.Timers.Timer(interval: 100);
                matrixTimer.Elapsed += MatrixTimer_Elapsed;
            }
            catch
            {
                device = null;
            }

        }

        public void SetMatrix(bool wakeUp = false)
        {

            if (!IsValid) return;

            int brightness = AppConfig.Get(name: "matrix_brightness");
            int running = AppConfig.Get(name: "matrix_running");

            bool auto = AppConfig.Is(name: "matrix_auto");

            if (brightness < 0) brightness = 0;
            if (running < 0) running = 0;

            BuiltInAnimation animation = new BuiltInAnimation(
                running: (BuiltInAnimation.Running)running,
                sleeping: BuiltInAnimation.Sleeping.Starfield,
                shutdown: BuiltInAnimation.Shutdown.SeeYa,
                startup: BuiltInAnimation.Startup.StaticEmergence
            );

            StopMatrixTimer();
            StopMatrixAudio();

            try
            {
                device.SetProvider();
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: ex.Message);
                return;
            }

            if (wakeUp && AppConfig.ContainsModel(contains: "401")) device.WakeUp();

            if (brightness == 0 || (auto && SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Online))
            {
                device.SetDisplayState(enable: false);
                device.SetDisplayState(enable: false); // some devices are dumb
                Logger.WriteLine(logMessage: "Matrix Off");
            }
            else
            {
                device.SetDisplayState(enable: true);
                device.SetBrightness(mode: (BrightnessMode)brightness);

                switch (running)
                {
                    case 2:
                        SetMatrixPicture(fileName: AppConfig.GetString(name: "matrix_picture"));
                        break;
                    case 3:
                        SetMatrixClock();
                        break;
                    case 4:
                        SetMatrixAudio();
                        break;
                    default:
                        device.SetBuiltInAnimation(enable: true, animation: animation);
                        Logger.WriteLine(logMessage: "Matrix builtin " + animation.AsByte);
                        break;

                }

                //mat.SetBrightness((BrightnessMode)brightness);
            }

        }
        private void StartMatrixTimer(int interval = 100)
        {
            matrixTimer.Interval = interval;
            matrixTimer.Start();
        }

        private void StopMatrixTimer()
        {
            matrixTimer.Stop();
        }


        private void MatrixTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            //if (!IsValid) return;

            switch (AppConfig.Get(name: "matrix_running"))
            {
                case 2:
                    device.PresentNextFrame();
                    break;
                case 3:
                    device.PresentClock();
                    break;
            }

        }


        public void SetMatrixClock()
        {
            device.SetBuiltInAnimation(enable: false);
            StartMatrixTimer(interval: 1000);
            Logger.WriteLine(logMessage: "Matrix Clock");
        }

        public void Dispose()
        {
            StopMatrixAudio();
        }

        void StopMatrixAudio()
        {
            if (AudioDevice is not null)
            {
                try
                {
                    AudioDevice.StopRecording();
                    AudioDevice.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(logMessage: ex.ToString());
                }
            }
        }

        void SetMatrixAudio()
        {
            if (!IsValid) return;

            device.SetBuiltInAnimation(enable: false);
            StopMatrixTimer();
            StopMatrixAudio();

            try
            {
                using (var enumerator = new MMDeviceEnumerator())
                using (MMDevice device = enumerator.GetDefaultAudioEndpoint(dataFlow: DataFlow.Render, role: Role.Console))
                {
                    AudioDevice = new WasapiLoopbackCapture(captureDevice: device);
                    WaveFormat fmt = AudioDevice.WaveFormat;

                    AudioValues = new double[fmt.SampleRate / 1000];
                    AudioDevice.DataAvailable += WaveIn_DataAvailable;
                    AudioDevice.StartRecording();
                    Logger.WriteLine(logMessage: "Matrix Audio");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: ex.ToString());
            }

        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            int bytesPerSamplePerChannel = AudioDevice.WaveFormat.BitsPerSample / 8;
            int bytesPerSample = bytesPerSamplePerChannel * AudioDevice.WaveFormat.Channels;
            int bufferSampleCount = e.Buffer.Length / bytesPerSample;

            if (bufferSampleCount >= AudioValues.Length)
            {
                bufferSampleCount = AudioValues.Length;
            }

            if (bytesPerSamplePerChannel == 2 && AudioDevice.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                for (int i = 0; i < bufferSampleCount; i++)
                    AudioValues[i] = BitConverter.ToInt16(value: e.Buffer, startIndex: i * bytesPerSample);
            }
            else if (bytesPerSamplePerChannel == 4 && AudioDevice.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                for (int i = 0; i < bufferSampleCount; i++)
                    AudioValues[i] = BitConverter.ToInt32(value: e.Buffer, startIndex: i * bytesPerSample);
            }
            else if (bytesPerSamplePerChannel == 4 && AudioDevice.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                for (int i = 0; i < bufferSampleCount; i++)
                    AudioValues[i] = BitConverter.ToSingle(value: e.Buffer, startIndex: i * bytesPerSample);
            }

            double[] paddedAudio = FftSharp.Pad.ZeroPad(input: AudioValues);
            double[] fftMag = FftSharp.Transform.FFTmagnitude(input: paddedAudio);

            PresentAudio(audio: fftMag);
        }

        private void DrawBar(int pos, double h)
        {
            int dx = pos * 2;
            int dy = 20;

            byte color;

            for (int y = 0; y < h - (h % 2); y++)
                for (int x = 0; x < 2 - (y % 2); x++)
                {
                    //color = (byte)(Math.Min(1,(h - y - 2)*2) * 255);
                    device.SetLedPlanar(x: x + dx, y: dy + y, value: (byte)(h * 255 / 30));
                    device.SetLedPlanar(x: x + dx, y: dy - y, value: 255);
                }
        }

        void PresentAudio(double[] audio)
        {

            if (Math.Abs(value: DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastPresent) < 70) return;
            lastPresent = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            device.Clear();

            int size = 20;
            double[] bars = new double[size];
            double max = 2, maxAverage;

            for (int i = 0; i < size; i++)
            {
                bars[i] = Math.Sqrt(d: audio[i] * 10000);
                if (bars[i] > max) max = bars[i];
            }

            maxes.Add(item: max);
            if (maxes.Count > 20) maxes.RemoveAt(index: 0);
            maxAverage = maxes.Average();

            for (int i = 0; i < size; i++) DrawBar(pos: 20 - i, h: bars[i] * 20 / maxAverage);

            device.Present();
        }


        public void OpenMatrixPicture()
        {
            string fileName = null;

            Thread t = new Thread(start: () =>
            {
                OpenFileDialog of = new OpenFileDialog();
                of.Filter = "Image Files (*.bmp;*.jpg;*.jpeg,*.png,*.gif)|*.BMP;*.JPG;*.JPEG;*.PNG;*.GIF";
                if (of.ShowDialog() == DialogResult.OK)
                {
                    fileName = of.FileName;
                }
                return;
            });

            t.SetApartmentState(state: ApartmentState.STA);
            t.Start();
            t.Join();

            if (fileName is not null)
            {
                AppConfig.Set(name: "matrix_picture", value: fileName);
                AppConfig.Set(name: "matrix_running", value: 2);

                SetMatrixPicture(fileName: fileName);
                settings.SetMatrixRunning(mode: 2);

            }

        }

        public void SetMatrixPicture(string fileName, bool visualise = true)
        {

            if (!IsValid) return;
            StopMatrixTimer();

            try
            {
                using (var fs = new FileStream(path: fileName, mode: FileMode.Open))
                //using (var ms = new MemoryStream())
                {
                    /*
                    ms.SetLength(0);
                    fs.CopyTo(ms);
                    ms.Position = 0;
                    */
                    using (Image image = Image.FromStream(stream: fs))
                    {
                        ProcessPicture(image: image);
                        Logger.WriteLine(logMessage: "Matrix " + fileName);
                    }

                    fs.Close();
                    if (visualise) settings.VisualiseMatrix(image: fileName);
                }
            }
            catch
            {
                Debug.WriteLine(message: "Error loading picture");
                return;
            }

        }

        protected void ProcessPicture(Image image)
        {
            device.SetBuiltInAnimation(enable: false);
            device.ClearFrames();

            int matrixX = AppConfig.Get(name: "matrix_x", empty: 0);
            int matrixY = AppConfig.Get(name: "matrix_y", empty: 0);

            int matrixZoom = AppConfig.Get(name: "matrix_zoom", empty: 100);
            int matrixContrast = AppConfig.Get(name: "matrix_contrast", empty: 100);
            
            int matrixSpeed = AppConfig.Get(name: "matrix_speed", empty: 50);

            MatrixRotation rotation = (MatrixRotation)AppConfig.Get(name: "matrix_rotation", empty: 0); 

            InterpolationMode matrixQuality = (InterpolationMode)AppConfig.Get(name: "matrix_quality", empty: 0);


            FrameDimension dimension = new FrameDimension(guid: image.FrameDimensionsList[0]);
            int frameCount = image.GetFrameCount(dimension: dimension);

            if (frameCount > 1)
            {
                var delayPropertyBytes = image.GetPropertyItem(propid: 0x5100).Value;
                var frameDelay = BitConverter.ToInt32(value: delayPropertyBytes) * 10;

                for (int i = 0; i < frameCount; i++)
                {
                    image.SelectActiveFrame(dimension: dimension, frameIndex: i);

                    if (rotation == MatrixRotation.Planar)
                        device.GenerateFrame(image: image, zoom: matrixZoom, panX: matrixX, panY: matrixY, quality: matrixQuality, contrast: matrixContrast);
                    else
                        device.GenerateFrameDiagonal(image: image, zoom: matrixZoom, panX: matrixX, panY: matrixY, quality: matrixQuality, contrast: matrixContrast);
                    
                    device.AddFrame();
                }


                Logger.WriteLine(logMessage: "GIF Delay:" + frameDelay);
                StartMatrixTimer(interval: Math.Max(val1: matrixSpeed, val2: frameDelay));

                //image.SelectActiveFrame(dimension, 0);

            }
            else
            {
                if (rotation == MatrixRotation.Planar)
                    device.GenerateFrame(image: image, zoom: matrixZoom, panX: matrixX, panY: matrixY, quality: matrixQuality, contrast: matrixContrast);
                else
                    device.GenerateFrameDiagonal(image: image, zoom: matrixZoom, panX: matrixX, panY: matrixY, quality: matrixQuality, contrast: matrixContrast);

                device.Present();
            }

        }


    }
}
