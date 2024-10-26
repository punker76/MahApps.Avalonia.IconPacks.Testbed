using Avalonia.Data;
using System;
using System.Globalization;

namespace IconPacks.Avalonia.Converter
{
    /// <summary>
    /// ValueConverter which converts the PackIconFlipOrientation enumeration value to ScaleX value of a ScaleTransformation.
    /// </summary>
    public class FlipToScaleXValueConverter : MarkupConverter
    {
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static FlipToScaleXValueConverter()
        {
        }

        private FlipToScaleXValueConverter()
        {
        }

        public static FlipToScaleXValueConverter Instance { get; } = new();

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
                return flip is PackIconFlipOrientation.Horizontal or PackIconFlipOrientation.Both
                    ? -1
                    : 1;
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