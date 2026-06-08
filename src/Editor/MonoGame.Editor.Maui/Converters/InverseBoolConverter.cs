using System.Globalization;

namespace MonoGame.Editor.Maui.Converters;

/// <summary>Niega un <see cref="bool"/> (p. ej. visibilidad inversa de un placeholder).</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;
}
