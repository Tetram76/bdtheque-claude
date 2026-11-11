namespace Bdtheque.Domain.Common;

/// <summary>
/// The ISBN as it is saved: trimmed, its check character in capitals. The check character of an ISBN-10 worth 10 is an X,
/// which ISBN writes in capitals (fonctionnel/regles-metier/validation-de-l-isbn.md); it is often typed in lower case.
/// The ISBN is otherwise kept as entered, separators included, its check digit never blocking it.
/// </summary>
public static class IsbnText
{
    public static string? Normalize(string? isbn) => DomainText.NullIfBlank(isbn)?.ToUpperInvariant();
}
