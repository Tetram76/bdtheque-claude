namespace Bdtheque.Frontend.Components.Controls;

/// <summary>The state of the record a <see cref="SaveBar"/> saves, which it tells.</summary>
public enum SaveBarState
{
    /// <summary>A record being created, not yet saved.</summary>
    New,

    /// <summary>An existing record whose form has changes not yet saved.</summary>
    Unsaved,

    /// <summary>An existing record whose form is as saved.</summary>
    Unchanged,
}
