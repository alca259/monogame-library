using MauiColor = Microsoft.Maui.Graphics.Color;

namespace MonoGame.Editor.Maui.Views.Dialogs;

public sealed partial class RgbaColorPickerDialog : ContentPage
{
    private float _hue;
    private float _saturation = 1f;
    private float _brightness = 1f;
    private int   _alpha      = 255;

    private bool _svDragging;
    private bool _hueDragging;
    private bool _alphaDragging;
    private bool _updating;

    private readonly SvPickerDrawable    _svDrawable    = new SvPickerDrawable();
    private readonly HueBarDrawable      _hueDrawable   = new HueBarDrawable();
    private readonly AlphaBarDrawable    _alphaDrawable = new AlphaBarDrawable();

    private readonly TaskCompletionSource<MauiColor?> _tcs = new();

    private RgbaColorPickerDialog() => InitializeComponent();

    public static async Task<MauiColor?> ShowAsync(INavigation navigation, MauiColor? initial = null)
    {
        var dlg = new RgbaColorPickerDialog();
        dlg.SvPicker.Drawable    = dlg._svDrawable;
        dlg.HuePicker.Drawable   = dlg._hueDrawable;
        dlg.AlphaPicker.Drawable = dlg._alphaDrawable;
        dlg.SetupGestures();
        if (initial is not null)
            dlg.LoadFromColor(initial);
        dlg.RefreshAll();
        await navigation.PushModalAsync(dlg);
        return await dlg._tcs.Task;
    }

    // ── Gesture setup ─────────────────────────────────────────────────────────

    private void SetupGestures()
    {
        var sv = new PointerGestureRecognizer();
        sv.PointerPressed  += (_, e) => { _svDragging = true;  OnSvPointer(e.GetPosition(SvPicker)); };
        sv.PointerMoved    += (_, e) => { if (_svDragging) OnSvPointer(e.GetPosition(SvPicker)); };
        sv.PointerReleased += (_, e) => { _svDragging = false; OnSvPointer(e.GetPosition(SvPicker)); };
        SvPicker.GestureRecognizers.Add(sv);

        var hue = new PointerGestureRecognizer();
        hue.PointerPressed  += (_, e) => { _hueDragging = true;  OnHuePointer(e.GetPosition(HuePicker)); };
        hue.PointerMoved    += (_, e) => { if (_hueDragging) OnHuePointer(e.GetPosition(HuePicker)); };
        hue.PointerReleased += (_, e) => { _hueDragging = false; OnHuePointer(e.GetPosition(HuePicker)); };
        HuePicker.GestureRecognizers.Add(hue);

        var alpha = new PointerGestureRecognizer();
        alpha.PointerPressed  += (_, e) => { _alphaDragging = true;  OnAlphaPointer(e.GetPosition(AlphaPicker)); };
        alpha.PointerMoved    += (_, e) => { if (_alphaDragging) OnAlphaPointer(e.GetPosition(AlphaPicker)); };
        alpha.PointerReleased += (_, e) => { _alphaDragging = false; OnAlphaPointer(e.GetPosition(AlphaPicker)); };
        AlphaPicker.GestureRecognizers.Add(alpha);
    }

    // ── Pointer handlers ──────────────────────────────────────────────────────

    private void OnSvPointer(Point? pos)
    {
        if (pos is null) return;
        double w = SvPicker.Width;
        double h = SvPicker.Height;
        if (w <= 0 || h <= 0) return;
        _saturation = (float)Math.Clamp(pos.Value.X / w, 0.0, 1.0);
        _brightness = 1f - (float)Math.Clamp(pos.Value.Y / h, 0.0, 1.0);
        RefreshAll();
    }

    private void OnHuePointer(Point? pos)
    {
        if (pos is null) return;
        double h = HuePicker.Height;
        if (h <= 0) return;
        _hue = (float)Math.Clamp(pos.Value.Y / h, 0.0, 1.0);
        RefreshAll();
    }

    private void OnAlphaPointer(Point? pos)
    {
        if (pos is null) return;
        double w = AlphaPicker.Width;
        if (w <= 0) return;
        _alpha = (int)Math.Clamp(pos.Value.X / w * 255, 0.0, 255.0);
        RefreshAll();
    }

    // ── State management ──────────────────────────────────────────────────────

    private void LoadFromColor(MauiColor c)
    {
        _alpha = (int)Math.Round(c.Alpha * 255);
        RgbToHsv(c.Red, c.Green, c.Blue, out _hue, out _saturation, out _brightness);
    }

    private void RefreshAll()
    {
        _updating = true;

        MauiColor rgb     = HsvToColor(_hue, _saturation, _brightness);
        MauiColor current = MauiColor.FromRgba(rgb.Red, rgb.Green, rgb.Blue, _alpha / 255f);

        _svDrawable.SetHsv(_hue, _saturation, _brightness);
        SvPicker.Invalidate();

        _hueDrawable.Hue = _hue;
        HuePicker.Invalidate();

        _alphaDrawable.SetColor(rgb, _alpha / 255f);
        AlphaPicker.Invalidate();

        ColorPreviewSwatch.BackgroundColor = current;
        HexEntry.Text = $"#{(int)Math.Round(rgb.Red   * 255):X2}" +
                        $"{(int)Math.Round(rgb.Green * 255):X2}" +
                        $"{(int)Math.Round(rgb.Blue  * 255):X2}" +
                        $"{_alpha:X2}";

        _updating = false;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnHexEntryCompleted(object sender, EventArgs e)
    {
        if (_updating) return;
        string raw = (HexEntry.Text ?? string.Empty).Trim().TrimStart('#');
        if (raw.Length is not (6 or 8)) return;
        try
        {
            int r = Convert.ToInt32(raw[..2], 16);
            int g = Convert.ToInt32(raw[2..4], 16);
            int b = Convert.ToInt32(raw[4..6], 16);
            int a = raw.Length == 8 ? Convert.ToInt32(raw[6..8], 16) : 255;
            _alpha = a;
            RgbToHsv(r / 255f, g / 255f, b / 255f, out _hue, out _saturation, out _brightness);
            RefreshAll();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RgbaColorPicker] Invalid hex color format, using default: {ex.Message}");
        }
    }

    private void OnAlphaReset(object sender, EventArgs e)
    {
        _alpha = 255;
        RefreshAll();
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    private void OnCancel(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        _ = Navigation.PopModalAsync();
    }

    private void OnSubmit(object sender, EventArgs e)
    {
        MauiColor rgb = HsvToColor(_hue, _saturation, _brightness);
        _tcs.TrySetResult(MauiColor.FromRgba(rgb.Red, rgb.Green, rgb.Blue, _alpha / 255f));
        _ = Navigation.PopModalAsync();
    }

    // ── Color conversions ─────────────────────────────────────────────────────

    internal static MauiColor HsvToColor(float h, float s, float v)
    {
        if (s < 1e-6f) return MauiColor.FromRgb(v, v, v);
        float hh = h * 6f;
        if (hh >= 6f) hh = 0f;
        int   i = (int)hh;
        float f = hh - i;
        float p = v * (1f - s);
        float q = v * (1f - s * f);
        float t = v * (1f - s * (1f - f));
        return i switch
        {
            0 => MauiColor.FromRgb(v, t, p),
            1 => MauiColor.FromRgb(q, v, p),
            2 => MauiColor.FromRgb(p, v, t),
            3 => MauiColor.FromRgb(p, q, v),
            4 => MauiColor.FromRgb(t, p, v),
            _ => MauiColor.FromRgb(v, p, q),
        };
    }

    private static void RgbToHsv(float r, float g, float b,
                                   out float h, out float s, out float v)
    {
        float max   = MathF.Max(r, MathF.Max(g, b));
        float min   = MathF.Min(r, MathF.Min(g, b));
        float delta = max - min;
        v = max;
        s = max < 1e-6f ? 0f : delta / max;
        if (delta < 1e-6f) { h = 0f; return; }
        if (max == r)      h = (g - b) / delta % 6f;
        else if (max == g) h = (b - r) / delta + 2f;
        else               h = (r - g) / delta + 4f;
        h /= 6f;
        if (h < 0f) h += 1f;
    }

    // ── Private drawables ─────────────────────────────────────────────────────

    private sealed class SvPickerDrawable : IDrawable
    {
        private static readonly LinearGradientPaint WhiteToTransparent = new()
        {
            StartColor = Colors.White,
            EndColor   = Colors.Transparent,
            StartPoint = new Point(0, 0),
            EndPoint   = new Point(1, 0),
        };

        private static readonly LinearGradientPaint TransparentToBlack = new()
        {
            StartColor = Colors.Transparent,
            EndColor   = Colors.Black,
            StartPoint = new Point(0, 0),
            EndPoint   = new Point(0, 1),
        };

        private MauiColor _hueColor   = Colors.Red;
        private float     _saturation = 1f;
        private float     _brightness = 1f;

        public void SetHsv(float hue, float saturation, float brightness)
        {
            _hueColor   = HsvToColor(hue, 1f, 1f);
            _saturation = saturation;
            _brightness = brightness;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            canvas.FillColor = _hueColor;
            canvas.FillRectangle(dirtyRect);
            canvas.RestoreState();

            canvas.SaveState();
            canvas.SetFillPaint(WhiteToTransparent, dirtyRect);
            canvas.FillRectangle(dirtyRect);
            canvas.RestoreState();

            canvas.SaveState();
            canvas.SetFillPaint(TransparentToBlack, dirtyRect);
            canvas.FillRectangle(dirtyRect);
            canvas.RestoreState();

            float cx = dirtyRect.Width  * _saturation;
            float cy = dirtyRect.Height * (1f - _brightness);
            canvas.SaveState();
            canvas.StrokeSize  = 1.5f;
            canvas.StrokeColor = Colors.White;
            canvas.DrawCircle(cx, cy, 6);
            canvas.StrokeColor = MauiColor.FromArgb("#80000000");
            canvas.DrawCircle(cx, cy, 7);
            canvas.RestoreState();
        }
    }

    private sealed class HueBarDrawable : IDrawable
    {
        private static readonly LinearGradientPaint Rainbow = new()
        {
            StartPoint    = new Point(0, 0),
            EndPoint      = new Point(0, 1),
            GradientStops =
            [
                new PaintGradientStop(0.000f, MauiColor.FromArgb("#FF0000")),
                new PaintGradientStop(0.167f, MauiColor.FromArgb("#FFFF00")),
                new PaintGradientStop(0.333f, MauiColor.FromArgb("#00FF00")),
                new PaintGradientStop(0.500f, MauiColor.FromArgb("#00FFFF")),
                new PaintGradientStop(0.667f, MauiColor.FromArgb("#0000FF")),
                new PaintGradientStop(0.833f, MauiColor.FromArgb("#FF00FF")),
                new PaintGradientStop(1.000f, MauiColor.FromArgb("#FF0000")),
            ],
        };

        public float Hue { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            canvas.SetFillPaint(Rainbow, dirtyRect);
            canvas.FillRectangle(dirtyRect);
            canvas.RestoreState();

            float y = dirtyRect.Height * Hue;
            canvas.SaveState();
            canvas.StrokeSize  = 2f;
            canvas.StrokeColor = Colors.White;
            canvas.DrawLine(0, y, dirtyRect.Width, y);
            canvas.RestoreState();
        }
    }

    private sealed class AlphaBarDrawable : IDrawable
    {
        private static readonly MauiColor CheckerGray  = MauiColor.FromArgb("#888888");
        private static readonly MauiColor CheckerWhite = Colors.White;

        private LinearGradientPaint _colorPaint = new()
        {
            StartColor = Colors.Transparent,
            EndColor   = Colors.Red,
            StartPoint = new Point(0, 0),
            EndPoint   = new Point(1, 0),
        };
        private float _alpha = 1f;

        public void SetColor(MauiColor color, float alpha)
        {
            _alpha = alpha;
            _colorPaint = new LinearGradientPaint
            {
                StartColor = Colors.Transparent,
                EndColor   = color,
                StartPoint = new Point(0, 0),
                EndPoint   = new Point(1, 0),
            };
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            const float cell = 6f;
            int cols = (int)(dirtyRect.Width  / cell) + 1;
            int rows = (int)(dirtyRect.Height / cell) + 1;
            canvas.SaveState();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    canvas.FillColor = (r + c) % 2 == 0 ? CheckerGray : CheckerWhite;
                    canvas.FillRectangle(
                        c * cell, r * cell,
                        MathF.Min(cell, dirtyRect.Width  - c * cell),
                        MathF.Min(cell, dirtyRect.Height - r * cell));
                }
            canvas.RestoreState();

            canvas.SaveState();
            canvas.SetFillPaint(_colorPaint, dirtyRect);
            canvas.FillRectangle(dirtyRect);
            canvas.RestoreState();

            float x = dirtyRect.Width * _alpha;
            canvas.SaveState();
            canvas.StrokeSize  = 2f;
            canvas.StrokeColor = Colors.White;
            canvas.DrawLine(x, 0, x, dirtyRect.Height);
            canvas.RestoreState();
        }
    }
}
