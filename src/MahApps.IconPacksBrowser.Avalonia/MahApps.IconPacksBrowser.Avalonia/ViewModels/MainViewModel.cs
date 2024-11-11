using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Operators;
using FluentAvalonia.UI.Controls;
using IconPacks.Avalonia;
using MahApps.IconPacksBrowser.Avalonia.Helper;
using ReactiveUI;

namespace MahApps.IconPacksBrowser.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const int PAGE_SIZE = 100;
    private const int FIRST_PAGE = 1;
    private readonly ISubject<PageRequest> _pager;

    public static MainViewModel Instance { get; } = new();

    public MainViewModel()
    {
        _SelectedIconPack = AvailableIconPacks[0];

        var filterByText = this.WhenAnyValue(x => x.FilterText, x => x.SelectedIconPack)
            .Throttle(TimeSpan.FromMilliseconds(350), RxApp.MainThreadScheduler)
            .Select(IconFilter);

        _pager = new BehaviorSubject<PageRequest>(new PageRequest(FIRST_PAGE, PAGE_SIZE));

        _iconsCache.Connect()
            .Filter(filterByText)
            .Sort(SortExpressionComparer<IIconViewModel>.Ascending(e => e.Identifier))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _visibleIcons)
            .Subscribe();

        LoadIconPacks().SafeFireAndForget();
    }
    
    [ObservableProperty] int _TotalItems;
    
    private async Task LoadIconPacks()
    {
        var availableIconPacks = new List<(Type EnumType, Type IconPackType)>(
            new[]
            {
                (typeof(PackIconBoxIconsKind), typeof(PackIconBoxIcons)),
            });

        foreach (var (enumType, iconPackType) in availableIconPacks)
        {
            var iconPack = new IconPackViewModel(this, enumType, iconPackType);
            AvailableIconPacks.Add(new NavigationViewItem() { Content = iconPack.Caption, Tag = iconPack });
            _iconsCache.AddOrUpdate(await iconPack.LoadIconsAsync(enumType, iconPackType));
        }
    }

    /// <summary>
    /// Gets the navigation view items for all icon packs
    /// </summary>
    public List<NavigationViewItemBase> AvailableIconPacks { get; } =
    [
        new NavigationViewItem() { Content = "All Icons" },
        new NavigationViewItemSeparator()
    ];

    private readonly SourceCache<IIconViewModel, string> _iconsCache = new(x => x.Identifier);

    private ReadOnlyObservableCollection<IIconViewModel> _visibleIcons;

    public ReadOnlyObservableCollection<IIconViewModel> VisibleIcons => _visibleIcons;

    [ObservableProperty] private NavigationViewItemBase _SelectedIconPack;


    [ObservableProperty] private IIconViewModel? _SelectedIcon;


    [ObservableProperty] string? _FilterText;

    Func<IIconViewModel, bool> IconFilter((string? filterText, NavigationViewItemBase selectedIocnPack) args) => icon =>
    {
        return
            // Filter for IconPackType
            (args.selectedIocnPack == AvailableIconPacks[0] || icon.MetaData.Name == args.selectedIocnPack.Content?.ToString()) &&

            // Filter for IconName
            (string.IsNullOrWhiteSpace(args.filterText) || icon.Name.Contains(args.filterText.Trim(), StringComparison.OrdinalIgnoreCase));
    };

    private async Task DoCopyTextToClipboard(string? text)
    {
        if (text != null)
        {
            await this.SetClipboardContent(text);
        }
    }

    [RelayCommand]
    public async Task CopyTextToClipboardAsync(string? text)
    {
        await DoCopyTextToClipboard(text);
    }

    [RelayCommand]
    public async Task CopyToClipboardTextAsync(IIconViewModel icon)
    {
        await DoCopyTextToClipboard(icon.CopyToClipboardText);
    }


    [RelayCommand]
    public async Task CopyToClipboardAsContentTextAsync(IIconViewModel icon)
    {
        await DoCopyTextToClipboard(icon.CopyToClipboardAsContentText);
    }

    [RelayCommand]
    public async Task CopyToClipboardAsGeometryTextAsync(IIconViewModel icon)
    {
        await DoCopyTextToClipboard(icon.CopyToClipboardAsGeometryText);
    }

    [RelayCommand]
    public async Task CopyToClipboardAsPathIconTextAsync(IIconViewModel icon)
    {
        await DoCopyTextToClipboard(icon.CopyToClipboardAsPathIconText);
    }
}