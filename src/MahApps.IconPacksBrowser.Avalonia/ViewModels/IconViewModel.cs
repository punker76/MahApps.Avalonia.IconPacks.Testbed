using System;
using System.ComponentModel;
using System.Linq;
using IconPacks.Avalonia.Core;
using IconPacks.Avalonia.Core.Attributes;
using MahApps.IconPacksBrowser.Avalonia.Helper;
using SkiaSharp;

namespace MahApps.IconPacksBrowser.Avalonia.ViewModels;

public interface IIconViewModel
{
    /// <summary>
    /// Gets the unique identifier by returning "IconPackName|Name" combination
    /// </summary>
    string Identifier => $"{IconPackName}|{Name}";

    string Name { get; set; }
    string IconPackName { get; }
    string Description { get; }
    Type IconPackType { get; set; }
    Enum Value { get; set; }
    MetaDataAttribute MetaData { get; set; }
    string? CopyToClipboardText { get; }
    string? CopyToClipboardWpfGeometry { get; }
    string? CopyToClipboardAsContentText { get; }
    string? CopyToClipboardAsPathIconText { get; }
    string? CopyToClipboardAsGeometryText { get; }
}

public class IconViewModel : ViewModelBase, IIconViewModel
{
    public IconViewModel(Type enumType, Type packType, Enum k, MetaDataAttribute metaData)
    {
        Name = k.ToString();
        IconPackType = packType;
        IconType = enumType;
        Value = k;
        MetaData = metaData;
    }

    public string? CopyToClipboardText => ExportHelper.FillTemplate(ExportHelper.ClipboardWpf, new ExportParameters(this)); // $"<iconPacks:{IconPackType.Name} Kind=\"{Name}\" />";

    public string? CopyToClipboardWpfGeometry =>
        ExportHelper.FillTemplate(ExportHelper.ClipboardWpfGeometry, new ExportParameters(this)); // $"<iconPacks:{IconPackType.Name} Kind=\"{Name}\" />";

    public string? CopyToClipboardAsContentText =>
        ExportHelper.FillTemplate(ExportHelper.ClipboardContent, new ExportParameters(this)); // $"{{iconPacks:{IconPackType.Name.Replace("PackIcon", "")} Kind={Name}}}";

    public string? CopyToClipboardAsPathIconText =>
        ExportHelper.FillTemplate(ExportHelper.ClipboardUwp, new ExportParameters(this)); // $"<iconPacks:{IconPackType.Name.Replace("PackIcon", "PathIcon")} Kind=\"{Name}\" />";

    public string? CopyToClipboardAsGeometryText => ExportHelper.FillTemplate(ExportHelper.ClipboardData, new ExportParameters(this)); // GetPackIconControlBase().Data;

    public string Name { get; set; }

    public string IconPackName => IconPackType.Name.Replace("PackIcon", "");

    public string Description => GetDescription(Value);

    public Type IconPackType { get; set; }

    public Type IconType { get; set; }

    public Enum Value { get; set; }

    public MetaDataAttribute MetaData { get; set; }

    internal static string GetDescription(Enum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        return fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() is DescriptionAttribute attribute ? attribute.Description : value.ToString();
    }
}