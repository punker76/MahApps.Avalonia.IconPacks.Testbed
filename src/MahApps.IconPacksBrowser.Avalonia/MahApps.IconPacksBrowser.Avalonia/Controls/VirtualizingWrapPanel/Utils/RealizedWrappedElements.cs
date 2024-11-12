using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;

namespace MahApps.IconPacksBrowser.Avalonia.Controls.Utils
{
    /// <summary>
    /// Stores the realized element state for a virtualizing panel that arranges its children
    /// in a stack layout, wrapping around when layout reaches the end, such as <see cref="VirtualizingWrapPanel"/>.
    /// </summary>
    internal class RealizedWrappedElements
    {
        private int _firstIndex;
        private List<Control?>? _elements;
        private List<double>? _sizesU;
        private List<double>? _sizesV;
        private double _startU;
        private double _startV;
        private bool _startUUnstable;
        private bool _startVUnstable;

        /// <summary>
        /// Gets the number of realized elements.
        /// </summary>
        public int Count => _elements?.Count ?? 0;

        /// <summary>
        /// Gets the index of the first realized element, or -1 if no elements are realized.
        /// </summary>
        public int FirstIndex => _elements?.Count > 0 ? _firstIndex : -1;

        /// <summary>
        /// Gets the index of the last realized element, or -1 if no elements are realized.
        /// </summary>
        public int LastIndex => _elements?.Count > 0 ? _firstIndex + _elements.Count - 1 : -1;

        /// <summary>
        /// Gets the elements.
        /// </summary>
        public IReadOnlyList<Control?> Elements => _elements ??= new List<Control?>();

        /// <summary>
        /// Gets the sizes of the elements on the primary axis.
        /// </summary>
        public IReadOnlyList<double> SizeU => _sizesU ??= new List<double>();
        
        /// <summary>
        /// Gets the sizes of the elements on the secondary axis.
        /// </summary>
        public IReadOnlyList<double> SizeV => _sizesV ??= new List<double>();

        /// <summary>
        /// Gets the position of the first element on the primary axis, or NaN if the position is
        /// unstable.
        /// </summary>
        public double StartU => _startUUnstable ? double.NaN : _startU;
        
        /// <summary>
        /// Gets the position of the first element on the secondary axis, or NaN if the position is
        /// unstable.
        /// </summary>
        public double StartV => _startVUnstable ? double.NaN : _startV;

        /// <summary>
        /// Adds a newly realized element to the collection.
        /// </summary>
        /// <param name="index">The index of the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="u">The position of the element on the primary axis.</param>
        /// <param name="sizeU">The size of the element on the primary axis.</param>
        public void Add(int index, Control element, double u, double v, double sizeU, double sizeV)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            _elements ??= new List<Control?>();
            _sizesU ??= new List<double>();
            _sizesV ??= new List<double>();

            if (Count == 0)
            {
                _elements.Add(element);
                _sizesU.Add(sizeU);
                _sizesV.Add(sizeV);
                _startU = u;
                _startV = v;
                _firstIndex = index;
            }
            else if (index == LastIndex + 1)
            {
                _elements.Add(element);
                _sizesU.Add(sizeU);
                _sizesV.Add(sizeV);
            }
            else if (index == FirstIndex - 1)
            {
                --_firstIndex;
                _elements.Insert(0, element);
                _sizesU.Insert(0, sizeU);
                _sizesV.Insert(0, sizeV);
                _startU = u;
                _startV = v;
            }
            else
            {
                throw new NotSupportedException("Can only add items to the beginning or end of realized elements.");
            }
        }

        /// <summary>
        /// Gets the element at the specified index, if realized.
        /// </summary>
        /// <param name="index">The index in the source collection of the element to get.</param>
        /// <returns>The element if realized; otherwise null.</returns>
        public Control? GetElement(int index)
        {
            var i = index - FirstIndex;
            if (i >= 0 && i < _elements?.Count)
                return _elements[i];
            return null;
        }

        /// <summary>
        /// Gets the position of the element with the requested index on the primary axis, if realized.
        /// </summary>
        /// <returns>
        /// The position of the element, or NaN if the element is not realized.
        /// </returns>
        public double GetElementU(int index)
        {
            if (index < FirstIndex || _sizesU is null)
                return double.NaN;

            var endIndex = index - FirstIndex;

            if (endIndex >= _sizesU.Count)
                return double.NaN;

            var u = StartU;

            for (var i = 0; i < endIndex; ++i)
                u += _sizesU[i];
            
            return u;
        }
        
        /// <summary>
        /// Gets the position of the element with the requested index on the secondary axis, if realized.
        /// </summary>
        /// <returns>
        /// The position of the element, or NaN if the element is not realized.
        /// </returns>
        public double GetElementV(int index)
        {
            if (index < FirstIndex || _sizesV is null)
                return double.NaN;

            var endIndex = index - FirstIndex;

            if (endIndex >= _sizesV.Count)
                return double.NaN;

            var u = StartU;

            for (var i = 0; i < endIndex; ++i)
                u += _sizesV[i];
            
            return u;
        }

        /// <summary>
        /// Gets the index of the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The index or -1 if the element is not present in the collection.</returns>
        public int GetIndex(Control element)
        {
            return _elements?.IndexOf(element) is int index && index >= 0 ? index + FirstIndex : -1;
        }

        /// <summary>
        /// Updates the elements in response to items being inserted into the source collection.
        /// </summary>
        /// <param name="index">The index in the source collection of the insert.</param>
        /// <param name="count">The number of items inserted.</param>
        /// <param name="updateElementIndex">A method used to update the element indexes.</param>
        public void ItemsInserted(int index, int count, Action<Control, int, int> updateElementIndex)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (_elements is null || _elements.Count == 0)
                return;

            // Get the index within the realized _elements collection.
            var first = FirstIndex;
            var realizedIndex = index - first;

            if (realizedIndex < Count)
            {
                // The insertion point affects the realized elements. Update the index of the
                // elements after the insertion point.
                var elementCount = _elements.Count;
                var start = Math.Max(realizedIndex, 0);

                for (var i = start; i < elementCount; ++i)
                {
                    if (_elements[i] is not Control element)
                        continue;
                    var oldIndex = i + first;
                    updateElementIndex(element, oldIndex, oldIndex + count);
                }

                if (realizedIndex < 0)
                {
                    // The insertion point was before the first element, update the first index.
                    _firstIndex += count;
                    _startUUnstable = true;
                    _startVUnstable = true;
                }
                else
                {
                    // The insertion point was within the realized elements, insert an empty space
                    // in _elements and _sizes.
                    _elements!.InsertMany(realizedIndex, null, count);
                    _sizesU!.InsertMany(realizedIndex, double.NaN, count);
                    _sizesV!.InsertMany(realizedIndex, double.NaN, count);
                }
            }
        }

        /// <summary>
        /// Updates the elements in response to items being removed from the source collection.
        /// </summary>
        /// <param name="index">The index in the source collection of the remove.</param>
        /// <param name="count">The number of items removed.</param>
        /// <param name="updateElementIndex">A method used to update the element indexes.</param>
        /// <param name="recycleElement">A method used to recycle elements.</param>
        public void ItemsRemoved(
            int index,
            int count,
            Action<Control, int, int> updateElementIndex,
            Action<Control> recycleElement)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (_elements is null || _elements.Count == 0)
                return;

            // Get the removal start and end index within the realized _elements collection.
            var first = FirstIndex;
            var last = LastIndex;
            var startIndex = index - first;
            var endIndex = (index + count) - first;

            if (endIndex < 0)
            {
                // The removed range was before the realized elements. Update the first index and
                // the indexes of the realized elements.
                _firstIndex -= count;
                _startUUnstable = true;
                _startVUnstable = true;

                var newIndex = _firstIndex;
                for (var i = 0; i < _elements.Count; ++i)
                {
                    if (_elements[i] is Control element)
                        updateElementIndex(element, newIndex + count, newIndex);
                    ++newIndex;
                }
            }
            else if (startIndex < _elements.Count)
            {
                // Recycle and remove the affected elements.
                var start = Math.Max(startIndex, 0);
                var end = Math.Min(endIndex, _elements.Count);

                for (var i = start; i < end; ++i)
                {
                    if (_elements[i] is Control element)
                    {
                        _elements[i] = null;
                        recycleElement(element);
                    }
                }

                _elements.RemoveRange(start, end - start);
                _sizesU!.RemoveRange(start, end - start);
                _sizesV!.RemoveRange(start, end - start);

                // If the remove started before and ended within our realized elements, then our new
                // first index will be the index where the remove started. Mark StartU as unstable
                // because we can't rely on it now to estimate element heights.
                if (startIndex <= 0 && end < last)
                {
                    _firstIndex = first = index;
                    _startUUnstable = true;
                    _startVUnstable = true;
                }

                // Update the indexes of the elements after the removed range.
                end = _elements.Count;
                var newIndex = first + start;
                for (var i = start; i < end; ++i)
                {
                    if (_elements[i] is Control element)
                        updateElementIndex(element, newIndex + count, newIndex);
                    ++newIndex;
                }
            }
        }

        /// <summary>
        /// Updates the elements in response to items being replaced in the source collection.
        /// </summary>
        /// <param name="index">The index in the source collection of the remove.</param>
        /// <param name="count">The number of items removed.</param>
        /// <param name="recycleElement">A method used to recycle elements.</param>
        public void ItemsReplaced(int index, int count, Action<Control> recycleElement)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (_elements is null || _elements.Count == 0)
                return;

            // Get the index within the realized _elements collection.
            var startIndex = index - FirstIndex;
            var endIndex = Math.Min(startIndex + count, Count);

            if (startIndex >= 0 && endIndex > startIndex)
            {
                for (var i = startIndex; i < endIndex; ++i)
                {
                    if (_elements[i] is { } element)
                    {
                        recycleElement(element);
                        _elements[i] = null;
                        _sizesU![i] = double.NaN;
                        _sizesV![i] = double.NaN;
                    }
                }
            }
        }

        /// <summary>
        /// Recycles all elements in response to the source collection being reset.
        /// </summary>
        /// <param name="recycleElement">A method used to recycle elements.</param>
        public void ItemsReset(Action<Control> recycleElement)
        {
            if (_elements is null || _elements.Count == 0)
                return;

            for (var i = 0; i < _elements.Count; i++)
            {
                if (_elements[i] is Control e)
                {
                    _elements[i] = null;
                    recycleElement(e);
                }
            }

            _startU = _startV = _firstIndex = 0;
            _elements?.Clear();
            _sizesU?.Clear();
            _sizesV?.Clear();

        }

        /// <summary>
        /// Recycles elements before a specific index.
        /// </summary>
        /// <param name="index">The index in the source collection of new first element.</param>
        /// <param name="recycleElement">A method used to recycle elements.</param>
        public void RecycleElementsBefore(int index, Action<Control, int> recycleElement)
        {
            if (index <= FirstIndex || _elements is null || _elements.Count == 0)
                return;

            if (index > LastIndex)
            {
                RecycleAllElements(recycleElement);
            }
            else
            {
                var endIndex = index - FirstIndex;

                for (var i = 0; i < endIndex; ++i)
                {
                    if (_elements[i] is Control e)
                    {
                        _elements[i] = null;
                        recycleElement(e, i + FirstIndex);
                    }
                }

                _elements.RemoveRange(0, endIndex);
                _sizesU!.RemoveRange(0, endIndex);
                _sizesV!.RemoveRange(0, endIndex);
                _firstIndex = index;
            }
        }

        /// <summary>
        /// Recycles elements after a specific index.
        /// </summary>
        /// <param name="index">The index in the source collection of new last element.</param>
        /// <param name="recycleElement">A method used to recycle elements.</param>
        public void RecycleElementsAfter(int index, Action<Control, int> recycleElement)
        {
            if (index >= LastIndex || _elements is null || _elements.Count == 0)
                return;

            if (index < FirstIndex)
            {
                RecycleAllElements(recycleElement);
            }
            else
            {
                var startIndex = (index + 1) - FirstIndex;
                var count = _elements.Count;

                for (var i = startIndex; i < count; ++i)
                {
                    if (_elements[i] is Control e)
                    {
                        _elements[i] = null;
                        recycleElement(e, i + FirstIndex);
                    }
                }

                _elements.RemoveRange(startIndex, _elements.Count - startIndex);
                _sizesU!.RemoveRange(startIndex, _sizesU.Count - startIndex);
                _sizesV!.RemoveRange(startIndex, _sizesV.Count - startIndex);
            }
        }

        /// <summary>
        /// Recycles all realized elements.
        /// </summary>
        /// <param name="recycleElement">A method used to recycle elements.</param>
        public void RecycleAllElements(Action<Control, int> recycleElement)
        {
            if (_elements is null || _elements.Count == 0)
                return;

            for (var i = 0; i < _elements.Count; i++)
            {
                if (_elements[i] is Control e)
                {
                    _elements[i] = null;
                    recycleElement(e, i + FirstIndex);
                }
            }

            _startU = _startV = _firstIndex = 0;
            _elements?.Clear();
            _sizesU?.Clear();
            _sizesV?.Clear();
        }

        /// <summary>
        /// Resets the element list and prepares it for reuse.
        /// </summary>
        public void ResetForReuse()
        {
            if (_elements is null || _elements.Count == 0)
                return;

            for (var i = 0; i < _elements.Count; i++)
            {
                if (_elements[i] is Control e)
                {
                    _elements[i] = null;
                }
            }
            
            _startU = _startV = _firstIndex = 0;
            _startUUnstable = false;
            _startVUnstable = false;
            _elements?.Clear();
            _sizesU?.Clear();
            _sizesV?.Clear();
        }

        /// <summary>
        /// Validates that <see cref="StartU"/> is still valid.
        /// </summary>
        /// <param name="orientation">The panel orientation.</param>
        /// <remarks>
        /// If the U size of any element in the realized elements has changed, then the value of
        /// <see cref="StartU"/> should be considered unstable.
        /// </remarks>
        public void ValidateStartU(Orientation orientation)
        {
            if (_elements is null || _sizesU is null || _startUUnstable)
                return;

            for (var i = 0; i < _elements.Count; ++i)
            {
                if (_elements[i] is not { } element)
                    continue;

                var sizeU = orientation == Orientation.Horizontal
                    ? element.DesiredSize.Width
                    : element.DesiredSize.Height;

                if (Math.Abs(sizeU - _sizesU[i]) > 0.01)
                {
                    _startUUnstable = true;
                    break;
                }
            }
        }
        
        /// <summary>
        /// Validates that <see cref="StartU"/> is still valid.
        /// </summary>
        /// <param name="orientation">The panel orientation.</param>
        /// <remarks>
        /// If the U size of any element in the realized elements has changed, then the value of
        /// <see cref="StartU"/> should be considered unstable.
        /// </remarks>
        public void ValidateStartV(Orientation orientation)
        {
            if (_elements is null || _sizesV is null || _startVUnstable)
                return;

            for (var i = 0; i < _elements.Count; ++i)
            {
                if (_elements[i] is not { } element)
                    continue;

                var sizeV = orientation == Orientation.Horizontal
                    ? element.DesiredSize.Height
                    : element.DesiredSize.Width;

                if (sizeV != _sizesV[i])
                {
                    _startVUnstable = true;
                    break;
                }
            }
        }
    }
}