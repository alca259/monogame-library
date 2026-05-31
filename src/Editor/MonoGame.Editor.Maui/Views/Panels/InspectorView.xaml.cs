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

    private Action<GameObjectSelectedEvent>? _onObjectSelected;
    private Action<UndoPerformedEvent>?      _onUndo;
    private Action<RedoPerformedEvent>?      _onRedo;
    private Action<EditorStateChangedEvent>? _onStateChanged;

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
        if (Handler is not null) Subscribe();
        else Unsubscribe();
    }

    // ── EventBus ─────────────────────────────────────────────────────────────

    private void Subscribe()
    {
        _onObjectSelected = e => MainThread.BeginInvokeOnMainThread(() => { _selected = e.GameObject; RefreshInspector(); });
        _onUndo           = _ => MainThread.BeginInvokeOnMainThread(() => RefreshInspector());
        _onRedo           = _ => MainThread.BeginInvokeOnMainThread(() => RefreshInspector());
        _onStateChanged   = e => MainThread.BeginInvokeOnMainThread(() =>
            InspectorContent.IsEnabled = e.NewState is EditorState.Editing);

        _bus.Subscribe(_onObjectSelected);
        _bus.Subscribe(_onUndo);
        _bus.Subscribe(_onRedo);
        _bus.Subscribe(_onStateChanged);
    }

    private void Unsubscribe()
    {
        if (_onObjectSelected is not null) _bus.Unsubscribe(_onObjectSelected);
        if (_onUndo           is not null) _bus.Unsubscribe(_onUndo);
        if (_onRedo           is not null) _bus.Unsubscribe(_onRedo);
        if (_onStateChanged   is not null) _bus.Unsubscribe(_onStateChanged);
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

    // ── Behaviour cards ───────────────────────────────────────────────────────

    private void BuildBehaviourCards()
    {
        BehaviourCardsStack.Children.Clear();

        if (_selected is null) return;

        foreach (EditorBehaviour behaviour in _selected.Behaviours)
            BehaviourCardsStack.Children.Add(BuildBehaviourCard(behaviour, _selected));
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
            chevron.Text   = nowCollapsed ? "▶" : "▼";
            body.IsVisible = !nowCollapsed;
        };

        Switch enabledSwitch = new()
        {
            IsToggled       = behaviour.Enabled,
            Scale           = 0.7,
            VerticalOptions = LayoutOptions.Center,
        };
        enabledSwitch.Toggled += (_, te) =>
        {
            bool prev = behaviour.Enabled;
            EditorContext.Instance.Commands.Execute(
                new SetPropertyCommand<bool>($"Toggle {shortName}", prev, te.Value, v => behaviour.Enabled = v));
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
                new ColumnDefinition(new GridLength(44, GridUnitType.Absolute)),
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
        header.Add(enabledSwitch, 2, 0);
        header.Add(removeBtn, 3, 0);

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
        View control = value.ValueKind switch
        {
            JsonValueKind.True or JsonValueKind.False => BuildBoolPropertyControl(behaviour, key, value.GetBoolean()),
            JsonValueKind.Number                      => BuildNumberPropertyControl(behaviour, key, value.GetDouble()),
            _                                         => BuildStringPropertyControl(behaviour, key, value.ToString()),
        };

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

        GameObjectRegistry registry = new();
        registry.Scan();

        string? typeName = await AddBehaviourDialog.ShowAsync(page.Navigation, registry);
        if (string.IsNullOrEmpty(typeName)) return;

        EditorContext.Instance.Commands.Execute(
            new AddBehaviourCommand(_selected, new EditorBehaviour { TypeName = typeName }));
        BuildBehaviourCards();
    }
}
