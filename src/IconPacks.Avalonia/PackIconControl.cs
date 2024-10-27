﻿using System;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Media;
using IconPacks.Avalonia;

namespace IconPacks.Avalonia
{
    /// <summary>
    /// </summary>
    [PseudoClasses(":PackIconBoxIcons")]
    public class PackIconControl : PackIconControlBase
    {
        public static readonly StyledProperty<Enum> KindProperty
            = AvaloniaProperty.Register<PackIconControl, Enum>(nameof(Kind));

        /// <summary>
        /// Gets or sets the icon to display.
        /// </summary>
        public Enum Kind
        {
            get { return GetValue(KindProperty); }
            set { SetValue(KindProperty, value); }
        }

        // We override OnPropertyChanged of the base class. That way we can react on property changes
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            // if the changed property is the KindProperty, we need to update the stars
            if (change.Property == KindProperty)
            {
                UpdateData();
            }
        }

        protected override void SetKind<TKind>(TKind iconKind)
        {
            this.SetCurrentValue(KindProperty, iconKind);
        }

        protected override void UpdateData()
        {
            this.PseudoClasses.Set(":PackIconBoxIcons", Kind is PackIconBoxIconsKind);

            if (Kind != default(Enum))
            {
                string data = null;
                PackIconControlDataFactory.DataIndex.Value?.TryGetValue(Kind, out data);
                this.Data = data != null ? StreamGeometry.Parse(data) : null;
            }
            else
            {
                this.Data = null;
            }
        }
    }
}