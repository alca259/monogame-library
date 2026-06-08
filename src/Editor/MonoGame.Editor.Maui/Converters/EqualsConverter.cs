using System.Globalization;

namespace MonoGame.Editor.Maui.Converters;

/// <summary>
/// Devuelve <c>true</c> si el valor enlazado es igual al <c>ConverterParameter</c>
/// (comparación por <see cref="object.Equals(object?, object?)"/>, con strings
/// convertidos por <see cref="object.ToString"/>). Útil para visibilidad/selección
/// tipo radio (pestañas activas, filtros, secciones de shader, etc.).
/// </summary>
public sealed class EqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
