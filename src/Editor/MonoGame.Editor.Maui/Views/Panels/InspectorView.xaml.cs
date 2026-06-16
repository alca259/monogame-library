using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using MonoGame.Editor.Core.Attributes;

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
        _vm.TransformOnlyRefreshRequested += RefreshTransformOnly;
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
        PosXStepper.ValueCommitted  += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyPosX(v); };
        PosYStepper.ValueCommitted  += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyPosY(v); };
        PosZStepper.ValueCommitted  += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyPosZ(v); };
        RotXStepper.ValueCommitted  += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyRotX(v); };
        RotYStepper.ValueCommitted  += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyRotY(v); };
        RotZStepper.ValueCommitted  += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyRotZ(v); };
        ScaleXStepper.ValueCommitted += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyScaleX(v); };
        ScaleYStepper.ValueCommitted += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyScaleY(v); };
        ScaleZStepper.ValueCommitted += (_, v) => { if (!_suppressTransformEvents) _vm.ApplyScaleZ(v); };
    }

    private void OnObjectActiveChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_suppressTransformEvents) return;
        _vm.SetActive(e.Value);
    }

    // ── Refresh (Transform values + behaviour cards) ────────────────────────────

    /// <summary>
    /// Actualiza SÓLO los steppers del Transform sin reconstruir las tarjetas de Behaviour.
    /// Se invoca en respuesta a <see cref="InspectorViewModel.TransformOnlyRefreshRequested"/>
    /// (cambio de propiedad) para evitar el bucle Slider.ValueChanged → SetProperty → rebuild.
    /// </summary>
    private void RefreshTransformOnly()
    {
        EditorGameObject? selected = _vm.Selected;
        if (selected is null) return;

        _suppressTransformEvents = true;
        ObjectActiveCheck.IsChecked = selected.Active;
        PosXStepper.Value   = selected.Position.X;
        PosYStepper.Value   = selected.Position.Y;
        PosZStepper.Value   = selected.Position.Z;
        RotXStepper.Value   = selected.Rotation.X;
        RotYStepper.Value   = selected.Rotation.Y;
        RotZStepper.Value   = selected.Rotation.Z;
        ScaleXStepper.Value = selected.Scale.X;
        ScaleYStepper.Value = selected.Scale.Y;
        ScaleZStepper.Value = selected.Scale.Z;
        _suppressTransformEvents = false;
        // NO BuildBehaviourCards()
    }

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
        PosXStepper.Value  = selected.Position.X;
        PosYStepper.Value  = selected.Position.Y;
        PosZStepper.Value  = selected.Position.Z;
        RotXStepper.Value  = selected.Rotation.X;
        RotYStepper.Value  = selected.Rotation.Y;
        RotZStepper.Value  = selected.Rotation.Z;
        ScaleXStepper.Value = selected.Scale.X;
        ScaleYStepper.Value = selected.Scale.Y;
        ScaleZStepper.Value = selected.Scale.Z;
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
            _vm.Registry.RegisteredTypes.TryGetValue(behaviour.TypeName, out Type? type);
            if (type is not null) EnsurePropertiesPopulated(behaviour, type);
            BehaviourCardsStack.Children.Add(BuildBehaviourCard(behaviour, selected, type));
        }
    }

    // IncludeFields = true para serializar los structs de MonoGame (Vector2.X, Vector3.Z, etc.
    // son FIELDS no properties y el serializador por defecto los ignora produciendo "{}").
    private static readonly JsonSerializerOptions s_fieldOptions = new() { IncludeFields = true };

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
                    ? JsonSerializer.SerializeToElement(value, prop.PropertyType, s_fieldOptions)
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
            try { return JsonSerializer.SerializeToElement(Activator.CreateInstance(type)!, type, s_fieldOptions); }
            catch (Exception ex) { Log($"[Inspector] Failed to create default instance for type {type.Name}: {ex.Message}", LogLevel.Debug); }
        }
        return JsonSerializer.SerializeToElement(string.Empty);
    }

    private View BuildBehaviourCard(EditorBehaviour behaviour, EditorGameObject owner, Type? type)
    {
        string shortName = GetShortTypeName(behaviour.TypeName);
        bool collapsed = _collapsedBehaviours.Contains(behaviour.TypeName);

        // Body — custom editor o property rows con atributos
        VerticalStackLayout body = new() { Spacing = 0 };

        Drawers.BehaviourEditor? customEditor = Drawers.BehaviourEditorRegistry.GetEditor(behaviour.TypeName);
        if (customEditor is null && type is not null)
            customEditor = Drawers.BehaviourEditorRegistry.GetEditor(type);

        if (customEditor is not null)
        {
            Drawers.BehaviourEditorRegistry.PrepareEditor(customEditor);
            body.Children.Add(customEditor.BuildInspector(behaviour, owner));
        }
        else
        {
            foreach (KeyValuePair<string, JsonElement> prop in behaviour.Properties)
            {
                PropertyInfo? pi = type?.GetProperty(prop.Key);

                if (pi?.GetCustomAttribute<EditorHideAttribute>() is not null) continue;

                if (pi?.GetCustomAttribute<EditorHeaderAttribute>() is { } hdr)
                    body.Children.Add(Drawers.PropertyControlHelper.BuildHeaderSeparator(hdr.Title));

                View? row = BuildPropertyRow(behaviour, prop.Key, prop.Value, pi);
                if (row is not null)
                    body.Children.Add(row);
            }
        }

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

    private static View? BuildPropertyRow(EditorBehaviour behaviour, string key, JsonElement value, PropertyInfo? pi)
    {
        bool readOnly = pi?.GetCustomAttribute<EditorReadOnlyAttribute>() is not null;

        EditorPropertyAttribute? propAttr = pi?.GetCustomAttribute<EditorPropertyAttribute>();
        string label = propAttr?.Label ?? key;
        string? textColor = propAttr?.LabelTextColor;
        string? bgColor = propAttr?.LabelBackgroundColor;

        // [EditorFilePicker] → Entry readonly + botón "…"
        if (pi?.GetCustomAttribute<EditorFilePickerAttribute>() is { } fp)
        {
            string sv = value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
            return Drawers.PropertyControlHelper.BuildFilePickerField(label, sv,
                DialogService.Navigation,
                EditorContext.Instance.ActiveProject?.RootPath ?? "",
                fp.Extensions,
                v => Drawers.PropertyControlHelper.SetProperty(behaviour, key, JsonSerializer.SerializeToElement(v)),
                readOnly);
        }

        // [EditorRange] + número → slider
        if (pi?.GetCustomAttribute<EditorRangeAttribute>() is { } range && value.ValueKind == JsonValueKind.Number)
        {
            return Drawers.PropertyControlHelper.BuildSliderField(label, value.GetDouble(), range.Min, range.Max,
                v => Drawers.PropertyControlHelper.SetProperty(behaviour, key, JsonSerializer.SerializeToElement(v)),
                readOnly, textColor, bgColor);
        }

        // Detección por tipo de JsonElement
        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return Drawers.PropertyControlHelper.BuildBoolField(label, value.GetBoolean(),
                v => Drawers.PropertyControlHelper.SetProperty(behaviour, key, JsonSerializer.SerializeToElement(v)),
                readOnly);

        if (value.ValueKind == JsonValueKind.Number)
            return Drawers.PropertyControlHelper.BuildNumberField(label, value.GetDouble(),
                v => Drawers.PropertyControlHelper.SetProperty(behaviour, key, JsonSerializer.SerializeToElement(v)),
                readOnly, textColor, bgColor);

        if (value.ValueKind == JsonValueKind.Object && Drawers.PropertyControlHelper.IsColorValue(value))
            return Drawers.PropertyControlHelper.BuildColorField(label, value,
                nv => Drawers.PropertyControlHelper.SetProperty(behaviour, key, nv));

        // Vector3 antes que Vector2 para evitar falsos positivos (V3 también tiene X e Y)
        if (Drawers.PropertyControlHelper.IsVector3Value(value))
        {
            (double vx, double vy, double vz) = Drawers.PropertyControlHelper.GetVector3(value);
            return Drawers.PropertyControlHelper.BuildVector3Field(label, vx, vy, vz,
                v => Drawers.PropertyControlHelper.SetProperty(behaviour, key,
                    Drawers.PropertyControlHelper.SerializeVector3(v,
                        Drawers.PropertyControlHelper.GetVector3(behaviour.Properties.TryGetValue(key, out var cur) ? cur : value).Y,
                        Drawers.PropertyControlHelper.GetVector3(behaviour.Properties.TryGetValue(key, out var cur2) ? cur2 : value).Z)),
                v => Drawers.PropertyControlHelper.SetProperty(behaviour, key,
                    Drawers.PropertyControlHelper.SerializeVector3(
                        Drawers.PropertyControlHelper.GetVector3(behaviour.Properties.TryGetValue(key, out var cur) ? cur : value).X, v,
                        Drawers.PropertyControlHelper.GetVector3(behaviour.Properties.TryGetValue(key, out var cur2) ? cur2 : value).Z)),
                v => Drawers.PropertyControlHelper.SetProperty(behaviour, key,
                    Drawers.PropertyControlHelper.SerializeVector3(
                        Drawers.PropertyControlHelper.GetVector3(behaviour.Properties.TryGetValue(key, out var cur) ? cur : value).X,
                        Drawers.PropertyControlHelper.GetVector3(behaviour.Properties.TryGetValue(key, out var cur2) ? cur2 : value).Y, v)),
                readOnly);
        }

        if (Drawers.PropertyControlHelper.IsVector2Value(value))
        {
            (double vx, double vy) = Drawers.PropertyControlHelper.GetVector2(value);
            return Drawers.PropertyControlHelper.BuildVector2Field(label, vx, vy,
                v => Drawers.PropertyControlHelper.SetProperty(behaviour, key,
                    Drawers.PropertyControlHelper.SerializeVector2(v,
                        Drawers.PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue(key, out var cur) ? cur : value).Y)),
                v => Drawers.PropertyControlHelper.SetProperty(behaviour, key,
                    Drawers.PropertyControlHelper.SerializeVector2(
                        Drawers.PropertyControlHelper.GetVector2(behaviour.Properties.TryGetValue(key, out var cur) ? cur : value).X, v)),
                readOnly);
        }

        // Fallback: texto
        string strValue = value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.ToString();
        return Drawers.PropertyControlHelper.BuildTextField(label, strValue,
            v => Drawers.PropertyControlHelper.SetProperty(behaviour, key, JsonSerializer.SerializeToElement(v)),
            readOnly, textColor, bgColor);
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
