namespace Bdtheque.Frontend.Components.Controls;

/// <summary>What a <see cref="Dialog"/> asks, which its icon and its role say.</summary>
public enum DialogTone
{
    /// <summary>The confirmation of a deletion (visuel/liste-albums.md § Contenu, suppression).</summary>
    Deletion,

    /// <summary>A refusal, which only closes (e.g. a deletion that a reference blocks).</summary>
    Refusal,

    /// <summary>A choice to make before going on (e.g. the other album of a merge).</summary>
    Choice,
}
