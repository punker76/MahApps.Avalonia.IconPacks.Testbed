using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Skia;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia;
using IconPacks.Avalonia.BootstrapIcons;
using IconPacks.Avalonia.BoxIcons;
using IconPacks.Avalonia.CircumIcons;
using IconPacks.Avalonia.Codicons;
using IconPacks.Avalonia.Coolicons;
using IconPacks.Avalonia.Core;
using IconPacks.Avalonia.Entypo;
using IconPacks.Avalonia.EvaIcons;
using IconPacks.Avalonia.FeatherIcons;
using IconPacks.Avalonia.FileIcons;
using IconPacks.Avalonia.Fontaudio;
using IconPacks.Avalonia.FontAwesome;
using IconPacks.Avalonia.FontAwesome5;
using IconPacks.Avalonia.FontAwesome6;
using IconPacks.Avalonia.Fontisto;
using IconPacks.Avalonia.ForkAwesome;
using IconPacks.Avalonia.GameIcons;
using IconPacks.Avalonia.Ionicons;
using IconPacks.Avalonia.JamIcons;
using IconPacks.Avalonia.KeyruneIcons;
using IconPacks.Avalonia.Lucide;
using IconPacks.Avalonia.Material;
using IconPacks.Avalonia.MaterialDesign;
using IconPacks.Avalonia.MaterialLight;
using IconPacks.Avalonia.MemoryIcons;
using IconPacks.Avalonia.Microns;
using IconPacks.Avalonia.MingCuteIcons;
using IconPacks.Avalonia.Modern;
using IconPacks.Avalonia.MynaUIIcons;
using IconPacks.Avalonia.Octicons;
using IconPacks.Avalonia.PhosphorIcons;
using IconPacks.Avalonia.PicolIcons;
using IconPacks.Avalonia.PixelartIcons;
using IconPacks.Avalonia.RadixIcons;
using IconPacks.Avalonia.RemixIcon;
using IconPacks.Avalonia.RPGAwesome;
using IconPacks.Avalonia.SimpleIcons;
using IconPacks.Avalonia.Typicons;
using IconPacks.Avalonia.Unicons;
using IconPacks.Avalonia.VaadinIcons;
using IconPacks.Avalonia.WeatherIcons;
using IconPacks.Avalonia.Zondicons;
using MahApps.IconPacksBrowser.Avalonia.Properties;
using MahApps.IconPacksBrowser.Avalonia.ViewModels;
using SkiaSharp;

namespace MahApps.IconPacksBrowser.Avalonia.Helper;

internal static class ExportHelper
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


    internal static SKPath? GetPath(Enum kind)
    {
        try
        {
            var packIconDataFactory = typeof(PackIconDataFactory<>).MakeGenericType(kind.GetType());
            var dataIndex = packIconDataFactory.GetProperty("DataIndex")!.GetValue(null);
            var dictionary = dataIndex!.GetType().GetProperty("Value")!.GetValue(dataIndex)!;

            object[] args = [kind, string.Empty];
            dictionary.GetType().GetMethod("TryGetValue")!.Invoke(dictionary, args);

            var skPath = SKPath.ParseSvgPathData(args[1] as string);

            // Transform if needed
            // TODO: Would be great to have an upstream API to get this information to not duplicate the code elsewhere
            switch (kind)
            {
                case PackIconBootstrapIconsKind:
                case PackIconBoxIconsKind:
                case PackIconCodiconsKind:
                case PackIconCooliconsKind:
                case PackIconEvaIconsKind:
                case PackIconFileIconsKind:
                case PackIconFontaudioKind:
                case PackIconFontistoKind:
                case PackIconForkAwesomeKind:
                case PackIconJamIconsKind:
                case PackIconLucideKind:
                case PackIconMingCuteIconsKind:
                case PackIconMynaUIIconsKind:
                case PackIconRPGAwesomeKind:
                case PackIconTypiconsKind:
                case PackIconVaadinIconsKind:
                    skPath.Transform(SKMatrix.CreateScale(1, -1));
                    break;
            }

            skPath.FillType = SKPathFillType.EvenOdd;
            
            return skPath;
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static SKPath MoveIntoBounds(this SKPath path, float width, float height)
    {
        var scale = Math.Max(width, height) / Math.Max(path.Bounds.Width, path.Bounds.Height);
        path.Transform(SKMatrix.CreateScale(scale, scale));
        path.Transform(SKMatrix.CreateTranslation(
            - path.Bounds.Left - (path.Bounds.Width - width) / 2, 
            - path.Bounds.Top - (path.Bounds.Width - width) / 2));
        
        return path;
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

    internal static async Task SaveAsPngAsync(IIconViewModel icon)
    {
        await using var saveFileStream = await MainViewModel.Instance.SaveFileDialogAsync(filters: new[]
        {
            FilePickerFileTypes.ImagePng
        });
        
        int renderWidth = Settings.Default.IconPreviewSize;
        int renderHeight = Settings.Default.IconPreviewSize;
        
        using var path = GetPath(icon.Value)?.MoveIntoBounds(renderWidth, renderHeight);

        using var bitmap = new SKBitmap(new SKImageInfo(renderWidth, renderHeight));
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint();

        paint.IsAntialias = true;
        
        if (Settings.Default.IconBackground != null)
        {
            canvas.DrawColor(Settings.Default.IconBackground.Value.ToSKColor());
        }

        paint.Color = Settings.Default.IconForeground.ToSKColor();
        paint.IsStroke = icon.Value is PackIconFeatherIconsKind;

        canvas.DrawPath(path, paint);

        if (saveFileStream is { CanWrite: true })
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var encodedImage = image.Encode(SKEncodedImageFormat.Png, 100);
            encodedImage.SaveTo(saveFileStream);
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
        this.Background = Settings.Default.IconBackground.ToString() ?? Colors.Black.ToString();
        this.StrokeColor = Settings.Default.IconForeground.ToString();
        this.StrokeWidth = icon.Value is PackIconFeatherIconsKind ? "2" : "0"; // TODO: We need an API to read these values
        this.StrokeLineCap = nameof(PenLineCap.Round);
        this.StrokeLineJoin = nameof(PenLineJoin.Round);
        this.TransformMatrix = Matrix.Identity.ToString();

        this.IconPackHomepage = metaData?.ProjectUrl;
        this.IconPackLicense = metaData?.LicenseUrl;

        this.PathData = ExportHelper.GetPath(icon.Value)?.ToSvgPathData() ?? string.Empty;
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