using FluentAvalonia.UI.Controls;

namespace MahApps.IconPacksBrowser.Avalonia.ViewModels;

public class NavigationItemViewModelBase
{
    public object? Tag { get; init; }

    public string? Title { get; init; }

    public IconSource? Icon { get; init; }
}

public class SeparatorNavigationItemViewModel : NavigationItemViewModelBase;

public class IconPackNavigationItemViewModel : NavigationItemViewModelBase
{
    public IconPackNavigationItemViewModel(IconPackViewModel iconPack)
    {
        Title = iconPack.Caption;
        Tag = iconPack;
        IconPack = iconPack;
    }

    public IconPackViewModel IconPack { get; }
}

public class AllIconPacksNavigationItemViewModel : NavigationItemViewModelBase
{
    public AllIconPacksNavigationItemViewModel()
    {
        Title = "All Icons";
    }

    public MainViewModel MainViewModel => MainViewModel.Instance;
}