using System.Globalization;

namespace MonoGame.Editor.Maui.Converters;

/// <summary>
/// Convierte un <see cref="bool"/> en un <see cref="Color"/> según el parámetro
/// <c>"activeHex|inactiveHex"</c> (por ejemplo <c>"#4A9EFF|#252528"</c>). Sustituye el
/// coloreado imperativo de botones de herramienta, pills y segmentos de estado.
/// </summary>
public sealed class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool active = value is true;
        if (parameter is not string spec) return Colors.Transparent;

        int sep = spec.IndexOf('|');
        if (sep < 0) return Color.FromArgb(spec);

        string hex = active ? spec[..sep] : spec[(sep + 1)..];
        return Color.FromArgb(hex);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
