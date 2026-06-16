using System.Text.Json;
using MauiShapes = Microsoft.Maui.Controls.Shapes;

namespace MonoGame.Editor.Maui.Drawers;

/// <summary>
/// Helpers estáticos compartidos entre <see cref="InspectorView"/> y <see cref="BehaviourEditor"/>
/// para construir controles de propiedad con soporte de deshacer/rehacer y colores de label.
/// </summary>
internal static class PropertyControlHelper
{
    // ── Fila base ─────────────────────────────────────────────────────────────

    internal static View BuildPropertyRow(string labelText, View control,
        string? textColor = null, string? bgColor = null)
    {
        Label label = new()
        {
            Text = labelText,
            Style = (Style)Application.Current!.Resources["LabelSecondary"],
            VerticalOptions = LayoutOptions.Center,
        };

        if (textColor is not null)
            label.TextColor = Color.FromArgb(textColor);

        View labelContainer;
        if (bgColor is not null)
        {
            labelContainer = new Border
            {
                BackgroundColor = Color.FromArgb(bgColor),
                StrokeThickness = 0,
                Padding = new Thickness(4, 2),
                VerticalOptions = LayoutOptions.Center,
                StrokeShape = new MauiShapes.RoundRectangle { CornerRadius = 3 },
                Content = label,
            };
        }
        else
        {
            labelContainer = label;
        }

        Grid row = new()
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(90, GridUnitType.Absolute)),
                new ColumnDefinition(GridLength.Star),
            ],
            Padding = new Thickness(10, 4),
            ColumnSpacing = 6,
        };
        row.Add(labelContainer, 0, 0);
        row.Add(control, 1, 0);
        return row;
    }

    // ── Separador de sección ──────────────────────────────────────────────────

    internal static View BuildHeaderSeparator(string title)
    {
        return new VerticalStackLayout
        {
            Spacing = 0,
            Margin = new Thickness(0, 8, 0, 2),
            Children =
            {
                new Label
                {
                    Text = title,
                    Style = (Style)Application.Current!.Resources["SectionTitle"],
                    Margin = new Thickness(10, 0),
                },
                new BoxView { Color = Color.FromArgb("#34343A"), HeightRequest = 1 },
            },
        };
    }

    // ── Controles de edición ──────────────────────────────────────────────────

    internal static View BuildSliderField(string label, double value, double min, double max,
        Action<double> onChange, bool readOnly = false, string? textColor = null, string? bgColor = null)
    {
        double clampedValue = Math.Clamp(value, min, Math.Max(min + 0.0001, max));

        Slider slider = new()
        {
            Minimum = min,
            Maximum = Math.Max(min + 0.0001, max),
            Value = clampedValue,
            IsEnabled = !readOnly,
            MinimumTrackColor = Color.FromArgb("#4A9EFF"),
            ThumbColor = Color.FromArgb("#4A9EFF"),
        };

        Label valueLabel = new()
        {
            Text = clampedValue.ToString("G4"),
            Style = (Style)Application.Current!.Resources["LabelSecondary"],
            WidthRequest = 48,
            HorizontalTextAlignment = TextAlignment.End,
            VerticalOptions = LayoutOptions.Center,
        };

        slider.ValueChanged += (_, e) =>
        {
            valueLabel.Text = e.NewValue.ToString("G4");
            onChange(e.NewValue);
        };

        Grid container = new()
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(new GridLength(50, GridUnitType.Absolute)),
            ],
            ColumnSpacing = 4,
        };
        container.Add(slider, 0, 0);
        container.Add(valueLabel, 1, 0);

        return BuildPropertyRow(label, container, textColor, bgColor);
    }

    internal static View BuildNumberField(string label, double value, Action<double> onChange,
        bool readOnly = false, string? textColor = null, string? bgColor = null)
    {
        Controls.AxisStepper stepper = new()
        {
            ShowAxisTag = false,
            Value = value,
            Step = 0.1,
            IsEnabled = !readOnly,
        };
        stepper.ValueCommitted += (_, v) => onChange(v);
        return BuildPropertyRow(label, stepper, textColor, bgColor);
    }

    internal static View BuildTextField(string label, string value, Action<string> onChange,
        bool readOnly = false, string? textColor = null, string? bgColor = null)
    {
        Border shell = new() { Style = (Style)Application.Current!.Resources["InputShell"] };
        Entry entry = new()
        {
            Style = (Style)Application.Current!.Resources["InputEntry"],
            Text = value,
            IsReadOnly = readOnly,
        };
        entry.Completed += (_, _) => onChange(entry.Text ?? string.Empty);
        shell.Content = entry;
        return BuildPropertyRow(label, shell, textColor, bgColor);
    }

    internal static View BuildBoolField(string label, bool value, Action<bool> onChange,
        bool readOnly = false)
    {
        CheckBox check = new()
        {
            IsChecked = value,
            Color = Color.FromArgb("#4A9EFF"),
            IsEnabled = !readOnly,
        };
        check.CheckedChanged += (_, e) => onChange(e.Value);
        return BuildPropertyRow(label, check);
    }

    internal static View BuildColorField(string label, JsonElement value, Action<JsonElement> onChange)
    {
        int r = value.TryGetProperty("R", out JsonElement rp) ? rp.GetInt32() : 0;
        int g = value.TryGetProperty("G", out JsonElement gp) ? gp.GetInt32() : 0;
        int b = value.TryGetProperty("B", out JsonElement bp) ? bp.GetInt32() : 0;
        int a = value.TryGetProperty("A", out JsonElement ap) ? ap.GetInt32() : 255;

        Color initialColor = Color.FromRgba(r, g, b, a);

        Border swatch = new()
        {
            BackgroundColor = initialColor,
            WidthRequest = 32,
            HeightRequest = 20,
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#505058"),
            StrokeShape = new MauiShapes.RoundRectangle { CornerRadius = 3 },
        };

        Label hexLabel = new()
        {
            Text = $"#{r:X2}{g:X2}{b:X2}{a:X2}",
            Style = (Style)Application.Current!.Resources["LabelSecondary"],
            VerticalOptions = LayoutOptions.Center,
        };

        TapGestureRecognizer tap = new();
        tap.Tapped += async (_, _) =>
        {
            if (Services.DialogService.Navigation is not { } nav) return;
            Color? picked = await Views.Dialogs.RgbaColorPickerDialog.ShowAsync(nav, swatch.BackgroundColor);
            if (picked is null) return;

            swatch.BackgroundColor = picked;
            int nr = (int)(picked.Red * 255);
            int ng = (int)(picked.Green * 255);
            int nb = (int)(picked.Blue * 255);
            int na = (int)(picked.Alpha * 255);
            hexLabel.Text = $"#{nr:X2}{ng:X2}{nb:X2}{na:X2}";

            uint packed = ((uint)na << 24) | ((uint)nb << 16) | ((uint)ng << 8) | (uint)nr;
            var colorData = new { R = (byte)nr, G = (byte)ng, B = (byte)nb, A = (byte)na, PackedValue = packed };
            onChange(JsonSerializer.SerializeToElement(colorData));
        };

        Grid container = new()
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(36, GridUnitType.Absolute)),
                new ColumnDefinition(GridLength.Star),
            ],
            ColumnSpacing = 6,
        };
        container.Add(swatch, 0, 0);
        container.Add(hexLabel, 1, 0);
        container.GestureRecognizers.Add(tap);

        return BuildPropertyRow(label, container);
    }

    internal static View BuildFilePickerField(string label, string value,
        INavigation? navigation, string baseFolder, string[]? extensions, Action<string> onChange,
        bool readOnly = false)
    {
        Label pathLabel = new()
        {
            Text = string.IsNullOrEmpty(value) ? "(none)" : System.IO.Path.GetFileName(value),
            Style = (Style)Application.Current!.Resources["LabelSecondary"],
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
        };
        ToolTipProperties.SetText(pathLabel, value);

        Button browseBtn = new()
        {
            Text = "…",
            WidthRequest = 28,
            HeightRequest = 28,
            Padding = Thickness.Zero,
            Style = (Style)Application.Current!.Resources["FlatButton"],
            IsEnabled = !readOnly,
        };

        browseBtn.Clicked += async (_, _) =>
        {
            INavigation? nav = navigation ?? Services.DialogService.Navigation;
            if (nav is null || string.IsNullOrEmpty(baseFolder)) return;

            string? relPath = await Views.Dialogs.RelativePathPickerDialog.ShowAsync(
                nav, baseFolder, filesMode: true, extensions: extensions, title: "Select file");
            if (relPath is null) return;

            pathLabel.Text = System.IO.Path.GetFileName(relPath);
            ToolTipProperties.SetText(pathLabel, relPath);
            onChange(relPath);
        };

        Grid container = new()
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(new GridLength(32, GridUnitType.Absolute)),
            ],
            ColumnSpacing = 4,
        };
        container.Add(pathLabel, 0, 0);
        container.Add(browseBtn, 1, 0);

        return BuildPropertyRow(label, container);
    }

    // ── Comando de propiedad (undo/redo) ──────────────────────────────────────

    internal static void SetProperty(EditorBehaviour behaviour, string key, JsonElement newValue)
    {
        behaviour.Properties.TryGetValue(key, out JsonElement prev);
        EditorContext.Instance.Commands.Execute(
            new SetPropertyCommand<JsonElement>($"Set {key}", prev, newValue,
                v => behaviour.Properties[key] = v));
        // Publicar GameObjectPropertyChangedEvent en lugar de GameObjectSelectedEvent para evitar
        // que el Inspector reconstruya las tarjetas de Behaviour mientras el usuario edita un control.
        if (EditorContext.Instance.SelectedObject is { } obj)
            EditorContext.Instance.EventBus.Publish(new GameObjectPropertyChangedEvent(obj));
    }

    // ── Vector2 helpers ───────────────────────────────────────────────────────

    /// <summary>Extrae X e Y de un JsonElement que representa un Vector2 serializado.</summary>
    internal static (double X, double Y) GetVector2(JsonElement el) => (
        el.TryGetProperty("X", out JsonElement x) ? x.GetDouble() : 0.0,
        el.TryGetProperty("Y", out JsonElement y) ? y.GetDouble() : 0.0);

    /// <summary>Fila con dos steppers X/Y (colores rojo/verde) para propiedades Vector2.</summary>
    internal static View BuildVector2Field(string label, double x, double y,
        Action<double> onX, Action<double> onY, bool readOnly = false)
    {
        Controls.AxisStepper stepX = new() { Axis = "X", Value = x, Step = 0.1, IsEnabled = !readOnly };
        Controls.AxisStepper stepY = new() { Axis = "Y", Value = y, Step = 0.1, IsEnabled = !readOnly };
        stepX.ValueCommitted += (_, v) => onX(v);
        stepY.ValueCommitted += (_, v) => onY(v);

        Grid container = new()
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            ],
            ColumnSpacing = 4,
        };
        container.Add(stepX, 0, 0);
        container.Add(stepY, 1, 0);
        return BuildPropertyRow(label, container);
    }

    /// <summary>Serializa un Vector2 como objeto anónimo JSON {X, Y}.</summary>
    internal static JsonElement SerializeVector2(double x, double y)
        => JsonSerializer.SerializeToElement(new { X = (float)x, Y = (float)y });

    /// <summary>Extrae X, Y y Z de un JsonElement que representa un Vector3 serializado.</summary>
    internal static (double X, double Y, double Z) GetVector3(JsonElement el) => (
        el.TryGetProperty("X", out JsonElement x) ? x.GetDouble() : 0.0,
        el.TryGetProperty("Y", out JsonElement y) ? y.GetDouble() : 0.0,
        el.TryGetProperty("Z", out JsonElement z) ? z.GetDouble() : 0.0);

    /// <summary>Fila con tres steppers X/Y/Z para propiedades Vector3.</summary>
    internal static View BuildVector3Field(string label, double x, double y, double z,
        Action<double> onX, Action<double> onY, Action<double> onZ, bool readOnly = false)
    {
        Controls.AxisStepper stepX = new() { Axis = "X", Value = x, Step = 0.1, IsEnabled = !readOnly };
        Controls.AxisStepper stepY = new() { Axis = "Y", Value = y, Step = 0.1, IsEnabled = !readOnly };
        Controls.AxisStepper stepZ = new() { Axis = "Z", Value = z, Step = 0.1, IsEnabled = !readOnly };
        stepX.ValueCommitted += (_, v) => onX(v);
        stepY.ValueCommitted += (_, v) => onY(v);
        stepZ.ValueCommitted += (_, v) => onZ(v);

        Grid container = new()
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            ],
            ColumnSpacing = 4,
        };
        container.Add(stepX, 0, 0);
        container.Add(stepY, 1, 0);
        container.Add(stepZ, 2, 0);
        return BuildPropertyRow(label, container);
    }

    /// <summary>Serializa un Vector3 como objeto anónimo JSON {X, Y, Z}.</summary>
    internal static JsonElement SerializeVector3(double x, double y, double z)
        => JsonSerializer.SerializeToElement(new { X = (float)x, Y = (float)y, Z = (float)z });

    // ── Helpers internos ──────────────────────────────────────────────────────

    internal static bool IsColorValue(JsonElement value)
        => value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty("R", out _)
        && value.TryGetProperty("G", out _)
        && value.TryGetProperty("B", out _)
        && value.TryGetProperty("A", out _);

    /// <summary>Detecta un objeto JSON {X, Y} sin campo R (para no confundir con Color).</summary>
    internal static bool IsVector2Value(JsonElement value)
        => value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty("X", out _)
        && value.TryGetProperty("Y", out _)
        && !value.TryGetProperty("R", out _)
        && !value.TryGetProperty("Z", out _);

    /// <summary>Detecta un objeto JSON {X, Y, Z}.</summary>
    internal static bool IsVector3Value(JsonElement value)
        => value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty("X", out _)
        && value.TryGetProperty("Y", out _)
        && value.TryGetProperty("Z", out _);
}
