using System.Linq.Expressions;
using System.Reflection;
using Bdtheque.Domain.Entities;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// An ISBN compared without its separators, on both sides — the ISBN searched and the ISBN entered —, by the lists and by the
/// advanced search alike (choix-implementation/api/recherche.md): an ISBN is entered as printed, with or without hyphens and
/// spaces, and often pasted from an office application, whose spaces and hyphens may be non-breaking.
/// </summary>
internal static class IsbnSearch
{
    // Spaces (ordinary, non-breaking, narrow non-breaking, thin) and hyphens (hyphen-minus, hyphen, non-breaking hyphen,
    // figure dash, en dash).
    private static readonly string[] Separators = [" ", "\u00A0", "\u202F", "\u2009", "-", "\u2010", "\u2011", "\u2012", "\u2013"];

    private static readonly MethodInfo Replace = typeof(string).GetMethod(nameof(string.Replace), [typeof(string), typeof(string)])!;

    /// <summary>The ISBN entered, without its separators, as the database compares it.</summary>
    public static readonly Expression<Func<Edition, string?>> Normalized = BuildNormalized();

    /// <summary>The ISBN searched, without its separators.</summary>
    public static string Normalize(string isbn) =>
        Separators.Aggregate(isbn, (text, separator) => text.Replace(separator, "", StringComparison.Ordinal));

    // e.Isbn.Replace(" ", "").Replace("\u00A0", "")…, which EF Core translates into nested REPLACE.
    private static Expression<Func<Edition, string?>> BuildNormalized()
    {
        var edition = Expression.Parameter(typeof(Edition), "e");
        Expression isbn = Expression.Property(edition, nameof(Edition.Isbn));
        foreach (var separator in Separators)
            isbn = Expression.Call(isbn, Replace, Expression.Constant(separator), Expression.Constant(""));
        return Expression.Lambda<Func<Edition, string?>>(isbn, edition);
    }
}
