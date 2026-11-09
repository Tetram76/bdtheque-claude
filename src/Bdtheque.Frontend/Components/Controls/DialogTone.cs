namespace Bdtheque.Frontend.Components.Controls;

/// <summary>What a <see cref="Dialog"/> asks, which its icon and its role say.</summary>
public enum DialogTone
{
    /// <summary>The confirmation of a deletion (visuel/liste-albums/contenu.md, suppression).</summary>
    Deletion,

    /// <summary>A refusal, which only closes (e.g. a deletion that a reference blocks).</summary>
    Refusal,

    /// <summary>A choice to make before going on (e.g. the other album of a merge).</summary>
    Choice,

    /// <summary>A record created on the spot from another one being entered (e.g. an author, from an album).</summary>
    Creation,

    /// <summary>What the user may want to read before going on (e.g. their changes, before reloading a record).</summary>
    Information,
}
