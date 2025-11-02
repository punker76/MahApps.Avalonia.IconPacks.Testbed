using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using FluentAvalonia.UI.Controls;
using MahApps.IconPacksBrowser.Avalonia.ViewModels;

namespace MahApps.IconPacksBrowser.Avalonia.Helper;

public class MenuItemTemplateSelector : DataTemplateSelector
{
    [Content]
    public required IDataTemplate ItemTemplate { get; set; }

    public required IDataTemplate SeparatorTemplate { get; set; }

    protected override IDataTemplate SelectTemplateCore(object item)
    {
        return item is  SeparatorNavigationItemViewModel ? SeparatorTemplate : ItemTemplate;
    }
}