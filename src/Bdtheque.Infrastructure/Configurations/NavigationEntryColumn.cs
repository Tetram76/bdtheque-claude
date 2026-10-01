using Bdtheque.Domain.Common;

namespace Bdtheque.Infrastructure.Configurations;

/// <summary>
/// Shared mapping of the stored initial-based navigation entry (series, albums, authors).
/// </summary>
internal static class NavigationEntryColumn
{
    public const string Name = "NavigationEntry";
    public const int MaxLength = 1;

    // Defence in depth for out-of-band writes (raw SQL, future Firebird import tool): the
    // entry's consistency with the sort key cannot be checked by the database — that would
    // reimplement the French collation rules in SQL — but its set of values can.
    public static readonly string ValidValuesSql =
        $"\"{Name}\" IN ({string.Join(", ", NavigationEntryCalculator.Entries.Select(entry => $"'{entry}'"))})";
}
