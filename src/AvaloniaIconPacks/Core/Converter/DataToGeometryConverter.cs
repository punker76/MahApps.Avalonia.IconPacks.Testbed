using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MahApps.Metro.IconPacks.Converter
{
    /// <summary>
    /// GeometryConverter - Converter class for converting instances of other types to and from Geometry instances
    /// </summary>
    public sealed class DataToGeometryConverter : IValueConverter
    {
        public static readonly DataToGeometryConverter Instance = new DataToGeometryConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string data ? PathGeometry.Parse(data) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}