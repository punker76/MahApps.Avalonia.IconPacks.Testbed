using Avalonia.Data;
using System;
using System.Globalization;

namespace IconPacks.Avalonia.Converter
{
    public class NullToUnsetValueConverter : MarkupConverter
    {
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static NullToUnsetValueConverter()
        {
        }

        private NullToUnsetValueConverter()
        {
        }

        public static NullToUnsetValueConverter Instance { get; } = new();

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }

        /// <inheritdoc />
        protected override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value ?? BindingNotification.UnsetValue;
        }

        /// <inheritdoc />
        protected override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BindingNotification.UnsetValue;
        }
    }
}