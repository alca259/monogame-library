using Alca.MonoGame.Kernel.UI.Controls.Base;
using Alca.MonoGame.Kernel.UI.Input;
using System.Globalization;

namespace Alca.MonoGame.Kernel.UI.Controls.Input;

/// <summary>Single-line numeric input field. Filters input to digits, one minus sign, and (optionally) one decimal separator.</summary>
public sealed class NumericBox : TextBoxBase
{
    #region Properties

    /// <summary>When true, only integers are accepted (no decimal point).</summary>
    public bool IsInt { get; set; }

    /// <summary>Minimum allowed value; enforced when the field loses focus.</summary>
    public float MinValue { get; set; } = float.MinValue;

    /// <summary>Maximum allowed value; enforced when the field loses focus.</summary>
    public float MaxValue { get; set; } = float.MaxValue;

    /// <summary>Step used for Up/Down arrow increment. 0 = no step behaviour.</summary>
    public float Step { get; set; } = 1f;

    /// <summary>Number of decimal places used when reformatting the value. -1 = auto (up to 6 significant digits).</summary>
    public int DecimalPlaces { get; set; } = -1;

    /// <summary>Returns the current text parsed as an integer. Parses lazily; do not call inside Draw.</summary>
    public int IntValue => int.TryParse(Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? i : 0;

    /// <summary>Returns the current text parsed as a float. Parses lazily; do not call inside Draw.</summary>
    public float FloatValue => float.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0f;

    #endregion

    #region Constructor

    /// <summary>Creates a new NumericBox.</summary>
    public NumericBox(SpriteFont? font, Texture2D? pixel, GameWindow? window)
        : base(font, pixel, window) { }

    #endregion

    #region Input filtering

    /// <inheritdoc/>
    protected override bool AcceptChar(char c)
    {
        if (char.IsDigit(c))
        {
            var (selMin, selMax) = HasSelection ? GetSelectionRange() : (_cursorIndex, _cursorIndex);
            string candidate = _text.ToString(0, selMin) + c + _text.ToString(selMax, _text.Length - selMax);

            if (IsInt)
            {
                if (!long.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out long val))
                    return false;
                double effMin = Math.Max((double)MinValue, int.MinValue);
                double effMax = Math.Min((double)MaxValue, int.MaxValue);
                return val >= effMin && val <= effMax;
            }

            // For float: allow intermediate states (e.g. "3." or "-"); reject only when parsed value is out of range.
            if (double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out double fVal))
                return fVal >= (double)MinValue && fVal <= (double)MaxValue;
            return true;
        }

        if (c == '-' && _cursorIndex == 0 && !Text.Contains('-'))
            return MinValue < 0;

        if (!IsInt && (c == '.' || c == ',') && !Text.Contains('.') && !Text.Contains(','))
            return true;

        return false;
    }

    #endregion

    #region Input — Up/Down increment

    /// <inheritdoc/>
    protected override void ProcessUIInput(UIInputContext input)
    {
        base.ProcessUIInput(input);

        if (Step > 0f)
        {
            if (input.MoveUp?.IsPressed == true) ApplyStep(Step);
            else if (input.MoveDown?.IsPressed == true) ApplyStep(-Step);
        }
    }

    private void ApplyStep(float delta)
    {
        float next = MathHelper.Clamp(FloatValue + delta, MinValue, MaxValue);
        SetFromFloat(next);
    }

    private void SetFromFloat(float v)
    {
        string formatted = IsInt
            ? ((int)v).ToString(CultureInfo.InvariantCulture)
            : DecimalPlaces >= 0
                ? v.ToString("F" + DecimalPlaces, CultureInfo.InvariantCulture)
                : v.ToString("0.######", CultureInfo.InvariantCulture);

        _text.Clear();
        _text.Append(formatted);
        _cursorIndex = _text.Length;
        _selectionStart = _cursorIndex;
        MarkDirty();
    }

    #endregion

    #region Blur validation

    /// <inheritdoc/>
    protected override void OnBlur()
    {
        float v = MathHelper.Clamp(FloatValue, MinValue, MaxValue);
        SetFromFloat(v);
    }

    #endregion

    #region Layout / Draw

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        int height = (_font?.LineSpacing ?? 16) + 10;
        DesiredSize = new Vector2(Math.Min(120f, availableSize.X), height);
    }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;
        DrawSingleLine(spriteBatch, Text);
    }

    #endregion
}
