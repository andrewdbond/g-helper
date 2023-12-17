using System.Drawing.Drawing2D;

namespace GHelper.Helpers
{

    static class Drawing
    {

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(width: diameter, height: diameter);
            Rectangle arc = new Rectangle(location: bounds.Location, size: size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(rect: bounds);
                return path;
            }

            path.AddArc(rect: arc, startAngle: 180, sweepAngle: 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(rect: arc, startAngle: 270, sweepAngle: 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(rect: arc, startAngle: 0, sweepAngle: 90);
            arc.X = bounds.Left;
            path.AddArc(rect: arc, startAngle: 90, sweepAngle: 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int cornerRadius)
        {
            using (GraphicsPath path = RoundedRect(bounds: bounds, radius: cornerRadius))
            {
                graphics.FillPath(brush: brush, path: path);
            }
        }
    }

    public enum ToastIcon
    {
        BrightnessUp,
        BrightnessDown,
        BacklightUp,
        BacklightDown,
        Touchpad,
        Microphone,
        MicrophoneMute,
        FnLock,
        Battery,
        Charger
    }

    public class ToastForm : OSDNativeForm
    {

        protected static string toastText = "Balanced";
        protected static ToastIcon? toastIcon = null;

        protected static System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        public ToastForm()
        {
            timer.Tick += timer_Tick;
            timer.Enabled = false;
            timer.Interval = 2000;
        }

        protected override void PerformPaint(PaintEventArgs e)
        {
            Brush brush = new SolidBrush(color: Color.FromArgb(alpha: 150, baseColor: Color.Black));
            e.Graphics.FillRoundedRectangle(brush: brush, bounds: Bound, cornerRadius: 10);

            StringFormat format = new StringFormat();
            format.LineAlignment = StringAlignment.Center;
            format.Alignment = StringAlignment.Center;

            Bitmap? icon = null;

            switch (toastIcon)
            {
                case ToastIcon.BrightnessUp:
                    icon = Properties.Resources.brightness_up;
                    break;
                case ToastIcon.BrightnessDown:
                    icon = Properties.Resources.brightness_down;
                    break;
                case ToastIcon.BacklightUp:
                    icon = Properties.Resources.backlight_up;
                    break;
                case ToastIcon.BacklightDown:
                    icon = Properties.Resources.backlight_down;
                    break;
                case ToastIcon.Microphone:
                    icon = Properties.Resources.icons8_microphone_96;
                    break;
                case ToastIcon.MicrophoneMute:
                    icon = Properties.Resources.icons8_mute_unmute_96;
                    break;
                case ToastIcon.Touchpad:
                    icon = Properties.Resources.icons8_touchpad_96;
                    break;
                case ToastIcon.FnLock:
                    icon = Properties.Resources.icons8_function;
                    break;
                case ToastIcon.Battery:
                    icon = Properties.Resources.icons8_charged_battery_96;
                    break;
                case ToastIcon.Charger:
                    icon = Properties.Resources.icons8_charging_battery_96;
                    break;

            }

            int shiftX = 0;

            if (icon is not null)
            {
                e.Graphics.DrawImage(image: icon, x: 18, y: 18, width: 64, height: 64);
                shiftX = 40;
            }

            e.Graphics.DrawString(s: toastText,
                font: new Font(familyName: "Segoe UI", emSize: 36f, style: FontStyle.Bold, unit: GraphicsUnit.Pixel),
                brush: new SolidBrush(color: Color.White),
                point: new PointF(x: Bound.Width / 2 + shiftX, y: Bound.Height / 2),
            format: format);

        }

        public void RunToast(string text, ToastIcon? icon = null)
        {

            if (AppConfig.Is(name: "disable_osd")) return;

            Program.settingsForm.Invoke(method: delegate
            {
                //Hide();
                timer.Stop();

                toastText = text;
                toastIcon = icon;

                Screen screen1 = Screen.FromHandle(hwnd: Handle);

                Width = Math.Max(val1: 300, val2: 100 + toastText.Length * 22);
                Height = 100;
                X = (screen1.Bounds.Width - Width) / 2;
                Y = screen1.Bounds.Height - 300 - Height;

                Show();
                timer.Start();
            });

        }

        private void timer_Tick(object? sender, EventArgs e)
        {
            //Debug.WriteLine("Toast end");
            Hide();
            timer.Stop();
        }
    }
}
