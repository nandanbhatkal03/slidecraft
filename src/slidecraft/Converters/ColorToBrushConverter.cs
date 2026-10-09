using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace slidecraft.Converters;

/// <summary>
/// Converts a hex colour string (RRGGBB) to a <see cref="SolidColorBrush"/>.
/// </summary>
public sealed class ColorToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && hex.Length == 6)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString("#" + hex);
                return new SolidColorBrush(color);
            }
            catch
            {
                // Fall through to default
            }
        }
        return Brushes.Black;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SolidColorBrush brush)
        {
            var c = brush.Color;
            return $"{c.R:X2}{c.G:X2}{c.B:X2}";
        }
        return "000000";
    }
}
