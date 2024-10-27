using System;
using System.Collections.Generic;
using Avalonia.Platform;

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
            using var iconJsonStream = AssetLoader.Open(new Uri($"avares://{typeof(TEnum).Assembly.GetName().Name}/Resources/Icons.json"));
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<TEnum, string>>(iconJsonStream);
        }
    }
}