using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using MahApps.IconPacksBrowser.Avalonia.Controls.Utils;

namespace MahApps.IconPacksBrowser.Avalonia.Controls;

internal class ItemContainerManagerItemsChangedEventArgs
{
    public NotifyCollectionChangedAction Action { get; }

    public ItemContainerManagerItemsChangedEventArgs(NotifyCollectionChangedAction action)
    {
        Action = action;
    }
}

internal class ItemContainerManager
{
    /// <summary>
    /// Occurs when the <see cref="Items"/> collection changes.
    /// </summary>
    public event EventHandler<ItemContainerManagerItemsChangedEventArgs>? ItemsChanged;

    /// <summary>
    /// Indicates whether containers are recycled or not.
    /// </summary>
    public bool IsRecycling { get; set; }

    /// <summary>
    /// Collection that contains the items for which containers are generated.
    /// </summary>
    public ItemCollection Items => itemsPanel.GetItems()!;

    /// <summary>
    /// Dictionary that contains the realised containers. The keys are the items, the values are the containers.
    /// </summary>
    public IReadOnlyDictionary<object, Control> RealizedContainers => realizedContainers;

    /// <summary>
    /// Collection that contains the cached containers. Always emtpy if <see cref="IsRecycling"/> is false.
    /// </summary>
    public IReadOnlyCollection<Control> CachedContainers => cachedContainers;

    private readonly Dictionary<object, Control> realizedContainers = new Dictionary<object, Control>();

    private readonly HashSet<Control> cachedContainers = new HashSet<Control>();

    private readonly VirtualizingWrapPanel itemsPanel;

    private readonly Action<Control> addInternalChild;
    private readonly Action<Control> removeInternalChild;

    public ItemContainerManager(
        VirtualizingWrapPanel panel, 
        
        Action<Control> addInternalChild,
        Action<Control> removeInternalChild)
    {
        this.itemsPanel = panel;
        this.addInternalChild = addInternalChild;
        this.removeInternalChild = removeInternalChild;
        Items.CollectionChanged += ItemContainerGenerator_ItemsChanged;
    }

    public Control Realize(int itemIndex)
    {
        var item = Items[itemIndex];

        if (realizedContainers.TryGetValue(item, out var existingContainer))
        {
            return existingContainer;
        }
        
        var container = itemsPanel.GetOrCreateElement(Items, itemIndex);

        cachedContainers.Remove(container);
        realizedContainers.Add(item, container);

       // addInternalChild(container);

        return container;
    }

    public int GetIndex(Control element)
    {
        return realizedContainers.Values.IndexOf(element) is int index && index >= 0 ? index : -1;
    }

    
    public void Virtualize(Control container)
    {
        int itemIndex = itemsPanel.GetIndexFromContainer(container);

        if (itemIndex == -1) // the item is already virtualized (can happen when grouping)
        {
            realizedContainers.Remove(realizedContainers.Where(entry => entry.Value == container).Single().Key);

            if (IsRecycling)
            {
                cachedContainers.Add(container);
            }
            else
            {
                removeInternalChild(container);
            }

            return;
        }

        var item = Items[itemIndex];

        // TODO var generatorPosition = itemsControl. recyclingItemContainerGenerator.GeneratorPositionFromIndex(itemIndex);

        if (IsRecycling)
        {
            realizedContainers.Remove(item);
            cachedContainers.Add(container);
        }
        else
        {
            realizedContainers.Remove(item);
            removeInternalChild(container);
        }
    }

    // public int FindItemIndexOfContainer(Control container)
    // {
    //     return  Items.IndexOf(itemsPanel.GetIndexFromContainer(container));
    // }

    private void ItemContainerGenerator_ItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            realizedContainers.Clear();
            cachedContainers.Clear();
            // children collection is cleared automatically

            ItemsChanged?.Invoke(this, new ItemContainerManagerItemsChangedEventArgs(e.Action));
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove
                 || e.Action == NotifyCollectionChangedAction.Replace)
        {
            foreach (var (item, container) in realizedContainers.Where(entry => !Items.Contains(entry.Key)).ToList())
            {
                realizedContainers.Remove(item);

                if (IsRecycling)
                {
                    cachedContainers.Add(container);
                }
                else
                {
                    removeInternalChild(container);
                }
            }

            ItemsChanged?.Invoke(this, new ItemContainerManagerItemsChangedEventArgs(e.Action));
        }
        else
        {
            ItemsChanged?.Invoke(this, new ItemContainerManagerItemsChangedEventArgs(e.Action));
        }
    }

    private static void InvalidateMeasureRecursively(Control element)
    {
        element.InvalidateMeasure();

        foreach (var child in element.GetVisualChildren())
        {
            if (child is Control control)
            {
                InvalidateMeasureRecursively(control);
            }
        }
    }

    public Control? GetRealizedElement(int index)
    {
        var item = Items[index];

        return item is not null ? realizedContainers.GetValueOrDefault(item) : null;
    }
}