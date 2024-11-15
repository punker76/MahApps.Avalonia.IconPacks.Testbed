using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using MahApps.IconPacksBrowser.Avalonia.Controls.Utils;

namespace MahApps.IconPacksBrowser.Avalonia.Controls
{
    /// <summary>
    /// A implementation of a wrap panel that supports virtualization and can be used in horizontal and vertical orientation.
    /// </summary>
    public class VirtualizingWrapPanel : VirtualizingPanel
    {
        private static readonly Size EmptySize = new Size(0, 0);

        static VirtualizingWrapPanel()
        {
            AffectsMeasure<VirtualizingWrapPanel>(
                OrientationProperty,
                ItemSizeProperty,
                AllowDifferentSizedItemsProperty,
                ItemSizeProviderProperty);

            AffectsArrange<VirtualizingWrapPanel>(
                SpacingModeProperty,
                StretchItemsProperty,
                IsGridLayoutEnabledProperty);
        }

        public static readonly StyledProperty<Orientation> OrientationProperty =
            WrapPanel.OrientationProperty.AddOwner<VirtualizingWrapPanel>(
                new StyledPropertyMetadata<Orientation>(Orientation.Horizontal));
        //TODO (obj, args) => ((VirtualizingWrapPanel)obj).Orientation_Changed()));

        public static readonly StyledProperty<Size> ItemSizeProperty =
            AvaloniaProperty.Register<VirtualizingWrapPanel, Size>(nameof(ItemSize), EmptySize);
        // TODO (obj, args) => ((VirtualizingWrapPanel)obj).ItemSize_Changed()));

        public static readonly StyledProperty<bool> AllowDifferentSizedItemsProperty =
            AvaloniaProperty.Register<VirtualizingWrapPanel, bool>(nameof(AllowDifferentSizedItems), false);

        // TODO (obj, args) => ((VirtualizingWrapPanel)obj).AllowDifferentSizedItems_Changed()));

        public static readonly StyledProperty<IItemSizeProvider?> ItemSizeProviderProperty =
            AvaloniaProperty.Register<VirtualizingWrapPanel, IItemSizeProvider?>(nameof(ItemSizeProvider), null);

        public static readonly StyledProperty<SpacingMode> SpacingModeProperty =
            AvaloniaProperty.Register<VirtualizingWrapPanel, SpacingMode>(nameof(SpacingMode), SpacingMode.Uniform);

        public static readonly StyledProperty<bool> StretchItemsProperty =
            AvaloniaProperty.Register<VirtualizingWrapPanel, bool>(nameof(StretchItems), false);

        public static readonly StyledProperty<bool> IsGridLayoutEnabledProperty =
            AvaloniaProperty.Register<VirtualizingWrapPanel, bool>(nameof(IsGridLayoutEnabled), true);

        private static readonly AttachedProperty<object?> RecycleKeyProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingStackPanel, Control, object?>("RecycleKey");

        private static readonly object s_itemIsItsOwnContainer = new object();
        private readonly Action<Control, int> _recycleElement;
        private readonly Action<Control> _recycleElementOnItemRemoved;
        private readonly Action<Control, int, int> _updateElementIndex;
        private int _scrollToIndex = -1;
        private Control? _scrollToElement;
        private bool _isInLayout;
        private bool _isWaitingForViewportUpdate;
        private double _lastEstimatedElementSizeU = 25;
        private RealizedWrapElements? _measureElements;
        private RealizedWrapElements? _realizedElements;
        private IScrollAnchorProvider? _scrollAnchorProvider;
        private Rect _viewport;
        private Dictionary<object, Stack<Control>>? _recyclePool;
        private Control? _focusedElement;
        private int _focusedIndex = -1;
        private Control? _realizingElement;
        private int _realizingIndex = -1;

        public VirtualizingWrapPanel()
        {
            _recycleElement = RecycleElement;
            _recycleElementOnItemRemoved = RecycleElementOnItemRemoved;
            _updateElementIndex = UpdateElementIndex;
            EffectiveViewportChanged += OnEffectiveViewportChanged;
        }

        /// <summary>
        /// Gets or sets a value that specifies the orientation in which items are arranged before wrapping. The default value is <see cref="Orientation.Horizontal"/>.
        /// </summary>
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that specifies the size of the items. The default value is <see cref="Size.Empty"/>. 
        /// If the value is <see cref="Size.Empty"/> the item size is determined by measuring the first realized item.
        /// </summary>
        public Size ItemSize
        {
            get => (Size)GetValue(ItemSizeProperty);
            set => SetValue(ItemSizeProperty, value);
        }

        /// <summary>
        /// Specifies whether items can have different sizes. The default value is false. If this property is enabled, 
        /// it is strongly recommended to also set the <see cref="ItemSizeProvider"/> property. Otherwise, the position 
        /// of the items is not always guaranteed to be correct.
        /// </summary>
        public bool AllowDifferentSizedItems
        {
            get => (bool)GetValue(AllowDifferentSizedItemsProperty);
            set => SetValue(AllowDifferentSizedItemsProperty, value);
        }

        /// <summary>
        /// Specifies an instance of <see cref="IItemSizeProvider"/> which provides the size of the items. In order to allow
        /// different sized items, also enable the <see cref="AllowDifferentSizedItems"/> property.
        /// </summary>
        public IItemSizeProvider? ItemSizeProvider
        {
            get => GetValue(ItemSizeProviderProperty);
            set => SetValue(ItemSizeProviderProperty, value);
        }

        /// <summary>
        /// Gets or sets the spacing mode used when arranging the items. The default value is <see cref="SpacingMode.Uniform"/>.
        /// </summary>
        public SpacingMode SpacingMode
        {
            get => GetValue(SpacingModeProperty);
            set => SetValue(SpacingModeProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that specifies if the items get stretched to fill up remaining space. The default value is false.
        /// </summary>
        /// <remarks>
        /// The MaxWidth and MaxHeight properties of the ItemContainerStyle can be used to limit the stretching. 
        /// In this case the use of the remaining space will be determined by the SpacingMode property. 
        /// </remarks>
        public bool StretchItems
        {
            get => GetValue(StretchItemsProperty);
            set => SetValue(StretchItemsProperty, value);
        }

        /// <summary>
        /// Specifies whether the items are arranged in a grid-like layout. The default value is <c>true</c>.
        /// When set to <c>true</c>, the items are arranged based on the number of items that can fit in a row. 
        /// When set to <c>false</c>, the items are arranged based on the number of items that are actually placed in the row. 
        /// </summary>
        /// <remarks>
        /// If <see cref="AllowDifferentSizedItems"/> is enabled, this property has no effect and the items are always 
        /// arranged based on the number of items that are actually placed in the row.
        /// </remarks>
        public bool IsGridLayoutEnabled
        {
            get => GetValue(IsGridLayoutEnabledProperty);
            set => SetValue(IsGridLayoutEnabledProperty, value);
        }

        /// <summary>
        /// Gets the index of the first realized element, or -1 if no elements are realized.
        /// </summary>
        public int FirstRealizedIndex => _realizedElements?.FirstIndex ?? -1;

        /// <summary>
        /// Gets the index of the last realized element, or -1 if no elements are realized.
        /// </summary>
        public int LastRealizedIndex => _realizedElements?.LastIndex ?? -1;

        private static readonly Size FallbackItemSize = new Size(48, 48);


        /// <summary>
        /// The cache length before and after the viewport. 
        /// </summary>
        private VirtualizationCacheLength cacheLength;

        /// <summary>
        /// The Unit of the cache length. Can be Pixel, Item or Page. 
        /// When the ItemsOwner is a group item it can only be pixel or item.
        /// </summary>
        private VirtualizationCacheLengthUnit cacheLengthUnit;

        private Size? sizeOfFirstItem;

        private Size? averageItemSizeCache;

        private int startItemIndex = -1;
        private int endItemIndex = -1;

        private double startItemOffsetX = 0;
        private double startItemOffsetY = 0;

        private double knownExtendX = 0;


        protected override Size MeasureOverride(Size availableSize)
        { 
            var items = Items;

            if (items.Count == 0)
                return default;

            var orientation = Orientation;

            // If we're bringing an item into view, ignore any layout passes until we receive a new
            // effective viewport.
            if (_isWaitingForViewportUpdate)
                return EstimateDesiredSize(orientation, items.Count);

            _isInLayout = true;

            try
            {
                // _realizedElements?.ValidateStartU(Orientation);
                _realizedElements ??= new();
                _measureElements ??= new();

                // We handle horizontal and vertical layouts here so X and Y are abstracted to:
                // - Horizontal layouts: U = horizontal, V = vertical
                // - Vertical layouts: U = vertical, V = horizontal
                // var viewport = CalculateMeasureViewport(items);

                // If the viewport is disjunct then we can recycle everything.
                // if (viewport.viewportIsDisjunct)
                //     _realizedElements.RecycleAllElements(_recycleElement);

                // Do the measure, creating/recycling elements as necessary to fill the viewport. Don't
                // write to _realizedElements yet, only _measureElements.
                RealizeAndVirtualizeItems(items, availableSize);

                // Now swap the measureElements and realizedElements collection.
                (_measureElements, _realizedElements) = (_realizedElements, _measureElements);
                _measureElements.ResetForReuse();

                // If there is a focused element is outside the visible viewport (i.e.
                // _focusedElement is non-null), ensure it's measured.
                _focusedElement?.Measure(availableSize);

                return CalculateDesiredSize(orientation, items.Count);
            }
            finally
            {
                _isInLayout = false;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_realizedElements is null)
                return default;

            _isInLayout = true;

            try
            {
                if (startItemIndex == -1)
                {
                    return finalSize;
                }

                if (_realizedElements.Count < endItemIndex - startItemIndex + 1)
                {
                    throw new InvalidOperationException("Items must be distinct");
                }

                // var viewport = CalculateMeasureViewport(Items);

                double x = startItemOffsetX; // + GetX(_viewport.TopLeft);
                double y = startItemOffsetY; // - GetY(_viewport.TopLeft);
                double rowHeight = 0;
                var rowChilds = new List<Control>();
                var childSizes = new List<Size>();

                for (int i = startItemIndex; i <= endItemIndex; i++)
                {
                    var item = Items[i];
                    var child = _realizedElements.GetElement(i);

                    Size? upfrontKnownItemSize = GetUpfrontKnownItemSize(item);

                    Size childSize = upfrontKnownItemSize ?? _realizedElements.GetElementSize(i) ?? FallbackItemSize;

                    if (rowChilds.Count > 0 && x + GetWidth(childSize) > GetWidth(finalSize))
                    {
                        ArrangeRow(GetWidth(finalSize), rowChilds, childSizes, y);
                        x = 0;
                        y += rowHeight;
                        rowHeight = 0;
                        rowChilds.Clear();
                        childSizes.Clear();
                    }

                    x += GetWidth(childSize);
                    rowHeight = Math.Max(rowHeight, GetHeight(childSize));
                    rowChilds.Add(child);
                    childSizes.Add(childSize);
                    
                    _scrollAnchorProvider?.RegisterAnchorCandidate(child);
                }

                if (rowChilds.Any())
                {
                    ArrangeRow(GetWidth(finalSize), rowChilds, childSizes, y);
                }

                // Ensure that the focused element is in the correct position.                
                if (_focusedElement is not null && _focusedIndex >= 0)
                {
                    var startPoint = FindItemOffset(_focusedIndex);

                    startItemOffsetX = GetX(startPoint);
                    startItemOffsetY = GetY(startPoint);

                    var rect = Orientation == Orientation.Horizontal
                        ? new Rect(startItemOffsetX, startItemOffsetY, _focusedElement.DesiredSize.Width, finalSize.Height)
                        : new Rect(startItemOffsetY, startItemOffsetX, finalSize.Width, _focusedElement.DesiredSize.Height);
                    _focusedElement.Arrange(rect);
                }

                return finalSize;

                // RealizeAndVirtualizeItems(_viewport);
                // var orientation = Orientation;
                // var u = _realizedElements!.StartU;
                //
                // for (var i = 0; i < _realizedElements.Count; ++i)
                // {
                //     var e = _realizedElements.Elements[i];
                //
                //     if (e is not null)
                //     {
                //         var size = _realizedElements.Sizes[i];
                //         var rect = orientation == Orientation.Horizontal ? new Rect(u, 0, GetWidth(size), finalSize.Height) : new Rect(0, u, finalSize.Width, GetHeight(size));
                //         e.Arrange(rect);
                //         _scrollAnchorProvider?.RegisterAnchorCandidate(e);
                //         u += orientation == Orientation.Horizontal ? rect.Width : rect.Height;
                //     }
            }
            //
            // 
            //
            // return finalSize;

            finally
            {
                _isInLayout = false;

                // TODO: RaiseEvent(new RoutedEventArgs(Orientation == Orientation.Horizontal ? HorizontalSnapPointsChangedEvent : VerticalSnapPointsChangedEvent));
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _scrollAnchorProvider = this.FindAncestorOfType<IScrollAnchorProvider>();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _scrollAnchorProvider = null;
        }

        protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
        {
            InvalidateMeasure();

            if (_realizedElements is null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    _realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems!.Count, _updateElementIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, _updateElementIndex, _recycleElementOnItemRemoved);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    _realizedElements.ItemsReplaced(e.OldStartingIndex, e.OldItems!.Count, _recycleElementOnItemRemoved);
                    break;
                case NotifyCollectionChangedAction.Move:
                    _realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, _updateElementIndex, _recycleElementOnItemRemoved);
                    _realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems!.Count, _updateElementIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _realizedElements.ItemsReset(_recycleElementOnItemRemoved);
                    break;
            }
        }

        protected override void OnItemsControlChanged(ItemsControl? oldValue)
        {
            base.OnItemsControlChanged(oldValue);

            if (oldValue is not null)
                oldValue.PropertyChanged -= OnItemsControlPropertyChanged;
            if (ItemsControl is not null)
                ItemsControl.PropertyChanged += OnItemsControlPropertyChanged;
        }

        protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            var count = Items.Count;
            var fromControl = from as Control;

            if (count == 0 ||
                (fromControl is null && direction is not NavigationDirection.First and not NavigationDirection.Last))
                return null;

            var horiz = Orientation == Orientation.Horizontal;
            var fromIndex = fromControl != null ? IndexFromContainer(fromControl) : -1;
            var toIndex = fromIndex;

            switch (direction)
            {
                case NavigationDirection.First:
                    toIndex = 0;
                    break;
                case NavigationDirection.Last:
                    toIndex = count - 1;
                    break;
                case NavigationDirection.Next:
                    ++toIndex;
                    break;
                case NavigationDirection.Previous:
                    --toIndex;
                    break;
                case NavigationDirection.Left:
                    if (horiz)
                        --toIndex;
                    break;
                case NavigationDirection.Right:
                    if (horiz)
                        ++toIndex;
                    break;
                case NavigationDirection.Up:
                    if (!horiz)
                        --toIndex;
                    break;
                case NavigationDirection.Down:
                    if (!horiz)
                        ++toIndex;
                    break;
                default:
                    return null;
            }

            if (fromIndex == toIndex)
                return from;

            if (wrap)
            {
                if (toIndex < 0)
                    toIndex = count - 1;
                else if (toIndex >= count)
                    toIndex = 0;
            }

            return ScrollIntoView(toIndex);
        }

        protected override IEnumerable<Control>? GetRealizedContainers()
        {
            return _realizedElements?.Elements.Where(x => x is not null)!;
        }

        protected override Control? ContainerFromIndex(int index)
        {
            if (index < 0 || index >= Items.Count)
                return null;
            if (_scrollToIndex == index)
                return _scrollToElement;
            if (_focusedIndex == index)
                return _focusedElement;
            if (index == _realizingIndex)
                return _realizingElement;
            if (GetRealizedElement(index) is { } realized)
                return realized;
            if (Items[index] is Control c && c.GetValue(RecycleKeyProperty) == s_itemIsItsOwnContainer)
                return c;
            return null;
        }

        protected override int IndexFromContainer(Control container)
        {
            if (container == _scrollToElement)
                return _scrollToIndex;
            if (container == _focusedElement)
                return _focusedIndex;
            if (container == _realizingElement)
                return _realizingIndex;
            return _realizedElements?.GetIndex(container) ?? -1;
        }

        protected override Control? ScrollIntoView(int index)
        {
            var items = Items;

            if (_isInLayout || index < 0 || index >= items.Count || _realizedElements is null || !IsEffectivelyVisible)
                return null;

            if (GetRealizedElement(index) is Control element)
            {
                element.BringIntoView();
                return element;
            }
            else if (this.GetVisualRoot() is ILayoutRoot root)
            {
                // Create and measure the element to be brought into view. Store it in a field so that
                // it can be re-used in the layout pass.
                var scrollToElement = GetOrCreateElement(items, index);
                scrollToElement.Measure(Size.Infinity);

                // var viewport = CalculateMeasureViewport(Items);

                // Get the expected position of the element and put it in place.
                var start = FindItemOffset(index);
                var rect = Orientation == Orientation.Horizontal
                    ? new Rect(GetX(start), 0, scrollToElement.DesiredSize.Width, scrollToElement.DesiredSize.Height)
                    : new Rect(0, GetX(start), scrollToElement.DesiredSize.Width, scrollToElement.DesiredSize.Height);
                scrollToElement.Arrange(rect);

                // Store the element and index so that they can be used in the layout pass.
                _scrollToElement = scrollToElement;
                _scrollToIndex = index;

                // If the item being brought into view was added since the last layout pass then
                // our bounds won't be updated, so any containing scroll viewers will not have an
                // updated extent. Do a layout pass to ensure that the containing scroll viewers
                // will be able to scroll the new item into view.
                if (!Bounds.Contains(rect) && !_viewport.Contains(rect))
                {
                    _isWaitingForViewportUpdate = true;
                    root.ExecuteLayoutPass();
                    _isWaitingForViewportUpdate = false;
                }

                // Try to bring the item into view.
                scrollToElement.BringIntoView();

                // If the viewport does not contain the item to scroll to, set _isWaitingForViewportUpdate:
                // this should cause the following chain of events:
                // - Measure is first done with the old viewport (which will be a no-op, see MeasureOverride)
                // - The viewport is then updated by the layout system which invalidates our measure
                // - Measure is then done with the new viewport.
                _isWaitingForViewportUpdate = !_viewport.Contains(rect);
                root.ExecuteLayoutPass();

                // If for some reason the layout system didn't give us a new viewport during the layout, we
                // need to do another layout pass as the one that took place was a no-op.
                if (_isWaitingForViewportUpdate)
                {
                    _isWaitingForViewportUpdate = false;
                    InvalidateMeasure();
                    root.ExecuteLayoutPass();
                }

                // During the previous BringIntoView, the scroll width extent might have been out of date if
                // elements have different widths. Because of that, the ScrollViewer might not scroll to the correct offset.
                // After the previous BringIntoView, Y offset should be correct and an extra layout pass has been executed,
                // hence the width extent should be correct now, and we can try to scroll again.
                scrollToElement.BringIntoView();

                _scrollToElement = null;
                _scrollToIndex = -1;
                return scrollToElement;
            }

            return null;
        }


        internal IReadOnlyList<Control?> GetRealizedElements()
        {
            return _realizedElements?.Elements ?? Array.Empty<Control>();
        }

        // private MeasureViewport CalculateMeasureViewport(IReadOnlyList<object?> items)
        // {
        //     Debug.Assert(_realizedElements is not null);
        //
        //     var viewport = _viewport;
        //
        //     // Get the viewport in the orientation direction.
        //     var viewportStart = GetY(_viewport.TopLeft);
        //     var viewportEnd = GetY(_viewport.BottomRight);
        //     var viewportWidth = GetWidth(_viewport.Size);
        //
        //     // Get or estimate the anchor element from which to start realization. If we are
        //     // scrolling to an element, use that as the anchor element. Otherwise, estimate the
        //     // anchor element based on the current viewport.
        //     int anchorIndex;
        //     double anchorU;
        //
        //     if (_scrollToIndex >= 0 && _scrollToElement is not null)
        //     {
        //         anchorIndex = _scrollToIndex;
        //         anchorU = _scrollToElement.Bounds.Top;
        //     }
        //     else
        //     {
        //         GetOrEstimateAnchorElementForViewport(
        //             viewportStart,
        //             viewportEnd,
        //             items.Count,
        //             out anchorIndex,
        //             out anchorU);
        //     }
        //
        //     // Check if the anchor element is not within the currently realized elements.
        //     var disjunct = anchorIndex < _realizedElements.FirstIndex ||
        //                    anchorIndex > _realizedElements.LastIndex;
        //
        //     return new MeasureViewport
        //     {
        //         anchorIndex = anchorIndex,
        //         anchorU = anchorU,
        //         viewportUStart = viewportStart,
        //         viewportUEnd = viewportEnd,
        //         viewportWidth = viewportWidth,
        //         viewportIsDisjunct = disjunct,
        //     };
        // }

        private void GetOrEstimateAnchorElementForViewport(
            double viewportStartU,
            double viewportEndU,
            int itemCount,
            out int index,
            out double position)
        {
            // We have no elements, or we're at the start of the viewport.
            if (itemCount <= 0 || MathUtilities.IsZero(viewportStartU))
            {
                index = 0;
                position = 0;
                return;
            }

            // If we have realised elements and a valid StartU then try to use this information to
            // get the anchor element.
            if (_realizedElements?.StartU is { } u && !double.IsNaN(u))
            {
                var orientation = Orientation;

                for (var i = 0; i < _realizedElements.Elements.Count; ++i)
                {
                    if (_realizedElements.Elements[i] is not { } element)
                        continue;

                    var sizeU = orientation == Orientation.Horizontal ? element.DesiredSize.Width : element.DesiredSize.Height;
                    var endU = u + sizeU;

                    if (endU > viewportStartU && u < viewportEndU)
                    {
                        index = _realizedElements.FirstIndex + i;
                        position = u;
                        return;
                    }

                    u = endU;
                }
            }

            // We don't have any realized elements in the requested viewport, or can't rely on
            // StartU being valid. Estimate the index using only the estimated element size.
            var estimatedSize = CalculateAverageItemSize();

            var itemsPerRow = Math.Floor(GetWidth(_viewport.Size) / GetWidth(estimatedSize));

            // Estimate the element at the start of the viewport.
            var startIndex = Math.Min((int)(viewportStartU / GetHeight(estimatedSize) / itemsPerRow), itemCount - 1);
            index = startIndex;
            position = startIndex * GetHeight(estimatedSize);
        }

        private Size CalculateDesiredSize(Orientation orientation, int itemCount)
        {
            var itemSize = GetAverageItemSize();

            var viewportWidth = GetWidth(_viewport.Size);
            
            if (itemCount == 0 || MathUtilities.IsZero(viewportWidth)) return new Size(0, 0);
            
            return orientation == Orientation.Horizontal
                ? new Size(viewportWidth, GetHeight(itemSize) * itemCount / Math.Ceiling(viewportWidth / GetWidth(itemSize)))
                : new Size(GetHeight(itemSize) * itemCount / Math.Ceiling(viewportWidth / GetWidth(itemSize)), viewportWidth);
            
            var sizeU = 0.0;
            var sizeV = viewportWidth;

            if (endItemIndex >= 0)
            {
                var itemsPerRow = Math.Floor(GetWidth(_viewport.Size) / GetWidth(averageItemSizeCache ?? itemSize));
                var remaining = Math.Ceiling((itemCount - endItemIndex - 1) / itemsPerRow);
                sizeU = GetY(_viewport.BottomRight) + (remaining * GetHeight(averageItemSizeCache ?? itemSize));
            }

            return orientation == Orientation.Horizontal ? new(sizeU, sizeV) : new(sizeV, sizeU);
        }

        private Size EstimateDesiredSize(Orientation orientation, int itemCount)
        {
            if (_scrollToIndex >= 0 && _scrollToElement is not null)
            {
                // We have an element to scroll to, so we can estimate the desired size based on the
                // element's position and the remaining elements.
                var remaining = itemCount - _scrollToIndex - 1;
                var u = orientation == Orientation.Horizontal ? _scrollToElement.Bounds.Right : _scrollToElement.Bounds.Bottom;
                var sizeU = u + (remaining * _lastEstimatedElementSizeU);
                return orientation == Orientation.Horizontal 
                    ? new(sizeU, DesiredSize.Height) 
                    : new(DesiredSize.Width, sizeU);
            }

            return DesiredSize;
        }


        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == OrientationProperty)
            {
                // MouseWheelScrollDirection = Orientation == Orientation.Horizontal
                //     ? ScrollDirection.Vertical
                //     : ScrollDirection.Horizontal;
                // SetVerticalOffset(0);
                // SetHorizontalOffset(0);
                InvalidateMeasure();
                InvalidateArrange();
            }

            if (change.Property == AllowDifferentSizedItemsProperty || change.Property == ItemSizeProperty)
            {
                foreach (var child in Children)
                {
                    child.InvalidateMeasure();
                }
            }
        }


        private void RealizeAndVirtualizeItems(
            IReadOnlyList<object?> items,
            Size availableSize)
        {
            FindStartIndexAndOffset();
            VirtualizeItemsBeforeStartIndex();
            RealizeItemsAndFindEndIndex();
            VirtualizeItemsAfterEndIndex();
        }

        private Size GetAverageItemSize()
        {
            if (!ItemSize.NearlyEquals(EmptySize))
            {
                return ItemSize;
            }
            else if (!AllowDifferentSizedItems)
            {
                return sizeOfFirstItem ?? FallbackItemSize;
            }
            else
            {
                return averageItemSizeCache ??= CalculateAverageItemSize();
            }
        }

        private Point FindItemOffset(int itemIndex)
        {
            double x = 0, y = 0, rowHeight = 0;

            for (int i = 0; i <= itemIndex; i++)
            {
                Size itemSize = GetAssumedItemSize(Items[i]);

                if (x != 0 && x + GetWidth(itemSize) > GetWidth(_viewport.Size))
                {
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                }

                if (i != itemIndex)
                {
                    x += GetWidth(itemSize);
                    rowHeight = Math.Max(rowHeight, GetHeight(itemSize));
                }
            }

            return CreatePoint(x, y);
        }


        private void FindStartIndexAndOffset()
        {
            if (GetY(_viewport.TopLeft) == 0 && GetY(_viewport.BottomRight) == 0)
            {
                startItemIndex = -1;
                startItemOffsetX = 0;
                startItemOffsetY = 0;
                return;
            }

            double startOffsetY = DetermineStartOffsetY();

            if (startOffsetY <= 0)
            {
                startItemIndex = Items.Count > 0 ? 0 : -1;
                startItemOffsetX = 0;
                startItemOffsetY = 0;
                return;
            }

            startItemIndex = -1;

            double x = 0, y = 0, rowHeight = 0;
            int indexOfFirstRowItem = 0;

            int itemIndex = 0;
            foreach (var item in Items)
            {
                Size itemSize = GetAssumedItemSize(item);

                if (x + GetWidth(itemSize) > GetWidth(_viewport.Size) && x != 0)
                {
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                    indexOfFirstRowItem = itemIndex;
                }

                x += GetWidth(itemSize);
                rowHeight = Math.Max(rowHeight, GetHeight(itemSize));

                if (y + rowHeight > startOffsetY)
                {
                    if (cacheLengthUnit == VirtualizationCacheLengthUnit.Item)
                    {
                        startItemIndex = Math.Max(indexOfFirstRowItem - (int)cacheLength.CacheBeforeViewport, 0);
                        var itemOffset = FindItemOffset(startItemIndex);
                        startItemOffsetX = GetX(itemOffset);
                        startItemOffsetY = GetY(itemOffset);
                    }
                    else
                    {
                        startItemIndex = indexOfFirstRowItem;
                        startItemOffsetX = 0;
                        startItemOffsetY = y;
                    }

                    break;
                }

                itemIndex++;
            }

            // make sure that at least one item is realized to allow correct calculation of the extend
            if (startItemIndex == -1 && Items.Count > 0)
            {
                startItemIndex = Items.Count - 1;
                startItemOffsetX = x;
                startItemOffsetY = y;
            }
        }

        private void RealizeItemsAndFindEndIndex()
        {
            if (startItemIndex == -1)
            {
                endItemIndex = -1;
                knownExtendX = 0;
                return;
            }

            int newEndItemIndex = Items.Count - 1;
            bool endItemIndexFound = false;

            double endOffsetY = DetermineEndOffsetY();

            double x = startItemOffsetX;
            double y = startItemOffsetY;
            double rowHeight = 0;

            knownExtendX = 0;

            for (int itemIndex = startItemIndex; itemIndex <= newEndItemIndex; itemIndex++)
            {
                if (itemIndex == 0)
                {
                    sizeOfFirstItem = null;
                }

                object item = Items[itemIndex];

                var container = GetOrCreateElement(Items, itemIndex);

                if (container == _scrollToElement)
                {
                    _scrollToIndex = -1;
                    _scrollToElement = null;
                }

                Size? upfrontKnownItemSize = GetUpfrontKnownItemSize(item);

                container.Measure(upfrontKnownItemSize ?? Size.Infinity);

                var containerSize = DetermineContainerSize(item, container, upfrontKnownItemSize);

                _measureElements!.Add(itemIndex, container, 0, containerSize);

                if (AllowDifferentSizedItems == false && sizeOfFirstItem is null)
                {
                    sizeOfFirstItem = containerSize;
                }

                if (x != 0 && x + GetWidth(containerSize) > GetWidth(_viewport.Size))
                {
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                }

                x += GetWidth(containerSize);
                knownExtendX = Math.Max(x, knownExtendX);
                rowHeight = Math.Max(rowHeight, GetHeight(containerSize));

                if (endItemIndexFound == false)
                {
                    if (y >= endOffsetY
                        || (AllowDifferentSizedItems == false
                            && x + GetWidth(sizeOfFirstItem!.Value) > GetWidth(_viewport.Size)
                            && y + rowHeight >= endOffsetY))
                    {
                        endItemIndexFound = true;

                        newEndItemIndex = itemIndex;

                        if (cacheLengthUnit == VirtualizationCacheLengthUnit.Item)
                        {
                            newEndItemIndex = Math.Min(newEndItemIndex + (int)cacheLength.CacheAfterViewport,
                                Items.Count - 1);
                            // loop continues unitl newEndItemIndex is reached
                        }
                    }
                }
            }

            endItemIndex = newEndItemIndex;
            Debug.WriteLine($"Start: {startItemIndex} - End: {endItemIndex}");
        }

        private Size DetermineContainerSize(object item, Control container, Size? upfrontKnownItemSize)
        {
            Size containerSize = upfrontKnownItemSize ?? container.DesiredSize;

            return containerSize;
        }

        private void VirtualizeItemsBeforeStartIndex()
        {
            _realizedElements!.RecycleElementsAfter(startItemIndex, RecycleElement);
            
            // var containers = ItemContainerManager.Elements;
            // foreach (var container in containers.Where(container => container != bringIntoViewContainer))
            // {
            //     int itemIndex = GetIndexFromContainer(container);
            //
            //     if (itemIndex < startItemIndex)
            //     {
            //         ItemContainerManager.Virtualize(container);
            //     }
            // }
        }

        private void VirtualizeItemsAfterEndIndex()
        {
            _realizedElements!.RecycleElementsAfter(endItemIndex, RecycleElement);
            
            // var containers = ItemContainerManager.Elements;
            // foreach (var container in containers.Where(container => container != bringIntoViewContainer))
            // {
            //     int itemIndex = GetIndexFromContainer(container);
            //
            //     if (itemIndex > endItemIndex)
            //     {
            //         
            //     }
            // }
        }

        // private void UpdateExtent()
        // {
        //     Size extent;
        //
        //     if (startItemIndex == -1)
        //     {
        //         extent = new Size(0, 0);
        //     }
        //     else if (!AllowDifferentSizedItems)
        //     {
        //         extent = CalculateExtentForSameSizedItems();
        //     }
        //     else
        //     {
        //         extent = CalculateExtentForDifferentSizedItems();
        //     }
        //
        //     if (extent != Extent)
        //     {
        //         Extent = extent;
        //     }
        // }

        private Size CalculateExtentForSameSizedItems()
        {
            var itemSize = !ItemSize.NearlyEquals(EmptySize) ? ItemSize : sizeOfFirstItem!.Value;
            int itemsPerRow = (int)Math.Max(1, Math.Floor(GetWidth(_viewport.Size) / GetWidth(itemSize)));
            double extentY = Math.Ceiling(((double)Items.Count) / itemsPerRow) * GetHeight(itemSize);
            return CreateSize(knownExtendX, extentY);
        }

        private Size CalculateExtentForDifferentSizedItems()
        {
            double x = 0;
            double y = 0;
            double rowHeight = 0;

            foreach (var item in Items)
            {
                Size itemSize = GetAssumedItemSize(item);

                if (x + GetWidth(itemSize) > GetWidth(_viewport.Size))
                {
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                }

                x += GetWidth(itemSize);
                rowHeight = Math.Max(rowHeight, GetHeight(itemSize));
            }

            return CreateSize(knownExtendX, y + rowHeight);
        }

        // private Size CalculateDesiredSize(Size availableSize)
        // {
        //     double desiredWidth = Math.Min(availableSize.Width, Extent.Width);
        //     double desiredHeight = Math.Min(availableSize.Height, Extent.Height);
        //
        //     return new Size(desiredWidth, desiredHeight);
        // }

        private double DetermineStartOffsetY()
        {
            double cacheLength = 0;

            if (cacheLengthUnit == VirtualizationCacheLengthUnit.Page)
            {
                cacheLength = this.cacheLength.CacheBeforeViewport * GetHeight(_viewport.Size); // viewport.viewportHeight;
            }
            else if (cacheLengthUnit == VirtualizationCacheLengthUnit.Pixel)
            {
                cacheLength = this.cacheLength.CacheBeforeViewport;
            }
            
            return Math.Max(GetY(_viewport.TopLeft) - cacheLength, 0);
        }

        private double DetermineEndOffsetY()
        {
            double cacheLength = 0;

            if (cacheLengthUnit == VirtualizationCacheLengthUnit.Page)
            {
                cacheLength = this.cacheLength.CacheAfterViewport * GetHeight(_viewport.Size);
            }
            else if (cacheLengthUnit == VirtualizationCacheLengthUnit.Pixel)
            {
                cacheLength = this.cacheLength.CacheAfterViewport;
            }

            return Math.Max(0, GetY(_viewport.BottomRight) + cacheLength);
        }

        private Size? GetUpfrontKnownItemSize(object item)
        {
            if (!ItemSize.NearlyEquals(EmptySize))
            {
                return ItemSize;
            }

            if (!AllowDifferentSizedItems && sizeOfFirstItem != null)
            {
                return sizeOfFirstItem;
            }

            if (ItemSizeProvider != null)
            {
                return ItemSizeProvider.GetSizeForItem(item);
            }

            return null;
        }

        private Size GetAssumedItemSize(object item)
        {
            var index = Items.IndexOf(item);

            if (GetUpfrontKnownItemSize(item) is Size upfrontKnownItemSize)
            {
                return upfrontKnownItemSize;
            }

            if (_realizedElements!.GetElementSize(index) is Size cachedItemSize)
            {
                return cachedItemSize;
            }

            return GetAverageItemSize();
        }

        private void ArrangeRow(double rowWidth, List<Control> children, List<Size> childSizes, double y)
        {
            double summedUpChildWidth;
            double extraWidth = 0;

            if (AllowDifferentSizedItems)
            {
                summedUpChildWidth = childSizes.Sum(childSize => GetWidth(childSize));

                if (StretchItems)
                {
                    double unusedWidth = rowWidth - summedUpChildWidth;
                    extraWidth = unusedWidth / children.Count;
                    summedUpChildWidth = rowWidth;
                }
            }
            else
            {
                double childWidth = GetWidth(childSizes[0]);
                int itemsPerRow = IsGridLayoutEnabled
                    ? (int)Math.Max(Math.Floor(rowWidth / childWidth), 1)
                    : children.Count;

                if (StretchItems)
                {
                    var firstChild = children[0];
                    double maxWidth = Orientation == Orientation.Horizontal
                        ? firstChild.MaxWidth
                        : firstChild.MaxHeight;
                    double stretchedChildWidth = Math.Min(rowWidth / itemsPerRow, maxWidth);
                    stretchedChildWidth =
                        Math.Max(stretchedChildWidth, childWidth); // ItemSize might be greater than MaxWidth/MaxHeight
                    extraWidth = stretchedChildWidth - childWidth;
                    summedUpChildWidth = itemsPerRow * stretchedChildWidth;
                }
                else
                {
                    summedUpChildWidth = itemsPerRow * childWidth;
                }
            }

            double innerSpacing = 0;
            double outerSpacing = 0;

            if (summedUpChildWidth < rowWidth)
            {
                CalculateRowSpacing(rowWidth, children, summedUpChildWidth, out innerSpacing, out outerSpacing);
            }

            double x = -GetX(_viewport.TopLeft) + outerSpacing;

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                Size childSize = childSizes[i];
                child.Arrange(CreateRect(x, y, GetWidth(childSize) + extraWidth, GetHeight(childSize)));
                x += GetWidth(childSize) + extraWidth + innerSpacing;
            }
        }

        private void CalculateRowSpacing(double rowWidth, List<Control> children, double summedUpChildWidth,
            out double innerSpacing, out double outerSpacing)
        {
            int childCount;

            if (AllowDifferentSizedItems)
            {
                childCount = children.Count;
            }
            else
            {
                childCount = IsGridLayoutEnabled
                    ? (int)Math.Max(1, Math.Floor(rowWidth / GetWidth(sizeOfFirstItem!.Value)))
                    : children.Count;
            }

            double unusedWidth = Math.Max(0, rowWidth - summedUpChildWidth);

            switch (SpacingMode)
            {
                case SpacingMode.Uniform:
                    innerSpacing = outerSpacing = unusedWidth / (childCount + 1);
                    break;

                case SpacingMode.BetweenItemsOnly:
                    innerSpacing = unusedWidth / Math.Max(childCount - 1, 1);
                    outerSpacing = 0;
                    break;

                case SpacingMode.StartAndEndOnly:
                    innerSpacing = 0;
                    outerSpacing = unusedWidth / 2;
                    break;

                case SpacingMode.None:
                default:
                    innerSpacing = 0;
                    outerSpacing = 0;
                    break;
            }
        }

        private Size CalculateAverageItemSize()
        {
            if (_realizedElements!.Sizes.Count > 0)
            {
                return new Size(
                    Math.Round(_realizedElements!.Sizes.Average(size => size.Width)),
                    Math.Round(_realizedElements!.Sizes.Average(size => size.Height)));
            }

            return FallbackItemSize;
        }

        private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            // var vertical = Orientation == Orientation.Vertical;
            var oldViewportStart = GetY(_viewport.TopLeft); // vertical ? ScrollOffset.Top : _viewport.Left;
            var oldViewportEnd = GetY(_viewport.BottomRight); // vertical ? _viewport.Bottom : _viewport.Right;

            _viewport = e.EffectiveViewport.Intersect(new(Bounds.Size));
            _isWaitingForViewportUpdate = false;

            var newViewportStart = GetY(_viewport.TopLeft); // vertical ? _viewport.Top : _viewport.Left;
            var newViewportEnd = GetY(_viewport.BottomRight); // ? _viewport.Bottom : _viewport.Right);

            if (!MathUtilities.AreClose(oldViewportStart, newViewportStart) ||
                !MathUtilities.AreClose(oldViewportEnd, newViewportEnd))
            {
                InvalidateMeasure();
            }
        }


        private void OnItemsControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (_focusedElement is not null &&
                e.Property == KeyboardNavigation.TabOnceActiveElementProperty &&
                e.GetOldValue<IInputElement?>() == _focusedElement)
            {
                // TabOnceActiveElement has moved away from _focusedElement so we can recycle it.
                RecycleElement(_focusedElement, _focusedIndex);
                _focusedElement = null;
                _focusedIndex = -1;
            }
        }

        #region scroll info

        // TODO determine exact scoll amount for item based scrolling when AllowDifferentSizedItems is true

        // protected override double GetLineUpScrollAmount()
        // {
        //     return -Math.Min(GetAverageItemSize().Height * ScrollLineDeltaItem, ViewportSize.Height);
        // }
        //
        // protected override double GetLineDownScrollAmount()
        // {
        //     return Math.Min(GetAverageItemSize().Height * ScrollLineDeltaItem, ViewportSize.Height);
        // }
        //
        // protected override double GetLineLeftScrollAmount()
        // {
        //     return -Math.Min(GetAverageItemSize().Width * ScrollLineDeltaItem, ViewportSize.Width);
        // }
        //
        // protected override double GetLineRightScrollAmount()
        // {
        //     return Math.Min(GetAverageItemSize().Width * ScrollLineDeltaItem, ViewportSize.Width);
        // }
        //
        // protected override double GetMouseWheelUpScrollAmount()
        // {
        //     return -Math.Min(GetAverageItemSize().Height * MouseWheelDeltaItem, ViewportSize.Height);
        // }
        //
        // protected override double GetMouseWheelDownScrollAmount()
        // {
        //     return Math.Min(GetAverageItemSize().Height * MouseWheelDeltaItem, ViewportSize.Height);
        // }
        //
        // protected override double GetMouseWheelLeftScrollAmount()
        // {
        //     return -Math.Min(GetAverageItemSize().Width * MouseWheelDeltaItem, ViewportSize.Width);
        // }
        //
        // protected override double GetMouseWheelRightScrollAmount()
        // {
        //     return Math.Min(GetAverageItemSize().Width * MouseWheelDeltaItem, ViewportSize.Width);
        // }
        //
        // protected override double GetPageUpScrollAmount()
        // {
        //     return -ViewportSize.Height;
        // }
        //
        // protected override double GetPageDownScrollAmount()
        // {
        //     return ViewportSize.Height;
        // }
        //
        // protected override double GetPageLeftScrollAmount()
        // {
        //     return -ViewportSize.Width;
        // }
        //
        // protected override double GetPageRightScrollAmount()
        // {
        //     return ViewportSize.Width;
        // }

        #endregion

        #region orientation aware helper methods

        private double GetX(Point point) => Orientation == Orientation.Horizontal ? point.X : point.Y;
        private double GetY(Point point) => Orientation == Orientation.Horizontal ? point.Y : point.X;
        private double GetRowWidth(Size size) => Orientation == Orientation.Vertical ? size.Width : size.Height;
        private double GetWidth(Size size) => Orientation == Orientation.Horizontal ? size.Width : size.Height;
        private double GetHeight(Size size) => Orientation == Orientation.Horizontal ? size.Height : size.Width;

        private Point CreatePoint(double x, double y) =>
            Orientation == Orientation.Horizontal ? new Point(x, y) : new Point(y, x);

        private Size CreateSize(double width, double height) => Orientation == Orientation.Horizontal
            ? new Size(width, height)
            : new Size(height, width);

        private Rect CreateRect(double x, double y, double width, double height) =>
            Orientation == Orientation.Horizontal ? new Rect(x, y, width, height) : new Rect(y, x, height, width);

        #endregion

        private Control GetOrCreateElement(IReadOnlyList<object?> items, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            if ((GetRealizedElement(index) ??
                 GetRealizedElement(index, ref _focusedIndex, ref _focusedElement) ??
                 GetRealizedElement(index, ref _scrollToIndex, ref _scrollToElement)) is { } realized)
                return realized;

            var item = items[index];
            var generator = ItemContainerGenerator!;

            if (generator.NeedsContainer(item, index, out var recycleKey))
            {
                return GetRecycledElement(item, index, recycleKey) ??
                       CreateElement(item, index, recycleKey);
            }
            else
            {
                return GetItemAsOwnContainer(item, index);
            }
        }

        private Control? GetRealizedElement(int index)
        {
            return _realizedElements?.GetElement(index);
        }

        private static Control? GetRealizedElement(
            int index,
            ref int specialIndex,
            ref Control? specialElement)
        {
            if (specialIndex == index)
            {
                Debug.Assert(specialElement is not null);

                var result = specialElement;
                specialIndex = -1;
                specialElement = null;
                return result;
            }

            return null;
        }

        private Control GetItemAsOwnContainer(object? item, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var controlItem = (Control)item!;
            var generator = ItemContainerGenerator!;

            if (!controlItem.IsSet(RecycleKeyProperty))
            {
                generator.PrepareItemContainer(controlItem, controlItem, index);
                // TODO: Handle this here or in ItemsContainerManaager?
                AddInternalChild(controlItem);
                controlItem.SetValue(RecycleKeyProperty, s_itemIsItsOwnContainer);
                generator.ItemContainerPrepared(controlItem, item, index);
            }

            controlItem.SetCurrentValue(Visual.IsVisibleProperty, true);
            return controlItem;
        }

        private Control? GetRecycledElement(object? item, int index, object? recycleKey)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            if (recycleKey is null)
                return null;

            var generator = ItemContainerGenerator!;

            if (_recyclePool?.TryGetValue(recycleKey, out var recyclePool) == true && recyclePool.Count > 0)
            {
                var recycled = recyclePool.Pop();
                recycled.SetCurrentValue(Visual.IsVisibleProperty, true);
                generator.PrepareItemContainer(recycled, item, index);
                generator.ItemContainerPrepared(recycled, item, index);
                return recycled;
            }

            return null;
        }

        private Control CreateElement(object? item, int index, object? recycleKey)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var generator = ItemContainerGenerator!;
            var container = generator.CreateContainer(item, index, recycleKey);

            container.SetValue(RecycleKeyProperty, recycleKey);
            generator.PrepareItemContainer(container, item, index);
            // TODO: Handle this here or in ItemsContainerManaager?
            AddInternalChild(container);
            generator.ItemContainerPrepared(container, item, index);

            return container;
        }

        private void RecycleElement(Control element, int index)
        {
            Debug.Assert(ItemsControl is not null);
            Debug.Assert(ItemContainerGenerator is not null);

            _scrollAnchorProvider?.UnregisterAnchorCandidate(element);

            var recycleKey = element.GetValue(RecycleKeyProperty);

            if (recycleKey is null)
            {
                RemoveInternalChild(element);
            }
            else if (recycleKey == s_itemIsItsOwnContainer)
            {
                element.SetCurrentValue(Visual.IsVisibleProperty, false);
            }
            else if (KeyboardNavigation.GetTabOnceActiveElement(ItemsControl) == element)
            {
                _focusedElement = element;
                _focusedIndex = index;
            }
            else
            {
                // RemoveInternalChild(element);
                ItemContainerGenerator!.ClearItemContainer(element);
                PushToRecyclePool(recycleKey, element);
                element.SetCurrentValue(Visual.IsVisibleProperty, false);
            }
        }

        private void RecycleElementOnItemRemoved(Control element)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var recycleKey = element.GetValue(RecycleKeyProperty);

            if (recycleKey is null || recycleKey == s_itemIsItsOwnContainer)
            {
                RemoveInternalChild(element);
            }
            else
            {
                // RemoveInternalChild(element);
                ItemContainerGenerator!.ClearItemContainer(element);
                PushToRecyclePool(recycleKey, element);
                element.SetCurrentValue(Visual.IsVisibleProperty, false);
            }
        }

        private void PushToRecyclePool(object recycleKey, Control element)
        {
            _recyclePool ??= new();

            if (!_recyclePool.TryGetValue(recycleKey, out var pool))
            {
                pool = new();
                _recyclePool.Add(recycleKey, pool);
            }

            pool.Push(element);
        }

        private void UpdateElementIndex(Control element, int oldIndex, int newIndex)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            ItemContainerGenerator.ItemContainerIndexChanged(element, oldIndex, newIndex);
        }


        // private struct MeasureViewport
        // {
        //     public int anchorIndex;
        //     public double anchorU;
        //     public double viewportUStart;
        //     public double viewportUEnd;
        //     public double measuredV;
        //     public double realizedEndU;
        //     public int lastIndex;
        //     public bool viewportIsDisjunct;
        //     public double viewportHeight => viewportUEnd - viewportUStart;
        //     public double viewportWidth;
        // }
    }
}