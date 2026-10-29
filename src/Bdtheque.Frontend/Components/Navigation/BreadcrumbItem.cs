namespace Bdtheque.Frontend.Components.Navigation;

/// <summary>A level of a <see cref="Breadcrumb"/>: its label, and the page it leads to, absent for the current page.</summary>
public sealed record BreadcrumbItem(string Label, string? Href = null);
