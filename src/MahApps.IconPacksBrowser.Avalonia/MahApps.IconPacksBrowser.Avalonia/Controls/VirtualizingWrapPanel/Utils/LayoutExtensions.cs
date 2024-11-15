using System.Reflection;
using Avalonia.Layout;

namespace MahApps.IconPacksBrowser.Avalonia.Controls.Utils;

public static class LayoutExtensions
{
    public static void ExecuteLayoutPass(this ILayoutRoot root)
    {
        var layoutManager = root.GetType().GetProperty("LayoutManager", BindingFlags.NonPublic); 
        layoutManager?.GetType().GetMethod("ExecuteLayoutPass", BindingFlags.Public)?.Invoke(root, null);
    }
}