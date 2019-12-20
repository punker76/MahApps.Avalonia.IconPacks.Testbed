using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaIconPacks
{
    /// <summary>
    /// The default Avalonia theme.
    /// </summary>
    public class DefaultTheme : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Default"/> class.
        /// </summary>
        public DefaultTheme()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
