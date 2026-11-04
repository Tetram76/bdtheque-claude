namespace Bdtheque.Domain.Entities;

/// <summary>
/// A day an owned edition was in the spotlight of the dashboard (fonctionnel/page-accueil.md, À la
/// une): the edition of that date for every time zone, and the history from which the draw counts the spotlights
/// of an edition (modele-metier/edition.md, Nombre de mises à la une).
/// </summary>
/// <remarks>
/// Not a record: neither in the change log nor a write on the edition. Part of its edition, deleted with it. Only
/// ever inserted by the draw of a day (choix-implementation/domaine/edition-a-la-une-mise-en-oeuvre.md).
/// </remarks>
public sealed class EditionSpotlight
{
    /// <summary>The date of the visitor, in the time zone of the browser, shared by every time zone.</summary>
    public DateOnly Day { get; private set; }

    public Guid EditionId { get; private set; }
    public Edition Edition { get; private set; } = null!;

    // EF Core parameterless constructor
    private EditionSpotlight() { }
}
