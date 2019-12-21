using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Styling;
#if NETFX_CORE || WINDOWS_UWP
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
#elif AVALONIA
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
#else
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
#endif

namespace MahApps.Metro.IconPacks
{
    /// <summary>
    /// Class PackIconControlBase which is the base class for any PackIcon control.
    /// </summary>
    public abstract class PackIconControlBase : PackIconBase
    {

#if NETFX_CORE || WINDOWS_UWP
        private long _opacityRegisterToken;
        private long _visibilityRegisterToken;

        public PackIconControlBase()
        {
            this.Loaded += (sender, args) =>
            {
                this._opacityRegisterToken = this.RegisterPropertyChangedCallback(OpacityProperty, this.CoerceSpinProperty);
                this._visibilityRegisterToken = this.RegisterPropertyChangedCallback(VisibilityProperty, this.CoerceSpinProperty);
            };
            this.Unloaded += (sender, args) =>
            {
                this.UnregisterPropertyChangedCallback(OpacityProperty, this._opacityRegisterToken);
                this.UnregisterPropertyChangedCallback(VisibilityProperty, this._visibilityRegisterToken);
            };
        }

        private void CoerceSpinProperty(DependencyObject sender, DependencyProperty dp)
        {
            var packIcon = sender as PackIconControlBase;
            if (packIcon != null && (dp == OpacityProperty || dp == VisibilityProperty))
            {
                var spin = this.Spin && packIcon.Visibility == Visibility.Visible && packIcon.SpinDuration > 0 && packIcon.Opacity > 0;
                packIcon.ToggleSpinAnimation(spin);
            }
        }
#elif AVALONIA
        static PackIconControlBase()
        {
            PseudoClass<PackIconControlBase>(SpinProperty, ":spin");
        }

        public PackIconControlBase()
        {
            OpacityProperty.Changed.Subscribe(CoerceSpinProperty);
            IsVisibleProperty.Changed.Subscribe(CoerceSpinProperty);
            SpinProperty.Changed.Subscribe(CoerceSpinProperty);
            SpinDurationProperty.Changed.Subscribe(e =>
            {
                if (e.Sender is PackIconControlBase packIcon && e.OldValue != e.NewValue && packIcon.Spin && e.NewValue is double)
                {
                    packIcon.StopSpinAnimation();
                    packIcon.BeginSpinAnimation();
                }
            });
            SpinEasingFunctionProperty.Changed.Subscribe(e =>
            {
                if (e.Sender is PackIconControlBase packIcon && e.OldValue != e.NewValue && packIcon.Spin)
                {
                    packIcon.StopSpinAnimation();
                    packIcon.BeginSpinAnimation();
                }
            });
            SpinAutoReverseProperty.Changed.Subscribe(e =>
            {
                if (e.Sender is PackIconControlBase packIcon && e.OldValue != e.NewValue && packIcon.Spin && e.NewValue is bool)
                {
                    packIcon.StopSpinAnimation();
                    packIcon.BeginSpinAnimation();
                }
            });
        }

        private void CoerceSpinProperty(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is PackIconControlBase packIcon)
            {
                var spin = this.Spin && packIcon.IsVisible && packIcon.SpinDuration > 0 && packIcon.Opacity > 0;
                packIcon.ToggleSpinAnimation(spin);
            }
        }
#else
        static PackIconControlBase()
        {
            OpacityProperty.OverrideMetadata(typeof(PackIconControlBase), new UIPropertyMetadata(1d, (d, e) => { d.CoerceValue(SpinProperty); }));
            VisibilityProperty.OverrideMetadata(typeof(PackIconControlBase), new UIPropertyMetadata(Visibility.Visible, (d, e) => { d.CoerceValue(SpinProperty); }));
        }
#endif

#if (NETFX_CORE || WINDOWS_UWP)
        protected static readonly DependencyProperty DataProperty
            = DependencyProperty.Register(nameof(Data), typeof(string), typeof(PackIconControlBase), new PropertyMetadata(""));

        /// <summary>
        /// Gets the path data for the current icon kind.
        /// </summary>
        public string Data
        {
            get { return (string)GetValue(DataProperty); }
            protected set { SetValue(DataProperty, value); }
        }
#elif AVALONIA
        public static readonly StyledProperty<string> DataProperty
            = AvaloniaProperty.Register<PackIconControlBase, string>(nameof(Data));

        /// <summary>
        /// Gets the path data for the current icon kind.
        /// </summary>
        public string Data
        {
            get { return (string)this.GetValue(DataProperty); }
            protected set { this.SetValue(DataProperty, value); }
        }
#else
        private static readonly DependencyPropertyKey DataPropertyKey
            = DependencyProperty.RegisterReadOnly(nameof(Data), typeof(string), typeof(PackIconControlBase), new PropertyMetadata(""));

        // ReSharper disable once StaticMemberInGenericType
        public static readonly DependencyProperty DataProperty = DataPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the path data for the current icon kind.
        /// </summary>
        [TypeConverter(typeof(GeometryConverter))]
        public string Data
        {
            get { return (string)GetValue(DataProperty); }
            protected set { SetValue(DataPropertyKey, value); }
        }
#endif

#if NETFX_CORE || WINDOWS_UWP
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.UpdateData();

            this.CoerceSpinProperty(this, SpinProperty);

            if (this.Spin)
            {
                this.StopSpinAnimation();
                this.BeginSpinAnimation();
            }
        }
#elif AVALONIA
        /// <inheritdoc />
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            this.UpdateData();

            var spin = this.Spin && this.IsVisible && this.SpinDuration > 0 && this.Opacity > 0;
            this.ToggleSpinAnimation(spin);

            if (this.Spin)
            {
                this.StopSpinAnimation();
                this.BeginSpinAnimation();
            }
        }
#else
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.UpdateData();

            this.CoerceValue(SpinProperty);

            if (this.Spin)
            {
                this.StopSpinAnimation();
                this.BeginSpinAnimation();
            }
        }
#endif

        /// <summary>
        /// Identifies the Flip dependency property.
        /// </summary>
#if AVALONIA
        public static readonly StyledProperty<PackIconFlipOrientation> FlipProperty
            = AvaloniaProperty.Register<PackIconControlBase, PackIconFlipOrientation>(nameof(Flip));
#else
        public static readonly DependencyProperty FlipProperty
            = DependencyProperty.Register(
                nameof(Flip),
                typeof(PackIconFlipOrientation),
                typeof(PackIconControlBase),
                new PropertyMetadata(PackIconFlipOrientation.Normal));
#endif

        /// <summary>
        /// Gets or sets the flip orientation.
        /// </summary>
        public PackIconFlipOrientation Flip
        {
            get { return (PackIconFlipOrientation)this.GetValue(FlipProperty); }
            set { this.SetValue(FlipProperty, value); }
        }

        /// <summary>
        /// Identifies the RotationAngle dependency property.
        /// </summary>
#if NETFX_CORE || WINDOWS_UWP
        public static readonly DependencyProperty RotationAngleProperty
            = DependencyProperty.Register(
                nameof(RotationAngle),
                typeof(double),
                typeof(PackIconControlBase),
                new PropertyMetadata(0d));
#elif AVALONIA
        public static readonly StyledProperty<double> RotationAngleProperty
            = AvaloniaProperty.Register<PackIconControlBase, double>(
                nameof(RotationAngle),
                0d,
                false,
                BindingMode.OneWay,
                (packIcon, value) =>
                {
                    var val = (double) value;
                    return val < 0 ? 0d : (val > 360 ? 360d : value);
                });
#else
        public static readonly DependencyProperty RotationAngleProperty
            = DependencyProperty.Register(
                nameof(RotationAngle),
                typeof(double),
                typeof(PackIconControlBase),
                new PropertyMetadata(0d, null, (dependencyObject, value) =>
                {
                    var val = (double)value;
                    return val < 0 ? 0d : (val > 360 ? 360d : value);
                }));
#endif

        /// <summary>
        /// Gets or sets the rotation (angle).
        /// </summary>
        /// <value>The rotation.</value>
        public double RotationAngle
        {
            get { return (double)this.GetValue(RotationAngleProperty); }
            set { this.SetValue(RotationAngleProperty, value); }
        }

        /// <summary>
        /// Identifies the Spin dependency property.
        /// </summary>
#if NETFX_CORE || WINDOWS_UWP
        public static readonly DependencyProperty SpinProperty
            = DependencyProperty.Register(
                nameof(Spin),
                typeof(bool),
                typeof(PackIconControlBase),
                new PropertyMetadata(default(bool), SpinPropertyChangedCallback));

        private static void SpinPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is PackIconControlBase packIcon && e.OldValue != e.NewValue && e.NewValue is bool)
            {
                packIcon.ToggleSpinAnimation((bool)e.NewValue);
            }
        }
#elif AVALONIA
        public static readonly StyledProperty<bool> SpinProperty
            = AvaloniaProperty.Register<PackIconControlBase, bool>(
                nameof(Spin),
                false,
                false,
                BindingMode.OneWay,
                (packIcon, value) =>
                {
                    if (!packIcon.IsVisible || packIcon.Opacity <= 0 || packIcon.SpinDuration <= 0.0)
                    {
                        return false;
                    }

                    return value;
                });
#else
        public static readonly DependencyProperty SpinProperty
            = DependencyProperty.Register(
                nameof(Spin),
                typeof(bool),
                typeof(PackIconControlBase),
                new PropertyMetadata(default(bool), SpinPropertyChangedCallback, SpinPropertyCoerceValueCallback));

        private static object SpinPropertyCoerceValueCallback(DependencyObject dependencyObject, object value)
        {
            if (dependencyObject is PackIconControlBase packIcon && (!packIcon.IsVisible || packIcon.Opacity <= 0 || packIcon.SpinDuration <= 0.0))
            {
                return false;
            }
            return value;
        }

        private static void SpinPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is PackIconControlBase packIcon && e.OldValue != e.NewValue && e.NewValue is bool)
            {
                packIcon.ToggleSpinAnimation((bool)e.NewValue);
            }
        }
#endif

        private void ToggleSpinAnimation(bool spin)
        {
            if (spin)
            {
                this.BeginSpinAnimation();
            }
            else
            {
                this.StopSpinAnimation();
            }
        }

        // private Storyboard spinningStoryboard;
        // private FrameworkElement _innerGrid;
        // private FrameworkElement InnerGrid => this._innerGrid ?? (this._innerGrid = this.GetTemplateChild("PART_InnerGrid") as FrameworkElement);

        private void BeginSpinAnimation()
        {
            // var spinAnimationStyle = new Style(x => x.OfType<PackIconControlBase>().Template().Name("PART_InnerGrid"));
            // var animation = new Animation();
            // animation.Duration = TimeSpan.FromSeconds(this.SpinDuration);
            // animation.IterationCount = IterationCount.Infinite;
            //
            // var keyFrameStart = new KeyFrame() {Cue = new Cue(0)};
            // keyFrameStart.Setters.Add(new Setter(RotateTransform.AngleProperty, 0d));
            // animation.Children.Add(keyFrameStart);
            //
            // var keyFrameEnd = new KeyFrame() { Cue = new Cue(1) };
            // keyFrameEnd.Setters.Add(new Setter(RotateTransform.AngleProperty, 360d));
            // animation.Children.Add(keyFrameEnd);
            //
            // spinAnimationStyle.Animations.Add(animation);
            //
            // this.Styles.Add(spinAnimationStyle);

            //             var element = this.InnerGrid;
//             if (null == element)
//             {
//                 return;
//             }
//             var transformGroup = element.RenderTransform as TransformGroup ?? new TransformGroup();
//             var rotateTransform = transformGroup.Children.OfType<RotateTransform>().LastOrDefault();
//
//             if (rotateTransform != null)
//             {
//                 rotateTransform.Angle = 0;
//             }
//             else
//             {
//                 transformGroup.Children.Add(new RotateTransform());
//                 element.RenderTransform = transformGroup;
//             }
//
//             var animation = new DoubleAnimation
//             {
//                 From = 0,
//                 To = 360,
//                 AutoReverse = this.SpinAutoReverse,
//                 EasingFunction = this.SpinEasingFunction,
//                 RepeatBehavior = RepeatBehavior.Forever,
//                 Duration = new Duration(TimeSpan.FromSeconds(this.SpinDuration))
//             };
//
//             var storyboard = new Storyboard();
//             storyboard.Children.Add(animation);
//             Storyboard.SetTarget(animation, element);
//
// #if NETFX_CORE || WINDOWS_UWP
//             Storyboard.SetTargetProperty(animation, $"(RenderTransform).(TransformGroup.Children)[{transformGroup.Children.Count - 1}].(Angle)");
// #else
//             Storyboard.SetTargetProperty(animation, new PropertyPath($"(0).(1)[{transformGroup.Children.Count - 1}].(2)", RenderTransformProperty, TransformGroup.ChildrenProperty, RotateTransform.AngleProperty));
// #endif
//
//             spinningStoryboard = storyboard;
//             storyboard.Begin();
        }

        private void StopSpinAnimation()
        {
            // var storyboard = spinningStoryboard;
            // storyboard?.Stop();
            // spinningStoryboard = null;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the inner icon is spinning.
        /// </summary>
        /// <value><c>true</c> if spin; otherwise, <c>false</c>.</value>
        public bool Spin
        {
            get { return (bool)this.GetValue(SpinProperty); }
            set { this.SetValue(SpinProperty, value); }
        }

        /// <summary>
        /// Identifies the SpinDuration dependency property.
        /// </summary>
#if NETFX_CORE || WINDOWS_UWP
        public static readonly DependencyProperty SpinDurationProperty
            = DependencyProperty.Register(
                nameof(SpinDuration),
                typeof(double),
                typeof(PackIconControlBase),
                new PropertyMetadata(1d, SpinDurationPropertyChangedCallback));
#elif AVALONIA
        public static readonly StyledProperty<double> SpinDurationProperty
            = AvaloniaProperty.Register<PackIconControlBase, double>(
                nameof(SpinDuration),
                1d,
                false,
                BindingMode.OneWay,
                (iconPack, value) =>
                {
                    var val = (double) value;
                    return val < 0 ? 0d : value;
                });
#else
        public static readonly DependencyProperty SpinDurationProperty
            = DependencyProperty.Register(
                nameof(SpinDuration),
                typeof(double),
                typeof(PackIconControlBase),
                new PropertyMetadata(1d, SpinDurationPropertyChangedCallback, (dependencyObject, value) =>
                {
                    var val = (double)value;
                    return val < 0 ? 0d : value;
                }));

        private static void SpinDurationPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is PackIconControlBase packIcon && e.OldValue != e.NewValue && packIcon.Spin && e.NewValue is double)
            {
                packIcon.StopSpinAnimation();
                packIcon.BeginSpinAnimation();
            }
        }
#endif

        /// <summary>
        /// Gets or sets the duration of the spinning animation (in seconds). This will also restart the spin animation.
        /// </summary>
        /// <value>The duration of the spin in seconds.</value>
        public double SpinDuration
        {
            get { return (double)this.GetValue(SpinDurationProperty); }
            set { this.SetValue(SpinDurationProperty, value); }
        }

        /// <summary>
        /// Identifies the SpinEasingFunction dependency property.
        /// </summary>
#if NETFX_CORE || WINDOWS_UWP
        public static readonly DependencyProperty SpinEasingFunctionProperty
            = DependencyProperty.Register(
                nameof(SpinEasingFunction),
                typeof(EasingFunctionBase),
                typeof(PackIconControlBase),
                new PropertyMetadata(null, SpinEasingFunctionPropertyChangedCallback));

        private static void SpinEasingFunctionPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is PackIconControlBase packIcon && e.OldValue != e.NewValue && packIcon.Spin)
            {
                packIcon.StopSpinAnimation();
                packIcon.BeginSpinAnimation();
            }
        }
#elif AVALONIA
        public static readonly StyledProperty<Easing> SpinEasingFunctionProperty
            = AvaloniaProperty.Register<PackIconControlBase, Easing>(
                nameof(SpinEasingFunction),
                defaultValue: new LinearEasing());
#else
        public static readonly DependencyProperty SpinEasingFunctionProperty
            = DependencyProperty.Register(
                nameof(SpinEasingFunction),
                typeof(IEasingFunction),
                typeof(PackIconControlBase),
                new PropertyMetadata(null, SpinEasingFunctionPropertyChangedCallback));

        private static void SpinEasingFunctionPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is PackIconControlBase packIcon && e.OldValue != e.NewValue && packIcon.Spin)
            {
                packIcon.StopSpinAnimation();
                packIcon.BeginSpinAnimation();
            }
        }
#endif

        /// <summary>
        /// Gets or sets the EasingFunction of the spinning animation. This will also restart the spin animation.
        /// </summary>
        /// <value>The spin easing function.</value>
#if NETFX_CORE || WINDOWS_UWP
        public EasingFunctionBase SpinEasingFunction
        {
            get { return (EasingFunctionBase)this.GetValue(SpinEasingFunctionProperty); }
            set { this.SetValue(SpinEasingFunctionProperty, value); }
        }
#elif AVALONIA
        public Easing SpinEasingFunction
        {
            get { return (Easing)this.GetValue(SpinEasingFunctionProperty); }
            set { this.SetValue(SpinEasingFunctionProperty, value); }
        }
#else
        public IEasingFunction SpinEasingFunction
        {
            get { return (IEasingFunction)this.GetValue(SpinEasingFunctionProperty); }
            set { this.SetValue(SpinEasingFunctionProperty, value); }
        }
#endif

#if AVALONIA
        public static readonly StyledProperty<bool> SpinAutoReverseProperty
            = AvaloniaProperty.Register<PackIconControlBase, bool>(nameof(SpinAutoReverse));
#else
        /// <summary>
        /// Identifies the SpinAutoReverse dependency property.
        /// </summary>
        public static readonly DependencyProperty SpinAutoReverseProperty
            = DependencyProperty.Register(
                nameof(SpinAutoReverse),
                typeof(bool),
                typeof(PackIconControlBase),
                new PropertyMetadata(default(bool), SpinAutoReversePropertyChangedCallback));

        private static void SpinAutoReversePropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is PackIconControlBase packIcon && e.OldValue != e.NewValue && packIcon.Spin && e.NewValue is bool)
            {
                packIcon.StopSpinAnimation();
                packIcon.BeginSpinAnimation();
            }
        }
#endif

        /// <summary>
        /// Gets or sets the AutoReverse of the spinning animation. This will also restart the spin animation.
        /// </summary>
        /// <value><c>true</c> if [spin automatic reverse]; otherwise, <c>false</c>.</value>
        public bool SpinAutoReverse
        {
            get { return (bool)this.GetValue(SpinAutoReverseProperty); }
            set { this.SetValue(SpinAutoReverseProperty, value); }
        }
    }
}