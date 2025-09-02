namespace ERP.SharedKernel.Contracts;

/// <summary>
/// Represents a navigation item in the application menu.
/// </summary>
public class NavigationItem
{
    /// <summary>
    /// Gets or sets the display text for the navigation item.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL/path for the navigation item.
    /// </summary>
    public string Href { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon CSS class for the navigation item.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the order/priority of this navigation item (lower numbers appear first).
    /// </summary>
    public int Order { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether this navigation item is currently active/selected.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the child navigation items (for nested menus).
    /// </summary>
    public List<NavigationItem> Children { get; set; } = new();
}

/// <summary>
/// Interface for plugins that want to contribute navigation items to the main application menu.
/// </summary>
public interface INavigationProvider
{
    /// <summary>
    /// Gets the navigation items that this plugin contributes to the main menu.
    /// </summary>
    /// <returns>A collection of navigation items.</returns>
    IEnumerable<NavigationItem> GetNavigationItems();
}