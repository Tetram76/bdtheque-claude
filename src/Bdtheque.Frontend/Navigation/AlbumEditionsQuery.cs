using Bdtheque.Frontend.Audience;
using Microsoft.AspNetCore.WebUtilities;

namespace Bdtheque.Frontend.Navigation;

/// <summary>
/// The dialog of the page of the editions and purchase intents of an album (visuel/editions-album/contenu.md) its address
/// carries, the page being rendered statically like the lists of the administration (choix-implementation/socle/frontend.md):
/// the deletion of an edition, or the removal of an intent. Each link of a row leads to the
/// page with its dialog, and the closing of the dialog to the page alone. A single dialog at a time.
/// </summary>
/// <param name="DeleteEdition">The edition whose deletion the dialog confirms.</param>
/// <param name="RemoveIntent">The intent whose removal the dialog confirms.</param>
public sealed record AlbumEditionsQuery(Guid? DeleteEdition = null, Guid? RemoveIntent = null)
{
    internal const string DeleteEditionName = "delete-edition";
    internal const string RemoveIntentName = "remove-intent";

    /// <summary>The page with the dialog of the deletion of an edition.</summary>
    public static AlbumEditionsQuery ForEditionDeletion(Guid edition) => new(DeleteEdition: edition);

    /// <summary>The page with the dialog of the removal of an intent.</summary>
    public static AlbumEditionsQuery ForIntentRemoval(Guid intent) => new(RemoveIntent: intent);

    /// <summary>
    /// The dialog the address of the page carries (its query, with or without its <c>?</c>); what it cannot read, which only an
    /// address written by hand carries, is left out, and of several dialogs, only the first.
    /// </summary>
    public static AlbumEditionsQuery Parse(string query)
    {
        var values = QueryHelpers.ParseQuery(query);
        if (values.TryGetValue(DeleteEditionName, out var edition) && Guid.TryParse(edition, out var editionId))
            return ForEditionDeletion(editionId);
        if (values.TryGetValue(RemoveIntentName, out var intent) && Guid.TryParse(intent, out var intentId))
            return ForIntentRemoval(intentId);
        return new AlbumEditionsQuery();
    }

    /// <summary>The address of the page of the album with this dialog.</summary>
    public string Href(Guid albumId) =>
        Parameters is { } parameters ? $"{AppPaths.AdminAlbumEditions(albumId)}?{parameters}" : AppPaths.AdminAlbumEditions(albumId);

    /// <summary>
    /// The action the request of this address records for the audience measurement (fonctionnel/mesure-audience.md, Fonctions
    /// suivies): the dialog opened, with what it is about as its parameters. None for the page alone.
    /// </summary>
    public (AudienceFunction Function, string Parameters)? Action(Guid albumId)
    {
        var function = this switch
        {
            { DeleteEdition: not null } => AudienceFunctions.AdminAlbumEditionsEditionDeletion,
            { RemoveIntent: not null } => AudienceFunctions.AdminAlbumEditionsIntentRemoval,
            _ => null,
        };
        // The album too, so that the same dialog on two albums reads as two parameters.
        return function is null ? null : (function, $"album={albumId}&{Parameters}");
    }

    private string? Parameters => this switch
    {
        { DeleteEdition: { } edition } => $"{DeleteEditionName}={edition}",
        { RemoveIntent: { } intent } => $"{RemoveIntentName}={intent}",
        _ => null,
    };
}
