namespace Bdtheque.Frontend.Components.Lists;

/// <summary>What the confirmation of a <see cref="DeletionDialog"/> does to its record, which its sentence and its button say.</summary>
public enum DeletionKind
{
    /// <summary>The record is deleted.</summary>
    Deletion,

    /// <summary>The record is removed: an intent of acquisition.</summary>
    Removal,

    /// <summary>An intent on an edition becomes one on the whole album, the edition targeted deleted.</summary>
    ConversionToAlbum,
}
