namespace Bdtheque.Frontend.Components.Lists;

/// <summary>
/// A set of what the administration manages, whose mention the block of its list and its block on the home of the administration
/// share (visuel/listes/declinaison-par-theme.md, visuel/administration/contenu.md): the same set bears the same mention on
/// both (#474). Its mentions, one per set and named by it, are the texts of the enumeration itself (<c>RecordSet.resx</c>):
/// a localizer finds its resources by the full name of its type, whatever that type is.
/// </summary>
public enum RecordSet
{
    /// <summary>The records: series, albums, editions, authors and publishers.</summary>
    Records,

    /// <summary>The reference data: genres and universes.</summary>
    Reference,

    /// <summary>The purchase intents, which have no record of their own.</summary>
    Acquisitions,

    /// <summary>The records to correct or complete: the block « À vérifier » and its lists (the ISBN to check).</summary>
    Checks,
}
