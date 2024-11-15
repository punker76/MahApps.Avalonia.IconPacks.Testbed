using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using IconPacks.Avalonia.BootstrapIcons;
using IconPacks.Avalonia.BoxIcons;
using IconPacks.Avalonia.CircumIcons;
using IconPacks.Avalonia.Codicons;
using IconPacks.Avalonia.Coolicons;
using IconPacks.Avalonia.Entypo;
using IconPacks.Avalonia.EvaIcons;
using IconPacks.Avalonia.FeatherIcons;
using IconPacks.Avalonia.FileIcons;
using IconPacks.Avalonia.Fontaudio;
using IconPacks.Avalonia.FontAwesome;
using IconPacks.Avalonia.Fontisto;
using IconPacks.Avalonia.ForkAwesome;
using IconPacks.Avalonia.GameIcons;
using IconPacks.Avalonia.Ionicons;
using IconPacks.Avalonia.JamIcons;
using IconPacks.Avalonia.Lucide;
using IconPacks.Avalonia.Material;
using IconPacks.Avalonia.MaterialDesign;
using IconPacks.Avalonia.MaterialLight;
using IconPacks.Avalonia.MemoryIcons;
using IconPacks.Avalonia.Microns;
using IconPacks.Avalonia.Modern;
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
            .Sort(SortExpressionComparer<IIconViewModel>.Ascending(e => e.IconPackName).ThenByAscending(e => e.Name))
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
                (typeof(PackIconBootstrapIconsKind), typeof(PackIconBootstrapIcons)),
                (typeof(PackIconBoxIconsKind), typeof(PackIconBoxIcons)),
                (typeof(PackIconCircumIconsKind), typeof(PackIconCircumIcons)),
                (typeof(PackIconCodiconsKind), typeof(PackIconCodicons)),
                (typeof(PackIconCooliconsKind), typeof(PackIconCoolicons)),
                (typeof(PackIconEntypoKind), typeof(PackIconEntypo)),
                (typeof(PackIconEvaIconsKind), typeof(PackIconEvaIcons)),
                (typeof(PackIconFeatherIconsKind), typeof(PackIconFeatherIcons)),
                (typeof(PackIconFileIconsKind), typeof(PackIconFileIcons)),
                (typeof(PackIconFontaudioKind), typeof(PackIconFontaudio)),
                (typeof(PackIconFontAwesomeKind), typeof(PackIconFontAwesome)),
                (typeof(PackIconFontistoKind), typeof(PackIconFontisto)),
                (typeof(PackIconForkAwesomeKind), typeof(PackIconForkAwesome)),
                (typeof(PackIconGameIconsKind), typeof(PackIconGameIcons)),
                (typeof(PackIconIoniconsKind), typeof(PackIconIonicons)),
                (typeof(PackIconJamIconsKind), typeof(PackIconJamIcons)),
                (typeof(PackIconLucideKind), typeof(PackIconLucide)),
                (typeof(PackIconMaterialKind), typeof(PackIconMaterial)),
                (typeof(PackIconMaterialLightKind), typeof(PackIconMaterialLight)),
                (typeof(PackIconMaterialDesignKind), typeof(PackIconMaterialDesign)),
                (typeof(PackIconMemoryIconsKind), typeof(PackIconMemoryIcons)),
                (typeof(PackIconMicronsKind), typeof(PackIconMicrons)),
                (typeof(PackIconModernKind), typeof(PackIconModern)),
                (typeof(PackIconOcticonsKind), typeof(PackIconOcticons)),
                (typeof(PackIconPhosphorIconsKind), typeof(PackIconPhosphorIcons)),
                (typeof(PackIconPicolIconsKind), typeof(PackIconPicolIcons)),
                (typeof(PackIconPixelartIconsKind), typeof(PackIconPixelartIcons)),
                (typeof(PackIconRadixIconsKind), typeof(PackIconRadixIcons)),
                (typeof(PackIconRemixIconKind), typeof(PackIconRemixIcon)),
                (typeof(PackIconRPGAwesomeKind), typeof(PackIconRPGAwesome)),
                (typeof(PackIconSimpleIconsKind), typeof(PackIconSimpleIcons)),
                (typeof(PackIconTypiconsKind), typeof(PackIconTypicons)),
                (typeof(PackIconUniconsKind), typeof(PackIconUnicons)),
                (typeof(PackIconVaadinIconsKind), typeof(PackIconVaadinIcons)),
                (typeof(PackIconWeatherIconsKind), typeof(PackIconWeatherIcons)),
                (typeof(PackIconZondiconsKind), typeof(PackIconZondicons)),
            });

        var loadIconsTasks = new List<Task<IEnumerable<IIconViewModel>>>();

        foreach (var (enumType, iconPackType) in availableIconPacks)
        {
            var iconPack = new IconPackViewModel(this, enumType, iconPackType);
            AvailableIconPacks.Add(new NavigationViewItem() { Content = iconPack.Caption, Tag = iconPack });
            loadIconsTasks.Add(iconPack.LoadIconsAsync(enumType, iconPackType));
        }

        var icons = (await Task.WhenAll(loadIconsTasks)).SelectMany(x => x);
        
         _iconsCache.Edit(async (e) =>
         {
             e.Load(icons);
         });
    }

    /// <summary>
    /// Gets the navigation view items for all icon packs
    /// </summary>
    public ObservableCollection<NavigationViewItemBase> AvailableIconPacks { get; } =
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