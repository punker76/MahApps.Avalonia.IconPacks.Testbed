using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MahApps.IconPacksBrowser.Avalonia.Controls.Utils;

internal static class CollectionUtils
{
    public static bool Contains(this IEnumerable items, object item)
    {
        return items.IndexOf(item) != -1;
    }

    public static bool TryGetCountFast(this IEnumerable? items, out int count)
    {
        if (items != null)
        {
            if (items is ICollection collection)
            {
                count = collection.Count;
                return true;
            }
            else if (items is IReadOnlyCollection<object> readOnly)
            {
                count = readOnly.Count;
                return true;
            }
        }

        count = 0;
        return false;
    }

    public static int Count(this IEnumerable? items)
    {
        if (TryGetCountFast(items, out var count))
        {
            return count;
        }
        else if (items != null)
        {
            return Enumerable.Count(items.Cast<object>());
        }
        else
        {
            return 0;
        }
    }

    public static int IndexOf(this IEnumerable items, object item)
    {
        _ = items ?? throw new ArgumentNullException(nameof(items));

        var list = items as IList;

        if (list != null)
        {
            return list.IndexOf(item);
        }
        else
        {
            int index = 0;

            foreach (var i in items)
            {
                if (ReferenceEquals(i, item))
                {
                    return index;
                }

                ++index;
            }

            return -1;
        }
    }

    public static object? ElementAt(this IEnumerable items, int index)
    {
        _ = items ?? throw new ArgumentNullException(nameof(items));

        var list = items as IList;

        if (list != null)
        {
            return list[index];
        }
        else
        {
            return Enumerable.ElementAt(items.Cast<object>(), index);
        }
    }
    
    public static NotifyCollectionChangedEventArgs ResetEventArgs { get; } = new(NotifyCollectionChangedAction.Reset);

    public static void InsertMany<T>(this List<T> list, int index, T item, int count)
    {
        var repeat = FastRepeat<T>.Instance;
        repeat.Count = count;
        repeat.Item = item;
        list.InsertRange(index, FastRepeat<T>.Instance);
        repeat.Item = default;
    }

    private class FastRepeat<T> : ICollection<T>
    {
        public static readonly FastRepeat<T> Instance = new();
        public int Count { get; set; }
        public bool IsReadOnly => true;
        [AllowNull] public T Item { get; set; }
        public void Add(T item) => throw new NotImplementedException();
        public void Clear() => throw new NotImplementedException();
        public bool Contains(T item) => throw new NotImplementedException();
        public bool Remove(T item) => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();

        public void CopyTo(T[] array, int arrayIndex)
        {
            var end = arrayIndex + Count;

            for (var i = arrayIndex; i < end; ++i)
            {
                array[i] = Item;
            }
        }
    }
}