using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MahApps.Metro.IconPacks
{
    /// <summary>
    /// GeometryConverter - Converter class for converting instances of other types to and from Geometry instances
    /// </summary>
    public sealed class DataToGeometryConverter : IValueConverter
    {
        /// <summary>Attempts to convert to a Geometry from the given object.</summary>
        /// <param name="value">The object to convert to an instance of Geometry.</param>
        /// <param name="targetType">The type of the target.</param>
        /// <param name="parameter">A user-defined parameter.</param>
        /// <param name="culture">The culture to use.</param>
        /// <returns>The Geometry which was constructed.</returns>
        /// <remarks>
        /// This method should not throw exceptions. If the value is not convertible, return
        /// a <see cref="T:Avalonia.Data.BindingNotification" /> in an error state. Any exceptions thrown will be
        /// treated as an application exception.
        /// </remarks>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string data)
            {
                return PathGeometry.Parse(data);
            }

            return null;
        }

        /// <summary>Converts a value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the target.</param>
        /// <param name="parameter">A user-defined parameter.</param>
        /// <param name="culture">The culture to use.</param>
        /// <returns>The converted value.</returns>
        /// <remarks>
        /// This method should not throw exceptions. If the value is not convertible, return
        /// a <see cref="T:Avalonia.Data.BindingNotification" /> in an error state. Any exceptions thrown will be
        /// treated as an application exception.
        /// </remarks>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}