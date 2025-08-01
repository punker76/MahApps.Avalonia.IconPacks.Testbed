using Avalonia;
using Avalonia.Controls;

namespace MahApps.IconPacksBrowser.Avalonia.Controls;

public partial class CopyToClipboardTextBox : UserControl
{
    public CopyToClipboardTextBox()
    {
        InitializeComponent();
    }
    
    /// <summary>Identifies the <see cref="TextToCopy"/> dependency property.</summary>
    public static readonly DirectProperty<CopyToClipboardTextBox, string?> TextToCopyProperty
        = AvaloniaProperty.RegisterDirect<CopyToClipboardTextBox, string?>(
            nameof(TextToCopy), 
            o => o.TextToCopy, 
            (o, v) => o.TextToCopy = v);

    private string? _textToCopy;
    public string? TextToCopy
    {
        get => _textToCopy;
        set => SetAndRaise(TextToCopyProperty, ref _textToCopy, value);
    }

}