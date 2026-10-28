using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Infrastructure;

/// <summary>Reads the value of an attribute from the values of its properties, in the order the attribute lists them.</summary>
internal delegate ValueTask<ChangeLogValue?> AttributeReader(object?[] values, ChangeLogLabels labels, CancellationToken cancellationToken);

/// <summary>An attribute of a record, read from one or several of its properties (e.g. an amount and its currency).</summary>
/// <param name="EnumType">The enumeration of the attribute, whose values <see cref="EnumValue"/> records.</param>
internal sealed record AttributeMapping(ChangeLogAttribute Attribute, string[] Properties, AttributeReader Read, Type? EnumType = null);

/// <summary>
/// How the change log records a type of record: the attributes its properties form, in the order
/// a modification lists them, and the properties deliberately left out.
/// </summary>
internal sealed record RecordMapping(RecordType RecordType, AttributeMapping[] Attributes, string[] LeftOut);

/// <summary>
/// The records of the change log and their attributes (modele-metier.md § Entrée du journal des
/// modifications). Every property of a record is either recorded here or left out on purpose, which
/// <c>ChangeLogMappingTests</c> checks; the elements a record carries without a record of their own
/// (genres, universes, contributions, visuals) are recorded by <see cref="ChangeLogRecorder"/>.
/// </summary>
internal static class ChangeLogMappings
{
    public static readonly IReadOnlyDictionary<Type, RecordMapping> Records = new Dictionary<Type, RecordMapping>
    {
        [typeof(Album)] = new(
            RecordType.Album,
            [
                Text(ChangeLogAttribute.Title, nameof(Album.Title)),
                Text(ChangeLogAttribute.SortKey, nameof(Album.SortKey)),
                Flag(ChangeLogAttribute.ManualSortKey, nameof(Album.IsManualSortKey)),
                Reference<Series>(ChangeLogAttribute.Series, nameof(Album.SeriesId)),
                Enum<Album>(ChangeLogAttribute.AlbumType, nameof(Album.Type)),
                Flag(ChangeLogAttribute.SpecialIssue, nameof(Album.IsSpecialIssue)),
                Number(ChangeLogAttribute.VolumeNumber, nameof(Album.VolumeNumber)),
                Number(ChangeLogAttribute.StartVolumeNumber, nameof(Album.StartVolumeNumber)),
                Number(ChangeLogAttribute.EndVolumeNumber, nameof(Album.EndVolumeNumber)),
                PartialDate(ChangeLogAttribute.FirstPublicationDate, nameof(Album.FirstPublicationYear), nameof(Album.FirstPublicationMonth)),
                Text(ChangeLogAttribute.Summary, nameof(Album.Summary)),
                Text(ChangeLogAttribute.PersonalNotes, nameof(Album.PersonalNotes)),
                Enum<Album>(ChangeLogAttribute.Rating, nameof(Album.Rating)),
            ],
            // Derived from the sort key, recorded itself.
            [nameof(Album.NavigationEntry)]),

        [typeof(Series)] = new(
            RecordType.Series,
            [
                Text(ChangeLogAttribute.Title, nameof(Series.Title)),
                Text(ChangeLogAttribute.SortKey, nameof(Series.SortKey)),
                Flag(ChangeLogAttribute.ManualSortKey, nameof(Series.IsManualSortKey)),
                Enum<Series>(ChangeLogAttribute.Status, nameof(Series.Status)),
                Number(ChangeLogAttribute.TheoreticalVolumeCount, nameof(Series.TheoreticalVolumeCount)),
                Flag(ChangeLogAttribute.Complete, nameof(Series.IsComplete)),
                Flag(ChangeLogAttribute.ExcludeFromMissingVolumes, nameof(Series.ExcludeFromMissingVolumes)),
                Flag(ChangeLogAttribute.ExcludeFromReleaseEstimates, nameof(Series.ExcludeFromReleaseEstimates)),
                Text(ChangeLogAttribute.Summary, nameof(Series.Summary)),
                Text(ChangeLogAttribute.PersonalNotes, nameof(Series.PersonalNotes)),
                Reference<Publisher>(ChangeLogAttribute.TemplatePublisher, nameof(Series.TemplatePublisherId)),
                Reference<PublisherCollection>(ChangeLogAttribute.TemplatePublisherCollection, nameof(Series.TemplatePublisherCollectionId)),
                Enum<Series>(ChangeLogAttribute.TemplateBinding, nameof(Series.TemplateBinding)),
                Enum<Series>(ChangeLogAttribute.TemplateOrientation, nameof(Series.TemplateOrientation)),
                Enum<Series>(ChangeLogAttribute.TemplateReadingDirection, nameof(Series.TemplateReadingDirection)),
                Enum<Series>(ChangeLogAttribute.TemplateFormat, nameof(Series.TemplateFormat)),
                Enum<Series>(ChangeLogAttribute.TemplateEditionCategory, nameof(Series.TemplateEditionCategory)),
                Enum<Series>(ChangeLogAttribute.TemplateCondition, nameof(Series.TemplateCondition)),
                Flag(ChangeLogAttribute.TemplateColor, nameof(Series.TemplateIsColor)),
            ],
            // Derived from the sort key, recorded itself.
            [nameof(Series.NavigationEntry)]),

        [typeof(Edition)] = new(
            RecordType.Edition,
            [
                Reference<Publisher>(ChangeLogAttribute.Publisher, nameof(Edition.PublisherId)),
                Reference<PublisherCollection>(ChangeLogAttribute.PublisherCollection, nameof(Edition.PublisherCollectionId)),
                Number(ChangeLogAttribute.PublicationYear, nameof(Edition.PublicationYear)),
                Text(ChangeLogAttribute.Isbn, nameof(Edition.Isbn)),
                Enum<Edition>(ChangeLogAttribute.Binding, nameof(Edition.Binding)),
                Enum<Edition>(ChangeLogAttribute.Orientation, nameof(Edition.Orientation)),
                Enum<Edition>(ChangeLogAttribute.ReadingDirection, nameof(Edition.ReadingDirection)),
                Enum<Edition>(ChangeLogAttribute.Format, nameof(Edition.Format)),
                Number(ChangeLogAttribute.PageCount, nameof(Edition.PageCount)),
                Enum<Edition>(ChangeLogAttribute.EditionCategory, nameof(Edition.Category)),
                Flag(ChangeLogAttribute.Dedicated, nameof(Edition.IsDedicated)),
                Flag(ChangeLogAttribute.Color, nameof(Edition.IsColor)),
                Enum<Edition>(ChangeLogAttribute.Condition, nameof(Edition.Condition)),
                Enum<Edition>(ChangeLogAttribute.AcquisitionMode, nameof(Edition.AcquisitionMode)),
                Flag(ChangeLogAttribute.SecondHand, nameof(Edition.IsSecondHand)),
                Date(ChangeLogAttribute.AcquisitionDate, nameof(Edition.AcquisitionDate)),
                Amount(ChangeLogAttribute.AcquisitionPrice, nameof(Edition.AcquisitionAmount), nameof(Edition.AcquisitionCurrency)),
                Flag(ChangeLogAttribute.Free, nameof(Edition.IsFree)),
                Amount(ChangeLogAttribute.InitialValue, nameof(Edition.InitialValueAmount), nameof(Edition.InitialValueCurrency)),
                Text(ChangeLogAttribute.PersonalReference, nameof(Edition.PersonalReference)),
                Text(ChangeLogAttribute.PersonalNotes, nameof(Edition.PersonalNotes)),
            ],
            // An edition never moves to another album; it enters the collection when it becomes
            // owned, a date set on save like the creation date, never entered.
            [nameof(Edition.AlbumId), nameof(Edition.EnteredCollectionAt)]),

        // An intent is never modified: a conversion replaces it by another. Its target is in its label.
        [typeof(PurchaseIntent)] = new(RecordType.PurchaseIntent, [], [nameof(PurchaseIntent.AlbumId), nameof(PurchaseIntent.EditionId)]),

        [typeof(Author)] = new(
            RecordType.Author,
            [
                Text(ChangeLogAttribute.LastName, nameof(Author.LastName)),
                Text(ChangeLogAttribute.FirstName, nameof(Author.FirstName)),
                Text(ChangeLogAttribute.Pseudonym, nameof(Author.Pseudonym)),
                Text(ChangeLogAttribute.Biography, nameof(Author.Biography)),
                Text(ChangeLogAttribute.Nationality, nameof(Author.Nationality)),
            ],
            // Derived from the identity, never entered.
            [nameof(Author.SortKey), nameof(Author.NavigationEntry)]),

        [typeof(Publisher)] = new(
            RecordType.Publisher,
            [Text(ChangeLogAttribute.Name, nameof(Publisher.Name)), Text(ChangeLogAttribute.Website, nameof(Publisher.Website))],
            []),

        // A collection never moves to another publisher.
        [typeof(PublisherCollection)] = new(
            RecordType.PublisherCollection, [Text(ChangeLogAttribute.Name, nameof(PublisherCollection.Name))], [nameof(PublisherCollection.PublisherId)]),

        [typeof(Genre)] = new(RecordType.Genre, [Text(ChangeLogAttribute.Label, nameof(Genre.Label))], []),

        [typeof(Universe)] = new(
            RecordType.Universe,
            [
                Text(ChangeLogAttribute.Name, nameof(Universe.Name)),
                Text(ChangeLogAttribute.Description, nameof(Universe.Description)),
                Reference<Universe>(ChangeLogAttribute.ParentUniverse, nameof(Universe.ParentId)),
            ],
            []),

        [typeof(SavedSearch)] = new(
            RecordType.SavedSearch,
            [
                Text(ChangeLogAttribute.Name, nameof(SavedSearch.Name)),
                Enum<SavedSearch>(ChangeLogAttribute.SearchedEntity, nameof(SavedSearch.Entity)),
                Scalar(ChangeLogAttribute.SearchCriteria, nameof(SavedSearch.Criteria), value => value is string criteria ? new SearchCriteriaValue(criteria) : null),
                // With the default value of each variable, the constant it replaces: read from the criteria too,
                // so that a new default value shows as a change of the variables.
                new(ChangeLogAttribute.SearchVariables, [nameof(SavedSearch.Criteria), nameof(SavedSearch.Variables)],
                    (values, _, _) => ValueTask.FromResult<ChangeLogValue?>(
                        values is [string criteria, SearchVariables variables]
                            ? new SearchVariablesValue(variables
                                .Select(v => new SearchVariableValue(v.Name, v.Location, SavedSearch.DefaultValueOf(criteria, v)))
                                .ToList())
                            : null)),
            ],
            []),

        [typeof(ApplicationSettings)] = new(
            RecordType.ApplicationSettings,
            [
                Enum<ApplicationSettings>(ChangeLogAttribute.AlbumLabelFormat, nameof(ApplicationSettings.AlbumLabelFormat)),
                Enum<ApplicationSettings>(ChangeLogAttribute.DisplayCulture, nameof(ApplicationSettings.DisplayCulture)),
            ],
            []),
    };

    private static AttributeMapping Text(ChangeLogAttribute attribute, string property) =>
        Scalar(attribute, property, value => value is string text ? new TextValue(text) : null);

    private static AttributeMapping Number(ChangeLogAttribute attribute, string property) =>
        Scalar(attribute, property, value => value is int number ? new NumberValue(number) : null);

    private static AttributeMapping Flag(ChangeLogAttribute attribute, string property) =>
        Scalar(attribute, property, value => value is bool flag ? new FlagValue(flag) : null);

    private static AttributeMapping Date(ChangeLogAttribute attribute, string property) =>
        Scalar(attribute, property, value => value is DateOnly date ? new DateValue(date) : null);

    // Its persisted value, which keeps its meaning forever (choix-implementation/persistance.md § Conventions de persistance).
    private static AttributeMapping Enum<TRecord>(ChangeLogAttribute attribute, string property)
    {
        var type = typeof(TRecord).GetProperty(property)!.PropertyType;
        return Scalar(attribute, property, value => value is System.Enum member ? new EnumValue(Convert.ToInt32(member)) : null) with
        {
            EnumType = Nullable.GetUnderlyingType(type) ?? type,
        };
    }

    private static AttributeMapping PartialDate(ChangeLogAttribute attribute, string yearProperty, string monthProperty) =>
        new(attribute, [yearProperty, monthProperty],
            (values, _, _) => ValueTask.FromResult<ChangeLogValue?>(values[0] is int year ? new PartialDateValue(year, (int?)values[1]) : null));

    private static AttributeMapping Amount(ChangeLogAttribute attribute, string amountProperty, string currencyProperty) =>
        new(attribute, [amountProperty, currencyProperty],
            (values, _, _) => ValueTask.FromResult<ChangeLogValue?>(
                values is [decimal amount, string currency] ? new AmountValue(amount, currency) : null));

    private static AttributeMapping Reference<TRecord>(ChangeLogAttribute attribute, string idProperty)
        where TRecord : Domain.Entities.Common.EntityBase =>
        new(attribute, [idProperty],
            async (values, labels, cancellationToken) =>
                values[0] is Guid id ? new ReferenceValue(id, await labels.OfAsync<TRecord>(id, cancellationToken)) : null);

    private static AttributeMapping Scalar(ChangeLogAttribute attribute, string property, Func<object?, ChangeLogValue?> read) =>
        new(attribute, [property], (values, _, _) => ValueTask.FromResult(read(values[0])));
}
