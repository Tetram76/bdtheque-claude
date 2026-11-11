using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bdtheque.Infrastructure;

/// <summary>
/// Uniqueness checked when the transaction commits, rather than row by row: a deferrable unique constraint
/// (choix-implementation/persistance/unicite-verifiee-a-la-validation.md).
/// </summary>
/// <remarks>
/// For a uniqueness that one save of an aggregate can permute (two collections of a publisher swapping their
/// names). EF Core orders the commands of a save along the unique indexes of its model, and cannot order two
/// updates each taking the value of the other (dotnet/efcore#15319); PostgreSQL checks a non-deferrable unique
/// index row by row, even within one statement. The index is therefore declared non-unique in the model, which
/// EF Core no longer orders by, and <see cref="BdthequeMigrationsSqlGenerator"/> creates it as a deferrable
/// unique constraint of the same name.
/// </remarks>
public static class DeferredUniqueness
{
    /// <summary>Annotation marking an index of the model as unique at commit.</summary>
    public const string Annotation = "Bdtheque:UniqueAtCommit";

    /// <summary>Makes the index unique, checked when the transaction commits.</summary>
    internal static IndexBuilder<T> IsUniqueAtCommit<T>(this IndexBuilder<T> index) =>
        index.IsUnique(false).HasAnnotation(Annotation, true);

    /// <summary>Whether the index is unique, checked when the transaction commits.</summary>
    public static bool IsUniqueAtCommit(this IReadOnlyIndex index) => index[Annotation] is true;
}
