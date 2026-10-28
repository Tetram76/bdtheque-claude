using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// Append-only historical record of every (member name, explicit value) pair ever assigned to
/// each domain enum — including retired members whose C# declaration has since been removed.
/// Never remove, rename, or renumber an entry here when a member is removed from its enum: doing
/// so would let a later member silently reuse a value that already means something different in
/// already-persisted rows (see choix-implementation/persistance.md § Conventions de persistance). Add a new
/// entry whenever a new member is introduced; a retired member's entry stays forever so its value
/// remains reserved. This holds from the first deployment recorded in journal-evenements.md: until
/// then no row is persisted anywhere, and values may still be renumbered to keep the business order.
/// </summary>
internal static class EnumValueLedger
{
    public static readonly IReadOnlyDictionary<Type, (string Name, int Value)[]> Entries =
        new Dictionary<Type, (string Name, int Value)[]>
        {
            [typeof(AlbumType)] = [("Regular", 1), ("Omnibus", 2)],
            [typeof(AlbumRating)] = [("VeryPoor", 1), ("Poor", 2), ("Average", 3), ("Good", 4), ("VeryGood", 5)],
            [typeof(BindingType)] = [("Paperback", 1), ("Hardcover", 2)],
            [typeof(BookOrientation)] = [("Portrait", 1), ("Landscape", 2)],
            [typeof(ContributionRole)] = [("Scenarist", 1), ("Illustrator", 2), ("Colorist", 3)],
            [typeof(EditionCategory)] = [("FirstEdition", 1), ("SpecialEdition", 2), ("LimitedEdition", 3)],
            [typeof(EditionCondition)] =
                [("Excellent", 1), ("VeryGood", 2), ("Good", 3), ("Average", 4), ("Poor", 5), ("VeryPoor", 6)],
            [typeof(EditionFormat)] = [("Pocket", 1), ("Medium", 2), ("Standard", 3), ("Large", 4), ("Special", 5)],
            [typeof(ReadingDirection)] = [("LeftToRight", 1), ("RightToLeft", 2)],
            [typeof(SeriesStatus)] = [("InProgress", 1), ("Completed", 2), ("Abandoned", 3)],
            [typeof(AcquisitionMode)] = [("Purchase", 1), ("Gift", 2), ("Trade", 3), ("Won", 4), ("Inherited", 5)],
            [typeof(VisualType)] =
                [("Cover", 1), ("Dedication", 2), ("Endpaper", 3), ("Plate", 4), ("BackCover", 5)],
            [typeof(RecordType)] =
            [
                ("Album", 1), ("Edition", 2), ("Series", 3), ("PurchaseIntent", 4), ("Author", 5), ("Publisher", 6),
                ("PublisherCollection", 7), ("Genre", 8), ("Universe", 9), ("SavedSearch", 10), ("ApplicationSettings", 11),
            ],
            [typeof(EntityKind)] =
            [
                ("Album", 1), ("Series", 2), ("Universe", 3), ("Edition", 4), ("EditionVisual", 5), ("PublisherCollection", 6),
                ("Contribution", 7), ("PurchaseIntent", 8), ("Author", 9), ("Publisher", 10), ("Genre", 11), ("SavedSearch", 12),
            ],
            [typeof(ChangeLogOperation)] = [("Creation", 1), ("Modification", 2), ("Deletion", 3)],
            [typeof(ChangeLogAttribute)] =
            [
                ("Title", 1), ("SortKey", 2), ("ManualSortKey", 3), ("Summary", 4), ("PersonalNotes", 5), ("Genre", 6),
                ("Universe", 7), ("Contribution", 8), ("Series", 9), ("AlbumType", 10), ("SpecialIssue", 11),
                ("VolumeNumber", 12), ("StartVolumeNumber", 13), ("EndVolumeNumber", 14), ("FirstPublicationDate", 15),
                ("Rating", 16), ("Status", 17), ("TheoreticalVolumeCount", 18), ("Complete", 19),
                ("ExcludeFromMissingVolumes", 20), ("ExcludeFromReleaseEstimates", 21), ("TemplatePublisher", 22),
                ("TemplatePublisherCollection", 23), ("TemplateBinding", 24), ("TemplateOrientation", 25),
                ("TemplateReadingDirection", 26), ("TemplateFormat", 27), ("TemplateEditionCategory", 28),
                ("TemplateCondition", 29), ("TemplateColor", 30), ("Publisher", 31), ("PublisherCollection", 32),
                ("PublicationYear", 33), ("Isbn", 34), ("Binding", 35), ("Orientation", 36), ("ReadingDirection", 37),
                ("Format", 38), ("PageCount", 39), ("EditionCategory", 40), ("Dedicated", 41), ("Color", 42),
                ("Condition", 43), ("AcquisitionMode", 44), ("SecondHand", 45), ("AcquisitionDate", 46),
                ("AcquisitionPrice", 47), ("Free", 48), ("InitialValue", 49), ("PersonalReference", 50), ("Visual", 51),
                ("LastName", 52), ("FirstName", 53), ("Pseudonym", 54), ("Biography", 55), ("Nationality", 56),
                ("Name", 57), ("Website", 58), ("Description", 59), ("ParentUniverse", 60), ("Label", 61),
                ("SearchedEntity", 62), ("SearchCriteria", 63), ("SearchVariables", 64), ("AlbumLabelFormat", 65),
                ("DisplayCulture", 66),
            ],
            [typeof(DeviceType)] = [("Computer", 1), ("Tablet", 2), ("Smartphone", 3)],
            [typeof(RenderingEngine)] = [("Blink", 1), ("Gecko", 2), ("WebKit", 3), ("Other", 4)],
            [typeof(AlbumLabelFormat)] = [("TitleSeriesVolume", 1), ("VolumeTitleSeries", 2)],
            [typeof(DisplayCulture)] = [("FrenchFrance", 1)],
        };
}
