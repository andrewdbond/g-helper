using System.Drawing.Drawing2D;

namespace GHelper.UI
{
    public static class GraphicsExtensions
    {
        public static void DrawCircle(this Graphics g, Pen pen,
                                      float centerX, float centerY, float radius)
        {
            g.DrawEllipse(pen: pen, x: centerX - radius, y: centerY - radius,
                          width: radius + radius, height: radius + radius);
        }

        public static void FillCircle(this Graphics g, Brush brush,
                                      float centerX, float centerY, float radius)
        {
            g.FillEllipse(brush: brush, x: centerX - radius, y: centerY - radius,
                          width: radius + radius, height: radius + radius);
        }
    }

    public class Slider : Control
    {
        private float _radius;
        private PointF _thumbPos;
        private SizeF _barSize;
        private PointF _barPos;


        public Color accentColor = Color.FromArgb(alpha: 255, red: 58, green: 174, blue: 239);
        public Color borderColor = Color.White;

        public event EventHandler ValueChanged;

        public Slider()
        {
            // This reduces flicker
            DoubleBuffered = true;
        }


        private int _min = 0;
        public int Min
        {
            get => _min;
            set
            {
                _min = value;
                RecalculateParameters();
            }
        }

        private int _max = 100;
        public int Max
        {
            get => _max;
            set
            {
                _max = value;
                RecalculateParameters();
            }
        }


        private int _step = 1;
        public int Step
        {
            get => _step;
            set
            {
                _step = value;
            }
        }
        private int _value = 50;
        public int Value
        {
            get => _value;
            set
            {

                value = (int)Math.Round(a: value / (float)_step) * _step;

                if (_value != value)
                {
                    _value = value;
                    ValueChanged?.Invoke(sender: this, e: EventArgs.Empty);
                    RecalculateParameters();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e: e);

            Brush brushAccent = new SolidBrush(color: accentColor);
            Brush brushEmpty = new SolidBrush(color: Color.Gray);
            Brush brushBorder = new SolidBrush(color: borderColor);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillRectangle(brush: brushEmpty,
                x: _barPos.X, y: _barPos.Y, width: _barSize.Width, height: _barSize.Height);
            e.Graphics.FillRectangle(brush: brushAccent,
                x: _barPos.X, y: _barPos.Y, width: _thumbPos.X - _barPos.X, height: _barSize.Height);

            e.Graphics.FillCircle(brush: brushBorder, centerX: _thumbPos.X, centerY: _thumbPos.Y, radius: _radius);
            e.Graphics.FillCircle(brush: brushAccent, centerX: _thumbPos.X, centerY: _thumbPos.Y, radius: 0.7f * _radius);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e: e);
            RecalculateParameters();
        }

        private void RecalculateParameters()
        {
            _radius = 0.4F * ClientSize.Height;
            _barSize = new SizeF(width: ClientSize.Width - 2 * _radius, height: ClientSize.Height * 0.15F);
            _barPos = new PointF(x: _radius, y: (ClientSize.Height - _barSize.Height) / 2);
            _thumbPos = new PointF(
                x: _barSize.Width / (Max - Min) * (Value - Min) + _barPos.X,
                y: _barPos.Y + 0.5f * _barSize.Height);
            Invalidate();
        }

        bool _moving = false;
        SizeF _delta;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e: e);

            // Difference between tumb and mouse position.
            _delta = new SizeF(width: e.Location.X - _thumbPos.X, height: e.Location.Y - _thumbPos.Y);
            if (_delta.Width * _delta.Width + _delta.Height * _delta.Height <= _radius * _radius)
            {
                // Clicking inside thumb.
                _moving = true;
            }

            _calculateValue(e: e);

        }

        private void _calculateValue(MouseEventArgs e)
        {
            float thumbX = e.Location.X; // - _delta.Width;
            if (thumbX < _barPos.X)
            {
                thumbX = _barPos.X;
            }
            else if (thumbX > _barPos.X + _barSize.Width)
            {
                thumbX = _barPos.X + _barSize.Width;
            }
            Value = (int)Math.Round(a: Min + (thumbX - _barPos.X) * (Max - Min) / _barSize.Width);

        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e: e);
            if (_moving)
            {
                _calculateValue(e: e);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e: e);
            _moving = false;
        }

    }

}