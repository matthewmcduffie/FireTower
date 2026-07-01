using System.Globalization;
using System.Windows.Data;

namespace FireTower.Tray.Converters;

/// <summary>
/// Inverts a boolean value, primarily for enabling a control when an IsBusy flag is false.
/// </summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !(value is true);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !(value is true);
}
