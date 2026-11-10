using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// The values the form of a new edition starts from
/// (fonctionnel/regles-metier/initialisation-d-une-nouvelle-edition-depuis-la-serie.md): only a starting point,
/// never applied to an edition by the domain.
/// </summary>
public sealed record EditionPreset(
    Guid? PublisherId,
    Guid? PublisherCollectionId,
    EditionCategory? Category,
    EditionCondition? Condition,
    BindingType? Binding,
    BookOrientation? Orientation,
    ReadingDirection? ReadingDirection,
    EditionFormat? Format,
    bool IsColor)
{
    /// <summary>
    /// The values proposed to any new edition (modele-metier/edition.md), which the database never carries: those of an album
    /// without series, and the starting point of the template of a new series (modele-metier/serie.md).
    /// </summary>
    public static readonly EditionPreset Defaults = new(
        null, null, null, EditionCondition.Excellent, BindingType.Hardcover, BookOrientation.Portrait,
        Enums.ReadingDirection.LeftToRight, EditionFormat.Standard, IsColor: true);

    /// <summary>
    /// The template of the album's series, which prevails — a field it leaves empty is not
    /// pre-filled, colour then keeping the default of the edition —, or the default values for an
    /// album without series.
    /// </summary>
    public static EditionPreset For(Album album)
    {
        ArgumentNullException.ThrowIfNull(album);
        if (album.SeriesId is null)
            return Defaults;

        var series = album.Series
                     ?? throw new InvalidOperationException("The series of the album must be loaded to read its edition template.");
        return new EditionPreset(
            series.TemplatePublisherId,
            series.TemplatePublisherCollectionId,
            series.TemplateEditionCategory,
            series.TemplateCondition,
            series.TemplateBinding,
            series.TemplateOrientation,
            series.TemplateReadingDirection,
            series.TemplateFormat,
            series.TemplateIsColor ?? Defaults.IsColor);
    }
}
