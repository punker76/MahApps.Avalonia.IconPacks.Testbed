﻿using System;
#if NETFX_CORE || WINDOWS_UWP
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
#elif AVALONIA
using System.Reactive;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Styling;
#else
using System.ComponentModel;
using System.Linq;
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
        public PackIconControlBase()
        {
            Observable.CombineLatest(
                    this.GetObservable(SpinProperty).Select(_ => Unit.Default),
                    this.GetObservable(IsVisibleProperty).Select(_ => Unit.Default),
                    this.GetObservable(SpinDurationProperty).Select(_ => Unit.Default),
                    this.GetObservable(OpacityProperty).Select(_ => Unit.Default),
                    this.GetObservable(SpinEasingFunctionProperty).Select(_ => Unit.Default))
                .Select(_ => this.CalculateSpinning())
                .Subscribe(spin =>
                {
                    this.StopSpinAnimation();
                    if (spin)
                    {
                        this.BeginSpinAnimation();
                    }
                });
        }

        private bool CalculateSpinning()
        {
            return this.Spin && this.IsVisible && this.SpinDuration > 0 && this.Opacity > 0 && this.SpinEasingFunction != null;
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
#elif AVALONIA
        public static readonly StyledProperty<string> DataProperty
            = AvaloniaProperty.Register<PackIconControlBase, string>(nameof(Data));
#else
        private static readonly DependencyPropertyKey DataPropertyKey
            = DependencyProperty.RegisterReadOnly(nameof(Data), typeof(string), typeof(PackIconControlBase), new PropertyMetadata(""));

        // ReSharper disable once StaticMemberInGenericType
        public static readonly DependencyProperty DataProperty = DataPropertyKey.DependencyProperty;
#endif

        /// <summary>
        /// Gets the path data for the current icon kind.
        /// </summary>
#if !(NETFX_CORE || WINDOWS_UWP || AVALONIA)
        [TypeConverter(typeof(GeometryConverter))]
        public string Data
        {
            get { return (string)GetValue(DataProperty); }
            protected set { SetValue(DataPropertyKey, value); }
        }
#else
        public string Data
        {
            get { return (string)GetValue(DataProperty); }
            protected set { SetValue(DataProperty, value); }
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
        private Grid innerGrid;

        /// <inheritdoc />
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            this.innerGrid = e.NameScope.Find<Grid>("PART_InnerGrid");

            this.UpdateData();

            var spin = CalculateSpinning();
            if (spin)
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
            = AvaloniaProperty.Register<PackIconControlBase, bool>(nameof(Spin));
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

        /// <summary>
        /// Gets or sets a value indicating whether the inner icon is spinning.
        /// </summary>
        /// <value><c>true</c> if spin; otherwise, <c>false</c>.</value>
        public bool Spin
        {
            get { return (bool)this.GetValue(SpinProperty); }
            set { this.SetValue(SpinProperty, value); }
        }

#if AVALONIA
        private Animation spinAnimation = null;
        private IDisposable spinAnimationSubscription = null;

        private void BeginSpinAnimation()
        {
            if (this.innerGrid is null)
            {
                return;
            }

            var animation = spinAnimation ?? new Animation
            {
                Children =
                {
                    new KeyFrame()
                    {
                        Cue = new Cue(0),
                        Setters = {new Setter(RotateTransform.AngleProperty, 0d)}
                    },
                    new KeyFrame()
                    {
                        Cue = new Cue(1),
                        Setters = {new Setter(RotateTransform.AngleProperty, 360d)}
                    }
                }
            };

            animation.Duration = TimeSpan.FromSeconds(this.SpinDuration);
            animation.Easing = this.SpinEasingFunction;
            animation.IterationCount = IterationCount.Infinite;
            this.spinAnimation = animation;
            this.spinAnimationSubscription = animation.Apply(this.innerGrid, Avalonia.Animation.Clock.GlobalClock, Observable.Return(true), null);
        }

        private void StopSpinAnimation()
        {
            if (this.spinAnimation != null)
            {
                this.spinAnimation.IterationCount = new IterationCount(0);
                this.spinAnimationSubscription?.Dispose();
                this.spinAnimationSubscription = null;
            }
        }
#else
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

        private Storyboard spinningStoryboard;
        private FrameworkElement _innerGrid;
        private FrameworkElement InnerGrid => this._innerGrid ?? (this._innerGrid = this.GetTemplateChild("PART_InnerGrid") as FrameworkElement);

        private void BeginSpinAnimation()
        {
            var element = this.InnerGrid;
            if (null == element)
            {
                return;
            }
            var transformGroup = element.RenderTransform as TransformGroup ?? new TransformGroup();
            var rotateTransform = transformGroup.Children.OfType<RotateTransform>().LastOrDefault();

            if (rotateTransform != null)
            {
                rotateTransform.Angle = 0;
            }
            else
            {
                transformGroup.Children.Add(new RotateTransform());
                element.RenderTransform = transformGroup;
            }

            var animation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                AutoReverse = this.SpinAutoReverse,
                EasingFunction = this.SpinEasingFunction,
                RepeatBehavior = RepeatBehavior.Forever,
                Duration = new Duration(TimeSpan.FromSeconds(this.SpinDuration))
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, element);

#if NETFX_CORE || WINDOWS_UWP
            Storyboard.SetTargetProperty(animation, $"(RenderTransform).(TransformGroup.Children)[{transformGroup.Children.Count - 1}].(Angle)");
#else
            Storyboard.SetTargetProperty(animation, new PropertyPath($"(0).(1)[{transformGroup.Children.Count - 1}].(2)", RenderTransformProperty, TransformGroup.ChildrenProperty, RotateTransform.AngleProperty));
#endif

            spinningStoryboard = storyboard;
            storyboard.Begin();
        }

        private void StopSpinAnimation()
        {
            var storyboard = spinningStoryboard;
            storyboard?.Stop();
            spinningStoryboard = null;
        }
#endif

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
                new LinearEasing());
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