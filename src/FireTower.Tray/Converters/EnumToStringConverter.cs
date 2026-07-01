using System.Globalization;
using System.Windows.Data;

namespace FireTower.Tray.Converters;

/// <summary>
/// Renders any enum value as its name, so XAML bindings don't need a converter per enum type.
/// </summary>
public sealed class EnumToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value?.ToString() ?? string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
