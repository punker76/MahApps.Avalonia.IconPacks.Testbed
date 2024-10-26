using Avalonia.Data;
using System;
using System.Globalization;

namespace IconPacks.Avalonia.Converter
{
    /// <summary>
    /// ValueConverter which converts the PackIconFlipOrientation enumeration value to ScaleY value of a ScaleTransformation.
    /// </summary>
    public class FlipToScaleYValueConverter : MarkupConverter
    {
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static FlipToScaleYValueConverter()
        {
        }

        private FlipToScaleYValueConverter()
        {
        }

        public static FlipToScaleYValueConverter Instance { get; } = new();

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }

        /// <inheritdoc />
        protected override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PackIconFlipOrientation flip)
            {
                var scaleY = flip == PackIconFlipOrientation.Vertical || flip == PackIconFlipOrientation.Both ? -1 : 1;
                return scaleY;
            }

            return 1;
        }

        /// <inheritdoc />
        protected override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BindingNotification.UnsetValue;
        }
    }
}