using System;
using Avalonia;
#if (NETFX_CORE || WINDOWS_UWP)
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#else
using System.Globalization;
using System.Windows;
#endif

namespace MahApps.Metro.IconPacks.Converter
{
    /// <summary>
    /// ValueConverter which converts the PackIconFlipOrientation enumeration value to ScaleX value of a ScaleTransformation.
    /// </summary>
#if (NETFX_CORE || WINDOWS_UWP)
    public class FlipToScaleXValueConverter : MarkupConverter
    {
        private static FlipToScaleXValueConverter _instance;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static FlipToScaleXValueConverter()
        {
        }

        public static IValueConverter Instance { get; } = _instance ?? (_instance = new FlipToScaleXValueConverter());

        protected override object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is PackIconFlipOrientation)
            {
                var flip = (PackIconFlipOrientation)value;
                var scaleX = flip == PackIconFlipOrientation.Horizontal || flip == PackIconFlipOrientation.Both ? -1 : 1;
                return scaleX;
            }

            return DependencyProperty.UnsetValue;
        }

        protected override object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return DependencyProperty.UnsetValue;
        }
    }
#else
    public class FlipToScaleXValueConverter : MarkupConverter
    {
        private static FlipToScaleXValueConverter _instance;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static FlipToScaleXValueConverter()
        {
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new FlipToScaleXValueConverter());
        }

        protected override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is PackIconFlipOrientation)
            {
                var flip = (PackIconFlipOrientation) value;
                var scaleX = flip == PackIconFlipOrientation.Horizontal || flip == PackIconFlipOrientation.Both ? -1 : 1;
                return scaleX;
            }

            return AvaloniaProperty.UnsetValue;
        }

        protected override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }
    }
#endif
}