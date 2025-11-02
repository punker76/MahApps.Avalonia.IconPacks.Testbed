using System;
using System.Globalization;

namespace MahApps.IconPacksBrowser.Avalonia.Helper;

public static class MathHelper
{
    public static bool IsCloseToZero(this double value, double tolerance = 0.001)
    {
        return value.IsCloseTo(0, tolerance);
    }
    
    public static bool IsCloseTo(this double value, double other, double tolerance = 0.001)
    {
        return Math.Abs(value - other) < tolerance;
    }

    public static double GetDoubleOrDefault(object? value, double? defaultValue = null)
    {
        return value switch
        {
            double v => v,
            string s => double.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture),
            _ => defaultValue ?? throw new ArgumentException(null, nameof(value)),
        };
    }
}