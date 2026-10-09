using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace slidecraft.Converters;

/// <summary>
/// Converts a <see cref="bool"/> to <see cref="Visibility"/>.
/// <c>true</c> → <see cref="Visibility.Visible"/>,
/// <c>false</c> → <see cref="Visibility.Collapsed"/>.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}
