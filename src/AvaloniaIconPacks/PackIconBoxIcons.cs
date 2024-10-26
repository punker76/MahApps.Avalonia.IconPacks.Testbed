using System;
using Avalonia.Media;
#if (NETFX_CORE || WINDOWS_UWP)
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#elif AVALONIA
using Avalonia;
#else
using System.Windows;
#endif

namespace MahApps.Metro.IconPacks
{
    /// <summary>
    /// BoxIcons licensed under [SIL OFL 1.1](<see><cref>http://scripts.sil.org/OFL</cref></see>)
    /// Contributions, corrections and requests can be made on GitHub <see><cref>https://github.com/atisawd/boxicons</cref></see>.
    /// </summary>
    public class PackIconBoxIcons : PackIconControlBase
    {
#if AVALONIA
        public static readonly StyledProperty<PackIconBoxIconsKind> KindProperty
            = AvaloniaProperty.Register<PackIconBoxIcons, PackIconBoxIconsKind>(nameof(Kind));
#else
        public static readonly DependencyProperty KindProperty
            = DependencyProperty.Register(
                nameof(Kind),
                typeof(PackIconBoxIconsKind),
                typeof(PackIconBoxIcons),
                new PropertyMetadata(default(PackIconBoxIconsKind), KindPropertyChangedCallback));

        private static void KindPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue)
            {
                ((PackIconBoxIcons)dependencyObject).UpdateData();
            }
        }
#endif

        /// <summary>
        /// Gets or sets the icon to display.
        /// </summary>
        public PackIconBoxIconsKind Kind
        {
            get { return (PackIconBoxIconsKind) GetValue(KindProperty); }
            set { SetValue(KindProperty, value); }
        }

#if NETFX_CORE || WINDOWS_UWP
        public PackIconBoxIcons()
        {
            this.DefaultStyleKey = typeof(PackIconBoxIcons);
        }
#elif AVALONIA
        // We override OnPropertyChanged of the base class. That way we can react on property changes
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            // if the changed property is the NumberOfStarsProperty, we need to update the stars
            if (change.Property == KindProperty)
            {
                UpdateData();
            }
        }
#else
        static PackIconBoxIcons()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PackIconBoxIcons), new FrameworkPropertyMetadata(typeof(PackIconBoxIcons)));
        }
#endif

        protected internal override void SetKind<TKind>(TKind iconKind)
        {
#if NETFX_CORE || WINDOWS_UWP
            BindingOperations.SetBinding(this, PackIconBoxIcons.KindProperty, new Binding() { Source = iconKind, Mode = BindingMode.OneTime });
#elif AVALONIA
            this.SetValue(KindProperty, iconKind);
#else
            this.SetCurrentValue(KindProperty, iconKind);
#endif
        }

        protected override void UpdateData()
        {
            if (Kind != default(PackIconBoxIconsKind))
            {
                string data = null;
                PackIconBoxIconsDataFactory.DataIndex.Value?.TryGetValue(Kind, out data);
                this.Data = data != null ? StreamGeometry.Parse(data) : null;
            }
            else
            {
                this.Data = null;
            }
        }
    }
}