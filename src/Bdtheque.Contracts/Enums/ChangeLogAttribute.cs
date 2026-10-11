using System.Text.Json.Serialization;

namespace Bdtheque.Contracts.Enums;

/// <summary>
/// Attribute of a record changed by a modification, as the change log names it (modele-metier.md §
/// Entrée du journal des modifications). The type of the record gives it its context: the title of
/// an album and the title of a series are both <see cref="Title"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ChangeLogAttribute>))]
public enum ChangeLogAttribute
{
    // Album and series
    Title = 1,
    SortKey = 2,
    ManualSortKey = 3,
    Summary = 4,
    PersonalNotes = 5,
    Genre = 6,
    Universe = 7,
    Contribution = 8,

    // Album
    Series = 9,
    AlbumType = 10,
    SpecialIssue = 11,
    VolumeNumber = 12,
    StartVolumeNumber = 13,
    EndVolumeNumber = 14,
    FirstPublicationDate = 15,
    Rating = 16,

    // Series
    Status = 17,
    TheoreticalVolumeCount = 18,
    Complete = 19,
    ExcludeFromMissingVolumes = 20,
    ExcludeFromReleaseEstimates = 21,
    TemplatePublisher = 22,
    TemplatePublisherCollection = 23,
    TemplateBinding = 24,
    TemplateOrientation = 25,
    TemplateReadingDirection = 26,
    TemplateFormat = 27,
    TemplateEditionCategory = 28,
    TemplateCondition = 29,
    TemplateColor = 30,

    // Edition
    Publisher = 31,
    PublisherCollection = 32,
    PublicationYear = 33,
    Isbn = 34,
    Binding = 35,
    Orientation = 36,
    ReadingDirection = 37,
    Format = 38,
    PageCount = 39,
    EditionCategory = 40,
    Dedicated = 41,
    Color = 42,
    Condition = 43,
    AcquisitionMode = 44,
    SecondHand = 45,
    AcquisitionDate = 46,
    AcquisitionPrice = 47,
    Free = 48,
    InitialValue = 49,
    PersonalReference = 50,
    Visual = 51,

    // Author
    LastName = 52,
    FirstName = 53,
    Pseudonym = 54,
    Biography = 55,
    Nationality = 56,

    // Publisher, publisher collection, universe, genre
    Name = 57,
    Website = 58,
    Description = 59,
    ParentUniverse = 60,
    Label = 61,
}
