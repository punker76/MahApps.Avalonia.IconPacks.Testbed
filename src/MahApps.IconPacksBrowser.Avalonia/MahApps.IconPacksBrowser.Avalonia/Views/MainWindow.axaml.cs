using Avalonia.Controls;
using Avalonia.Media;
using FluentAvalonia.UI.Windowing;

namespace MahApps.IconPacksBrowser.Avalonia.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();

        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
        TitleBar.Height = 45;
        TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(50, 125,125,125);
        TitleBar.ButtonPressedForegroundColor = Colors.White;
        TitleBar.ExtendsContentIntoTitleBar = true;
        
        PlatformFeatures.SetWindowBorderColor(Colors.Green);
    }
}