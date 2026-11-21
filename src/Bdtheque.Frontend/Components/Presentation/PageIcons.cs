namespace Bdtheque.Frontend.Components.Presentation;

/// <summary>
/// The icon of a page that several buttons and entries of menus lead to, named once so that the same destination bears the same
/// icon wherever it is offered (visuel/listes/principes-communs.md, Actions).
/// </summary>
public static class PageIcons
{
    /// <summary>
    /// The page of the editions and purchase intents of an album: the icon of the editions, never that of the album, which tells an
    /// intent on the whole album apart from one on an edition.
    /// </summary>
    public const IconName AlbumEditions = IconName.Edition;
}
