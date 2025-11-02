using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Data.Converters;
using MahApps.IconPacksBrowser.Avalonia.Helper;

namespace MahApps.IconPacksBrowser.Avalonia.Converters;

public static class MathConverters
{
    public static MathMultiplyConverter Multiply { get; } = new MathMultiplyConverter();
}

public class MathMultiplyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var operand1 = MathHelper.GetDoubleOrDefault(value);
        var operand2 = MathHelper.GetDoubleOrDefault(parameter);
        return operand1 * operand2;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var operand1 = MathHelper.GetDoubleOrDefault(value);
        var operand2 = MathHelper.GetDoubleOrDefault(parameter);
        return operand1 / operand2;
    }
}