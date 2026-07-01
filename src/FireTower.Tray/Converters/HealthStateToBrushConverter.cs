using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FireTower.Shared.Enums;

namespace FireTower.Tray.Converters;

/// <summary>
/// Maps a <see cref="HealthState"/> to its status color, per the Status Colors table in
/// ui-guidelines.md.
/// </summary>
public sealed class HealthStateToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var state = value as HealthState? ?? HealthState.Unknown;
        var resourceKey = state switch
        {
            HealthState.Healthy => "HealthyBrush",
            HealthState.Warning => "WarningBrush",
            HealthState.Degraded => "WarningBrush",
            HealthState.Critical => "CriticalBrush",
            HealthState.Offline => "CriticalBrush",
            HealthState.Recovering => "RecoveringBrush",
            _ => "DisabledBrush",
        };

        return System.Windows.Application.Current.TryFindResource(resourceKey) as Brush ?? Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
