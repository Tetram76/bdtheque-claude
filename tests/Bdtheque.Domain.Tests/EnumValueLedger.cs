using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// Append-only historical record of every (member name, explicit value) pair ever assigned to
/// each domain enum — including retired members whose C# declaration has since been removed.
/// Never remove, rename, or renumber an entry here when a member is removed from its enum: doing
/// so would let a later member silently reuse a value that already means something different in
/// already-persisted rows (see contraintes-techniques.md § Conventions de persistance). Add a new
/// entry whenever a new member is introduced; a retired member's entry stays forever so its value
/// remains reserved.
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
                [("Excellent", 1), ("VeryGood", 2), ("Good", 3), ("Poor", 4), ("VeryPoor", 5)],
            [typeof(EditionFormat)] = [("Pocket", 1), ("Medium", 2), ("Standard", 3), ("Large", 4), ("Special", 5)],
            [typeof(ReadingDirection)] = [("LeftToRight", 1), ("RightToLeft", 2)],
            [typeof(SeriesStatus)] = [("InProgress", 1), ("Completed", 2), ("Abandoned", 3)],
            [typeof(AcquisitionMode)] = [("Purchase", 1), ("Gift", 2), ("Trade", 3), ("Won", 4), ("Inherited", 5)],
        };
}
