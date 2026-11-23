using Bdtheque.Contracts.Admin;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of the list of the albums of the consultation, which its address carries (<see cref="CatalogListQuery"/>):
/// its navigation by initial, its page and the possession of the album, the filter that limits the list to the collection
/// (fonctionnel/regles-metier/perimetre-de-la-consultation.md).
/// </summary>
/// <param name="State">The possession kept, among the states of an album; <c>null</c> for all the albums.</param>
public sealed record CatalogAlbumListQuery(string? Entry = null, AlbumCollectionState? State = null, int Page = 1) : CatalogListQuery(Entry, Page)
{
    internal const string StateName = "state";

    private static readonly CatalogListFunctions AlbumFunctions = new(
        AudienceFunctions.AlbumsFilter, AudienceFunctions.AlbumsInitial, AudienceFunctions.AlbumsPage);

    /// <summary>The whole list, as the menu of the consultation opens it.</summary>
    public static readonly CatalogAlbumListQuery All = new();

    protected override string Path => AppPaths.Albums;

    protected override string ApiRoute => "/catalog/albums";

    protected override CatalogListFunctions Functions => AlbumFunctions;

    protected override IEnumerable<(string Name, object? Value)> Filters => [(StateName, State)];

    /// <summary>
    /// The list on another possession, or on all the albums for <c>null</c>, back on its first page, under the same entry:
    /// each choice of the filter.
    /// </summary>
    public CatalogAlbumListQuery WithState(AlbumCollectionState? state) => this with { State = state, Page = 1 };

    /// <inheritdoc cref="ListQuery.Address"/>
    public static CatalogAlbumListQuery Parse(string query)
    {
        var address = new Address(query);
        return new CatalogAlbumListQuery(address.Entry, address.Enum<AlbumCollectionState>(StateName), address.Page);
    }
}
