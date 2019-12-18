#if (NETFX_CORE || WINDOWS_UWP)
using Windows.UI.Xaml.Controls;
#elif AVALONIA
using Avalonia.Controls.Primitives;
#else
using System.Windows.Controls;
#endif

namespace MahApps.Metro.IconPacks
{
    public abstract class PackIconBase : TemplatedControl
    {
        protected internal abstract void SetKind<TKind>(TKind iconKind);
        protected abstract void UpdateData();
    }
}