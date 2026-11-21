namespace Bdtheque.Frontend.Components.Lists;

/// <summary>
/// A set of what the administration manages, whose mention the block of its list and its block on the home of the administration
/// share (visuel/listes/declinaison-par-theme.md, visuel/administration/contenu.md): the same set bears the same mention on
/// both (#474).
/// </summary>
public enum RecordSet
{
    /// <summary>The records: series, albums, editions, authors and publishers.</summary>
    Records,

    /// <summary>The reference data: genres and universes.</summary>
    Reference,

    /// <summary>The purchase intents, which have no record of their own.</summary>
    Acquisitions,
}

/// <summary>The mention of each <see cref="RecordSet"/> (<c>RecordSetText.resx</c>), named by the set.</summary>
public sealed class RecordSetText;
