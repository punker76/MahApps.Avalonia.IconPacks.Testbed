using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Windowing;
using MahApps.IconPacksBrowser.Avalonia.ViewModels;

namespace MahApps.IconPacksBrowser.Avalonia.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // If we are on desktop, we need to add some margin in order to render the WindowButtons correctly.
        if (TopLevel.GetTopLevel(this) is AppWindow window)
        {
            WindowButtonsPlaceHolder.Width = window.TitleBar.RightInset;
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // If we are on desktop, we need to add some margin in order to render the WindowButtons correctly.
        if (TopLevel.GetTopLevel(this) is AppWindow window)
        {
            WindowButtonsPlaceHolder.Width = window.TitleBar.RightInset;
        }

        _ = (DataContext as MainViewModel)!.LoadIconPacksAsync();
    }
}