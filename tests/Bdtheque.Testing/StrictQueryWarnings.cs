using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bdtheque.Testing;

/// <summary>
/// Turns into exceptions, for the tests, the EF Core warnings that reveal a query to fix: a query
/// loading several collections in a single statement (cartesian product) would otherwise only be
/// noticed in the logs of production.
/// </summary>
public static class StrictQueryWarnings
{
    public static DbContextOptionsBuilder Apply(DbContextOptionsBuilder options) =>
        options.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
}
