using System;
using System.Globalization;
using Avalonia.Media;

namespace MahApps.Metro.IconPacks.Converter
{
    /// <summary>
    /// GeometryConverter - Converter class for converting instances of other types to and from Geometry instances
    /// </summary>
    public sealed class DataToGeometryConverter : MarkupConverter
    {
        /// <inheritdoc />
        protected override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string data ? PathGeometry.Parse(data) : null;
        }

        /// <inheritdoc />
        protected override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}