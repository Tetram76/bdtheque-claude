using Microsoft.AspNetCore.Mvc;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The search, navigation by initial and cross filters of the public list of the purchase intents: those of the list of the
/// albums, applied to the album of each intent, without the possession, of no interest there
/// (fonctionnel/regles-metier/perimetre-de-la-consultation.md). Bound as a whole from the query of the request
/// (<c>[AsParameters]</c>), like <see cref="AlbumFilters"/>, whose criteria they are.
/// </summary>
internal sealed record PurchaseIntentFilters(
    [FromQuery(Name = "q")] string? Text, string? Entry, Guid? SeriesId, Guid? AuthorId, Guid? PublisherId, Guid? PublisherCollectionId, Guid? GenreId,
    Guid? UniverseId)
{
    /// <summary>The same criteria, as the list of the albums applies them to its albums.</summary>
    public AlbumFilters OfAlbums => new(Text, Entry, SeriesId, AuthorId, PublisherId, PublisherCollectionId, GenreId, UniverseId, State: null);
}
