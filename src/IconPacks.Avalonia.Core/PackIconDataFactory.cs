using System;
using System.Collections.Generic;
using IconPacks.Avalonia.Utils;

namespace IconPacks.Avalonia
{
    public static class PackIconDataFactory<TEnum> where TEnum : struct, Enum
    {
        public static Lazy<IDictionary<TEnum, string>> DataIndex { get; }

        static PackIconDataFactory()
        {
            DataIndex = new Lazy<IDictionary<TEnum, string>>(Create);
        }

        public static IDictionary<TEnum, string> Create()
        {
            var json = System.Reflection.Assembly.GetAssembly(typeof(TEnum))?.ReadFile("Resources.Icons.json");
            return string.IsNullOrEmpty(json)
                ? new Dictionary<TEnum, string>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<TEnum, string>>(json);
        }
    }
}