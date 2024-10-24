using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;

namespace AvaloniaApp
{
    public class EnumsExtension : MarkupExtension
    {
        private readonly Type _type;

        public EnumsExtension(Type type) => _type = type;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new CompiledBindingExtension()
            {
                Mode = BindingMode.OneTime,
                DataType = typeof(IEnumerable<>).MakeGenericType(_type),
                Source = Enum.GetValues(_type)
            };
            return binding;
        }
    }
}