using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using MauiShapes = Microsoft.Maui.Controls.Shapes;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Panel derecho (Inspector). El estado, la selección, las pestañas y los comandos viven en
/// <see cref="InspectorViewModel"/>. La construcción dinámica de las tarjetas de behaviour y
/// el cableado de los <see cref="AxisStepper"/> de Transform se mantienen aquí (acoplados a
/// controles concretos), reaccionando a <see cref="InspectorViewModel.RefreshRequested"/>.
/// </summary>
public sealed partial class InspectorView : ContentView
{
    private readonly InspectorViewModel _vm = new();
    private bool _suppressTransformEvents;
    private readonly HashSet<string> _collapsedBehaviours = [];

    public InspectorView()
    {
        InitializeComponent();
        BindingContext = _vm;
        WireTransformCommands();
        _vm.RefreshRequested += RefreshInspector;
        _vm.PropertyChanged += OnViewModelPropertyChanged;
        UpdateTabContent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null) _vm.Attach();
        else _vm.Detach();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InspectorViewModel.ActiveTab))
            UpdateTabContent();
    }

    private void UpdateTabContent()
    {
        string tab = _vm.ActiveTab;
        InspectorContent.IsVisible = tab == "Inspector";
        MaterialContent.IsVisible = tab == "Material";
        UIThemeContent.IsVisible = tab == "UITheme";
        SpriteEditorContent.IsVisible = tab == "Sprite";
    }

    // ── Transform command wiring (commit semantics preserved) ───────────────────

    private void WireTransformCommands()
    {
        PosXStepper.ValueCommitted += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyPosX(v); };
        PosYStepper.ValueCommitted += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyPosY(v); };
        RotZStepper.ValueCommitted += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyRotZ(v); };
        ScaleXStepper.ValueCommitted += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyScaleX(v); };
        ScaleYStepper.ValueCommitted += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyScaleY(v); };
        DepthStepper.ValueCommitted += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyDepth(v); };
    }

    private void OnObjectActiveChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_suppressTransformEvents) return;
        _vm.SetActive(e.Value);
    }

    // ── Refresh (Transform values + behaviour cards) ────────────────────────────

    private void RefreshInspector()
    {
        EditorGameObject? selected = _vm.Selected;
        if (selected is null)
        {
            BehaviourCardsStack.Children.Clear();
            return;
        }

        _suppressTransformEvents = true;
        ObjectActiveCheck.IsChecked = selected.Active;
        PosXStepper.Value = selected.Position.X;
        PosYStepper.Value = selected.Position.Y;
        RotZStepper.Value = selected.Rotation;
        ScaleXStepper.Value = selected.Scale.X;
        ScaleYStepper.Value = selected.Scale.Y;
        DepthStepper.Value = selected.PositionZ;
        _suppressTransformEvents = false;

        BuildBehaviourCards();
    }

    // ── Behaviour cards (dynamic UI) ────────────────────────────────────────────

    private void BuildBehaviourCards()
    {
        BehaviourCardsStack.Children.Clear();

        EditorGameObject? selected = _vm.Selected;
        if (selected is null) return;

        foreach (EditorBehaviour behaviour in selected.Behaviours)
        {
            if (_vm.Registry.RegisteredTypes.TryGetValue(behaviour.TypeName, out Type? type))
                EnsurePropertiesPopulated(behaviour, type);
            BehaviourCardsStack.Children.Add(BuildBehaviourCard(behaviour, selected));
        }
    }

    private static void EnsurePropertiesPopulated(EditorBehaviour behaviour, Type type)
    {
        PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0
                        && !behaviour.Properties.ContainsKey(p.Name))
            .ToArray();

        if (props.Length == 0) return;

        object? instance = null;
        try { instance = Activator.CreateInstance(type); } catch (Exception ex) { Log($"[Inspector] Failed to instantiate behaviour type {type.Name}: {ex.Message}", LogLevel.Warning); }

        foreach (PropertyInfo prop in props)
        {
            try
            {
                object? value = instance is not null ? prop.GetValue(instance) : null;
                behaviour.Properties[prop.Name] = value is not null
                    ? JsonSerializer.SerializeToElement(value, prop.PropertyType)
                    : GetDefaultJsonElement(prop.PropertyType);
            }
            catch (Exception ex) { Log($"[Inspector] Failed to read property {prop.Name}: {ex.Message}", LogLevel.Warning); }
        }
    }

    private static JsonElement GetDefaultJsonElement(Type type)
    {
        if (type == typeof(bool)) return JsonSerializer.SerializeToElement(false);
        if (type == typeof(string)) return JsonSerializer.SerializeToElement(string.Empty);
        if (type.IsValueType)
        {
            try { return JsonSerializer.SerializeToElement(Activator.CreateInstance(type)!, type); }
            catch (Exception ex) { Log($"[Inspector] Failed to create default instance for type {type.Name}: {ex.Message}", LogLevel.Debug); }
        }
        return JsonSerializer.SerializeToElement(string.Empty);
    }

    private View BuildBehaviourCard(EditorBehaviour behaviour, EditorGameObject owner)
    {
        string shortName = GetShortTypeName(behaviour.TypeName);
        bool collapsed = _collapsedBehaviours.Contains(behaviour.TypeName);

        // Body — property rows
        VerticalStackLayout body = new() { Spacing = 0 };
        foreach (KeyValuePair<string, JsonElement> prop in behaviour.Properties)
            body.Children.Add(BuildPropertyRow(behaviour, prop.Key, prop.Value));
        body.IsVisible = !collapsed;

        // Header — chevron / type name / remove button
        Button chevron = new()
        {
            Text = collapsed ? "▶" : "▼",
            TextColor = Color.FromArgb("#6A6A72"),
            FontSize = 10,
            WidthRequest = 20,
            HeightRequest = 20,
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            Padding = Thickness.Zero,
            CornerRadius = 0,
            VerticalOptions = LayoutOptions.Center,
        };
        chevron.Clicked += (_, _) =>
        {
            bool nowCollapsed = !_collapsedBehaviours.Contains(behaviour.TypeName);
            if (nowCollapsed) _collapsedBehaviours.Add(behaviour.TypeName);
            else _collapsedBehaviours.Remove(behaviour.TypeName);
            BuildBehaviourCards();
        };

        Button removeBtn = new()
        {
            Text = "✕",
            FontSize = 10,
            WidthRequest = 20,
            HeightRequest = 20,
            Padding = new Thickness(0),
            CornerRadius = 4,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#9A9AA2"),
            VerticalOptions = LayoutOptions.Center,
        };
        removeBtn.Clicked += (_, _) =>
        {
            EditorContext.Instance.Commands.Execute(new RemoveBehaviourCommand(owner, behaviour));
            BuildBehaviourCards();
        };

        Grid header = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(20, GridUnitType.Absolute)),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(new GridLength(24, GridUnitType.Absolute)),
            },
            BackgroundColor = Color.FromArgb("#252528"),
            Padding = new Thickness(8, 4),
        };
        header.Add(chevron, 0, 0);
        header.Add(new Label
        {
            Text = shortName,
            Style = (Style)Application.Current!.Resources["SectionTitle"],
            VerticalOptions = LayoutOptions.Center,
        }, 1, 0);
        header.Add(removeBtn, 2, 0);

        return new VerticalStackLayout
        {
            Spacing = 0,
            Children =
            {
                new Border
                {
                    BackgroundColor = Color.FromArgb("#1E1E20"),
                    StrokeThickness = 0,
                    Content = new VerticalStackLayout
                    {
                        Spacing  = 0,
                        Children =
                        {
                            header,
                            new BoxView { Color = Color.FromArgb("#34343A"), HeightRequest = 1 },
                            body,
                        },
                    },
                },
                new BoxView { Color = Color.FromArgb("#34343A"), HeightRequest = 1 },
            },
        };
    }

    private static View BuildPropertyRow(EditorBehaviour behaviour, string key, JsonElement value)
    {
        View control;
        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            control = BuildBoolPropertyControl(behaviour, key, value.GetBoolean());
        else if (value.ValueKind == JsonValueKind.Number)
            control = BuildNumberPropertyControl(behaviour, key, value.GetDouble());
        else if (value.ValueKind == JsonValueKind.Object && IsColorValue(value))
            control = BuildColorPropertyControl(behaviour, key, value);
        else
            control = BuildStringPropertyControl(behaviour, key, value.ToString());

        Grid row = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(90, GridUnitType.Absolute)),
                new ColumnDefinition(GridLength.Star),
            },
            Padding = new Thickness(10, 4),
            ColumnSpacing = 6,
        };
        row.Add(new Label
        {
            Text = key,
            Style = (Style)Application.Current!.Resources["LabelSecondary"],
            VerticalOptions = LayoutOptions.Center,
        }, 0, 0);
        row.Add(control, 1, 0);
        return row;
    }

    private static bool IsColorValue(JsonElement value)
        => value.ValueKind == JsonValueKind.Object
        && value.TryGetProperty("R", out _)
        && value.TryGetProperty("G", out _)
        && value.TryGetProperty("B", out _)
        && value.TryGetProperty("A", out _);

    private static View BuildColorPropertyControl(EditorBehaviour behaviour, string key, JsonElement value)
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
            Text = ColorToHex(r, g, b, a),
            Style = (Style)Application.Current!.Resources["LabelSecondary"],
            VerticalOptions = LayoutOptions.Center,
        };

        Grid container = new()
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(new GridLength(36, GridUnitType.Absolute)),
                new ColumnDefinition(GridLength.Star),
            },
            ColumnSpacing = 6,
        };
        container.Add(swatch, 0, 0);
        container.Add(hexLabel, 1, 0);

        TapGestureRecognizer tap = new();
        tap.Tapped += async (_, _) =>
        {
            if (DialogService.Navigation is not { } navigation) return;

            Color? picked = await RgbaColorPickerDialog.ShowAsync(navigation, swatch.BackgroundColor);
            if (picked is null) return;

            swatch.BackgroundColor = picked;

            int nr = (int)(picked.Red * 255);
            int ng = (int)(picked.Green * 255);
            int nb = (int)(picked.Blue * 255);
            int na = (int)(picked.Alpha * 255);
            hexLabel.Text = ColorToHex(nr, ng, nb, na);

            uint packed = ((uint)na << 24) | ((uint)nb << 16) | ((uint)ng << 8) | (uint)nr;
            var colorData = new { R = (byte)nr, G = (byte)ng, B = (byte)nb, A = (byte)na, PackedValue = packed };

            JsonElement previous = behaviour.Properties[key];
            JsonElement next = JsonSerializer.SerializeToElement(colorData);
            EditorContext.Instance.Commands.Execute(
                new SetPropertyCommand<JsonElement>($"Set {key}", previous, next,
                    v => behaviour.Properties[key] = v));
        };
        container.GestureRecognizers.Add(tap);

        return container;
    }

    private static string ColorToHex(int r, int g, int b, int a)
        => $"#{r:X2}{g:X2}{b:X2}{a:X2}";

    private static View BuildBoolPropertyControl(EditorBehaviour behaviour, string key, bool current)
    {
        CheckBox check = new() { IsChecked = current, Color = Color.FromArgb("#4A9EFF") };
        check.CheckedChanged += (_, e) =>
        {
            JsonElement previous = behaviour.Properties[key];
            JsonElement next = JsonSerializer.SerializeToElement(e.Value);
            EditorContext.Instance.Commands.Execute(
                new SetPropertyCommand<JsonElement>($"Set {key}", previous, next, v => behaviour.Properties[key] = v));
        };
        return check;
    }

    private static View BuildNumberPropertyControl(EditorBehaviour behaviour, string key, double current)
    {
        AxisStepper stepper = new() { ShowAxisTag = false, Value = current, Step = 0.1 };
        stepper.ValueCommitted += (_, v) =>
        {
            JsonElement previous = behaviour.Properties[key];
            JsonElement next = JsonSerializer.SerializeToElement(v);
            EditorContext.Instance.Commands.Execute(
                new SetPropertyCommand<JsonElement>($"Set {key}", previous, next, e => behaviour.Properties[key] = e));
        };
        return stepper;
    }

    private static View BuildStringPropertyControl(EditorBehaviour behaviour, string key, string current)
    {
        Border shell = new() { Style = (Style)Application.Current!.Resources["InputShell"] };
        Entry entry = new() { Style = (Style)Application.Current!.Resources["InputEntry"], Text = current };
        entry.Completed += (_, _) =>
        {
            JsonElement previous = behaviour.Properties[key];
            JsonElement next = JsonSerializer.SerializeToElement(entry.Text ?? string.Empty);
            EditorContext.Instance.Commands.Execute(
                new SetPropertyCommand<JsonElement>($"Set {key}", previous, next, v => behaviour.Properties[key] = v));
        };
        shell.Content = entry;
        return shell;
    }

    private static string GetShortTypeName(string typeName)
    {
        ReadOnlySpan<char> span = typeName.AsSpan();
        int comma = span.IndexOf(',');
        if (comma >= 0) span = span[..comma];
        int dot = span.LastIndexOf('.');
        return (dot >= 0 ? span[(dot + 1)..] : span).ToString();
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    private static void Log(string message, LogLevel level = LogLevel.Info)
        => EditorContext.Instance.EventBus.Publish(new LogEntryAddedEvent(new LogEntry(DateTime.UtcNow, level, message)));
}
