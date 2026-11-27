using System.Diagnostics.CodeAnalysis;

namespace Bdtheque.Infrastructure;

/// <summary>
/// Built-in functions of PostgreSQL that the provider does not translate, mapped by <see cref="BdthequeDbContext"/>: usable in a
/// query only, which the database runs.
/// </summary>
public static class DatabaseFunctions
{
    /// <summary>
    /// The function <c>translate</c>: each character of <paramref name="text"/> found in <paramref name="from"/> replaced by the
    /// one at the same index of <paramref name="to"/>, removed when <paramref name="to"/> has none. It compares characters, not
    /// texts, so that PostgreSQL accepts it under a nondeterministic collation, where it refuses <c>replace</c>
    /// (choix-implementation/persistance/collation-des-colonnes-texte.md).
    /// </summary>
    /// <exception cref="InvalidOperationException">Called outside a query.</exception>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? Translate(string? text, string from, string to) =>
        throw new InvalidOperationException($"{nameof(Translate)} is translated into SQL, and runs in the database only.");
}
