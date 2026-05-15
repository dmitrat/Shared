namespace OutWit.Shared.Blazor.Shell.Model
{
    /// <summary>
    /// Represents a menu item in a login/profile dropdown.
    /// Defined here (rather than in any product-specific package) because
    /// product-side login UI components — e.g. WitIdentity's LoginDisplay —
    /// accept a list of these as a parameter.
    /// </summary>
    /// <param name="Label">Display text for the menu item.</param>
    /// <param name="Href">Navigation target URL.</param>
    /// <param name="Icon">MudBlazor icon string (e.g., <c>Icons.Material.Filled.Dashboard</c>).</param>
    public sealed record LoginMenuItem(string Label, string Href, string Icon);
}
