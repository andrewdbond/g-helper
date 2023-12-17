using Microsoft.Win32;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace GHelper.UI
{

    public class RForm : Form
    {

        public static Color colorEco = Color.FromArgb(alpha: 255, red: 6, green: 180, blue: 138);
        public static Color colorStandard = Color.FromArgb(alpha: 255, red: 58, green: 174, blue: 239);
        public static Color colorTurbo = Color.FromArgb(alpha: 255, red: 255, green: 32, blue: 32);
        public static Color colorCustom = Color.FromArgb(alpha: 255, red: 255, green: 128, blue: 0);


        public static Color buttonMain;
        public static Color buttonSecond;

        public static Color formBack;
        public static Color foreMain;
        public static Color borderMain;
        public static Color chartMain;
        public static Color chartGrid;

        [DllImport(dllName: "UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
        public static extern bool CheckSystemDarkModeStatus();

        [DllImport(dllName: "DwmApi")] //System.Runtime.InteropServices
        private static extern int DwmSetWindowAttribute(nint hwnd, int attr, int[] attrValue, int attrSize);

        public bool darkTheme = false;

        public static void InitColors(bool darkTheme)
        {
            if (darkTheme)
            {
                buttonMain = Color.FromArgb(alpha: 255, red: 55, green: 55, blue: 55);
                buttonSecond = Color.FromArgb(alpha: 255, red: 38, green: 38, blue: 38);

                formBack = Color.FromArgb(alpha: 255, red: 28, green: 28, blue: 28);
                foreMain = Color.FromArgb(alpha: 255, red: 240, green: 240, blue: 240);
                borderMain = Color.FromArgb(alpha: 255, red: 50, green: 50, blue: 50);

                chartMain = Color.FromArgb(alpha: 255, red: 35, green: 35, blue: 35);
                chartGrid = Color.FromArgb(alpha: 255, red: 70, green: 70, blue: 70);
            }
            else
            {
                buttonMain = SystemColors.ControlLightLight;
                buttonSecond = SystemColors.ControlLight;

                formBack = SystemColors.Control;
                foreMain = SystemColors.ControlText;
                borderMain = Color.LightGray;

                chartMain = SystemColors.ControlLightLight;
                chartGrid = Color.LightGray;
            }
        }

        private static bool IsDarkTheme()
        {
            string? uiMode = AppConfig.GetString(name: "ui_mode");

            if (uiMode is not null && uiMode.ToLower() == "dark")
            {
                return true;
            }

            if (uiMode is not null && uiMode.ToLower() == "light")
            {
                return false;
            }

            if (uiMode is not null && uiMode.ToLower() == "windows")
            {
                return CheckSystemDarkModeStatus();
            }

            using var key = Registry.CurrentUser.OpenSubKey(name: @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var registryValueObject = key?.GetValue(name: "AppsUseLightTheme");

            if (registryValueObject == null) return false;
            return (int)registryValueObject <= 0;
        }

        public bool InitTheme(bool setDPI = false)
        {
            bool newDarkTheme = IsDarkTheme();
            bool changed = darkTheme != newDarkTheme;
            darkTheme = newDarkTheme;

            InitColors(darkTheme: darkTheme);

            if (setDPI)
                ControlHelper.Resize(container: this);

            if (changed)
            {
                DwmSetWindowAttribute(hwnd: Handle, attr: 20, attrValue: new[] { darkTheme ? 1 : 0 }, attrSize: 4);
                ControlHelper.Adjust(container: this, invert: changed);
            }

            return changed;

        }

    }


    public class RCheckBox : CheckBox
    {

    }


    public class RComboBox : ComboBox
    {
        private Color borderColor = Color.Gray;
        [DefaultValue(type: typeof(Color), value: "Gray")]
        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                if (borderColor != value)
                {
                    borderColor = value;
                    Invalidate();
                }
            }
        }


        private Color buttonColor = Color.FromArgb(alpha: 255, red: 255, green: 255, blue: 255);
        [DefaultValue(type: typeof(Color), value: "255, 255, 255")]
        public Color ButtonColor
        {
            get { return buttonColor; }
            set
            {
                if (buttonColor != value)
                {
                    buttonColor = value;
                    Invalidate();
                }
            }
        }

        private Color arrowColor = Color.Black;
        [DefaultValue(type: typeof(Color), value: "Black")]
        public Color ArrowColor
        {
            get { return arrowColor; }
            set
            {
                if (arrowColor != value)
                {
                    arrowColor = value;
                    Invalidate();
                }
            }
        }


        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_PAINT && DropDownStyle != ComboBoxStyle.Simple)
            {
                var clientRect = ClientRectangle;
                var dropDownButtonWidth = SystemInformation.HorizontalScrollBarArrowWidth;
                var outerBorder = new Rectangle(location: clientRect.Location,
                    size: new Size(width: clientRect.Width - 1, height: clientRect.Height - 1));
                var innerBorder = new Rectangle(x: outerBorder.X + 1, y: outerBorder.Y + 1,
                    width: outerBorder.Width - dropDownButtonWidth - 2, height: outerBorder.Height - 2);
                var innerInnerBorder = new Rectangle(x: innerBorder.X + 1, y: innerBorder.Y + 1,
                    width: innerBorder.Width - 2, height: innerBorder.Height - 2);
                var dropDownRect = new Rectangle(x: innerBorder.Right + 1, y: innerBorder.Y,
                    width: dropDownButtonWidth, height: innerBorder.Height + 1);
                if (RightToLeft == RightToLeft.Yes)
                {
                    innerBorder.X = clientRect.Width - innerBorder.Right;
                    innerInnerBorder.X = clientRect.Width - innerInnerBorder.Right;
                    dropDownRect.X = clientRect.Width - dropDownRect.Right;
                    dropDownRect.Width += 1;
                }
                var innerBorderColor = Enabled ? BackColor : SystemColors.Control;
                var outerBorderColor = Enabled ? BorderColor : SystemColors.ControlDark;
                var buttonColor = Enabled ? ButtonColor : SystemColors.Control;
                var middle = new Point(x: dropDownRect.Left + dropDownRect.Width / 2,
                    y: dropDownRect.Top + dropDownRect.Height / 2);
                var arrow = new Point[]
                {
                new Point(x: middle.X - 3, y: middle.Y - 2),
                new Point(x: middle.X + 4, y: middle.Y - 2),
                new Point(x: middle.X, y: middle.Y + 2)
                };
                var ps = new PAINTSTRUCT();
                bool shoulEndPaint = false;
                nint dc;
                if (m.WParam == nint.Zero)
                {
                    dc = BeginPaint(hWnd: Handle, lpPaint: ref ps);
                    m.WParam = dc;
                    shoulEndPaint = true;
                }
                else
                {
                    dc = m.WParam;
                }

                var rgn = CreateRectRgn(x1: innerInnerBorder.Left, y1: innerInnerBorder.Top,
                    x2: innerInnerBorder.Right, y2: innerInnerBorder.Bottom);

                SelectClipRgn(hDC: dc, hRgn: rgn);
                DefWndProc(m: ref m);
                DeleteObject(hObject: rgn);
                rgn = CreateRectRgn(x1: clientRect.Left, y1: clientRect.Top,
                    x2: clientRect.Right, y2: clientRect.Bottom);
                SelectClipRgn(hDC: dc, hRgn: rgn);
                using (var g = Graphics.FromHdc(hdc: dc))
                {
                    using (var b = new SolidBrush(color: buttonColor))
                    {
                        g.FillRectangle(brush: b, rect: dropDownRect);
                    }
                    using (var b = new SolidBrush(color: arrowColor))
                    {
                        g.FillPolygon(brush: b, points: arrow);
                    }
                    using (var p = new Pen(color: innerBorderColor))
                    {
                        g.DrawRectangle(pen: p, rect: innerBorder);
                        g.DrawRectangle(pen: p, rect: innerInnerBorder);
                    }
                    using (var p = new Pen(color: outerBorderColor))
                    {
                        g.DrawRectangle(pen: p, rect: outerBorder);
                    }
                }
                if (shoulEndPaint)
                    EndPaint(hWnd: Handle, lpPaint: ref ps);
                DeleteObject(hObject: rgn);
            }
            else
                base.WndProc(m: ref m);
        }

        private const int WM_PAINT = 0xF;
        [StructLayout(layoutKind: LayoutKind.Sequential)]
        public struct RECT
        {
            public int L, T, R, B;
        }
        [StructLayout(layoutKind: LayoutKind.Sequential)]
        public struct PAINTSTRUCT
        {
            public nint hdc;
            public bool fErase;
            public int rcPaint_left;
            public int rcPaint_top;
            public int rcPaint_right;
            public int rcPaint_bottom;
            public bool fRestore;
            public bool fIncUpdate;
            public int reserved1;
            public int reserved2;
            public int reserved3;
            public int reserved4;
            public int reserved5;
            public int reserved6;
            public int reserved7;
            public int reserved8;
        }
        [DllImport(dllName: "user32.dll")]
        private static extern nint BeginPaint(nint hWnd,
            [In, Out] ref PAINTSTRUCT lpPaint);

        [DllImport(dllName: "user32.dll")]
        private static extern bool EndPaint(nint hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport(dllName: "gdi32.dll")]
        public static extern int SelectClipRgn(nint hDC, nint hRgn);

        [DllImport(dllName: "user32.dll")]
        public static extern int GetUpdateRgn(nint hwnd, nint hrgn, bool fErase);
        public enum RegionFlags
        {
            ERROR = 0,
            NULLREGION = 1,
            SIMPLEREGION = 2,
            COMPLEXREGION = 3,
        }
        [DllImport(dllName: "gdi32.dll")]
        internal static extern bool DeleteObject(nint hObject);

        [DllImport(dllName: "gdi32.dll")]
        private static extern nint CreateRectRgn(int x1, int y1, int x2, int y2);
    }

    public class RButton : Button
    {
        //Fields
        private int borderSize = 5;

        private int borderRadius = 5;
        public int BorderRadius
        {
            get { return borderRadius; }
            set
            {
                borderRadius = value;
            }
        }

        private Color borderColor = Color.Transparent;
        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                borderColor = value;
            }
        }


        private bool activated = false;
        public bool Activated
        {
            get { return activated; }
            set
            {
                if (activated != value)
                    Invalidate();
                activated = value;

            }
        }

        private bool secondary = false;
        public bool Secondary
        {
            get { return secondary; }
            set
            {
                secondary = value;
            }
        }

        public RButton()
        {
            DoubleBuffered = true;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
        }

        private GraphicsPath GetFigurePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float curveSize = radius * 2F;

            path.StartFigure();
            path.AddArc(x: rect.X, y: rect.Y, width: curveSize, height: curveSize, startAngle: 180, sweepAngle: 90);
            path.AddArc(x: rect.Right - curveSize, y: rect.Y, width: curveSize, height: curveSize, startAngle: 270, sweepAngle: 90);
            path.AddArc(x: rect.Right - curveSize, y: rect.Bottom - curveSize, width: curveSize, height: curveSize, startAngle: 0, sweepAngle: 90);
            path.AddArc(x: rect.X, y: rect.Bottom - curveSize, width: curveSize, height: curveSize, startAngle: 90, sweepAngle: 90);
            path.CloseFigure();
            return path;
        }


        protected override void OnPaint(PaintEventArgs pevent)
        {

            base.OnPaint(pevent: pevent);

            float ratio = pevent.Graphics.DpiX / 192.0f;
            int border = (int)(ratio * borderSize);

            Rectangle rectSurface = ClientRectangle;
            Rectangle rectBorder = Rectangle.Inflate(rect: rectSurface, x: -border, y: -border);

            Color borderDrawColor = activated ? borderColor : Color.Transparent;

            using (GraphicsPath pathSurface = GetFigurePath(rect: rectSurface, radius: borderRadius + border))
            using (GraphicsPath pathBorder = GetFigurePath(rect: rectBorder, radius: borderRadius))
            using (Pen penSurface = new Pen(color: Parent.BackColor, width: border))
            using (Pen penBorder = new Pen(color: borderDrawColor, width: border))
            {
                penBorder.Alignment = PenAlignment.Outset;
                pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Region = new Region(path: pathSurface);
                pevent.Graphics.DrawPath(pen: penSurface, path: pathSurface);
                pevent.Graphics.DrawPath(pen: penBorder, path: pathBorder);
            }

            if (!Enabled && ForeColor != SystemColors.ControlText)
            {
                var rect = pevent.ClipRectangle;
                if (Image is not null)
                {
                    rect.Y += Image.Height;
                    rect.Height -= Image.Height;
                }
                TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak;
                TextRenderer.DrawText(dc: pevent.Graphics, text: Text, font: Font, bounds: rect, foreColor: Color.Gray, flags: flags);
            }


        }

    }
}
