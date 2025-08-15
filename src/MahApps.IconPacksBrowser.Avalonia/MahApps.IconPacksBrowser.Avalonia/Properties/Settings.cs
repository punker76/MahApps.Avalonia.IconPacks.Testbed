using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MahApps.IconPacksBrowser.Avalonia.Properties;

public partial class Settings : ObservableObject
{
    public static Settings Default { get; } = new Settings();

    /// <summary>
    /// Gets or sets the folder with the export templates to use
    /// </summary>
    [ObservableProperty] private string? exportTemplatesDir;
    
    /// <summary>
    /// Gets or sets the preview background
    /// </summary>
    [ObservableProperty] private Color? iconBackground;
    
    /// <summary>
    /// Gets or sets the preview background
    /// </summary>
    [ObservableProperty] private Color iconForeground = Application.Current?.FindResource("SystemAccentColor") as Color?
                                                        ?? Colors.Green;

    /// <summary>
    /// Gets or sets the preview size
    /// </summary>
    [ObservableProperty] private double iconPreviewSize = 48;
    
    /// <summary>
    /// Gets or sets if the previewer is visible 
    /// </summary>
    [ObservableProperty] private bool isPreviewerVisible = false;

    [RelayCommand]
    private void ToggleIsPreviewerVisible()
    {
        IsPreviewerVisible = !IsPreviewerVisible;
    }
    

    partial void OnIconPreviewSizeChanging(double value)
    {
        // Make sure icon is not too small at all
        if (value < 8) value = 8;
    }
    
    
}