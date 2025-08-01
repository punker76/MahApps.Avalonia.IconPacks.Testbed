using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MahApps.IconPacksBrowser.Avalonia.Properties;

public partial class Settings : ObservableObject
{
    public static Settings Default { get; } = new Settings();
    
    /// <summary>
    /// Gets or sets the preview background
    /// </summary>
    [ObservableProperty] private Color? iconBackground;
    
    /// <summary>
    /// Gets or sets the preview background
    /// </summary>
    [ObservableProperty] private Color? iconForeground;

    /// <summary>
    /// Gets or sets the preview size
    /// </summary>
    [ObservableProperty] private double iconPreviewSize = 48;
    
    /// <summary>
    /// Gets or sets if the previewer is visible 
    /// </summary>
    [ObservableProperty] private bool isPreviewerVisible = false;
    

    partial void OnIconPreviewSizeChanging(double value)
    {
        // Make sure icon is not too small at all
        if (value < 8) value = 8;
    }
}