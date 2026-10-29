extern alias frontend;

using frontend::Bdtheque.Frontend.Components.Layout;
using frontend::Bdtheque.Frontend.Components.Navigation;
using frontend::Bdtheque.Frontend.Components.Presentation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bdtheque.EndToEnd.Tests.Headers;

/// <summary>
/// A page of the tests only, framed by the header of the administration, which presents the navigation shared by the
/// lists — fil d'Ariane, navigation par initiale, pagination — the way a list of the administration lays them out
/// (visuel/liste-albums.md § Contenu), before any list uses them. Unlike the pages of the administration, it needs no
/// login, which comes with #199. Routed by the application through <c>PageAssemblies</c>; written in C#, the alias of the
/// frontend not being reachable from Razor.
/// </summary>
[Route(Path)]
[Layout(typeof(AdminLayout))]
public sealed class AdminFramePage : ComponentBase
{
    public const string Path = "/test/admin-frame";

    public const int TotalCount = 2847;

    /// <summary>The entry of the navigation par initiale, none for the whole list.</summary>
    [SupplyParameterFromQuery(Name = "entry")]
    public string? Entry { get; set; }

    [SupplyParameterFromQuery(Name = "page")]
    public int? Page { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Breadcrumb>(0);
        builder.AddComponentParameter(1, nameof(Breadcrumb.Items),
            (IReadOnlyList<BreadcrumbItem>)[new BreadcrumbItem("Administration", "/admin"), new BreadcrumbItem("Albums")]);
        builder.CloseComponent();

        builder.OpenElement(10, "h1");
        builder.AddContent(11, "Albums");
        builder.CloseElement();

        builder.OpenComponent<Block>(20);
        builder.AddComponentParameter(21, nameof(Block.Title), "Liste");
        builder.AddComponentParameter(22, nameof(Block.ChildContent), (RenderFragment)List);
        builder.CloseComponent();
    }

    private void List(RenderTreeBuilder builder)
    {
        builder.OpenComponent<InitialNavigation>(0);
        builder.AddComponentParameter(1, nameof(InitialNavigation.Current), Entry);
        builder.AddComponentParameter(2, nameof(InitialNavigation.EntryHref),
            (Func<string?, string>)(entry => entry is null ? Path : $"{Path}?entry={Uri.EscapeDataString(entry)}"));
        builder.CloseComponent();

        builder.OpenComponent<Pagination>(10);
        builder.AddComponentParameter(11, nameof(Pagination.Number), Page ?? 1);
        builder.AddComponentParameter(12, nameof(Pagination.Size), 50);
        builder.AddComponentParameter(13, nameof(Pagination.TotalCount), TotalCount);
        builder.AddComponentParameter(14, nameof(Pagination.ItemsLabel), "Albums");
        builder.AddComponentParameter(15, nameof(Pagination.PageHref), (Func<int, string>)(page => $"{Path}?page={page}"));
        builder.CloseComponent();
    }
}
