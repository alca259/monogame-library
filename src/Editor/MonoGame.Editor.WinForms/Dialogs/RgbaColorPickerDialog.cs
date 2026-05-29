using System.Runtime.InteropServices;

namespace MonoGame.Editor.WinForms.Dialogs;

/// <summary>Diálogo modal para seleccionar un color RGBA con un selector de gradiente HSV.</summary>
public sealed class RgbaColorPickerDialog : Form
{
    private float _hue;        // 0–360
    private float _saturation; // 0–1
    private float _value;      // 0–1
    private byte  _alpha;      // 0–255

    private readonly Panel         _previewPanel;
    private readonly SatValPanel   _satValPanel;
    private readonly HueStripPanel _hueStripPanel;
    private readonly TrackBar      _alphaTrack;
    private readonly TextBox       _hexBox;
    private bool _updating;

    /// <summary>Color seleccionado cuando el usuario hizo clic en Elegir.</summary>
    public Microsoft.Xna.Framework.Color SelectedColor { get; private set; }

    public RgbaColorPickerDialog(Microsoft.Xna.Framework.Color initial)
    {
        SelectedColor = initial;
        RgbToHsv(initial.R, initial.G, initial.B, out _hue, out _saturation, out _value);
        _alpha = initial.A;

        Text            = "Select Color";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox     = false;
        MaximizeBox     = false;
        ShowInTaskbar   = false;
        StartPosition   = FormStartPosition.CenterParent;
        ClientSize      = new System.Drawing.Size(280, 330);

        _previewPanel = new Panel
        {
            Location    = new System.Drawing.Point(10, 10),
            Size        = new System.Drawing.Size(36, 36),
            BackColor   = ToDrawingColor(initial),
            BorderStyle = BorderStyle.FixedSingle,
        };

        Label titleLabel = new()
        {
            Text     = "Select Color",
            Location = new System.Drawing.Point(54, 20),
            AutoSize = true,
        };

        _satValPanel = new SatValPanel
        {
            Location = new System.Drawing.Point(10, 56),
            Size     = new System.Drawing.Size(220, 200),
        };
        _satValPanel.PositionChanged += OnSatValChanged;

        _hueStripPanel = new HueStripPanel
        {
            Location = new System.Drawing.Point(238, 56),
            Size     = new System.Drawing.Size(22, 200),
        };
        _hueStripPanel.HueChanged += OnHueChanged;

        Button resetBtn = new()
        {
            Text     = "×",
            Location = new System.Drawing.Point(238, 260),
            Size     = new System.Drawing.Size(22, 22),
        };
        resetBtn.Click += (_, _) => ResetColor();

        Label alphaLabel = new()
        {
            Text     = "A",
            Location = new System.Drawing.Point(10, 266),
            Width    = 14,
        };

        _alphaTrack = new TrackBar
        {
            Location      = new System.Drawing.Point(28, 258),
            Size          = new System.Drawing.Size(202, 30),
            Minimum       = 0,
            Maximum       = 255,
            Value         = _alpha,
            TickFrequency = 32,
        };
        _alphaTrack.ValueChanged += OnAlphaChanged;

        _hexBox = new TextBox
        {
            Location  = new System.Drawing.Point(10, 295),
            Width     = 130,
            MaxLength = 9,
            Text      = $"#{ToHex(initial)}",
        };
        _hexBox.Leave += OnHexLeave;

        Button cancelBtn = new()
        {
            Text         = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location     = new System.Drawing.Point(148, 292),
            Width        = 58,
        };

        Button okBtn = new()
        {
            Text         = "Choose",
            DialogResult = DialogResult.OK,
            Location     = new System.Drawing.Point(210, 292),
            Width        = 60,
        };
        okBtn.Click += (_, _) => SelectedColor = CurrentColor();

        AcceptButton = okBtn;
        CancelButton = cancelBtn;

        Controls.AddRange(new Control[]
        {
            _previewPanel, titleLabel,
            _satValPanel, _hueStripPanel, resetBtn,
            alphaLabel, _alphaTrack,
            _hexBox, cancelBtn, okBtn,
        });

        _satValPanel.SetHue(_hue);
        _satValPanel.SetPosition(_saturation, 1f - _value);
        _hueStripPanel.SetHue(_hue);
    }

    #region Event handlers

    private void OnSatValChanged(float sat, float valNorm)
    {
        if (_updating) return;
        _saturation = sat;
        _value      = 1f - valNorm;
        RefreshPreviewAndHex();
    }

    private void OnHueChanged(float hue)
    {
        if (_updating) return;
        _hue = hue;
        _satValPanel.SetHue(hue);
        RefreshPreviewAndHex();
    }

    private void OnAlphaChanged(object? sender, EventArgs e)
    {
        if (_updating) return;
        _alpha = (byte)_alphaTrack.Value;
        RefreshPreviewAndHex();
    }

    private void OnHexLeave(object? sender, EventArgs e)
    {
        string hex = _hexBox.Text.Trim().TrimStart('#');
        if (hex.Length != 8) return;

        try
        {
            byte r = Convert.ToByte(hex[..2], 16);
            byte g = Convert.ToByte(hex[2..4], 16);
            byte b = Convert.ToByte(hex[4..6], 16);
            byte a = Convert.ToByte(hex[6..8], 16);

            _updating = true;
            RgbToHsv(r, g, b, out _hue, out _saturation, out _value);
            _alpha = a;
            _satValPanel.SetHue(_hue);
            _satValPanel.SetPosition(_saturation, 1f - _value);
            _hueStripPanel.SetHue(_hue);
            _alphaTrack.Value      = a;
            _previewPanel.BackColor = System.Drawing.Color.FromArgb(a, r, g, b);
        }
        catch (FormatException) { }
        finally { _updating = false; }
    }

    private void ResetColor()
    {
        _updating   = true;
        _hue        = 0f;
        _saturation = 0f;
        _value      = 1f;
        _alpha      = 255;
        _satValPanel.SetHue(0f);
        _satValPanel.SetPosition(0f, 0f);
        _hueStripPanel.SetHue(0f);
        _alphaTrack.Value = 255;
        _updating = false;
        RefreshPreviewAndHex();
    }

    private void RefreshPreviewAndHex()
    {
        Microsoft.Xna.Framework.Color c = CurrentColor();
        _previewPanel.BackColor = ToDrawingColor(c);
        _updating = true;
        _hexBox.Text = $"#{ToHex(c)}";
        _updating = false;
    }

    #endregion

    #region Color math

    private Microsoft.Xna.Framework.Color CurrentColor()
    {
        HsvToRgb(_hue, _saturation, _value, out byte r, out byte g, out byte b);
        return new Microsoft.Xna.Framework.Color(r, g, b, _alpha);
    }

    private static System.Drawing.Color ToDrawingColor(Microsoft.Xna.Framework.Color c) =>
        System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);

    private static string ToHex(Microsoft.Xna.Framework.Color c) =>
        $"{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";

    private static void RgbToHsv(byte r, byte g, byte b,
        out float h, out float s, out float v)
    {
        float rf = r / 255f, gf = g / 255f, bf = b / 255f;
        float max   = Math.Max(rf, Math.Max(gf, bf));
        float min   = Math.Min(rf, Math.Min(gf, bf));
        float delta = max - min;

        v = max;
        s = max < 0.0001f ? 0f : delta / max;

        if (delta < 0.0001f) { h = 0f; return; }

        if (Math.Abs(max - rf) < 0.0001f)
            h = 60f * (((gf - bf) / delta) % 6f);
        else if (Math.Abs(max - gf) < 0.0001f)
            h = 60f * ((bf - rf) / delta + 2f);
        else
            h = 60f * ((rf - gf) / delta + 4f);

        if (h < 0f) h += 360f;
    }

    private static void HsvToRgb(float h, float s, float v,
        out byte r, out byte g, out byte b)
    {
        HsvToRgbF(h, s, v, out float rf, out float gf, out float bf);
        r = (byte)(rf * 255f);
        g = (byte)(gf * 255f);
        b = (byte)(bf * 255f);
    }

    internal static void HsvToRgbF(float h, float s, float v,
        out float r, out float g, out float b)
    {
        if (s < 0.0001f) { r = g = b = v; return; }

        float hh = h / 60f;
        int   i  = (int)hh;
        float ff = hh - i;
        float p  = v * (1f - s);
        float q  = v * (1f - s * ff);
        float t  = v * (1f - s * (1f - ff));

        switch (i % 6)
        {
            case 0:  r = v; g = t; b = p; return;
            case 1:  r = q; g = v; b = p; return;
            case 2:  r = p; g = v; b = t; return;
            case 3:  r = p; g = q; b = v; return;
            case 4:  r = t; g = p; b = v; return;
            default: r = v; g = p; b = q; return;
        }
    }

    #endregion

    #region Nested panels

    /// <summary>Panel que muestra el gradiente de saturación/valor para el tono actual.</summary>
    private sealed class SatValPanel : Control
    {
        private System.Drawing.Bitmap? _cache;
        private float _hue;
        private float _indicatorX; // 0–1 (saturación)
        private float _indicatorY; // 0–1 (1 - valor)

        public event Action<float, float>? PositionChanged;

        public SatValPanel() { DoubleBuffered = true; }

        public void SetHue(float hue)
        {
            if (Math.Abs(_hue - hue) < 0.01f) return;
            _hue = hue;
            _cache?.Dispose();
            _cache = null;
            Invalidate();
        }

        public void SetPosition(float x, float y)
        {
            _indicatorX = Math.Clamp(x, 0f, 1f);
            _indicatorY = Math.Clamp(y, 0f, 1f);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_cache is null || _cache.Width != Width || _cache.Height != Height)
                RebuildCache();

            if (_cache is not null)
                e.Graphics.DrawImage(_cache, 0, 0);

            float px = _indicatorX * (Width  - 1);
            float py = _indicatorY * (Height - 1);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using System.Drawing.Pen white = new(System.Drawing.Color.White, 1.5f);
            e.Graphics.DrawEllipse(white, px - 5f, py - 5f, 10f, 10f);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) UpdatePosition(e.X, e.Y);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) UpdatePosition(e.X, e.Y);
        }

        private void UpdatePosition(int mx, int my)
        {
            _indicatorX = Math.Clamp(mx / (float)(Width  - 1), 0f, 1f);
            _indicatorY = Math.Clamp(my / (float)(Height - 1), 0f, 1f);
            Invalidate();
            PositionChanged?.Invoke(_indicatorX, _indicatorY);
        }

        private void RebuildCache()
        {
            _cache?.Dispose();
            int w = Math.Max(1, Width);
            int h = Math.Max(1, Height);
            _cache = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            System.Drawing.Imaging.BitmapData data = _cache.LockBits(
                new System.Drawing.Rectangle(0, 0, w, h),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int stride  = data.Stride;
            byte[] buf  = new byte[stride * h];

            for (int y = 0; y < h; y++)
            {
                float val = 1f - y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float sat = x / (float)(w - 1);
                    HsvToRgbF(_hue, sat, val, out float rf, out float gf, out float bf);
                    int offset = y * stride + x * 4;
                    buf[offset]     = (byte)(bf * 255f);
                    buf[offset + 1] = (byte)(gf * 255f);
                    buf[offset + 2] = (byte)(rf * 255f);
                    buf[offset + 3] = 255;
                }
            }

            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            _cache.UnlockBits(data);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _cache?.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>Panel que muestra la franja de arco iris de tonos vertical.</summary>
    private sealed class HueStripPanel : Control
    {
        private System.Drawing.Bitmap? _cache;
        private float _hue; // 0–360

        public event Action<float>? HueChanged;

        public HueStripPanel() { DoubleBuffered = true; }

        public void SetHue(float hue)
        {
            _hue = hue;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_cache is null || _cache.Height != Height)
                RebuildCache();

            if (_cache is not null)
                e.Graphics.DrawImage(_cache, 0, 0);

            float py = _hue / 360f * (Height - 1);
            using System.Drawing.Pen white = new(System.Drawing.Color.White, 1.5f);
            e.Graphics.DrawLine(white, 0, py, Width, py);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) UpdateHue(e.Y);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) UpdateHue(e.Y);
        }

        private void UpdateHue(int my)
        {
            _hue = Math.Clamp(my / (float)(Height - 1), 0f, 1f) * 360f;
            Invalidate();
            HueChanged?.Invoke(_hue);
        }

        private void RebuildCache()
        {
            _cache?.Dispose();
            int w = Math.Max(1, Width);
            int h = Math.Max(1, Height);
            _cache = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            System.Drawing.Imaging.BitmapData data = _cache.LockBits(
                new System.Drawing.Rectangle(0, 0, w, h),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int stride = data.Stride;
            byte[] buf = new byte[stride * h];

            for (int y = 0; y < h; y++)
            {
                float hue = y / (float)(h - 1) * 360f;
                HsvToRgbF(hue, 1f, 1f, out float rf, out float gf, out float bf);
                byte rb = (byte)(rf * 255f);
                byte gb = (byte)(gf * 255f);
                byte bb = (byte)(bf * 255f);

                for (int x = 0; x < w; x++)
                {
                    int offset = y * stride + x * 4;
                    buf[offset]     = bb;
                    buf[offset + 1] = gb;
                    buf[offset + 2] = rb;
                    buf[offset + 3] = 255;
                }
            }

            Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            _cache.UnlockBits(data);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _cache?.Dispose();
            base.Dispose(disposing);
        }
    }

    #endregion
}
