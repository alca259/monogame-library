namespace MonoGame.Editor.Maui.Controls;

/// <summary>
/// Numeric field with a colored axis tag (X/Y/Z) and increment/decrement steppers.
/// Use <see cref="Axis"/> to set the axis color ("X" = red, "Y" = green, "Z" = blue).
/// Set <see cref="ShowAxisTag"/> to false for non-axis numeric inputs.
/// </summary>
public sealed partial class AxisStepper : ContentView
{
    #region Bindable properties

    public static readonly BindableProperty AxisProperty =
        BindableProperty.Create(nameof(Axis), typeof(string), typeof(AxisStepper), "X",
            propertyChanged: (b, _, _) => ((AxisStepper)b).ApplyAxis());

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(double), typeof(AxisStepper), 0.0,
            propertyChanged: (b, _, _) => ((AxisStepper)b).ApplyValue());

    public static readonly BindableProperty StepProperty =
        BindableProperty.Create(nameof(Step), typeof(double), typeof(AxisStepper), 1.0);

    public static readonly BindableProperty ShowAxisTagProperty =
        BindableProperty.Create(nameof(ShowAxisTag), typeof(bool), typeof(AxisStepper), true,
            propertyChanged: (b, _, _) => ((AxisStepper)b).ApplyShowTag());

    public string Axis
    {
        get => (string)GetValue(AxisProperty);
        set => SetValue(AxisProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public bool ShowAxisTag
    {
        get => (bool)GetValue(ShowAxisTagProperty);
        set => SetValue(ShowAxisTagProperty, value);
    }

    #endregion

    private bool _suppressEntry;

    public AxisStepper()
    {
        InitializeComponent();
        ApplyAxis();
        ApplyValue();
        ApplyShowTag();
    }

    private void ApplyAxis()
    {
        AxisLabel.Text = Axis;
        AxisTagBorder.BackgroundColor = Axis switch
        {
            "Y"   => (Color)Application.Current!.Resources["AxisGreen"],
            "Z"   => (Color)Application.Current!.Resources["AxisBlue"],
            _     => (Color)Application.Current!.Resources["AxisRed"],
        };
    }

    private void ApplyValue()
    {
        _suppressEntry = true;
        ValueEntry.Text = Value.ToString("F3");
        _suppressEntry = false;
    }

    private void ApplyShowTag()
    {
        AxisTagBorder.IsVisible = ShowAxisTag;
    }

    /// <summary>Raised when the user commits a value via Enter, focus loss, or the step buttons.</summary>
    public event EventHandler<double>? ValueCommitted;

    internal void OnEntryCompleted(object sender, EventArgs e)
    {
        if (_suppressEntry) return;
        if (double.TryParse(ValueEntry.Text, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out double v))
        {
            Value = v;
            ValueCommitted?.Invoke(this, v);
        }
    }

    internal void OnEntryUnfocused(object sender, FocusEventArgs e) => OnEntryCompleted(sender, e);

    internal void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressEntry) return;
        if (double.TryParse(e.NewTextValue, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out double v))
            Value = v;
    }

    internal void OnIncrement(object sender, EventArgs e)
    {
        Value += Step;
        ValueCommitted?.Invoke(this, Value);
    }

    internal void OnDecrement(object sender, EventArgs e)
    {
        Value -= Step;
        ValueCommitted?.Invoke(this, Value);
    }
}
