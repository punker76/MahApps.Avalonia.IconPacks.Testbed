using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MahApps.IconPacksBrowser.Avalonia.Converters;

public class BoundsToCornerRadiusConverter : IValueConverter
{
    public static BoundsToCornerRadiusConverter Instance { get; } = new BoundsToCornerRadiusConverter();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Rect rect)
        {
            return new CornerRadius(Math.Min(rect.Width, rect.Height) / 2);
        }
        return BindingOperations.DoNothing;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}