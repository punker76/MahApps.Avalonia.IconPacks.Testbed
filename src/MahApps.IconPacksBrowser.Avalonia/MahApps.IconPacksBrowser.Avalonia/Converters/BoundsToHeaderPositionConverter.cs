using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using MahApps.IconPacksBrowser.Avalonia.Controls;

namespace MahApps.IconPacksBrowser.Avalonia.Converters;

public class BoundsToHeaderPositionConverter : IValueConverter
{
    public static BoundsToHeaderPositionConverter Instance { get; } = new BoundsToHeaderPositionConverter();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var threshold = parameter as double? ?? 300;
        
        if (value is Rect bounds)
        {
            return bounds.Width < threshold
                ? HeaderPosition.Top
                : HeaderPosition.Left;
        }
        else
        {
            throw new ArgumentException(null, nameof(value));
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}