using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia.Core;
using MahApps.IconPacksBrowser.Avalonia.Properties;
using MahApps.IconPacksBrowser.Avalonia.ViewModels;

namespace MahApps.IconPacksBrowser.Avalonia.Helper;

internal class ExportHelper
{
    // SVG-File
    private static string? _SvgFileTemplate;

    internal static string? SvgFileTemplate => _SvgFileTemplate ??= LoadTemplateString("SVG.xml");

    // XAML-File (WPF)
    private static string? _WpfFileTemplate;

    internal static string? WpfFileTemplate => _WpfFileTemplate ??= LoadTemplateString("WPF.xml");

    // XAML-File (WPF)
    private static string? _UwpFileTemplate;

    internal static string? UwpFileTemplate => _UwpFileTemplate ??= LoadTemplateString("WPF.xml");

    // Clipboard - WPF
    private static string? _ClipboardWpf;

    internal static string ClipboardWpf => _ClipboardWpf ??= LoadTemplateString("Clipboard.WPF.xml");

    // Clipboard - WPF
    private static string? _ClipboardWpfGeometry;

    internal static string ClipboardWpfGeometry => _ClipboardWpfGeometry ??= LoadTemplateString("Clipboard.WPF.Geometry.xml");

    // Clipboard - UWP
    private static string? _ClipboardUwp;

    internal static string ClipboardUwp => _ClipboardUwp ??= LoadTemplateString("Clipboard.UWP.xml");

    // Clipboard - Content
    private static string? _ClipboardContent;

    internal static string ClipboardContent => _ClipboardContent ??= LoadTemplateString("Clipboard.Content.xml");

    // Clipboard - PathData
    private static string? _ClipboardData;

    internal static string ClipboardData => _ClipboardData ??= LoadTemplateString("Clipboard.PathData.xml");

    internal static string? FillTemplate(string template, ExportParameters parameters)
    {
        return template.Replace("@IconKind", parameters.IconKind)
            .Replace("@IconPackName", parameters.IconPackName)
            .Replace("@IconPackHomepage", parameters.IconPackHomepage)
            .Replace("@IconPackLicense", parameters.IconPackLicense)
            .Replace("@PageWidth", parameters.PageWidth)
            .Replace("@PageHeight", parameters.PageHeight)
            .CheckedReplace("@PathData", () => parameters.PathData) // avoid allocation of Lazy<string>
            .Replace("@FillColor", parameters.FillColor)
            .Replace("@Background", parameters.Background)
            .Replace("@StrokeColor", parameters.StrokeColor)
            .Replace("@StrokeWidth", parameters.StrokeWidth)
            .Replace("@StrokeLineCap", parameters.StrokeLineCap)
            .Replace("@StrokeLineJoin", parameters.StrokeLineJoin)
            .Replace("@TransformMatrix", parameters.TransformMatrix);
    }

    internal static string? LoadTemplateString(string fileName)
    {
        if (string.IsNullOrWhiteSpace(Settings.Default.ExportTemplatesDir) || !File.Exists(Path.Combine(Settings.Default.ExportTemplatesDir, fileName)))
        {
            var uri = new Uri($"avares://MahApps.IconPacksBrowser.Avalonia/Assets/ExportTemplates/{fileName}", UriKind.RelativeOrAbsolute);

            using var stream = AssetLoader.Open(uri);
            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }
        else
        {
            return File.ReadAllText(Path.Combine(Settings.Default.ExportTemplatesDir, fileName));
        }
    }

    internal static async Task SaveAsSvgAsync(IIconViewModel iconViewModel)
    {
        await using var saveFileStream = await MainViewModel.Instance.SaveFileDialogAsync(filters: new[]
        {
            new FilePickerFileType("svg")
            {
                Patterns = ["*.svg"]
            }
        });

        var fileContent = FillTemplate(SvgFileTemplate!, new ExportParameters(iconViewModel));

        if (saveFileStream is { CanWrite: true } && fileContent is { Length: > 0 })
        {
            await using var streamWriter = new StreamWriter(saveFileStream);
            await streamWriter.WriteAsync(fileContent);
        }
    }
}

internal struct ExportParameters
{
    /// <summary>
    /// Provides a default set of Export parameters. You should edit this to your needs.
    /// </summary>
    /// <param name="icon"></param>
    internal ExportParameters(IIconViewModel icon)
    {
        var metaData = icon.MetaData;

        this.IconKind = icon.Name;
        this.IconPackName = icon.IconPackType.Name.Replace("PackIcon", "");
        this.PageWidth = Settings.Default.IconPreviewSize.ToString(CultureInfo.InvariantCulture);
        this.PageHeight = Settings.Default.IconPreviewSize.ToString(CultureInfo.InvariantCulture);
        this.FillColor = Settings.Default.IconForeground.ToString();
        this.Background = Settings.Default.IconBackground.ToString();
        this.StrokeColor = Settings.Default.IconForeground.ToString();
        this.StrokeWidth = "0"; // TODO: We need an API to read these values
        this.StrokeLineCap = nameof(PenLineCap.Round);
        this.StrokeLineJoin = nameof(PenLineJoin.Round);
        this.TransformMatrix = Matrix.Identity.ToString();

        this.IconPackHomepage = metaData?.ProjectUrl;
        this.IconPackLicense = metaData?.LicenseUrl;

        this.PathData = (icon as IconViewModel)?.GetPathData() ?? string.Empty;
    }

    internal string IconKind { get; set; }
    internal string IconPackName { get; set; }
    internal string? IconPackHomepage { get; set; }
    internal string? IconPackLicense { get; set; }
    internal string PageWidth { get; set; }
    internal string PageHeight { get; set; }
    internal string? PathData { get; set; }
    internal string FillColor { get; set; }
    internal string Background { get; set; }
    internal string StrokeColor { get; set; }
    internal string StrokeWidth { get; set; }
    internal string StrokeLineCap { get; set; }
    internal string StrokeLineJoin { get; set; }
    internal string TransformMatrix { get; set; }
}

internal static class ExportHelperExtensions
{
    internal static string CheckedReplace(this string input, string oldValue, Func<string?> newValue)
    {
        if (input.Contains(oldValue))
        {
            return input.Replace(oldValue, newValue());
        }

        return input;
    }
}