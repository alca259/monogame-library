using System.Reflection;
using MauiShapes = Microsoft.Maui.Controls.Shapes;
using System.Text.Json;

namespace MonoGame.Editor.Maui.Views.Panels;

/// <summary>
/// Panel derecho: muestra las propiedades del objeto seleccionado (cabecera, Transform, behaviours dinámicos).
/// Se comunica a través de <see cref="IEditorEventBus"/>; sin MVVM.
/// </summary>
public sealed partial class InspectorView : ContentView
{
    private readonly IEditorEventBus _bus = EditorContext.Instance.EventBus;
    private EditorGameObject? _selected;
    private bool _suppressTransformEvents;
    private readonly HashSet<string> _collapsedBehaviours = [];
    private readonly GameObjectRegistry _registry = new();
    private bool _registryReady;

    private Action<GameObjectSelectedEvent>? _onObjectSelected;
    private Action<UndoPerformedEvent>?      _onUndo;
    private Action<RedoPerformedEvent>?      _onRedo;
    private Action<EditorStateChangedEvent>? _onStateChanged;
    private Action<ProjectOpenedEvent>?      _onProjectOpened;

    private static readonly Color ActiveTabFg   = Color.FromArgb("#E6E6E8");
    private static readonly Color InactiveTabFg = Color.FromArgb("#9A9AA2");

    public InspectorView()
    {
        InitializeComponent();
        WireTransformCommands();
        SetActiveInspectorTab(InspectorTabBtn);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null)
        {
            Subscribe();
            _registry.Scan();
            if (EditorContext.Instance.ActiveProject is not null)
                _ = ScanProjectAsync();
        }
        else
        {
            Unsubscribe();
        }
    }

    // ── EventBus ─────────────────────────────────────────────────────────────

    private void Subscribe()
    {
        _onObjectSelected = e => MainThread.BeginInvokeOnMainThread(() => { _selected = e.GameObject; RefreshInspector(); });
        _onUndo           = _ => MainThread.BeginInvokeOnMainThread(() => RefreshInspector());
        _onRedo           = _ => MainThread.BeginInvokeOnMainThread(() => RefreshInspector());
        _onStateChanged   = e => MainThread.BeginInvokeOnMainThread(() =>
            InspectorContent.IsEnabled = e.NewState is EditorState.Editing);
        _onProjectOpened  = evt => MainThread.BeginInvokeOnMainThread(() => _ = ScanProjectAsync());

        _bus.Subscribe(_onObjectSelected);
        _bus.Subscribe(_onUndo);
        _bus.Subscribe(_onRedo);
        _bus.Subscribe(_onStateChanged);
        _bus.Subscribe(_onProjectOpened);
    }

    private void Unsubscribe()
    {
        if (_onObjectSelected is not null) _bus.Unsubscribe(_onObjectSelected);
        if (_onUndo           is not null) _bus.Unsubscribe(_onUndo);
        if (_onRedo           is not null) _bus.Unsubscribe(_onRedo);
        if (_onStateChanged   is not null) _bus.Unsubscribe(_onStateChanged);
        if (_onProjectOpened  is not null) _bus.Unsubscribe(_onProjectOpened);
    }

    // ── Transform command wiring ──────────────────────────────────────────────

    private void WireTransformCommands()
    {
        PosXStepper.ValueCommitted += (_, v) =>
        {
            if (_selected is null) return;
            EditorContext.Instance.Commands.Execute(
                new MoveEntityCommand(_selected, new EditorVector2((float)v, _selected.Position.Y)));
        };

        PosYStepper.ValueCommitted += (_, v) =>
        {
            if (_selected is null) return;
            EditorContext.Instance.Commands.Execute(
                new MoveEntityCommand(_selected, new EditorVector2(_selected.Position.X, (float)v)));
        };

        RotZStepper.ValueCommitted += (_, v) =>
        {
            if (_selected is null) return;
            EditorContext.Instance.Commands.Execute(
                new RotateEntityCommand(_selected, _selected.Rotation, (float)v));
        };

        ScaleXStepper.ValueCommitted += (_, v) =>
        {
            if (_selected is null) return;
            EditorContext.Instance.Commands.Execute(
                new ScaleEntityCommand(_selected, new EditorVector2((float)v, _selected.Scale.Y)));
        };

        ScaleYStepper.ValueCommitted += (_, v) =>
        {
            if (_selected is null) return;
            EditorContext.Instance.Commands.Execute(
                new ScaleEntityCommand(_selected, new EditorVector2(_selected.Scale.X, (float)v)));
        };

        DepthStepper.ValueCommitted += (_, v) =>
        {
            if (_selected is null) return;
            EditorContext.Instance.Commands.Execute(
                new MoveEntityZCommand(_selected, _selected.PositionZ, (float)v));
        };
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    private void RefreshInspector()
    {
        bool hasSelection = _selected is not null;
        NoSelectionLabel.IsVisible    = !hasSelection;
        ObjectHeader.IsVisible        = hasSelection;
        TransformSection.IsVisible    = hasSelection;
        BehaviourCardsStack.IsVisible = hasSelection;
        AddBehaviourBtn.IsVisible     = hasSelection;

        if (_selected is null) return;

        ObjectNameLabel.Text = _selected.Name;
        ObjectIdLabel.Text   = _selected.Id.ToString()[..8];

        _suppressTransformEvents = true;
        ObjectActiveCheck.IsChecked = _selected.Active;
        PosXStepper.Value   = _selected.Position.X;
        PosYStepper.Value   = _selected.Position.Y;
        RotZStepper.Value   = _selected.Rotation;
        ScaleXStepper.Value = _selected.Scale.X;
        ScaleYStepper.Value = _selected.Scale.Y;
        DepthStepper.Value  = _selected.PositionZ;
        _suppressTransformEvents = false;

        BuildBehaviourCards();
    }

    // ── Registry scanning ─────────────────────────────────────────────────────

    private async Task ScanProjectAsync()
    {
        _registryReady = false;
        _registry.Scan();

        EditorProject? project = EditorContext.Instance.ActiveProject;
        if (project is null) { _registryReady = true; return; }

        try
        {
            ProjectSettings settings = await ProjectSettings.LoadAsync(project).ConfigureAwait(true);
            bool isDebug = settings.BuildConfiguration.Equals("Debug", StringComparison.OrdinalIgnoreCase);

            if (isDebug)
            {
                await ScanProjectAssembliesAsync(project, settings).ConfigureAwait(true);

                string sourceRoot = string.IsNullOrEmpty(project.GameSourcePath)
                    ? project.RootPath
                    : project.GameSourcePath;
                await _registry.ScanSourceAsync(sourceRoot).ConfigureAwait(true);
            }
        }
        catch (Exception) { }

        _registryReady = true;
        if (_selected is not null)
            BuildBehaviourCards();
    }

    private async Task ScanProjectAssembliesAsync(EditorProject project, ProjectSettings settings)
    {
        try
        {
            string[] csprojFiles = Directory.GetFiles(project.RootPath, "*.csproj",
                SearchOption.AllDirectories);

            List<Task> tasks = [];
            foreach (string csproj in csprojFiles)
            {
                string dir     = Path.GetDirectoryName(csproj) ?? string.Empty;
                string dllName = Path.GetFileNameWithoutExtension(csproj) + ".dll";
                string binDir  = Path.Combine(dir, "bin", settings.BuildConfiguration);
                if (!Directory.Exists(binDir)) continue;

                string? dllPath = Directory.GetFiles(binDir, dllName, SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (dllPath is not null)
                    tasks.Add(_registry.ScanFromAssemblyAsync(dllPath));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (IOException) { }
    }

    // ── Behaviour cards ───────────────────────────────────────────────────────

    private void BuildBehaviourCards()
    {
        BehaviourCardsStack.Children.Clear();

        if (_selected is null) return;

        foreach (EditorBehaviour behaviour in _selected.Behaviours)
        {
            if (_registry.RegisteredTypes.TryGetValue(behaviour.TypeName, out Type? type))
                EnsurePropertiesPopulated(behaviour, type);
            BehaviourCardsStack.Children.Add(BuildBehaviourCard(behaviour, _selected));
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
        try { instance = Activator.CreateInstance(type); } catch { }

        foreach (PropertyInfo prop in props)
        {
            try
            {
                object? value = instance is not null ? prop.GetValue(instance) : null;
                behaviour.Properties[prop.Name] = value is not null
                    ? JsonSerializer.SerializeToElement(value, prop.PropertyType)
                    : GetDefaultJsonElement(prop.PropertyType);
            }
            catch { }
        }
    }

    private static JsonElement GetDefaultJsonElement(Type type)
    {
        if (type == typeof(bool))   return JsonSerializer.SerializeToElement(false);
        if (type == typeof(string)) return JsonSerializer.SerializeToElement(string.Empty);
        if (type.IsValueType)
        {
            try { return JsonSerializer.SerializeToElement(Activator.CreateInstance(type)!, type); }
            catch { }
        }
        return JsonSerializer.SerializeToElement(string.Empty);
    }

    private View BuildBehaviourCard(EditorBehaviour behaviour, EditorGameObject owner)
    {
        string shortName = GetShortTypeName(behaviour.TypeName);
        bool collapsed   = _collapsedBehaviours.Contains(behaviour.TypeName);

        // Body — property rows
        VerticalStackLayout body = new() { Spacing = 0 };
        foreach (KeyValuePair<string, JsonElement> prop in behaviour.Properties)
            body.Children.Add(BuildPropertyRow(behaviour, prop.Key, prop.Value));
        body.IsVisible = !collapsed;

        // Header — chevron / type name / enabled switch / remove button
        Button chevron = new()
        {
            Text            = collapsed ? "▶" : "▼",
            TextColor       = Color.FromArgb("#6A6A72"),
            FontSize        = 10,
            WidthRequest    = 20,
            HeightRequest   = 20,
            BackgroundColor = Colors.Transparent,
            BorderWidth     = 0,
            Padding         = Thickness.Zero,
            CornerRadius    = 0,
            VerticalOptions = LayoutOptions.Center,
        };
        chevron.Clicked += (_, _) =>
        {
            bool nowCollapsed = !_collapsedBehaviours.Contains(behaviour.TypeName);
            if (nowCollapsed) _collapsedBehaviours.Add(behaviour.TypeName);
            else              _collapsedBehaviours.Remove(behaviour.TypeName);
            BuildBehaviourCards();
        };

        Button removeBtn = new()
        {
            Text            = "✕",
            FontSize        = 10,
            WidthRequest    = 20,
            HeightRequest   = 20,
            Padding         = new Thickness(0),
            CornerRadius    = 4,
            BackgroundColor = Colors.Transparent,
            TextColor       = Color.FromArgb("#9A9AA2"),
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
            Padding         = new Thickness(8, 4),
        };
        header.Add(chevron, 0, 0);
        header.Add(new Label
        {
            Text            = shortName,
            Style           = (Style)Application.Current!.Resources["SectionTitle"],
            VerticalOptions = LayoutOptions.Center,
        }, 1, 0);
        header.Add(removeBtn, 2, 0);

        return new VerticalStackLayout
        {
            Spacing  = 0,
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
            Padding       = new Thickness(10, 4),
            ColumnSpacing = 6,
        };
        row.Add(new Label
        {
            Text            = key,
            Style           = (Style)Application.Current!.Resources["LabelSecondary"],
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
            WidthRequest    = 32,
            HeightRequest   = 20,
            StrokeThickness = 1,
            Stroke          = Color.FromArgb("#505058"),
            StrokeShape     = new MauiShapes.RoundRectangle { CornerRadius = 3 },
        };

        Label hexLabel = new()
        {
            Text            = ColorToHex(r, g, b, a),
            Style           = (Style)Application.Current!.Resources["LabelSecondary"],
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
        container.Add(swatch,    0, 0);
        container.Add(hexLabel,  1, 0);

        TapGestureRecognizer tap = new();
        tap.Tapped += async (_, _) =>
        {
            Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page is null) return;

            Color? picked = await RgbaColorPickerDialog.ShowAsync(
                page.Navigation, swatch.BackgroundColor);
            if (picked is null) return;

            swatch.BackgroundColor = picked;

            int nr = (int)(picked.Red   * 255);
            int ng = (int)(picked.Green * 255);
            int nb = (int)(picked.Blue  * 255);
            int na = (int)(picked.Alpha * 255);
            hexLabel.Text = ColorToHex(nr, ng, nb, na);

            uint packed = ((uint)na << 24) | ((uint)nb << 16) | ((uint)ng << 8) | (uint)nr;
            var colorData = new { R = (byte)nr, G = (byte)ng, B = (byte)nb, A = (byte)na, PackedValue = packed };

            JsonElement previous = behaviour.Properties[key];
            JsonElement next     = JsonSerializer.SerializeToElement(colorData);
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
            JsonElement next     = JsonSerializer.SerializeToElement(e.Value);
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
            JsonElement next     = JsonSerializer.SerializeToElement(v);
            EditorContext.Instance.Commands.Execute(
                new SetPropertyCommand<JsonElement>($"Set {key}", previous, next, e => behaviour.Properties[key] = e));
        };
        return stepper;
    }

    private static View BuildStringPropertyControl(EditorBehaviour behaviour, string key, string current)
    {
        Border shell = new() { Style = (Style)Application.Current!.Resources["InputShell"] };
        Entry entry  = new() { Style = (Style)Application.Current!.Resources["InputEntry"], Text = current };
        entry.Completed += (_, _) =>
        {
            JsonElement previous = behaviour.Properties[key];
            JsonElement next     = JsonSerializer.SerializeToElement(entry.Text ?? string.Empty);
            EditorContext.Instance.Commands.Execute(
                new SetPropertyCommand<JsonElement>($"Set {key}", previous, next, v => behaviour.Properties[key] = v));
        };
        shell.Content = entry;
        return shell;
    }

    private static string GetShortTypeName(string typeName)
    {
        ReadOnlySpan<char> span  = typeName.AsSpan();
        int comma = span.IndexOf(',');
        if (comma >= 0) span = span[..comma];
        int dot = span.LastIndexOf('.');
        return (dot >= 0 ? span[(dot + 1)..] : span).ToString();
    }

    // ── Tab switching ─────────────────────────────────────────────────────────

    private void OnInspectorTabClicked(object sender, EventArgs e)
    {
        SetActiveInspectorTab(InspectorTabBtn);
        InspectorContent.IsVisible    = true;
        MaterialContent.IsVisible     = false;
        UIThemeContent.IsVisible      = false;
        SpriteEditorContent.IsVisible = false;
    }

    private void OnMaterialTabClicked(object sender, EventArgs e)
    {
        SetActiveInspectorTab(MaterialTabBtn);
        InspectorContent.IsVisible    = false;
        MaterialContent.IsVisible     = true;
        UIThemeContent.IsVisible      = false;
        SpriteEditorContent.IsVisible = false;
    }

    private void OnUIThemeTabClicked(object sender, EventArgs e)
    {
        SetActiveInspectorTab(UIThemeTabBtn);
        InspectorContent.IsVisible    = false;
        MaterialContent.IsVisible     = false;
        UIThemeContent.IsVisible      = true;
        SpriteEditorContent.IsVisible = false;
    }

    private void OnSpriteEditorTabClicked(object sender, EventArgs e)
    {
        SetActiveInspectorTab(SpriteEditorTabBtn);
        InspectorContent.IsVisible    = false;
        MaterialContent.IsVisible     = false;
        UIThemeContent.IsVisible      = false;
        SpriteEditorContent.IsVisible = true;
    }

    private void SetActiveInspectorTab(Button active)
    {
        foreach (Button btn in new[] { InspectorTabBtn, MaterialTabBtn, UIThemeTabBtn, SpriteEditorTabBtn })
            btn.TextColor = btn == active ? ActiveTabFg : InactiveTabFg;
    }

    // ── Object header events ──────────────────────────────────────────────────

    private void OnObjectActiveChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_selected is null || _suppressTransformEvents) return;
        bool prev = _selected.Active;
        bool next = e.Value;
        if (prev == next) return;
        EditorContext.Instance.Commands.Execute(
            new SetPropertyCommand<bool>("Set Active", prev, next, v => _selected.Active = v));
    }

    private async void OnAddBehaviourClicked(object sender, EventArgs e)
    {
        if (_selected is null) return;
        Page? page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page is null) return;

        if (!_registryReady)
            await ScanProjectAsync().ConfigureAwait(true);

        string? typeName = await AddBehaviourDialog.ShowAsync(
            page.Navigation, _registry,
            async () => await ScanProjectAsync().ConfigureAwait(true));
        if (string.IsNullOrEmpty(typeName)) return;

        EditorContext.Instance.Commands.Execute(
            new AddBehaviourCommand(_selected, new EditorBehaviour { TypeName = typeName }));
        BuildBehaviourCards();
    }
}
