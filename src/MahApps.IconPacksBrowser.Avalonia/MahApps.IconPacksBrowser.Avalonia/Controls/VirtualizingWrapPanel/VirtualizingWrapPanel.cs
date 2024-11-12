using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using MahApps.IconPacksBrowser.Avalonia.Controls.Utils;

namespace MahApps.IconPacksBrowser.Avalonia.Controls
{
    /// <summary>
    /// Positions child elements in sequential position from left to right, 
    /// breaking content to the next line at the edge of the containing box. 
    /// Subsequent ordering happens sequentially from top to bottom or from right to left, 
    /// depending on the value of the <see cref="Orientation"/> property.
    /// </summary>
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollSnapPointsInfo
    {
        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            StackPanel.OrientationProperty.AddOwner<VirtualizingWrapPanel>();

        /// <summary>
        /// Defines the <see cref="AreHorizontalSnapPointsRegular"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AreHorizontalSnapPointsRegularProperty =
            AvaloniaProperty.Register<VirtualizingWrapPanel, bool>(nameof(AreHorizontalSnapPointsRegular));

        /// <summary>
        /// Defines the <see cref="AreVerticalSnapPointsRegular"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AreVerticalSnapPointsRegularProperty =
            AvaloniaProperty.Register<VirtualizingWrapPanel, bool>(nameof(AreVerticalSnapPointsRegular));

        /// <summary>
        /// Defines the <see cref="HorizontalSnapPointsChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> HorizontalSnapPointsChangedEvent =
            RoutedEvent.Register<VirtualizingWrapPanel, RoutedEventArgs>(
                nameof(HorizontalSnapPointsChanged),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="VerticalSnapPointsChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> VerticalSnapPointsChangedEvent =
            RoutedEvent.Register<VirtualizingWrapPanel, RoutedEventArgs>(
                nameof(VerticalSnapPointsChanged),
                RoutingStrategies.Bubble);

        private static readonly AttachedProperty<object?> RecycleKeyProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingWrapPanel, Control, object?>("RecycleKey");

        private static readonly object s_itemIsItsOwnContainer = new object();
        private readonly Action<Control, int> _recycleElement;
        private readonly Action<Control> _recycleElementOnItemRemoved;
        private readonly Action<Control, int, int> _updateElementIndex;
        private int _scrollToIndex = -1;
        private Control? _scrollToElement;
        private bool _isInLayout;
        private bool _isWaitingForViewportUpdate;
        private double _lastEstimatedElementSizeU = 25;
        private double _lastEstimatedElementSizeV = 25;
        private int _elementsPerRow = 1;
        private RealizedWrappedElements? _measureElements;
        private RealizedWrappedElements? _realizedElements;
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
        /// Gets or sets the axis along which items are laid out.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the axis along which items are laid out.
        /// The default is Vertical.
        /// </value>
        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Occurs when the measurements for horizontal snap points change.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged
        {
            add => AddHandler(HorizontalSnapPointsChangedEvent, value);
            remove => RemoveHandler(HorizontalSnapPointsChangedEvent, value);
        }

        /// <summary>
        /// Occurs when the measurements for vertical snap points change.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged
        {
            add => AddHandler(VerticalSnapPointsChangedEvent, value);
            remove => RemoveHandler(VerticalSnapPointsChangedEvent, value);
        }

        /// <summary>
        /// Gets or sets whether the horizontal snap points for the <see cref="VirtualizingWrapPanel"/> are equidistant from each other.
        /// </summary>
        public bool AreHorizontalSnapPointsRegular
        {
            get => GetValue(AreHorizontalSnapPointsRegularProperty);
            set => SetValue(AreHorizontalSnapPointsRegularProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the vertical snap points for the <see cref="VirtualizingWrapPanel"/> are equidistant from each other.
        /// </summary>
        public bool AreVerticalSnapPointsRegular
        {
            get => GetValue(AreVerticalSnapPointsRegularProperty);
            set => SetValue(AreVerticalSnapPointsRegularProperty, value);
        }

        /// <summary>
        /// Gets the index of the first realized element, or -1 if no elements are realized.
        /// </summary>
        public int FirstRealizedIndex => _realizedElements?.FirstIndex ?? -1;

        /// <summary>
        /// Gets the index of the last realized element, or -1 if no elements are realized.
        /// </summary>
        public int LastRealizedIndex => _realizedElements?.LastIndex ?? -1;

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
                _realizedElements?.ValidateStartU(Orientation);
                _realizedElements?.ValidateStartV(Orientation);
                _realizedElements ??= new();
                _measureElements ??= new();

                // We handle horizontal and vertical layouts here so X and Y are abstracted to:
                // - Horizontal layouts: U = horizontal, V = vertical
                // - Vertical layouts: U = vertical, V = horizontal
                var viewport = CalculateMeasureViewport(items);

                // If the viewport is disjunct then we can recycle everything.
                if (viewport.viewportIsDisjunct)
                    _realizedElements.RecycleAllElements(_recycleElement);

                // Do the measure, creating/recycling elements as necessary to fill the viewport. Don't
                // write to _realizedElements yet, only _measureElements.
                RealizeElements(items, availableSize, ref viewport);

                // Now swap the measureElements and realizedElements collection.
                (_measureElements, _realizedElements) = (_realizedElements, _measureElements);
                _measureElements.ResetForReuse();

                // If there is a focused element is outside the visible viewport (i.e.
                // _focusedElement is non-null), ensure it's measured.
                _focusedElement?.Measure(availableSize);

                return CalculateDesiredSize(orientation, items.Count, viewport);
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
                var orientation = Orientation;
                var u = _realizedElements!.StartU;
                var v = 0d;

                for (var i = 0; i < _realizedElements.Count; ++i)
                {
                    var e = _realizedElements.Elements[i];

                    if (e is not null)
                    {
                        var sizeU = _realizedElements.SizeU[i];
                        var sizeV = _realizedElements.SizeV[i];

                        var nextLine = v + sizeV >
                                       (orientation == Orientation.Horizontal ? finalSize.Height : finalSize.Width);

                        if (nextLine)
                        {
                            u += sizeU;
                            v = 0;
                        }

                        var rect = orientation == Orientation.Horizontal
                            ? new Rect(u, v, sizeU, sizeV)
                            : new Rect(v, u, sizeV, sizeU);

                        e.Arrange(rect);
                        _scrollAnchorProvider?.RegisterAnchorCandidate(e);

                        v += orientation == Orientation.Horizontal ? rect.Height : rect.Width;
                    }
                }

                // Ensure that the focused element is in the correct position.
                if (_focusedElement is not null && _focusedIndex >= 0)
                {
                    u = GetOrEstimateElementU(_focusedIndex,
                        orientation == Orientation.Horizontal ? finalSize.Height : finalSize.Width);
					v = GetOrEstimateElementV(_focusedIndex,
                        orientation == Orientation.Horizontal ? finalSize.Height : finalSize.Width);
						
                    var rect = orientation == Orientation.Horizontal
                        ? new Rect(u, v, _focusedElement.DesiredSize.Width, finalSize.Height)
                        : new Rect(v, u, finalSize.Width, _focusedElement.DesiredSize.Height);
                    _focusedElement.Arrange(rect);
                }

                return finalSize;
            }
            finally
            {
                _isInLayout = false;

                RaiseEvent(new RoutedEventArgs(Orientation == Orientation.Horizontal
                    ? HorizontalSnapPointsChangedEvent
                    : VerticalSnapPointsChangedEvent));
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

                // Get the expected position of the element and put it in place.
                var anchorU = GetOrEstimateElementU(index,
                    Orientation == Orientation.Horizontal ? _viewport.Height : _viewport.Width);
					
				var anchorV = GetOrEstimateElementV(index,
                    Orientation == Orientation.Horizontal ? _viewport.Width : _viewport.Height);


                var rect = Orientation == Orientation.Horizontal
                    ? new Rect(anchorU, anchorV, scrollToElement.DesiredSize.Width, scrollToElement.DesiredSize.Height)
                    : new Rect(anchorV, anchorU, scrollToElement.DesiredSize.Width, scrollToElement.DesiredSize.Height);
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

        private MeasureViewport CalculateMeasureViewport(IReadOnlyList<object?> items)
        {
            Debug.Assert(_realizedElements is not null);

            var viewport = _viewport;

            // Get the viewport in the orientation direction.
            var viewportUStart = Orientation == Orientation.Horizontal ? viewport.X : viewport.Y;
            var viewportUEnd = Orientation == Orientation.Horizontal ? viewport.Right : viewport.Bottom;

            var viewportVEnd = Orientation == Orientation.Horizontal ? viewport.Bottom : viewport.Right;

            // Get or estimate the anchor element from which to start realization. If we are
            // scrolling to an element, use that as the anchor element. Otherwise, estimate the
            // anchor element based on the current viewport.
            int anchorIndex;
            double anchorU, anchorV;

            if (_scrollToIndex >= 0 && _scrollToElement is not null)
            {
                anchorIndex = _scrollToIndex;
                anchorU = _scrollToElement.Bounds.Top;
                anchorV = _scrollToElement.Bounds.Left;
            }
            else
            {
                GetOrEstimateAnchorElementForViewport(
                    viewportUStart,
                    viewportUEnd,
                    viewportVEnd,
                    items.Count,
                    out anchorIndex,
                    out anchorU, 
                    out anchorV);
            }

            // Check if the anchor element is not within the currently realized elements.
            var disjunct = anchorIndex < _realizedElements.FirstIndex ||
                           anchorIndex > _realizedElements.LastIndex;

            return new MeasureViewport
            {
                anchorIndex = anchorIndex,
                anchorU = anchorU,
                anchorV = anchorV,
                viewportUStart = viewportUStart,
                viewportUEnd = viewportUEnd,
                viewportVEnd = viewportVEnd,
                viewportIsDisjunct = disjunct,
            };
        }

        private Size CalculateDesiredSize(Orientation orientation, int itemCount, in MeasureViewport viewport)
        {
            var sizeU = 0.0;
            var sizeV = viewport.measuredV;

            if (viewport.lastIndex >= 0)
            {
                var remaining = itemCount - viewport.lastIndex - 1;
                sizeU = viewport.realizedEndU + (remaining * _lastEstimatedElementSizeU / _elementsPerRow);
            }

            return orientation == Orientation.Horizontal ? new(sizeU, viewport.viewportVEnd) : new(viewport.viewportVEnd, sizeU);
        }

        private Size EstimateDesiredSize(Orientation orientation, int itemCount)
        {
            if (_scrollToIndex >= 0 && _scrollToElement is not null)
            {
                // We have an element to scroll to, so we can estimate the desired size based on the
                // element's position and the remaining elements.
                var remaining = itemCount - _scrollToIndex - 1;
                var u = orientation == Orientation.Horizontal
                    ? _scrollToElement.Bounds.Right
                    : _scrollToElement.Bounds.Bottom;
                var sizeU = u + ((int)(remaining * 1.0 / _elementsPerRow) * _lastEstimatedElementSizeU );

                return orientation == Orientation.Horizontal
                    ? new(sizeU, DesiredSize.Height)
                    : new(DesiredSize.Width, sizeU);
            }

            return DesiredSize;
        }

        private double EstimateElementSizeU()
        {
            if (_realizedElements is null)
                return _lastEstimatedElementSizeU;

            var orientation = Orientation;
            var total = 0.0;
            var divisor = 0.0;

            // Average the desired size of the realized, measured elements.
            foreach (var element in _realizedElements.Elements)
            {
                if (element is null || !element.IsMeasureValid)
                    continue;
                var sizeU = orientation == Orientation.Horizontal
                    ? element.DesiredSize.Width
                    : element.DesiredSize.Height;
                total += sizeU;
                ++divisor;
            }

            // Check we have enough information on which to base our estimate.
            if (divisor == 0 || total == 0)
                return _lastEstimatedElementSizeU;

            // Store and return the estimate.
            return _lastEstimatedElementSizeU = total / divisor / _elementsPerRow;
        }

        private double EstimateElementSizeV()
        {
            if (_realizedElements is null)
                return _lastEstimatedElementSizeV;

            var orientation = Orientation;
            var total = 0.0;
            var divisor = 0;
            var viewportEndV = orientation == Orientation.Horizontal ? _viewport.Height : _viewport.Width;


            // Average the desired size of the realized, measured elements.
            foreach (var element in _realizedElements.Elements)
            {
                if (element is null || !element.IsMeasureValid)
                    continue;
                var sizeV = orientation == Orientation.Horizontal
                    ? element.DesiredSize.Height
                    : element.DesiredSize.Width;
                total += sizeV;
                divisor++;

                if (total + sizeV > viewportEndV)
                {
                    _elementsPerRow = divisor > 0 ? divisor : 1;
                    break;
                }
            }

            // Check we have enough information on which to base our estimate.
            if (divisor == 0 || total == 0)
            {
                _elementsPerRow = 1;
                return _lastEstimatedElementSizeV;
            }

            // Store and return the estimate.
            return _lastEstimatedElementSizeV = total / divisor;
        }


        private void GetOrEstimateAnchorElementForViewport(
            double viewportStartU,
            double viewportEndU,
            double viewportEndV,
            int itemCount,
            out int index,
            out double anchorU, 
            out double anchorV)
        {
            // We have no elements, or we're at the start of the viewport.
            if (itemCount <= 0 || MathUtilities.IsZero(viewportStartU))
            {
                index = 0;
                anchorU = 0;
                anchorV = 0;
                return;
            }

            // If we have realised elements and a valid StartU then try to use this information to
            // get the anchor element.
            if (_realizedElements?.StartU is { } u && !double.IsNaN(u))
            {
                var orientation = Orientation;

                var idx = 0; // helper to get the index to use
                var itemsPerRow = 0;
                var v = 0d;
                var endU = 0d;

                for (var i = 0; i < _realizedElements.Elements.Count; ++i)
                {
                    if (_realizedElements.Elements[i] is not { } element)
                        continue;

                    var sizeU = orientation == Orientation.Horizontal
                        ? element.DesiredSize.Width
                        : element.DesiredSize.Height;

                    var sizeV = orientation == Orientation.Horizontal
                        ? element.DesiredSize.Height
                        : element.DesiredSize.Width;


                    v += sizeV;


                    if (v > viewportEndV)
                    {
                        idx++;
                        v = 0;
                        endU = u + sizeU;
                    }

                    if (endU > viewportStartU && u < viewportEndU)
                    {
                        idx++;
                        itemsPerRow = (int)(viewportEndV / sizeV);
                        index = _realizedElements.FirstIndex + idx * itemsPerRow;
                        index = Math.Min(index, Items.Count - 1);
                        anchorU = u;
                        anchorV = v;
                        return;
                    }
                }
            }


            // We don't have any realized elements in the requested viewport, or can't rely on
            // StartU being valid. Estimate the index using only the estimated element size.
            var estimatedSizeU = EstimateElementSizeU();
            var estimatedSizeV = EstimateElementSizeV();

            // Estimate the element at the start of the viewport.
            var startIndex = Math.Min((int)(viewportStartU / estimatedSizeU / _elementsPerRow), itemCount - 1);
            index = startIndex;
            anchorU = (int)(startIndex * 1.0 / _elementsPerRow) * estimatedSizeU;
            anchorV = (int)(startIndex * 1.0 / _elementsPerRow) * estimatedSizeV;
        }

        private double GetOrEstimateElementU(int index, double viewportEndV)
        {
            // Return the position of the existing element if realized.
            var u = _realizedElements?.GetElementU(index) ?? double.NaN;

            if (!double.IsNaN(u))
                return u;

            // Estimate the element size.
            var estimatedSizeU = EstimateElementSizeU();
            var estimatedSizeV = EstimateElementSizeV();

            // TODO: Use _startU to work this out.
            return (int)(index * 1.0 / _elementsPerRow) * estimatedSizeU;
        }
        
        private double GetOrEstimateElementV(int index, double viewportEndV)
        {
            // Return the position of the existing element if realized.
            var v = _realizedElements?.GetElementV(index) ?? double.NaN;

            if (!double.IsNaN(v))
                return v;

            // Estimate the element size.
            var estimatedSizeU = EstimateElementSizeU();
            var estimatedSizeV = EstimateElementSizeV();

            // TODO: Use _startU to work this out.
            return (int)(index * 1.0 / _elementsPerRow) * estimatedSizeV;
        }


        private void RealizeElements(
            IReadOnlyList<object?> items,
            Size availableSize,
            ref MeasureViewport viewport)
        {
            Debug.Assert(_measureElements is not null);
            Debug.Assert(_realizedElements is not null);
            Debug.Assert(items.Count > 0);

            var index = viewport.anchorIndex;
            var horizontal = Orientation == Orientation.Horizontal;
            var u = viewport.anchorU;
            var v = viewport.anchorV;

            // If the anchor element is at the beginning of, or before, the start of the viewport
            // then we can recycle all elements before it.
            if (u <= viewport.anchorU)
                _realizedElements.RecycleElementsBefore(viewport.anchorIndex, _recycleElement);

            // Start at the anchor element and move forwards, realizing elements.
            do
            {
                _realizingIndex = index;
                var e = GetOrCreateElement(items, index);
                _realizingElement = e;
                e.Measure(availableSize);


                var sizeU = horizontal ? e.DesiredSize.Width : e.DesiredSize.Height;
                var sizeV = horizontal ? e.DesiredSize.Height : e.DesiredSize.Width;

                if (MathUtilities.IsZero(v)) v = sizeV;

                _measureElements!.Add(index, e, u, v - sizeV, sizeU, sizeV);
                viewport.measuredV = Math.Max(viewport.measuredV, sizeV);

                if (v + sizeV > viewport.viewportVEnd)
                {
                    u += sizeU;
                    v = sizeV;
                }
                else
                {
                    v += sizeV;
                }

                ++index;
                _realizingIndex = -1;
                _realizingElement = null;
            } while (u < viewport.viewportUEnd && index < items.Count);

            // Store the last index and end U position for the desired size calculation.
            viewport.lastIndex = index - 1;
            viewport.realizedEndU = u;

            // We can now recycle elements after the last element.
            _realizedElements.RecycleElementsAfter(viewport.lastIndex, _recycleElement);

            // Next move backwards from the anchor element, realizing elements.
            index = viewport.anchorIndex - 1;
            u = viewport.anchorU;

            while (u > viewport.viewportUStart && index >= 0)
            {
                var e = GetOrCreateElement(items, index);
                e.Measure(availableSize);

                var sizeU = horizontal ? e.DesiredSize.Width : e.DesiredSize.Height;
                var sizeV = horizontal ? e.DesiredSize.Height : e.DesiredSize.Width;

                if (v - sizeV > 0)
                {
                    u -= sizeU;
                    v = viewport.viewportVEnd;
                }
                else
                {
                    v -= sizeV;
                }

                _measureElements!.Add(index, e, u, v, sizeU, sizeV);
                viewport.measuredV = Math.Max(viewport.measuredV, sizeV);
                --index;
            }

            // We can now recycle elements before the first element.
            _realizedElements.RecycleElementsBefore(index + 1, _recycleElement);

            Debug.WriteLine("Created {0} elements", viewport.lastIndex - viewport.anchorIndex);
        }

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

        private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            var vertical = Orientation == Orientation.Vertical;
            var oldViewportStart = vertical ? _viewport.Top : _viewport.Left;
            var oldViewportEnd = vertical ? _viewport.Bottom : _viewport.Right;

            _viewport = e.EffectiveViewport.Intersect(new(Bounds.Size));
            _isWaitingForViewportUpdate = false;

            var newViewportStart = vertical ? _viewport.Top : _viewport.Left;
            var newViewportEnd = vertical ? _viewport.Bottom : _viewport.Right;

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

        /// <inheritdoc/>
        public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation,
            SnapPointsAlignment snapPointsAlignment)
        {
            if (_realizedElements == null)
                return new List<double>();

            return new VirtualizingWrapPanelSnapPointsList(_realizedElements, ItemsControl?.ItemsSource?.Count() ?? 0,
                orientation, Orientation, snapPointsAlignment, EstimateElementSizeU());
        }

        /// <inheritdoc/>
        public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment,
            out double offset)
        {
            offset = 0f;
            var firstRealizedChild = _realizedElements?.Elements.FirstOrDefault();

            if (firstRealizedChild == null)
            {
                return 0;
            }

            double snapPoint = 0;

            switch (Orientation)
            {
                case Orientation.Horizontal:
                    if (!AreHorizontalSnapPointsRegular)
                        throw new InvalidOperationException();

                    snapPoint = firstRealizedChild.Bounds.Width;
                    switch (snapPointsAlignment)
                    {
                        case SnapPointsAlignment.Near:
                            offset = 0;
                            break;
                        case SnapPointsAlignment.Center:
                            offset = (firstRealizedChild.Bounds.Right - firstRealizedChild.Bounds.Left) / 2;
                            break;
                        case SnapPointsAlignment.Far:
                            offset = firstRealizedChild.Bounds.Width;
                            break;
                    }

                    break;
                case Orientation.Vertical:
                    if (!AreVerticalSnapPointsRegular)
                        throw new InvalidOperationException();
                    snapPoint = firstRealizedChild.Bounds.Height;
                    switch (snapPointsAlignment)
                    {
                        case SnapPointsAlignment.Near:
                            offset = 0;
                            break;
                        case SnapPointsAlignment.Center:
                            offset = (firstRealizedChild.Bounds.Bottom - firstRealizedChild.Bounds.Top) / 2;
                            break;
                        case SnapPointsAlignment.Far:
                            offset = firstRealizedChild.Bounds.Height;
                            break;
                    }

                    break;
            }

            return snapPoint;
        }

        private struct MeasureViewport
        {
            public int anchorIndex;
            public double anchorU;
            public double anchorV;
            public double viewportUStart;
            public double viewportUEnd;
            public double viewportVEnd;
            public double measuredV;
            public double realizedEndU;
            public int lastIndex;
            public bool viewportIsDisjunct;
        }
    }
}