using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Windows.Forms;
using MonoGame.Editor.Winforms.Controls;
using MonoGame.Editor.Winforms.Theme;

namespace MonoGame.Editor.Winforms.Drawers;

/// <summary>
/// Helpers estáticos para construir filas de propiedades en las tarjetas de Behaviour.
/// Cada método Build* devuelve un control de altura fija 26 px listo para Dock=Top.
/// </summary>
internal static class PropertyControlHelper
{
    private const int RowH = 26;
    private const int LabelW = 90;

    // ── Ensamblaje de card ────────────────────────────────────────────────────

    /// <summary>
    /// Ensambla una lista de filas en un Panel vertical. Los controles se añaden en orden
    /// inverso para que Dock=Top los muestre en el orden natural de la lista.
    /// </summary>
    internal static Panel BuildCard(IReadOnlyList<Control> rows)
    {
        Panel card = new()
        {
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor    = EditorColors.PanelBackground,
        };
        for (int i = rows.Count - 1; i >= 0; i--)
        {
            rows[i].Dock = DockStyle.Top;
            card.Controls.Add(rows[i]);
        }
        return card;
    }

    // ── Fila base ─────────────────────────────────────────────────────────────

    internal static Panel BuildPropertyRow(string labelText, Control control,
        string? textColor = null, string? bgColor = null)
    {
        TableLayoutPanel row = new()
        {
            Height      = RowH,
            ColumnCount = 2,
            RowCount    = 1,
            BackColor   = EditorColors.PanelBackground,
            Margin      = Padding.Empty,
            Padding     = new Padding(2, 1, 2, 1),
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelW));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
        row.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        Label lbl = new()
        {
            Text      = labelText,
            ForeColor = textColor is not null
                ? ColorTranslator.FromHtml(textColor)
                : EditorColors.TextSecondary,
            BackColor = bgColor is not null
                ? ColorTranslator.FromHtml(bgColor)
                : EditorColors.PanelBackground,
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock      = DockStyle.Fill,
        };

        control.Dock = DockStyle.Fill;

        row.Controls.Add(lbl,     0, 0);
        row.Controls.Add(control, 1, 0);
        return row;
    }

    // ── Separador de sección ──────────────────────────────────────────────────

    internal static Panel BuildHeaderSeparator(string title)
    {
        Panel row = new()
        {
            Height    = 22,
            BackColor = EditorColors.PanelBackgroundAlt,
        };

        Label lbl = new()
        {
            Text      = title,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.PrimaryBold,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(6, 0, 0, 0),
            Dock      = DockStyle.Fill,
        };
        row.Controls.Add(lbl);
        return row;
    }

    // ── Slider ────────────────────────────────────────────────────────────────

    internal static Panel BuildSliderField(string label, double value, double min, double max,
        Action<double> onChange, bool readOnly = false)
    {
        double safeMax = Math.Max(min + 0.0001, max);
        double clamped = Math.Clamp(value, min, safeMax);

        TrackBar track = new()
        {
            Minimum   = 0,
            Maximum   = 1000,
            Value     = ValueToTick(clamped, min, safeMax),
            AutoSize  = false,
            Height    = RowH - 6,
            TickStyle = TickStyle.None,
            Enabled   = !readOnly,
            Dock      = DockStyle.Fill,
        };

        Label valLbl = new()
        {
            Text      = clamped.ToString("G4", CultureInfo.InvariantCulture),
            Width     = 48,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Small,
            Dock      = DockStyle.Right,
        };

        track.ValueChanged += (_, _) =>
        {
            double real = TickToValue(track.Value, min, safeMax);
            valLbl.Text = real.ToString("G4", CultureInfo.InvariantCulture);
            onChange(real);
        };

        TableLayoutPanel inner = new()
        {
            ColumnCount = 2,
            RowCount    = 1,
            Margin      = Padding.Empty,
            Padding     = Padding.Empty,
            BackColor   = EditorColors.PanelBackground,
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50f));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        inner.Controls.Add(track,  0, 0);
        inner.Controls.Add(valLbl, 1, 0);

        return BuildPropertyRow(label, inner);
    }

    // ── Número / stepper ──────────────────────────────────────────────────────

    internal static Panel BuildNumberField(string label, double value,
        Action<double> onChange, bool readOnly = false)
    {
        AxisStepper stepper = new()
        {
            ShowAxisTag = false,
            Value       = value,
            Step        = 0.1,
            Enabled     = !readOnly,
        };
        stepper.ValueCommitted += (_, _) => onChange(stepper.Value);
        return BuildPropertyRow(label, stepper);
    }

    // ── Texto ─────────────────────────────────────────────────────────────────

    internal static Panel BuildTextField(string label, string value,
        Action<string> onChange, bool readOnly = false)
    {
        TextBox tb = new()
        {
            Text        = value,
            ReadOnly    = readOnly,
            BackColor   = EditorColors.InputBackground,
            ForeColor   = EditorColors.TextPrimary,
            Font        = EditorFonts.Small,
            BorderStyle = BorderStyle.None,
        };
        tb.Leave += (_, _) => onChange(tb.Text);
        tb.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) onChange(tb.Text);
        };
        return BuildPropertyRow(label, tb);
    }

    // ── Bool ──────────────────────────────────────────────────────────────────

    internal static Panel BuildBoolField(string label, bool value,
        Action<bool> onChange, bool readOnly = false)
    {
        CheckBox chk = new()
        {
            Checked    = value,
            Enabled    = !readOnly,
            BackColor  = EditorColors.PanelBackground,
            ForeColor  = EditorColors.TextPrimary,
            CheckAlign = ContentAlignment.MiddleLeft,
        };
        chk.CheckedChanged += (_, _) => onChange(chk.Checked);
        return BuildPropertyRow(label, chk);
    }

    // ── Color ─────────────────────────────────────────────────────────────────

    internal static Panel BuildColorField(string label, JsonElement value,
        Action<JsonElement> onChange)
    {
        int r = value.TryGetProperty("R", out JsonElement rp) ? rp.GetInt32() : 0;
        int g = value.TryGetProperty("G", out JsonElement gp) ? gp.GetInt32() : 0;
        int b = value.TryGetProperty("B", out JsonElement bp) ? bp.GetInt32() : 0;
        int a = value.TryGetProperty("A", out JsonElement ap) ? ap.GetInt32() : 255;

        Panel swatch = new()
        {
            Width     = 28,
            BackColor = Color.FromArgb(a, r, g, b),
            BorderStyle = BorderStyle.FixedSingle,
            Cursor    = Cursors.Hand,
        };

        Label hexLbl = new()
        {
            Text      = $"#{r:X2}{g:X2}{b:X2}{a:X2}",
            ForeColor = EditorColors.TextSecondary,
            Font      = EditorFonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock      = DockStyle.Fill,
        };

        swatch.Click += (_, _) =>
        {
            using ColorDialog dlg = new()
            {
                Color         = swatch.BackColor,
                FullOpen      = true,
                AllowFullOpen = true,
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            Color picked = dlg.Color;
            swatch.BackColor = picked;
            int nr = picked.R, ng = picked.G, nb = picked.B, na = picked.A;
            hexLbl.Text = $"#{nr:X2}{ng:X2}{nb:X2}{na:X2}";

            uint packed = ((uint)na << 24) | ((uint)nb << 16) | ((uint)ng << 8) | (uint)nr;
            onChange(JsonSerializer.SerializeToElement(new { R = (byte)nr, G = (byte)ng, B = (byte)nb, A = (byte)na, PackedValue = packed }));
        };

        TableLayoutPanel inner = new()
        {
            ColumnCount = 2,
            RowCount    = 1,
            Margin      = Padding.Empty,
            Padding     = Padding.Empty,
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30f));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        inner.Controls.Add(swatch, 0, 0);
        inner.Controls.Add(hexLbl, 1, 0);

        return BuildPropertyRow(label, inner);
    }

    // ── File picker ───────────────────────────────────────────────────────────

    internal static Panel BuildFilePickerField(string label, string value,
        string baseFolder, string[]? extensions, Action<string> onChange, bool readOnly = false)
    {
        TextBox pathTb = new()
        {
            Text        = Path.GetFileName(value),
            ReadOnly    = true,
            BackColor   = EditorColors.InputBackground,
            ForeColor   = EditorColors.TextSecondary,
            Font        = EditorFonts.Small,
            BorderStyle = BorderStyle.None,
            Dock        = DockStyle.Fill,
        };

        Button browseBtn = new()
        {
            Text      = "…",
            Width     = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = EditorColors.PanelBackgroundAlt,
            ForeColor = EditorColors.TextPrimary,
            Font      = EditorFonts.Primary,
            Enabled   = !readOnly,
            Dock      = DockStyle.Right,
            TabStop   = false,
        };
        browseBtn.FlatAppearance.BorderSize = 0;

        string filterStr = BuildFileFilter(extensions);
        browseBtn.Click += (_, _) =>
        {
            using OpenFileDialog dlg = new()
            {
                Title            = "Select file",
                Filter           = filterStr,
                InitialDirectory = Directory.Exists(baseFolder) ? baseFolder : string.Empty,
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string rel = Path.GetRelativePath(baseFolder, dlg.FileName);
            pathTb.Text = Path.GetFileName(rel);
            onChange(rel);
        };

        Panel inner = new();
        inner.Controls.Add(pathTb);
        inner.Controls.Add(browseBtn);

        return BuildPropertyRow(label, inner);
    }

    // ── Vector2 ───────────────────────────────────────────────────────────────

    internal static (double X, double Y) GetVector2(JsonElement el) => (
        el.TryGetProperty("X", out JsonElement x) ? x.GetDouble() : 0.0,
        el.TryGetProperty("Y", out JsonElement y) ? y.GetDouble() : 0.0);

    internal static Panel BuildVector2Field(string label, double x, double y,
        Action<double> onX, Action<double> onY, bool readOnly = false)
    {
        AxisStepper spX = new() { Axis = "X", Value = x, Step = 0.1, Enabled = !readOnly };
        AxisStepper spY = new() { Axis = "Y", Value = y, Step = 0.1, Enabled = !readOnly };
        spX.ValueCommitted += (_, _) => onX(spX.Value);
        spY.ValueCommitted += (_, _) => onY(spY.Value);

        TableLayoutPanel inner = new()
        {
            ColumnCount = 2,
            RowCount    = 1,
            Margin      = Padding.Empty,
            Padding     = Padding.Empty,
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        spX.Dock = DockStyle.Fill; spY.Dock = DockStyle.Fill;
        inner.Controls.Add(spX, 0, 0);
        inner.Controls.Add(spY, 1, 0);

        return BuildPropertyRow(label, inner);
    }

    internal static JsonElement SerializeVector2(double x, double y)
        => JsonSerializer.SerializeToElement(new { X = (float)x, Y = (float)y });

    // ── Vector3 ───────────────────────────────────────────────────────────────

    internal static (double X, double Y, double Z) GetVector3(JsonElement el) => (
        el.TryGetProperty("X", out JsonElement x) ? x.GetDouble() : 0.0,
        el.TryGetProperty("Y", out JsonElement y) ? y.GetDouble() : 0.0,
        el.TryGetProperty("Z", out JsonElement z) ? z.GetDouble() : 0.0);

    internal static Panel BuildVector3Field(string label, double x, double y, double z,
        Action<double> onX, Action<double> onY, Action<double> onZ, bool readOnly = false)
    {
        AxisStepper spX = new() { Axis = "X", Value = x, Step = 0.1, Enabled = !readOnly };
        AxisStepper spY = new() { Axis = "Y", Value = y, Step = 0.1, Enabled = !readOnly };
        AxisStepper spZ = new() { Axis = "Z", Value = z, Step = 0.1, Enabled = !readOnly };
        spX.ValueCommitted += (_, _) => onX(spX.Value);
        spY.ValueCommitted += (_, _) => onY(spY.Value);
        spZ.ValueCommitted += (_, _) => onZ(spZ.Value);

        TableLayoutPanel inner = new()
        {
            ColumnCount = 3,
            RowCount    = 1,
            Margin      = Padding.Empty,
            Padding     = Padding.Empty,
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        spX.Dock = DockStyle.Fill; spY.Dock = DockStyle.Fill; spZ.Dock = DockStyle.Fill;
        inner.Controls.Add(spX, 0, 0);
        inner.Controls.Add(spY, 1, 0);
        inner.Controls.Add(spZ, 2, 0);

        return BuildPropertyRow(label, inner);
    }

    internal static JsonElement SerializeVector3(double x, double y, double z)
        => JsonSerializer.SerializeToElement(new { X = (float)x, Y = (float)y, Z = (float)z });

    // ── SetProperty (undo/redo) ───────────────────────────────────────────────

    internal static void SetProperty(EditorBehaviour behaviour, string key, JsonElement newValue)
    {
        behaviour.Properties.TryGetValue(key, out JsonElement prev);
        EditorContext.Instance.Commands.Execute(
            new SetPropertyCommand<JsonElement>($"Set {key}", prev, newValue,
                v => behaviour.Properties[key] = v));
        if (EditorContext.Instance.SelectedObject is { } obj)
            EditorContext.Instance.EventBus.Publish(new GameObjectPropertyChangedEvent(obj));
    }

    // ── Detección de tipos JSON ───────────────────────────────────────────────

    internal static bool IsColorValue(JsonElement v)
        => v.ValueKind == JsonValueKind.Object
        && v.TryGetProperty("R", out _) && v.TryGetProperty("G", out _)
        && v.TryGetProperty("B", out _) && v.TryGetProperty("A", out _);

    internal static bool IsVector2Value(JsonElement v)
        => v.ValueKind == JsonValueKind.Object
        && v.TryGetProperty("X", out _) && v.TryGetProperty("Y", out _)
        && !v.TryGetProperty("R", out _) && !v.TryGetProperty("Z", out _);

    internal static bool IsVector3Value(JsonElement v)
        => v.ValueKind == JsonValueKind.Object
        && v.TryGetProperty("X", out _) && v.TryGetProperty("Y", out _)
        && v.TryGetProperty("Z", out _);

    // ── Helpers internos ──────────────────────────────────────────────────────

    private static int ValueToTick(double v, double min, double max)
        => (int)Math.Round((v - min) / (max - min) * 1000.0);

    private static double TickToValue(int tick, double min, double max)
        => min + (double)tick / 1000.0 * (max - min);

    private static string BuildFileFilter(string[]? extensions)
    {
        if (extensions is null or { Length: 0 })
            return "All files (*.*)|*.*";
        string patterns = string.Join(";", extensions.Select(e => $"*{e}"));
        string desc     = string.Join(", ", extensions);
        return $"Files ({desc})|{patterns}|All files (*.*)|*.*";
    }
}
