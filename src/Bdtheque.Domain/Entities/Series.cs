using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// A continuity grouping albums together (Série), acting as a source of default
/// ("template") values for new editions and contributions attached to its albums.
/// </summary>
public sealed class Series : EntityBase
{
    public string Title { get; private set; } = string.Empty;
    public string SortKey { get; private set; } = string.Empty;
    public bool IsManualSortKey { get; private set; }

    public SeriesStatus? Status { get; private set; }
    public int? TheoreticalVolumeCount { get; private set; }
    public bool IsComplete { get; private set; }
    public bool ExcludeFromMissingVolumes { get; private set; }
    public string? Summary { get; private set; }
    public string? PersonalNotes { get; private set; }

    public BindingType? TemplateBinding { get; private set; }
    public BookOrientation? TemplateOrientation { get; private set; }
    public ReadingDirection? TemplateReadingDirection { get; private set; }
    public EditionFormat? TemplateFormat { get; private set; }
    public EditionCategory? TemplateEditionCategory { get; private set; }
    public EditionCondition? TemplateCondition { get; private set; }
    public bool? TemplateIsColor { get; private set; }

    public Guid? TemplatePublisherId { get; private set; }
    public Publisher? TemplatePublisher { get; private set; }
    public Guid? TemplatePublisherCollectionId { get; private set; }
    public PublisherCollection? TemplatePublisherCollection { get; private set; }

    // Read-only from outside: AddGenre/AddUniverse keep each association unique.
    private readonly List<Genre> _genres = [];
    public IReadOnlyCollection<Genre> Genres => _genres;
    private readonly List<Universe> _universes = [];
    public IReadOnlyCollection<Universe> Universes => _universes;

    // EF Core parameterless constructor
    private Series() { }

    public Series(string title)
    {
        SetTitle(title);
    }

    public void SetTitle(string title)
    {
        Title = DomainText.Required(title, DomainRules.SeriesTitleRequired, "A series must have a title.");
        if (!IsManualSortKey)
            SortKey = TitleSortKeyCalculator.Compute(Title);
    }

    public void SetSortKey(string sortKey)
    {
        SortKey = DomainText.Required(sortKey, DomainRules.SeriesSortKeyRequired, "A manual sort key must not be blank.");
        IsManualSortKey = true;
    }

    /// <summary>Recomputes the sort key from the current title and returns to automatic mode.</summary>
    public void ResetSortKey()
    {
        IsManualSortKey = false;
        SortKey = TitleSortKeyCalculator.Compute(Title);
    }

    public void SetStatus(SeriesStatus? status)
    {
        if (status is not null)
            EnumGuard.EnsureDefined(status.Value, nameof(status));
        Status = status;
    }

    public void SetTheoreticalVolumeCount(int? count)
    {
        if (count is <= 0)
            throw new DomainRuleViolationException(
                DomainRules.SeriesTheoreticalVolumeCountPositive, "Theoretical volume count must be positive when specified.");
        TheoreticalVolumeCount = count;
    }

    public void SetComplete(bool isComplete) => IsComplete = isComplete;

    public void SetExcludeFromMissingVolumes(bool exclude) => ExcludeFromMissingVolumes = exclude;

    public void SetSummary(string? summary) => Summary = DomainText.NullIfBlank(summary);

    public void SetPersonalNotes(string? notes) => PersonalNotes = DomainText.NullIfBlank(notes);

    public void AddGenre(Genre genre) => _genres.AddOnce(genre);

    public void RemoveGenre(Genre genre) => _genres.RemoveById(genre);

    public void AddUniverse(Universe universe) => _universes.AddOnce(universe);

    public void RemoveUniverse(Universe universe) => _universes.RemoveById(universe);

    public void SetTemplateBinding(BindingType? binding)
    {
        if (binding is not null)
            EnumGuard.EnsureDefined(binding.Value, nameof(binding));
        TemplateBinding = binding;
    }

    public void SetTemplateOrientation(BookOrientation? orientation)
    {
        if (orientation is not null)
            EnumGuard.EnsureDefined(orientation.Value, nameof(orientation));
        TemplateOrientation = orientation;
    }

    public void SetTemplateReadingDirection(ReadingDirection? readingDirection)
    {
        if (readingDirection is not null)
            EnumGuard.EnsureDefined(readingDirection.Value, nameof(readingDirection));
        TemplateReadingDirection = readingDirection;
    }

    public void SetTemplateFormat(EditionFormat? format)
    {
        if (format is not null)
            EnumGuard.EnsureDefined(format.Value, nameof(format));
        TemplateFormat = format;
    }

    public void SetTemplateEditionCategory(EditionCategory? category)
    {
        if (category is not null)
            EnumGuard.EnsureDefined(category.Value, nameof(category));
        TemplateEditionCategory = category;
    }

    public void SetTemplateCondition(EditionCondition? condition)
    {
        if (condition is not null)
            EnumGuard.EnsureDefined(condition.Value, nameof(condition));
        TemplateCondition = condition;
    }

    public void SetTemplateIsColor(bool? isColor) => TemplateIsColor = isColor;

    /// <summary>
    /// Sets the template publisher and, optionally, a template publisher collection, as a single
    /// atomic operation: a collection can only be set together with the publisher it belongs to,
    /// per the model constraint (a template collection requires a template publisher and must
    /// belong to it).
    /// </summary>
    public void SetTemplate(Publisher? publisher, PublisherCollection? collection)
    {
        if (collection is not null && (publisher is null || collection.PublisherId != publisher.Id))
            throw new DomainRuleViolationException(
                DomainRules.PublisherCollectionNotOfPublisher, "The template publisher collection must belong to the template publisher.");

        TemplatePublisher = publisher;
        TemplatePublisherId = publisher?.Id;
        TemplatePublisherCollection = collection;
        TemplatePublisherCollectionId = collection?.Id;
    }
}
