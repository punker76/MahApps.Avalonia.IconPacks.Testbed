using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;
using IconPacks.Avalonia.Material;

namespace MahApps.IconPacksBrowser.Avalonia.Converters;

public class GetAssemblyVersionConverter : IValueConverter
{
    public static GetAssemblyVersionConverter Instance { get; } = new GetAssemblyVersionConverter();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null 
            ? null 
            : FileVersionInfo.GetVersionInfo(Assembly.GetAssembly(value.GetType())!.Location).FileVersion;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}