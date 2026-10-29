namespace Bdtheque.Frontend.Components.Presentation;

/// <summary>What a <see cref="Button"/> stands for, which its color says.</summary>
public enum ButtonVariant
{
    /// <summary>Any action that is not the main one of its place.</summary>
    Secondary,

    /// <summary>The main action of its place (e.g. Nouvel album), in the accent of the theme.</summary>
    Primary,

    /// <summary>A deletion, in the color of deletion of the theme.</summary>
    Delete,
}
