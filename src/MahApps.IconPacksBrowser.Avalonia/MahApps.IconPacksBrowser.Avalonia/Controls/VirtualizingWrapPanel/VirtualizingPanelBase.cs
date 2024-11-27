using System;
using Avalonia;
using Avalonia.Controls;

namespace MahApps.IconPacksBrowser.Avalonia.Controls
{
    /// <summary>
    /// Base class for panels which are supporting virtualization.
    /// </summary>
    public abstract class VirtualizingPanelBase : VirtualizingPanel // TODO: , ILogicalScrollable
    {
        const double TOLERANCE = 0.001;
        
        public static readonly StyledProperty<double> ScrollLineDeltaProperty =
            AvaloniaProperty.Register<VirtualizingPanelBase, double>(nameof(ScrollLineDelta), 16.0);

        public static readonly StyledProperty<double> MouseWheelDeltaProperty =
            AvaloniaProperty.Register<VirtualizingPanelBase, double>(nameof(MouseWheelDelta), 48.0);

        public static readonly StyledProperty<int> ScrollLineDeltaItemProperty =
            AvaloniaProperty.Register<VirtualizingPanelBase, int>(nameof(ScrollLineDeltaItem), 1);

        public static readonly StyledProperty<int> MouseWheelDeltaItemProperty =
            AvaloniaProperty.Register<VirtualizingPanelBase, int>(nameof(MouseWheelDeltaItem), 3);

        public ScrollViewer? ScrollOwner { get; set; }

        public bool CanVerticallyScroll { get; set; }
        public bool CanHorizontallyScroll { get; set; }

        /// <summary>
        /// Scroll line delta for pixel based scrolling. The default value is 16 dp.
        /// </summary>
        public double ScrollLineDelta
        {
            get => GetValue(ScrollLineDeltaProperty);
            set => SetValue(ScrollLineDeltaProperty, value);
        }

        /// <summary>
        /// Mouse wheel delta for pixel based scrolling. The default value is 48 dp.
        /// </summary>        
        public double MouseWheelDelta
        {
            get => GetValue(MouseWheelDeltaProperty);
            set => SetValue(MouseWheelDeltaProperty, value);
        }

        /// <summary>
        /// Scroll line delta for item based scrolling. The default value is 1 item.
        /// </summary>
        public int ScrollLineDeltaItem
        {
            get => GetValue(ScrollLineDeltaItemProperty);
            set => SetValue(ScrollLineDeltaItemProperty, value);
        }

        /// <summary>
        /// Mouse wheel delta for item based scrolling. The default value is 3 items.
        /// </summary> 
        public int MouseWheelDeltaItem
        {
            get => GetValue(MouseWheelDeltaItemProperty);
            set => SetValue(MouseWheelDeltaItemProperty, value);
        }

        /// <summary>
        /// The direction in which the panel scrolls when user turns the mouse wheel.
        /// </summary>
        protected ScrollDirection MouseWheelScrollDirection { get; set; } = ScrollDirection.Vertical;
        
        public double ExtentWidth => Extent.Width;
        public double ExtentHeight => Extent.Height;

        public double HorizontalOffset => ScrollOffset.X;
        public double VerticalOffset => ScrollOffset.Y;

        public double ViewportWidth => ViewportSize.Width;
        public double ViewportHeight => ViewportSize.Height;

        public Size Extent { get; set; } = new Size(0, 0);
        
        protected Rect Viewport { get; set; } = new Rect(0, 0, 0, 0);
        protected Size ViewportSize => Viewport.Size;
        protected Point ScrollOffset => Viewport.TopLeft;
        
        public ScrollUnit ScrollUnit { get; set; }

        private bool previousVerticalScrollBarVisibilitye;
        private bool previousHorizontalScrollBarVisibilityd;
        
        
        //TODO: Seems like we don't need that 
        // public bool BringIntoView(Control visual, Rect rectangle)
        // {
        //     var transformedBounds = Visual.TransformToAncestor(this).TransformBounds(rectangle);
        //
        //     double offsetX = 0;
        //     double offsetY = 0;
        //
        //     double visibleX = 0;
        //     double visibleY = 0;
        //     double visibleWidth = Math.Min(rectangle.Width, ViewportWidth);
        //     double visibleHeight = Math.Min(rectangle.Height, ViewportHeight);
        //
        //     if (transformedBounds.Left < 0)
        //     {
        //         offsetX = transformedBounds.Left;
        //     }
        //     else if (transformedBounds.Right > ViewportWidth)
        //     {
        //         offsetX = Math.Min(transformedBounds.Right - ViewportWidth, transformedBounds.Left);
        //
        //         if (rectangle.Width > ViewportWidth)
        //         {
        //             visibleX = rectangle.Width - ViewportWidth;
        //         }
        //     }
        //
        //     if (transformedBounds.Top < 0)
        //     {
        //         offsetY = transformedBounds.Top;
        //     }
        //     else if (transformedBounds.Bottom > ViewportHeight)
        //     {
        //         offsetY = Math.Min(transformedBounds.Bottom - ViewportHeight, transformedBounds.Top);
        //
        //         if (rectangle.Height > ViewportHeight)
        //         {
        //             visibleY = rectangle.Height - ViewportHeight;
        //         }
        //     }
        //
        //     SetHorizontalOffset(HorizontalOffset + offsetX);
        //     SetVerticalOffset(VerticalOffset + offsetY);
        //
        //     return new Rect(visibleX, visibleY, visibleWidth, visibleHeight);
        // }

        // public void SetVerticalOffset(double offset)
        // {
        //     if (offset < 0 || ViewportSize.Height >= Extent.Height)
        //     {
        //         offset = 0;
        //     }
        //     else if (offset + ViewportSize.Height >= Extent.Height)
        //     {
        //         offset = Extent.Height - ViewportSize.Height;
        //     }
        //     
        //     if (Math.Abs(offset - ScrollOffset.Y) > TOLERANCE)
        //     {
        //         ScrollOffset = new Point(ScrollOffset.X, offset);
        //         ScrollOwner?.InvalidateArrange();
        //         InvalidateMeasure();
        //     }
        // }
        //
        // public void SetHorizontalOffset(double offset)
        // {
        //     if (offset < 0 || ViewportSize.Width >= Extent.Width)
        //     {
        //         offset = 0;
        //     }
        //     else if (offset + ViewportSize.Width >= Extent.Width)
        //     {
        //         offset = Extent.Width - ViewportSize.Width;
        //     }
        //
        //     if (offset != ScrollOffset.X)
        //     {
        //         ScrollOffset = new Point(offset, ScrollOffset.Y);
        //         // TODO: ScrollOwner?.InvalidateScrollInfo();
        //         InvalidateMeasure();
        //     }
        // }

        // TODO 
        // public virtual void LineUp()
        // {
        //     ScrollVertical(ScrollUnit == ScrollUnit.Pixel ? -ScrollLineDelta : GetLineUpScrollAmount());
        // }
        //
        // public virtual void LineDown()
        // {
        //     ScrollVertical(ScrollUnit == ScrollUnit.Pixel ? ScrollLineDelta : GetLineDownScrollAmount());
        // }
        //
        // public virtual void LineLeft()
        // {
        //     ScrollHorizontal(ScrollUnit == ScrollUnit.Pixel ? -ScrollLineDelta : GetLineLeftScrollAmount());
        // }
        //
        // public virtual void LineRight()
        // {
        //     ScrollHorizontal(ScrollUnit == ScrollUnit.Pixel ? ScrollLineDelta : GetLineRightScrollAmount());
        // }
        //
        // public virtual void MouseWheelUp()
        // {
        //     if (MouseWheelScrollDirection == ScrollDirection.Vertical)
        //     {
        //         ScrollVertical(ScrollUnit == ScrollUnit.Pixel ? -MouseWheelDelta : GetMouseWheelUpScrollAmount());
        //     }
        //     else
        //     {
        //         MouseWheelLeft();
        //     }
        // }
        //
        // public virtual void MouseWheelDown()
        // {
        //     if (MouseWheelScrollDirection == ScrollDirection.Vertical)
        //     {
        //         ScrollVertical(ScrollUnit == ScrollUnit.Pixel ? MouseWheelDelta : GetMouseWheelDownScrollAmount());
        //     }
        //     else
        //     {
        //         MouseWheelRight();
        //     }
        // }

        // public virtual void MouseWheelLeft()
        // {
        //     ScrollHorizontal(ScrollUnit == ScrollUnit.Pixel ? -MouseWheelDelta : GetMouseWheelLeftScrollAmount());
        // }
        //
        // public virtual void MouseWheelRight()
        // {
        //     ScrollHorizontal(ScrollUnit == ScrollUnit.Pixel ? MouseWheelDelta : GetMouseWheelRightScrollAmount());
        // }
        //
        // public virtual void PageUp()
        // {
        //     ScrollVertical(ScrollUnit == ScrollUnit.Pixel ? -ViewportSize.Height : GetPageUpScrollAmount());
        // }
        //
        // public virtual void PageDown()
        // {
        //     ScrollVertical(ScrollUnit == ScrollUnit.Pixel ? ViewportSize.Height : GetPageDownScrollAmount());
        // }
        //
        // public virtual void PageLeft()
        // {
        //     ScrollHorizontal(ScrollUnit == ScrollUnit.Pixel ? -ViewportSize.Width : GetPageLeftScrollAmount());
        // }
        //
        // public virtual void PageRight()
        // {
        //     ScrollHorizontal(ScrollUnit == ScrollUnit.Pixel ? ViewportSize.Width : GetPageRightScrollAmount());
        // }

        protected abstract double GetLineUpScrollAmount();
        protected abstract double GetLineDownScrollAmount();
        protected abstract double GetLineLeftScrollAmount();
        protected abstract double GetLineRightScrollAmount();

        protected abstract double GetMouseWheelUpScrollAmount();
        protected abstract double GetMouseWheelDownScrollAmount();
        protected abstract double GetMouseWheelLeftScrollAmount();
        protected abstract double GetMouseWheelRightScrollAmount();

        protected abstract double GetPageUpScrollAmount();
        protected abstract double GetPageDownScrollAmount();
        protected abstract double GetPageLeftScrollAmount();
        protected abstract double GetPageRightScrollAmount();

    //     private void ScrollVertical(double amount)
    //     {
    //         SetVerticalOffset(ScrollOffset.Y + amount);
    //     }
    //
    //     private void ScrollHorizontal(double amount)
    //     {
    //         SetHorizontalOffset(ScrollOffset.X + amount);
    //     }
    }

    public enum ScrollUnit
    {
        Pixel,
        Line
    }
}