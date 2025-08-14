using System;
using System.Globalization;
using Avalonia.Data.Converters;
using IconPacks.Avalonia.Core;

namespace MahApps.IconPacksBrowser.Avalonia.Converters;

public class BooleanToFlipConverter : IValueConverter
{
    public static BooleanToFlipConverter Instance { get; } = new ();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true 
            ? PackIconFlipOrientation.Both 
            : PackIconFlipOrientation.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}