using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using MahApps.IconPacksBrowser.Avalonia.Helper;

namespace MahApps.IconPacksBrowser.Avalonia.Controls;

public class PropertyResizer : TemplatedControl
{
    private Point? _lastPointerPosition;
    
    /// <summary>
    /// Defines the <see cref="PropertyToAdjustHorizontal" /> property
    /// </summary>
    public static readonly StyledProperty<double?> PropertyToAdjustHorizontalProperty =
        AvaloniaProperty.Register<PropertyResizer, double?>(
            nameof(PropertyToAdjustHorizontal), 
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the property to adjust horizontally
    /// </summary>
    public double? PropertyToAdjustHorizontal 
    {
        get { return GetValue(PropertyToAdjustHorizontalProperty); }
        set { SetValue(PropertyToAdjustHorizontalProperty, value); }
    }
    
    /// <summary>
    /// Defines the <see cref="PropertyToAdjustVertical" /> property
    /// </summary>
    public static readonly StyledProperty<double?> PropertyToAdjustVerticalProperty =
        AvaloniaProperty.Register<PropertyResizer, double?>(
            nameof(PropertyToAdjustVertical), 
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the property to adjust vertically
    /// </summary>
    public double? PropertyToAdjustVertical
    {
        get { return GetValue(PropertyToAdjustVerticalProperty); }
        set { SetValue(PropertyToAdjustVerticalProperty, value); }
    }

    /// <summary>
    /// Defines the <see cref="MinimumHorizontal" /> property
    /// </summary>
    public static readonly StyledProperty<double?> MinimumHorizontalProperty =
        AvaloniaProperty.Register<PropertyResizer, double?>(nameof(MinimumHorizontal));

    /// <summary>
    /// Gets or sets the Minimum to respect. Set this to <see langword="null"/> to allow any value
    /// </summary>
    public double? MinimumHorizontal
    {
        get { return GetValue(MinimumHorizontalProperty); }
        set { SetValue(MinimumHorizontalProperty, value); }
    }

    /// <summary>
    /// Defines the <see cref="MaximumHorizontal" /> property
    /// </summary>
    public static readonly StyledProperty<double?> MaximumHorizontalProperty =
        AvaloniaProperty.Register<PropertyResizer, double?>(nameof(MaximumHorizontal));

    /// <summary>
    /// Gets or sets the Maximum to respect. Set this to <see langword="null"/> to allow any value
    /// </summary>
    public double? MaximumHorizontal
    {
        get { return GetValue(MaximumHorizontalProperty); }
        set { SetValue(MaximumHorizontalProperty, value); }
    }
    
    
    /// <summary>
    /// Defines the <see cref="MinimumVertical" /> property
    /// </summary>
    public static readonly StyledProperty<double?> MinimumVerticalProperty =
        AvaloniaProperty.Register<PropertyResizer, double?>(nameof(MinimumVertical));

    /// <summary>
    /// Gets or sets the Minimum to respect. Set this to <see langword="null"/> to allow any value
    /// </summary>
    public double? MinimumVertical
    {
        get { return GetValue(MinimumVerticalProperty); }
        set { SetValue(MinimumVerticalProperty, value); }
    }

    /// <summary>
    /// Defines the <see cref="MaximumVertical" /> property
    /// </summary>
    public static readonly StyledProperty<double?> MaximumVerticalProperty =
        AvaloniaProperty.Register<PropertyResizer, double?>(nameof(MaximumVertical));

    /// <summary>
    /// Gets or sets the Maximum to respect. Set this to <see langword="null"/> to allow any value
    /// </summary>
    public double? MaximumVertical
    {
        get { return GetValue(MaximumVerticalProperty); }
        set { SetValue(MaximumVerticalProperty, value); }
    }
    
    /// <summary>
    /// Defines the <see cref="HorizontalValueWhenNullOrNan" /> property
    /// </summary>
    public static readonly StyledProperty<double?> HorizontalValueWhenNullOrNanProperty =
        AvaloniaProperty.Register<PropertyResizer, double?>(nameof(HorizontalValueWhenNullOrNan));

    /// <summary>
    /// Gets or sets the horizontal value to use if the value is <c>null</c> or <see cref="double.NaN"/>. 
    /// </summary>
    public double? HorizontalValueWhenNullOrNan
    {
        get { return GetValue(HorizontalValueWhenNullOrNanProperty); }
        set { SetValue(HorizontalValueWhenNullOrNanProperty, value); }
    }
    
    /// <summary>
    /// Defines the <see cref="VerticalValueWhenNullOrNan" /> property
    /// </summary>
    public static readonly StyledProperty<double?> VerticalValueWhenNullOrNanProperty =
        AvaloniaProperty.Register<PropertyResizer, double?>(nameof(VerticalValueWhenNullOrNan));

    /// <summary>
    /// Gets or sets the vertical value to use if the value is <c>null</c> or <see cref="double.NaN"/>. 
    /// </summary>
    public double? VerticalValueWhenNullOrNan
    {
        get { return GetValue(VerticalValueWhenNullOrNanProperty); }
        set { SetValue(VerticalValueWhenNullOrNanProperty, value); }
    }
    
    /// <summary>
    /// Defines the <see cref="DragIncrementHorizontally" /> property
    /// </summary>
    public static readonly StyledProperty<double> DragIncrementHorizontallyProperty =
        AvaloniaProperty.Register<PropertyResizer, double>(nameof(DragIncrementHorizontally), 1);

    /// <summary>
    /// Gets or sets the horizontal increment while dragging with Pointer
    /// </summary>
    public double DragIncrementHorizontally
    {
        get { return GetValue(DragIncrementHorizontallyProperty); }
        set { SetValue(DragIncrementHorizontallyProperty, value); }
    }

    /// <summary>
    /// Defines the <see cref="DragIncrementVertically" /> property
    /// </summary>
    public static readonly StyledProperty<double> DragIncrementVerticallyProperty =
        AvaloniaProperty.Register<PropertyResizer, double>(nameof(DragIncrementVertically), 1);

    /// <summary>
    /// Gets or sets the vertical increment while dragging with Pointer
    /// </summary>
    public double DragIncrementVertically
    {
        get { return GetValue(DragIncrementVerticallyProperty); }
        set { SetValue(DragIncrementVerticallyProperty, value); }
    }
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        e.Pointer.Capture(this);
        _lastPointerPosition = e.GetPosition(TopLevel.GetTopLevel(this));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (Equals(e.Pointer.Captured, this))
        {
            e.Pointer.Capture(null);
        }

        _lastPointerPosition = null;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (Equals(e.Pointer.Captured, this))
            {
                e.Pointer.Capture(null);
            }
            _lastPointerPosition = null;
            return;
        }
        
        if (_lastPointerPosition is null) return;
        
        var change = e.GetPosition(TopLevel.GetTopLevel(this)) - _lastPointerPosition.Value;

        _lastPointerPosition = e.GetPosition(TopLevel.GetTopLevel(this));

        UpdateValues(change.X * DragIncrementHorizontally, change.Y * DragIncrementVertically);
    }

    private void UpdateValues(double x, double y)
    {
        double newVal;
        
        if ((PropertyToAdjustHorizontal is not null || HorizontalValueWhenNullOrNan is not null)  && !x.IsCloseToZero())
        {
            if (PropertyToAdjustHorizontal is null or double.NaN)
            {
                PropertyToAdjustHorizontal = HorizontalValueWhenNullOrNan;
            }
            
            newVal = PropertyToAdjustHorizontal!.Value + x;
            
            if (newVal < (MinimumHorizontal ?? double.MinValue))
            {
                newVal = MinimumHorizontal ?? double.MinValue;
            }

            if (newVal > (MaximumHorizontal ?? double.MaxValue))
            {
                newVal = MaximumHorizontal ?? double.MaxValue;
            }
            
            SetCurrentValue(PropertyToAdjustHorizontalProperty, newVal);
        }

        if ((PropertyToAdjustVertical is not null || VerticalValueWhenNullOrNan is not null) && !y.IsCloseToZero())
        {
            if (PropertyToAdjustVertical is null or double.NaN)
            {
                PropertyToAdjustVertical = VerticalValueWhenNullOrNan;
            }
            
            newVal = PropertyToAdjustVertical!.Value + y;
            
            if (newVal < (MinimumVertical ?? double.MinValue))
            {
                newVal = MinimumVertical ?? double.MinValue;
            }

            if (newVal > (MaximumVertical ?? double.MaxValue))
            {
                newVal = MaximumVertical ?? double.MaxValue;
            }
            
            SetCurrentValue(PropertyToAdjustVerticalProperty, newVal);
        }
    }
}