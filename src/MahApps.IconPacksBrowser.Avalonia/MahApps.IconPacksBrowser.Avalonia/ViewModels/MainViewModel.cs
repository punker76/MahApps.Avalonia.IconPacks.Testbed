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
            .Page(_pager)
            .Do(change => PagingUpdate(change.Response))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _visibleIcons)
            .Subscribe();

        LoadIconPacks().SafeFireAndForget();
    }

    private void PagingUpdate(IPageResponse response)
    {
        TotalItems = response.TotalSize;
        CurrentPage = response.Page;
        TotalPages = response.Pages;
    }

    [ObservableProperty] int _TotalItems;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(FirstPageCommand), nameof(PreviousPageCommand), nameof(NextPageCommand))]
    int _CurrentPage;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(NextPageCommand), nameof(PreviousPageCommand), nameof(LastPageCommand))]
    int _TotalPages;

    private bool CanMoveToFirstPage() => CurrentPage > FIRST_PAGE;

    [RelayCommand(CanExecute = nameof(CanMoveToFirstPage), AllowConcurrentExecutions = false)]
    public Task FirstPage()
    {
        _pager.OnNext(new PageRequest(FIRST_PAGE, PAGE_SIZE));
        return Task.CompletedTask;
    }

    private bool CanMoveToPreviousPage() => CurrentPage > FIRST_PAGE;

    [RelayCommand(CanExecute = nameof(CanMoveToPreviousPage), AllowConcurrentExecutions = false)]
    private Task PreviousPage()
    {
        _pager.OnNext(new PageRequest(CurrentPage - 1, PAGE_SIZE));
        return Task.CompletedTask;
    }

    private bool CanMoveToNextPage() => CurrentPage < TotalPages;

    [RelayCommand(CanExecute = nameof(CanMoveToNextPage), AllowConcurrentExecutions = false)]
    private Task NextPage()
    {
        _pager.OnNext(new PageRequest(CurrentPage + 1, PAGE_SIZE));
        return Task.CompletedTask;
    }

    private bool CanMoveToLastPage() => CurrentPage < TotalPages;

    [RelayCommand(CanExecute = nameof(CanMoveToLastPage), AllowConcurrentExecutions = false)]
    private Task LastPage()
    {
        _pager.OnNext(new PageRequest(TotalPages, PAGE_SIZE));
        return Task.CompletedTask;
    }

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