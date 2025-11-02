using Avalonia;
using Avalonia.Controls.Primitives;

namespace MahApps.IconPacksBrowser.Avalonia.Controls;

public class HeaderedControl : HeaderedContentControl
{
    /// <summary>
    /// Defines the <see cref="HeaderPosition" /> property
    /// </summary>
    public static readonly StyledProperty<HeaderPosition> HeaderPositionProperty =
        AvaloniaProperty.Register<HeaderedControl, HeaderPosition>(nameof(HeaderPosition));

    /// <summary>
    /// Gets or sets the HeaderPosition
    /// </summary>
    public HeaderPosition HeaderPosition
    {
        get { return GetValue(HeaderPositionProperty); }
        set { SetValue(HeaderPositionProperty, value); }
    }
}

public enum HeaderPosition
{
    Left, Top
}