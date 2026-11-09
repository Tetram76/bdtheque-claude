using System.Globalization;
using Bdtheque.Frontend.Audience;
using Microsoft.AspNetCore.WebUtilities;

namespace Bdtheque.Frontend.Navigation;

/// <summary>
/// The state of the record of an album (visuel/fiche-album/contenu.md) its address carries, the page being rendered
/// statically: the edition chosen, whose detail and visuals the record shows, and the visual shown. Each link that changes
/// it leads to the record in its new state, without anchor: the enhanced navigation of Blazor keeps the page where it was,
/// whereas an anchor would scroll it to the part named. Nothing else is carried:
/// unfolding the authors of a role is a disclosure of the page itself, like the panel of the filters of a list.
/// </summary>
/// <param name="Edition">The edition chosen; the first one of the row when none, or when it is not one of the album's.</param>
/// <param name="Visual">The position of the visual shown among those of the edition, from 1; the first one when none.</param>
public sealed record AlbumRecordQuery(Guid? Edition = null, int? Visual = null)
{
    internal const string EditionName = "edition";
    internal const string VisualName = "visual";

    /// <summary>The record with an edition chosen, from its first visual.</summary>
    public static AlbumRecordQuery ForEdition(Guid edition) => new(edition);

    /// <summary>The record with a visual of an edition shown, the edition being always named, whichever the default.</summary>
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
    public string Href(Guid albumId)
    {
        var parameters = Parameters();
        if (parameters.Count == 0)
            return AppPaths.Album(albumId);
        return $"{AppPaths.Album(albumId)}?{string.Join('&', parameters.Select(p => $"{p.Name}={p.Value}"))}";
    }

    /// <summary>
    /// The action the request of this address records for the audience measurement (fonctionnel/mesure-audience.md,
    /// Fonctions suivies), with the state as its parameters: a visual shown, which every link to a visual names; otherwise
    /// an edition chosen, which only the link to an edition names without visual. None for the record as it opens.
    /// </summary>
    public (AudienceFunction Function, string Parameters)? Action
    {
        get
        {
            var parameters = Parameters();
            if (parameters.Count == 0)
                return null;
            var function = Visual is null ? AudienceFunctions.AlbumRecordEdition : AudienceFunctions.AlbumRecordVisual;
            return (function, string.Join('&', parameters.Select(p => $"{p.Name}={p.Value}")));
        }
    }

    private List<(string Name, string Value)> Parameters()
    {
        var parameters = new List<(string, string)>();
        if (Edition is { } edition)
            parameters.Add((EditionName, edition.ToString()));
        if (Visual is { } visual)
            parameters.Add((VisualName, visual.ToString(CultureInfo.InvariantCulture)));
        return parameters;
    }
}
