// Source thanks to https://github.com/vddCore/Starlight with some adjustments from me

using GHelper.AnimeMatrix.Communication;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Management;
using System.Text;

namespace Starlight.AnimeMatrix
{
    public class BuiltInAnimation
    {
        public enum Startup
        {
            GlitchConstruction,
            StaticEmergence
        }

        public enum Shutdown
        {
            GlitchOut,
            SeeYa
        }

        public enum Sleeping
        {
            BannerSwipe,
            Starfield
        }

        public enum Running
        {
            BinaryBannerScroll,
            RogLogoGlitch
        }

        public byte AsByte { get; }

        public BuiltInAnimation(
            Running running,
            Sleeping sleeping,
            Shutdown shutdown,
            Startup startup
        )
        {
            AsByte |= (byte)(((int)running & 0x01) << 0);
            AsByte |= (byte)(((int)sleeping & 0x01) << 1);
            AsByte |= (byte)(((int)shutdown & 0x01) << 2);
            AsByte |= (byte)(((int)startup & 0x01) << 3);
        }
    }

    public enum MatrixRotation
    {
        Planar,
        Diagonal
    }

    internal class AnimeMatrixPacket : Packet
    {
        public AnimeMatrixPacket(byte[] command)
            : base(reportId: 0x5E, packetLength: 640, data: command)
        {
        }
    }

    public enum AnimeType
    {
        GA401,
        GA402,
        GU604
    }



    public enum BrightnessMode : byte
    {
        Off = 0,
        Dim = 1,
        Medium = 2,
        Full = 3
    }



    public class AnimeMatrixDevice : Device
    {
        int UpdatePageLength = 490;
        int LedCount = 1450;

        byte[] _displayBuffer;
        List<byte[]> frames = new List<byte[]>();

        public int MaxRows = 61;
        public int MaxColumns = 34;
        public int LedStart = 0;

        public int FullRows = 11;

        private int frameIndex = 0;

        private static AnimeType _model = AnimeType.GA402;

        [System.Runtime.InteropServices.DllImport(dllName: "gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);
        private PrivateFontCollection fonts = new PrivateFontCollection();

        public AnimeMatrixDevice() : base(vendorId: 0x0B05, productId: 0x193B, maxFeatureReportLength: 640)
        {
            string model = GetModel();

            if (model.Contains(value: "401"))
            {
                _model = AnimeType.GA401;

                MaxColumns = 33;
                MaxRows = 55;
                LedCount = 1245;

                UpdatePageLength = 410;

                FullRows = 5;

                LedStart = 1;
            }

            if (model.Contains(value: "GU604"))
            {
                _model = AnimeType.GU604;

                MaxColumns = 39;
                MaxRows = 92;
                LedCount = 1711;
                UpdatePageLength = 630;

                FullRows = 9;
            }

            _displayBuffer = new byte[LedCount];

            LoadMFont();

        }

        private void LoadMFont()
        {
            byte[] fontData = GHelper.Properties.Resources.MFont;
            IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(cb: fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(source: fontData, startIndex: 0, destination: fontPtr, length: fontData.Length);
            uint dummy = 0;

            fonts.AddMemoryFont(memory: fontPtr, length: GHelper.Properties.Resources.MFont.Length);
            AddFontMemResourceEx(pbFont: fontPtr, cbFont: (uint)GHelper.Properties.Resources.MFont.Length, pdv: IntPtr.Zero, pcFonts: ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(ptr: fontPtr);
        }

        public string GetModel()
        {
            using (var searcher = new ManagementObjectSearcher(queryString: @"Select * from Win32_ComputerSystem"))
            {
                foreach (var process in searcher.Get())
                    return process[propertyName: "Model"].ToString();
            }

            return null;

        }

        public byte[] GetBuffer()
        {
            return _displayBuffer;
        }

        public void PresentNextFrame()
        {
            if (frameIndex >= frames.Count) frameIndex = 0;
            _displayBuffer = frames[index: frameIndex];
            Present();
            frameIndex++;
        }

        public void ClearFrames()
        {
            frames.Clear();
            frameIndex = 0;
        }

        public void AddFrame()
        {
            frames.Add(item: _displayBuffer.ToArray());
        }

        public void SendRaw(params byte[] data)
        {
            Set(packet: Packet<AnimeMatrixPacket>(command: data));
        }


        public int Width()
        {
            switch (_model)
            {
                case AnimeType.GA401:
                    return 33;
                case AnimeType.GU604:
                    return 39;
                default:
                    return 34;
            }
        }

        public int FirstX(int y)
        {
            switch (_model)
            {
                case AnimeType.GA401:
                    if (y < 5 && y % 2 == 0)
                    {
                        return 1;
                    }
                    return (int)Math.Ceiling(a: Math.Max(val1: 0, val2: y - 5) / 2F);
                case AnimeType.GU604:
                    if (y < 9 && y % 2 == 0)
                    {
                        return 1;
                    }
                    return (int)Math.Ceiling(a: Math.Max(val1: 0, val2: y - 9) / 2F);

                default:
                    return (int)Math.Ceiling(a: Math.Max(val1: 0, val2: y - 11) / 2F);
            }
        }


        public int Pitch(int y)
        {
            switch (_model)
            {
                case AnimeType.GA401:
                    switch (y)
                    {
                        case 0:
                        case 2:
                        case 4:
                            return 33;
                        case 1:
                        case 3:
                            return 35;
                        default:
                            return 36 - y / 2;
                    }

                case AnimeType.GU604:
                    switch (y)
                    {
                        case 0:
                        case 2:
                        case 4:
                        case 6:
                        case 8:
                            return 38;

                        case 1:
                        case 3:
                        case 5:
                        case 7:
                        case 9:
                            return 39;

                        default:
                            return Width() - FirstX(y: y);
                    }


                default:
                    return Width() - FirstX(y: y);
            }
        }


        public int RowToLinearAddress(int y)
        {
            int ret = LedStart;
            for (var i = 0; i < y; i++)
                ret += Pitch(y: i);

            return ret;
        }

        public void SetLedPlanar(int x, int y, byte value)
        {
            if (!IsRowInRange(row: y)) return;

            if (x >= FirstX(y: y) && x < Width())
                SetLedLinear(address: RowToLinearAddress(y: y) - FirstX(y: y) + x, value: value);
        }

        public void SetLedDiagonal(int x, int y, byte color, int deltaX = 0, int deltaY = 0)
        {
            x += deltaX;
            y -= deltaY;

            int plX = (x - y) / 2;
            int plY = x + y;
            SetLedPlanar(x: plX, y: plY, value: color);
        }


        public void WakeUp()
        {
            Set(packet: Packet<AnimeMatrixPacket>(command: Encoding.ASCII.GetBytes(s: "ASUS Tech.Inc.")));
        }

        public void SetLedLinear(int address, byte value)
        {
            if (!IsAddressableLed(address: address)) return;
            _displayBuffer[address] = value;
        }

        public void SetLedLinearImmediate(int address, byte value)
        {
            if (!IsAddressableLed(address: address)) return;
            _displayBuffer[address] = value;

            Set(packet: Packet<AnimeMatrixPacket>(0xC0, 0x02)
                .AppendData(data: BitConverter.GetBytes(value: (ushort)(address + 1)))
                .AppendData(data: BitConverter.GetBytes(value: (ushort)0x0001))
                .AppendData(value)
            );

            Set(packet: Packet<AnimeMatrixPacket>(0xC0, 0x03));
        }



        public void Clear(bool present = false)
        {
            for (var i = 0; i < _displayBuffer.Length; i++)
                _displayBuffer[i] = 0;

            if (present)
                Present();
        }

        public void Present()
        {

            int page = 0;
            int start, end;

            while (page * UpdatePageLength < LedCount)
            {
                start = page * UpdatePageLength;
                end = Math.Min(val1: LedCount, val2: (page + 1) * UpdatePageLength);

                Set(packet: Packet<AnimeMatrixPacket>(0xC0, 0x02)
                    .AppendData(data: BitConverter.GetBytes(value: (ushort)(start + 1)))
                    .AppendData(data: BitConverter.GetBytes(value: (ushort)(end - start)))
                    .AppendData(data: _displayBuffer[start..end])
                );

                page++;
            }

            Set(packet: Packet<AnimeMatrixPacket>(0xC0, 0x03));
        }

        public void SetDisplayState(bool enable)
        {
            if (enable)
            {
                Set(packet: Packet<AnimeMatrixPacket>(0xC3, 0x01)
                    .AppendData(0x00));
            }
            else
            {
                Set(packet: Packet<AnimeMatrixPacket>(0xC3, 0x01)
                    .AppendData(0x80));
            }
        }

        public void SetBrightness(BrightnessMode mode)
        {
            Set(packet: Packet<AnimeMatrixPacket>(0xC0, 0x04)
                .AppendData((byte)mode)
            );
        }

        public void SetBuiltInAnimation(bool enable)
        {
            var enabled = enable ? (byte)0x00 : (byte)0x80;
            Set(packet: Packet<AnimeMatrixPacket>(0xC4, 0x01, enabled));
        }

        public void SetBuiltInAnimation(bool enable, BuiltInAnimation animation)
        {
            SetBuiltInAnimation(enable: enable);
            Set(packet: Packet<AnimeMatrixPacket>(0xC5, animation.AsByte));
        }


        private void SetBitmapDiagonal(Bitmap bmp, int deltaX = 0, int deltaY = 0, int contrast = 100)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    var pixel = bmp.GetPixel(x: x, y: y);
                    var color = Math.Min(val1: (pixel.R + pixel.G + pixel.B) * contrast / 300, val2: 255);
                    if (color > 20)
                        SetLedDiagonal(x: x, y: y, color: (byte)color, deltaX: deltaX + (FullRows / 2) + 1, deltaY: deltaY - (FullRows / 2) - 1);
                }
            }
        }

        private void SetBitmapLinear(Bitmap bmp, int contrast = 100)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                    if (x % 2 == y % 2)
                    {
                        var pixel = bmp.GetPixel(x: x, y: y);
                        var color = Math.Min(val1: (pixel.R + pixel.G + pixel.B) * contrast / 300, val2: 255);
                        if (color > 20)
                            SetLedPlanar(x: x / 2, y: y, value: (byte)color);
                    }
            }
        }

        public void Text(string text, float fontSize = 10, int x = 0, int y = 0)
        {

            int width = MaxRows - FullRows;
            int height = MaxRows - FullRows;
            int textHeight, textWidth;

            using (Bitmap bmp = new Bitmap(width: width, height: height))
            {
                using (Graphics g = Graphics.FromImage(image: bmp))
                {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;

                    using (Font font = new Font(family: fonts.Families[0], emSize: fontSize, style: FontStyle.Regular, unit: GraphicsUnit.Pixel))
                    {
                        SizeF textSize = g.MeasureString(text: text, font: font);
                        textHeight = (int)textSize.Height;
                        textWidth = (int)textSize.Width;
                        g.DrawString(s: text, font: font, brush: Brushes.White, x: x, y: height - y);
                    }
                }

                SetBitmapDiagonal(bmp: bmp, deltaX: (width - textWidth), deltaY: height);

            }
        }

        public void PresentClock()
        {
            string second = (DateTime.Now.Second % 2 == 0) ? ":" : "  ";
            string time = DateTime.Now.ToString(format: "HH" + second + "mm");

            Clear();
            Text(text: time, fontSize: 15, x: 0, y: 25);
            Text(text: DateTime.Now.ToString(format: "yy'. 'MM'. 'dd"), fontSize: 11.5F, x: 0, y: 14);
            Present();

        }
        public void GenerateFrame(Image image, float zoom = 100, int panX = 0, int panY = 0, InterpolationMode quality = InterpolationMode.Default, int contrast = 100)
        {
            int width = MaxColumns / 2 * 6;
            int height = MaxRows;

            int targetWidth = MaxColumns * 2;

            float scale;

            using (Bitmap bmp = new Bitmap(width: targetWidth, height: height))
            {
                scale = Math.Min(val1: (float)width / (float)image.Width, val2: (float)height / (float)image.Height) * zoom / 100;

                using (var graph = Graphics.FromImage(image: bmp))
                {
                    var scaleWidth = (float)(image.Width * scale);
                    var scaleHeight = (float)(image.Height * scale);

                    graph.InterpolationMode = quality;
                    graph.CompositingQuality = CompositingQuality.HighQuality;
                    graph.SmoothingMode = SmoothingMode.AntiAlias;

                    graph.DrawImage(image: image, x: (float)Math.Round(a: targetWidth - (scaleWidth + panX) * targetWidth / width), y: -panY, width: (float)Math.Round(a: scaleWidth * targetWidth / width), height: scaleHeight);

                }

                Clear();
                SetBitmapLinear(bmp: bmp, contrast: contrast);
            }
        }

        public void GenerateFrameDiagonal(Image image, float zoom = 100, int panX = 0, int panY = 0, InterpolationMode quality = InterpolationMode.Default, int contrast = 100)
        {
            int width = MaxRows - FullRows;
            int height = MaxRows - FullRows*2;
            float scale;

            using (Bitmap bmp = new Bitmap(width: width, height: height))
            {
                scale = Math.Min(val1: (float)width / (float)image.Width, val2: (float)height / (float)image.Height) * zoom / 100;

                using (var graph = Graphics.FromImage(image: bmp))
                {
                    var scaleWidth = (float)(image.Width * scale);
                    var scaleHeight = (float)(image.Height * scale);

                    graph.InterpolationMode = quality;
                    graph.CompositingQuality = CompositingQuality.HighQuality;
                    graph.SmoothingMode = SmoothingMode.AntiAlias;

                    graph.DrawImage(image: image, x: width - scaleWidth, y: height - scaleHeight, width: scaleWidth, height: scaleHeight);

                }

                Clear();
                SetBitmapDiagonal(bmp: bmp, deltaX: -panX, deltaY: height + panY, contrast: contrast);
            }
        }


        private bool IsRowInRange(int row)
        {
            return (row >= 0 && row < MaxRows);
        }

        private bool IsAddressableLed(int address)
        {
            return (address >= 0 && address < LedCount);
        }
    }
}