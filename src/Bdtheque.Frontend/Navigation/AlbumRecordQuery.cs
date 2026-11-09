using System.Globalization;
using Bdtheque.Frontend.Audience;
using Microsoft.AspNetCore.WebUtilities;

namespace Bdtheque.Frontend.Navigation;

/// <summary>
/// The state of the record of an album (visuel/fiche-album/contenu.md) its address carries, the page being rendered
/// statically: the visual open in the viewer, a dialog above the record, named by its edition and its position. Each link
/// of a visual leads to the record with the viewer open on it, and its closing to the record alone: the enhanced navigation
/// of Blazor keeps the page where it was, the dialog changing nothing under it. Nothing else is carried: unfolding the
/// authors of a role is a disclosure of the page itself, like the panel of the filters of a list.
/// </summary>
/// <param name="Edition">The edition of the visual open; none open without it, or when it is not one of the collection.</param>
/// <param name="Visual">The position of the visual open among those of the edition, from 1; none open without it.</param>
public sealed record AlbumRecordQuery(Guid? Edition = null, int? Visual = null)
{
    internal const string EditionName = "edition";
    internal const string VisualName = "visual";

    /// <summary>The record with the viewer open on a visual of an edition.</summary>
    public static AlbumRecordQuery ForVisual(Guid edition, int position) => new(edition, position);

    /// <summary>
    /// The state the address of the record carries (its query, with or without its <c>?</c>); what it cannot read, which only
    /// an address written by hand carries, is left out.
    /// </summary>
    public static AlbumRecordQuery Parse(string query)
    {
        var values = QueryHelpers.ParseQuery(query);
        Guid? edition = values.TryGetValue(EditionName, out var e) && Guid.TryParse(e, out var id) ? id : null;
        int? visual = values.TryGetValue(VisualName, out var v) && int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n >= 1
            ? n
            : null;
        return new AlbumRecordQuery(edition, visual);
    }

    /// <summary>The address of the record of the album in this state.</summary>
    public string Href(Guid albumId) => Parameters is { } parameters ? $"{AppPaths.Album(albumId)}?{parameters}" : AppPaths.Album(albumId);

    /// <summary>
    /// The action the request of this address records for the audience measurement (fonctionnel/mesure-audience.md,
    /// Fonctions suivies), with the visual as its parameters: a visual shown in the viewer. None for the record alone.
    /// </summary>
    public (AudienceFunction Function, string Parameters)? Action =>
        Parameters is { } parameters ? (AudienceFunctions.AlbumRecordVisual, parameters) : null;

    // Both or nothing: a visual is named by its edition and its position.
    private string? Parameters => Edition is { } edition && Visual is { } visual
        ? string.Create(CultureInfo.InvariantCulture, $"{EditionName}={edition}&{VisualName}={visual}")
        : null;
}
