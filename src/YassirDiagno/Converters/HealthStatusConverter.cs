using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using YassirDiagno.Services;

namespace YassirDiagno.Converters;

public sealed class HealthStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is HealthStatus s)
        {
            return s switch
            {
                HealthStatus.Excellent => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                HealthStatus.Good      => new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
                HealthStatus.Warning   => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                HealthStatus.Critical  => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
                _                       => new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)),
            };
        }
        return new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class HealthStatusToLightBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is HealthStatus s)
        {
            return s switch
            {
                HealthStatus.Excellent => new SolidColorBrush(Color.FromArgb(0x33, 0x10, 0xB9, 0x81)),
                HealthStatus.Good      => new SolidColorBrush(Color.FromArgb(0x33, 0x3B, 0x82, 0xF6)),
                HealthStatus.Warning   => new SolidColorBrush(Color.FromArgb(0x33, 0xF5, 0x9E, 0x0B)),
                HealthStatus.Critical  => new SolidColorBrush(Color.FromArgb(0x33, 0xEF, 0x44, 0x44)),
                _                       => new SolidColorBrush(Color.FromArgb(0x33, 0x9C, 0xA3, 0xAF)),
            };
        }
        return new SolidColorBrush(Color.FromArgb(0x33, 0x9C, 0xA3, 0xAF));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
