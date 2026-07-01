using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FireTower.Tray.Converters;

/// <summary>
/// Collapses an element when its binding value is null, shows it otherwise.
/// Pass ConverterParameter="Invert" to reverse: visible when null, collapsed when not.
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNull = value is null;
        bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
        return (isNull ^ invert) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
