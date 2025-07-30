using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using ObservableCollections;
using R3;

namespace MahApps.IconPacksBrowser.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const int PAGE_SIZE = 100;
    private const int FIRST_PAGE = 1;

    public static MainViewModel Instance { get; } = new();

    public MainViewModel()
    {
        _SelectedIconPackNavigationItem = AvailableIconPacks[0];

        // for ui synchronization safety of viewmodel

        var view = _iconsCache.CreateView(x => x);

        var filter = new IconFilter(this);
       // view.AttachFilter(filter);
        
        this.ObservePropertyChanged(x=> x.FilterText)
            .Delay(TimeSpan.FromSeconds(0.3))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ => view.AttachFilter(filter));
        
        this.ObservePropertyChanged(x=> x.SelectedIconPack)
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ => view.AttachFilter(filter));
        
        VisibleIcons = view.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current);
        // var filterByText = this.WhenAnyValue(x => x.FilterText, x => x.SelectedIconPack)
        //     .Throttle(TimeSpan.FromMilliseconds(350), RxApp.MainThreadScheduler)
        //     .Select(IconFilter);
        //
        // _iconsCache.Connect()
        //     .Filter(filterByText)
        //     .Sort(SortExpressionComparer<IIconViewModel>.Ascending(e => e.IconPackName).ThenByAscending(e => e.Name))
        //     .ObserveOn(RxApp.MainThreadScheduler)
        //     .Bind(out _visibleIcons)
        //     .Subscribe();
        

        LoadIconPacks().SafeFireAndForget();
    }

    [ObservableProperty] int _TotalItems;

    private async Task LoadIconPacks()
    {
        var availableIconPacks = new List<(Type EnumType, Type IconPackType)>(
            [
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
                (typeof(PackIconZondiconsKind), typeof(PackIconZondicons))
            ])
            .Select(tuple =>
            {
                var iconPack = new IconPackViewModel(this, tuple.EnumType, tuple.IconPackType);
                AvailableIconPacks.Add(new IconPackNavigationItemViewModel(iconPack));
                return iconPack;
            });


        var loadIconsTasks = availableIconPacks.Select(ip => ip.LoadIconsAsync(ip.EnumType, ip.PackType));
        _iconsCache.AddRange((await Task.WhenAll(loadIconsTasks)).SelectMany(x => x));
        
        TotalItems = AvailableIconPacks.Count;
    }

    /// <summary>
    /// Gets the navigation view items for all icon packs
    /// </summary>
    public ObservableCollection<NavigationItemViewModelBase> AvailableIconPacks { get; } =
    [
        new AllIconPacksNavigationItemViewModel(),
        new SeparatorNavigationItemViewModel()
    ];

    private readonly ObservableList<IIconViewModel> _iconsCache = new();

    public NotifyCollectionChangedSynchronizedViewList<IIconViewModel> VisibleIcons { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedIconPack))]
    private NavigationItemViewModelBase _SelectedIconPackNavigationItem;

    public IconPackViewModel? SelectedIconPack => SelectedIconPackNavigationItem.Tag as IconPackViewModel;

    [ObservableProperty] private IIconViewModel? _SelectedIcon;


    [ObservableProperty] string? _FilterText;
    

    private class IconFilter(MainViewModel viewModel) : ISynchronizedViewFilter<IIconViewModel, IIconViewModel>
    {
        public bool IsMatch(IIconViewModel icon, IIconViewModel transformedIcon)
        {
            return 
                // Filter for IconPackType
                (viewModel.SelectedIconPackNavigationItem is AllIconPacksNavigationItemViewModel 
                 || icon.IconPackType == (viewModel.SelectedIconPackNavigationItem as IconPackNavigationItemViewModel)?.IconPackType)
                // Filter for IconName
                && (string.IsNullOrWhiteSpace(viewModel.FilterText) || icon.Name.Contains(viewModel.FilterText.Trim(), StringComparison.OrdinalIgnoreCase));
        }
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
    
    public string AppVersion { get; } = "v 1.0.0.0";
}