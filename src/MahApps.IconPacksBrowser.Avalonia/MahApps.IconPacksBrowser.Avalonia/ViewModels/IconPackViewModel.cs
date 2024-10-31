using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Data;
using IconPacks.Avalonia;
using IconPacks.Avalonia.Attributes;
using MahApps.IconPacksBrowser.Avalonia.Helper;

namespace MahApps.IconPacksBrowser.Avalonia.ViewModels;

public partial class IconPackViewModel : ViewModelBase
{
    private IconPackViewModel(MainViewModel mainViewModel)
    {
        this.MainViewModel = mainViewModel;
        
        // TODO
        // SaveAsPngCommand = new SimpleCommand((_) => SaveAsBitmapExecute(new PngBitmapEncoder()), (_) => SelectedIcon is not null);
        // SaveAsJpegCommand = new SimpleCommand((_) => SaveAsBitmapExecute(new JpegBitmapEncoder()), (_) => SelectedIcon is not null);
        // SaveAsBmpCommand = new SimpleCommand((_) => SaveAsBitmapExecute(new BmpBitmapEncoder()), (_) => SelectedIcon is not null);
    }

    public IconPackViewModel(MainViewModel mainViewModel, Type enumType, Type packType)
        : this(mainViewModel)
    {
        // Get the Name of the IconPack via Attributes
        this.MetaData = Attribute.GetCustomAttribute(packType, typeof(MetaDataAttribute)) as MetaDataAttribute;

        this.Caption = this.MetaData?.Name;

        this.LoadIconsAsync(enumType, packType).SafeFireAndForget();
    }

    public async Task<IEnumerable<IIconViewModel>> LoadIconsAsync(Type enumType, Type packType)
    {
        var collection = await Task.Run(() => GetIcons(enumType, packType).OrderBy(i => i.Name, StringComparer.InvariantCultureIgnoreCase).ToList());

        this.Icons = new ObservableCollection<IIconViewModel>(collection);
        this.IconCount = collection.Count;

        return Icons;
    }
    
    [ObservableProperty]
    private IEnumerable<IIconViewModel> _icons;
    
    [ObservableProperty]
    private int _iconCount;
    

    // TODO: Move To MainViewModel?
    private static bool FilterIconsPredicate(string filterText, IIconViewModel iconViewModel)
    {
        if (string.IsNullOrWhiteSpace(filterText))
        {
            return true;
        }
        else
        {
            var filterSubStrings = filterText.Split(new[] { '+', ',', ';', '&' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var filterSubString in filterSubStrings)
            {
                var filterOrSubStrings = filterSubString.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                var isInName = filterOrSubStrings.Any(x => iconViewModel.Name.IndexOf(x.Trim(), StringComparison.CurrentCultureIgnoreCase) >= 0);
                var isInDescription = filterOrSubStrings.Any(x => (iconViewModel.Description?.IndexOf(x.Trim(), StringComparison.CurrentCultureIgnoreCase) ?? -1) >= 0);

                if (!(isInName || isInDescription)) return false;
            }

            return true;
        }
    }

    private static MetaDataAttribute? GetMetaData(Type packType)
    {
        var metaData = Attribute.GetCustomAttribute(packType, typeof(MetaDataAttribute)) as MetaDataAttribute;
        return metaData;
    }

    private static IEnumerable<IIconViewModel> GetIcons(Type enumType, Type packType)
    {
        var metaData = GetMetaData(packType);
        return Enum.GetValues(enumType)
            .OfType<Enum>()
            .Where(k => k.ToString() != "None")
            .Select(k => new IconViewModel(enumType, packType, k, metaData!));
    }

    public MainViewModel MainViewModel { get; }

    public string? Caption { get; }

    public MetaDataAttribute? MetaData { get; }
    
    
    // [RelayCommand]
    // private async Task SaveAsSvgAsync()
    // {
    //     return;
    //     
    //     var progress = await dialogCoordinator.ShowProgressAsync(MainViewModel, "Export", "Saving selected icon as SVG-file");
    //     progress.SetIndeterminate();
    //
    //     try
    //     {
    //         var fileSaveDialog = new SaveFileDialog()
    //         {
    //             AddExtension = true,
    //             DefaultExt = "svg",
    //             FileName = $"{SelectedIcon.IconPackName}-{SelectedIcon.Name}",
    //             Filter = "SVG Drawing (*.svg)|*.svg",
    //             OverwritePrompt = true
    //         };
    //
    //         if (fileSaveDialog.ShowDialog() == true && SelectedIcon is IconViewModel icon)
    //         {
    //             var iconControl = icon.GetPackIconControlBase();
    //
    //             iconControl.BeginInit();
    //             iconControl.Width = Settings.Default.IconPreviewSize;
    //             iconControl.Height = Settings.Default.IconPreviewSize;
    //             iconControl.EndInit();
    //             iconControl.ApplyTemplate();
    //
    //             var iconPath = iconControl.FindChild<Path>();
    //
    //             var bBox = iconPath.Data.Bounds;
    //
    //             var svgSize = Math.Max(bBox.Width, bBox.Height);
    //             var scaleFactor = Settings.Default.IconPreviewSize / svgSize;
    //             var T = iconPath.LayoutTransform.Value;
    //
    //             T.Translate(-bBox.Left - (T.M11 < 0 ? bBox.Width : 0) + Math.Sign(T.M11) * (svgSize - bBox.Width) / 2,
    //                 -bBox.Top - (T.M22 < 0 ? bBox.Height : 0) + Math.Sign(T.M22) * (svgSize - bBox.Height) / 2);
    //             T.Scale(scaleFactor, scaleFactor);
    //
    //             var transform = string.Join(",", new[]
    //             {
    //                 T.M11.ToString(CultureInfo.InvariantCulture),
    //                 T.M21.ToString(CultureInfo.InvariantCulture),
    //                 T.M12.ToString(CultureInfo.InvariantCulture),
    //                 T.M22.ToString(CultureInfo.InvariantCulture),
    //                 (Math.Sign(T.M11) * T.OffsetX).ToString(CultureInfo.InvariantCulture),
    //                 (Math.Sign(T.M22) * T.OffsetY).ToString(CultureInfo.InvariantCulture)
    //             });
    //
    //             var parameters = new ExportParameters(SelectedIcon)
    //             {
    //                 FillColor = iconPath.Fill is not null
    //                     ? Settings.Default.IconForeground.ToString(CultureInfo.InvariantCulture).Remove(1, 2)
    //                     : "none", // We need to remove the alpha channel for svg
    //                 Background = Settings.Default.IconBackground.ToString(CultureInfo.InvariantCulture).Remove(1, 2),
    //                 PathData = iconControl.Data,
    //                 StrokeColor = iconPath.Stroke is not null
    //                     ? Settings.Default.IconForeground.ToString(CultureInfo.InvariantCulture).Remove(1, 2)
    //                     : "none", // We need to remove the alpha channel for svg
    //                 StrokeWidth = iconPath.Stroke is null ? "0" : (scaleFactor * iconPath.StrokeThickness).ToString(CultureInfo.InvariantCulture),
    //                 StrokeLineCap = iconPath.StrokeEndLineCap.ToString().ToLower(),
    //                 StrokeLineJoin = iconPath.StrokeLineJoin.ToString().ToLower(),
    //                 TransformMatrix = transform
    //             };
    //
    //             var svgFileTemplate = ExportHelper.SvgFileTemplate;
    //
    //             var svgFileContent = ExportHelper.FillTemplate(svgFileTemplate, parameters);
    //
    //             using IO.StreamWriter file = new IO.StreamWriter(fileSaveDialog.FileName);
    //             await file.WriteAsync(svgFileContent);
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         await dialogCoordinator.ShowMessageAsync(MainViewModel, "Error", e.Message);
    //     }
    //
    //     await progress.CloseAsync();
    // }
    //
    // public ICommand SaveAsWpfCommand { get; }
    //
    // private async void SaveAsWpf_Execute()
    // {
    //     var progress = await dialogCoordinator.ShowProgressAsync(MainViewModel, "Export", "Saving selected icon as WPF-XAML-file");
    //     progress.SetIndeterminate();
    //
    //     try
    //     {
    //         var fileSaveDialog = new SaveFileDialog()
    //         {
    //             AddExtension = true,
    //             DefaultExt = "xaml",
    //             FileName = $"{SelectedIcon.IconPackName}-{SelectedIcon.Name}",
    //             Filter = "WPF-XAML (*.xaml)|*.xaml",
    //             OverwritePrompt = true
    //         };
    //
    //         if (fileSaveDialog.ShowDialog() == true && SelectedIcon is IconViewModel icon)
    //         {
    //             var iconControl = icon.GetPackIconControlBase();
    //
    //             iconControl.BeginInit();
    //             iconControl.Width = Settings.Default.IconPreviewSize;
    //             iconControl.Height = Settings.Default.IconPreviewSize;
    //             iconControl.EndInit();
    //             iconControl.ApplyTemplate();
    //
    //             var iconPath = iconControl.FindChild<Path>();
    //
    //             var bBox = iconPath.Data.Bounds;
    //
    //             var xamlSize = Math.Max(bBox.Width, bBox.Height);
    //             var T = iconPath.LayoutTransform.Value;
    //
    //             var scaleFactor = Settings.Default.IconPreviewSize / xamlSize;
    //
    //             var wpfFileTemplate = ExportHelper.WpfFileTemplate;
    //
    //             var parameters = new ExportParameters(SelectedIcon)
    //             {
    //                 FillColor = iconPath.Fill is not null ? Settings.Default.IconForeground.ToString(CultureInfo.InvariantCulture) : "{x:Null}",
    //                 PathData = iconControl.Data,
    //                 StrokeColor = iconPath.Stroke is not null ? Settings.Default.IconForeground.ToString(CultureInfo.InvariantCulture) : "{x:Null}",
    //                 StrokeWidth = iconPath.Stroke is null ? "0" : (scaleFactor * iconPath.StrokeThickness).ToString(CultureInfo.InvariantCulture),
    //                 StrokeLineCap = iconPath.StrokeEndLineCap.ToString().ToLower(),
    //                 StrokeLineJoin = iconPath.StrokeLineJoin.ToString().ToLower(),
    //                 TransformMatrix = T.ToString(CultureInfo.InvariantCulture)
    //             };
    //
    //             var wpfFileContent = ExportHelper.FillTemplate(wpfFileTemplate, parameters);
    //
    //             using IO.StreamWriter file = new IO.StreamWriter(fileSaveDialog.FileName);
    //             await file.WriteAsync(wpfFileContent);
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         await dialogCoordinator.ShowMessageAsync(MainViewModel, "Error", e.Message);
    //     }
    //
    //     await progress.CloseAsync();
    // }
    //
    // public ICommand SaveAsUwpCommand { get; }
    //
    // private async void SaveAsUwp_Execute()
    // {
    //     var progress = await dialogCoordinator.ShowProgressAsync(MainViewModel, "Export", "Saving selected icon as WPF-XAML-file");
    //     progress.SetIndeterminate();
    //
    //     try
    //     {
    //         var fileSaveDialog = new SaveFileDialog()
    //         {
    //             AddExtension = true,
    //             DefaultExt = "xaml",
    //             FileName = $"{SelectedIcon.IconPackName}-{SelectedIcon.Name}",
    //             Filter = "UWP-XAML (*.xaml)|*.xaml",
    //             OverwritePrompt = true
    //         };
    //
    //         if (fileSaveDialog.ShowDialog() == true && SelectedIcon is IconViewModel icon)
    //         {
    //             var iconControl = icon.GetPackIconControlBase();
    //
    //             iconControl.BeginInit();
    //             iconControl.Width = Settings.Default.IconPreviewSize;
    //             iconControl.Height = Settings.Default.IconPreviewSize;
    //             iconControl.EndInit();
    //             iconControl.ApplyTemplate();
    //
    //             var iconPath = iconControl.FindChild<Path>();
    //
    //             var bBox = iconPath.Data.Bounds;
    //
    //             var xamlSize = Math.Max(bBox.Width, bBox.Height);
    //             var scaleFactor = Settings.Default.IconPreviewSize / xamlSize;
    //             var T = iconPath.LayoutTransform.Value;
    //
    //             var wpfFileTemplate = ExportHelper.UwpFileTemplate;
    //
    //             var parameters = new ExportParameters(SelectedIcon)
    //             {
    //                 FillColor = iconPath.Fill is not null ? iconPath.Fill.ToString(CultureInfo.InvariantCulture) : "{x:Null}",
    //                 PathData = iconControl.Data,
    //                 StrokeColor = iconPath.Stroke is not null ? iconPath.Stroke.ToString(CultureInfo.InvariantCulture) : "{x:Null}",
    //                 StrokeWidth = iconPath.Stroke is null ? "0" : (scaleFactor * iconPath.StrokeThickness).ToString(CultureInfo.InvariantCulture),
    //                 StrokeLineCap = iconPath.StrokeEndLineCap.ToString().ToLower(),
    //                 StrokeLineJoin = iconPath.StrokeLineJoin.ToString().ToLower(),
    //                 TransformMatrix = T.ToString(CultureInfo.InvariantCulture)
    //             };
    //
    //             var wpfFileContent = ExportHelper.FillTemplate(wpfFileTemplate, parameters);
    //
    //             using IO.StreamWriter file = new IO.StreamWriter(fileSaveDialog.FileName);
    //             await file.WriteAsync(wpfFileContent);
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         await dialogCoordinator.ShowMessageAsync(MainViewModel, "Error", e.Message);
    //     }
    //
    //     await progress.CloseAsync();
    // }
    //
    // public ICommand SaveAsPngCommand { get; }
    //
    // public ICommand SaveAsJpegCommand { get; }
    //
    // public ICommand SaveAsBmpCommand { get; }
    //
    // private async void SaveAsBitmapExecute(BitmapEncoder encoder)
    // {
    //     var progress = await dialogCoordinator.ShowProgressAsync(MainViewModel, "Export", "Saving selected icon as bitmap image");
    //     progress.SetIndeterminate();
    //
    //     try
    //     {
    //         var fileSaveDialog = new SaveFileDialog()
    //         {
    //             AddExtension = true,
    //             FileName = $"{SelectedIcon.IconPackName}-{SelectedIcon.Name}",
    //             OverwritePrompt = true
    //         };
    //
    //         fileSaveDialog.Filter = encoder switch
    //         {
    //             PngBitmapEncoder => "Png-File (*.png)|*.png",
    //             JpegBitmapEncoder => "Jpeg-File (*.jpg)|*.jpg",
    //             BmpBitmapEncoder => "Bmp-File (*.bmp)|*.bmp",
    //             _ => fileSaveDialog.Filter
    //         };
    //
    //         if (fileSaveDialog.ShowDialog() == true && SelectedIcon is IconViewModel icon)
    //         {
    //             var canvas = new Canvas
    //             {
    //                 Width = Settings.Default.IconPreviewSize,
    //                 Height = Settings.Default.IconPreviewSize,
    //                 Background = new SolidColorBrush(Settings.Default.IconBackground)
    //             };
    //
    //             var packIconControl = new PackIconControl();
    //             packIconControl.BeginInit();
    //             packIconControl.Kind = icon.Value as Enum;
    //             packIconControl.Width = Settings.Default.IconPreviewSize;
    //             packIconControl.Height = Settings.Default.IconPreviewSize;
    //             packIconControl.Foreground = new SolidColorBrush(Settings.Default.IconForeground);
    //
    //             packIconControl.EndInit();
    //             packIconControl.ApplyTemplate();
    //
    //             canvas.Children.Add(packIconControl);
    //
    //             var size = new Size(Settings.Default.IconPreviewSize, Settings.Default.IconPreviewSize);
    //             canvas.Measure(size);
    //             canvas.Arrange(new Rect(size));
    //
    //             var renderTargetBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
    //             renderTargetBitmap.Render(canvas);
    //
    //             encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
    //
    //             using var fileStream = new IO.FileStream(fileSaveDialog.FileName, IO.FileMode.Create);
    //             encoder.Save(fileStream);
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         await dialogCoordinator.ShowMessageAsync(MainViewModel, "Error", e.Message);
    //     }
    //
    //     await progress.CloseAsync();
    // }
}