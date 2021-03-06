#if (NETFX_CORE || WINDOWS_UWP)
using Windows.UI.Xaml.Controls;
#elif AVALONIA
using Avalonia.Controls.Primitives;
#else
using System.Windows.Controls;
#endif

namespace MahApps.Metro.IconPacks
{
#if AVALONIA
    public abstract class PackIconBase : TemplatedControl
#else
    public abstract class PackIconBase : Control
#endif
    {
        protected internal abstract void SetKind<TKind>(TKind iconKind);
        protected abstract void UpdateData();
    }
}