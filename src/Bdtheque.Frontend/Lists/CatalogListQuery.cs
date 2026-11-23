using Bdtheque.Frontend.Audience;

namespace Bdtheque.Frontend.Lists;

/// <summary>
/// The criteria of a list of the consultation, which its address carries (<see cref="ListQuery"/>): its navigation by
/// initial, its page and its own filters, among which the filter that limits it to the collection. A filter of a list of the
/// consultation is its own, and is kept only as long as the list: its navigation, its pagination and the return of the
/// browser keep it, the entry of the list in the menu of the consultation opens the list on the whole base
/// (fonctionnel/regles-metier/perimetre-de-la-consultation.md). Each list adds its filters, its paths and the functions the
/// audience measurement follows for it.
/// </summary>
/// <param name="Entry">The entry of the navigation by initial; <c>null</c> for « Toutes ».</param>
/// <param name="Page">The page of the list, from 1.</param>
public abstract record CatalogListQuery(string? Entry, int Page) : ListQuery(null, Entry, Page)
{
    /// <summary>The functions the audience measurement follows for the actions of the list.</summary>
    protected abstract CatalogListFunctions Functions { get; }

    /// <summary>
    /// The action of the list the request of this address records for the audience measurement
    /// (fonctionnel/mesure-audience.md, Fonctions suivies): that of its criteria (<see cref="ListQuery.CriteriaAction"/>), a
    /// list of the consultation having no search of its own. None for the whole list.
    /// </summary>
    public (AudienceFunction Function, string? Parameters)? Action =>
        CriteriaAction(null, Functions.Filter, Functions.Initial, Functions.Page);
}

/// <summary>The functions the audience measurement follows for the actions of a list of the consultation.</summary>
public sealed record CatalogListFunctions(AudienceFunction Filter, AudienceFunction Initial, AudienceFunction Page);
