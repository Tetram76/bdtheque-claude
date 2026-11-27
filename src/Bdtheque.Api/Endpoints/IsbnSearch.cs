using System.Linq.Expressions;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// An ISBN compared without its separators, on both sides — the ISBN searched and the ISBN entered —, by the lists and by the
/// advanced search alike (choix-implementation/api/recherche.md): an ISBN is entered as printed, with or without hyphens and
/// spaces, and often pasted from an office application, whose spaces and hyphens may be any of their typographic variants.
/// </summary>
internal static class IsbnSearch
{
    /// <summary>The spaces and hyphens an ISBN is compared without, their typographic variants included.</summary>
    internal const string Separators = " -" + SearchText.Spaces + SearchText.Dashes;

    /// <summary>The ISBN entered, without its separators, as the database compares it.</summary>
    public static readonly Expression<Func<Edition, string?>> Normalized = e => DatabaseFunctions.Translate(e.Isbn, Separators, "");

    /// <summary>The ISBN searched, without its separators.</summary>
    public static string Normalize(string isbn) => string.Concat(isbn.Where(character => !Separators.Contains(character)));
}
