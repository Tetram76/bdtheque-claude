using System.Text.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Controls;

namespace Bdtheque.Frontend.Entries;

/// <summary>
/// A series as its entry form holds it while it is entered (visuel/saisie-serie/contenu.md): the fields of its content, and the
/// records it references named for the form to present them. Persisted from the prerendering into the interactive circuit,
/// which does not read it again. It applies no rule of the series: the domain is their only source
/// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md), and reports what to correct when the series
/// is saved.
/// </summary>
public sealed class SeriesEntry
{
    /// <summary>The series; <c>null</c> for a new one, not saved yet.</summary>
    public Guid? Id { get; set; }

    /// <summary>The version of the series the form was read at, sent back with its modification.</summary>
    public uint Version { get; set; }

    /// <summary>The sort key the series was read with, shown in the automatic mode until its title changes.</summary>
    public string? StoredSortKey { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public string? Title { get; set; }

    /// <summary>The sort key entered by hand; <c>null</c> for the automatic mode.</summary>
    public string? ManualSortKey { get; set; }

    public SeriesStatus? Status { get; set; }

    public int? TheoreticalVolumeCount { get; set; }

    public bool IsComplete { get; set; }

    public bool ExcludeFromMissingVolumes { get; set; }

    public bool ExcludeFromReleaseEstimates { get; set; }

    public string? Summary { get; set; }

    public string? PersonalNotes { get; set; }

    public RecordOption? Publisher { get; set; }

    /// <summary>One of the collections of <see cref="Publisher"/>.</summary>
    public RecordOption? PublisherCollection { get; set; }

    // The values proposed to the new editions of the albums of the series; none for a value it does not propose.

    public EditionCategory? Category { get; set; }

    public EditionCondition? Condition { get; set; }

    public BindingType? Binding { get; set; }

    public EditionFormat? Format { get; set; }

    public BookOrientation? Orientation { get; set; }

    public ReadingDirection? ReadingDirection { get; set; }

    public bool? IsColor { get; set; }

    /// <summary>Its authors, genres and universes, those added on the spot included.</summary>
    public CitedRecords Cited { get; set; } = new();

    /// <summary>A new series, from what the API proposes to it: its new editions proposing what any new edition is proposed.</summary>
    public static SeriesEntry New(SeriesContent proposed)
    {
        var entry = new SeriesEntry();
        entry.Apply(proposed);
        return entry;
    }

    /// <summary>The series as the API gives it.</summary>
    public static SeriesEntry Of(SeriesForm form)
    {
        var references = form.References;
        var entry = new SeriesEntry
        {
            Id = form.Id,
            Version = form.Version,
            StoredSortKey = form.SortKey,
            CreatedAt = form.CreatedAt,
            ModifiedAt = form.ModifiedAt,
            Publisher = references.Publisher is { } publisher ? new RecordOption(publisher.Id, publisher.Name, null) : null,
            PublisherCollection = references.PublisherCollection is { } collection
                ? new RecordOption(collection.Id, collection.Name, collection.PublisherName)
                : null,
            Cited = CitedRecords.Of(form.Content.Contributions, references.Authors, references.Genres, references.Universes),
        };
        entry.Apply(form.Content);
        return entry;
    }

    private void Apply(SeriesContent content)
    {
        Title = content.Title;
        ManualSortKey = content.ManualSortKey;
        Status = content.Status;
        TheoreticalVolumeCount = content.TheoreticalVolumeCount;
        IsComplete = content.IsComplete;
        ExcludeFromMissingVolumes = content.ExcludeFromMissingVolumes;
        ExcludeFromReleaseEstimates = content.ExcludeFromReleaseEstimates;
        Summary = content.Summary;
        PersonalNotes = content.PersonalNotes;
        var template = content.EditionTemplate;
        Category = template.Category;
        Condition = template.Condition;
        Binding = template.Binding;
        Format = template.Format;
        Orientation = template.Orientation;
        ReadingDirection = template.ReadingDirection;
        IsColor = template.IsColor;
    }

    /// <summary>
    /// Chooses the publisher: the collection, one of those of the publisher, is removed when the publisher changes
    /// (visuel/saisie-serie/contenu.md).
    /// </summary>
    public void ChoosePublisher(RecordOption? publisher)
    {
        if (publisher?.Id != Publisher?.Id)
            PublisherCollection = null;
        Publisher = publisher;
    }

    /// <summary>The content to save: the whole form, and the records added on the spot that it still cites, to create with the series.</summary>
    public SeriesContent ToContent() => new(
        Title ?? "",
        ManualSortKey,
        Status,
        TheoreticalVolumeCount,
        IsComplete,
        ExcludeFromMissingVolumes,
        ExcludeFromReleaseEstimates,
        Summary,
        PersonalNotes,
        new SeriesEditionTemplate(Publisher?.Id, PublisherCollection?.Id, Category, Condition, Binding, Orientation, ReadingDirection, Format, IsColor),
        Cited.GenreIds(),
        Cited.UniverseIds(),
        Cited.Contributions(),
        Cited.Added());

    /// <summary>
    /// What tells two states of the form apart: the content it would save, the order of its associations aside, which the
    /// series does not keep.
    /// </summary>
    public string Fingerprint()
    {
        var content = ToContent();
        return JsonSerializer.Serialize(content with
        {
            GenreIds = [.. content.GenreIds.Order()],
            UniverseIds = [.. content.UniverseIds.Order()],
            Contributions = [.. content.Contributions.OrderBy(c => c.Role).ThenBy(c => c.AuthorId)],
        });
    }

    public SeriesEntry Copy() => JsonSerializer.Deserialize<SeriesEntry>(JsonSerializer.Serialize(this))!;
}
