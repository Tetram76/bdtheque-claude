namespace Bdtheque.Domain.Common;

/// <summary>
/// Validates the check digit of an ISBN-10 or ISBN-13 to flag likely typos on input.
/// This check is advisory only (fonctionnel/regles-metier/validation-de-l-isbn.md): some publishers have
/// shipped albums with a genuinely incorrect ISBN, so a caller must warn on <c>false</c>
/// rather than reject the value.
/// </summary>
public static class IsbnChecksumValidator
{
    public static bool IsValid(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return false;

        var characters = isbn.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray();

        return characters.Length switch
        {
            10 => IsValidIsbn10(characters),
            13 => IsValidIsbn13(characters),
            _ => false,
        };
    }

    /// <summary>
    /// The ISBN the application supposes for <paramref name="isbn"/> (fonctionnel/regles-metier/validation-de-l-isbn.md, ISBN
    /// corrigé): the characters entered, separators included, their check character alone computed again, so that the ISBN
    /// keeps its format. A consistent ISBN is its own.
    /// </summary>
    /// <returns>
    /// None for an ISBN the check character alone cannot make consistent: neither the length of an ISBN-10 nor that of an
    /// ISBN-13, a character out of place, an ISBN-13 without a Bookland prefix.
    /// </returns>
    public static string? Corrected(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return null;

        // The positions of the significant characters in the text entered: the check character is the last one.
        var positions = Enumerable.Range(0, isbn.Length).Where(i => !char.IsWhiteSpace(isbn[i]) && isbn[i] != '-').ToArray();
        var characters = positions.Select(i => isbn[i]).ToArray();
        var check = characters.Length switch
        {
            10 when characters[..9].All(char.IsAsciiDigit) && IsCheckCharacter10(characters[9]) => CheckCharacter10(characters),
            13 when characters.All(char.IsAsciiDigit) && IsBookland(characters) => CheckCharacter13(characters),
            _ => (char?)null,
        };
        return check is { } c ? string.Concat(isbn.AsSpan(0, positions[^1]), c.ToString(), isbn.AsSpan(positions[^1] + 1)) : null;
    }

    private static bool IsCheckCharacter10(char character) => char.IsAsciiDigit(character) || character is 'X' or 'x';

    private static char CheckCharacter10(char[] characters)
    {
        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (characters[i] - '0') * (10 - i);
        var check = (11 - sum % 11) % 11;
        return check == 10 ? 'X' : (char)('0' + check);
    }

    private static char CheckCharacter13(char[] characters)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += i % 2 == 0 ? characters[i] - '0' : (characters[i] - '0') * 3;
        return (char)('0' + (10 - sum % 10) % 10);
    }

    // Every ISBN-13 is an EAN-13, but not every EAN-13 is an ISBN-13: only the "Bookland" prefixes 978/979 identify one.
    private static bool IsBookland(char[] characters) =>
        characters[0] == '9' && characters[1] == '7' && characters[2] is '8' or '9';

    private static bool IsValidIsbn10(char[] characters)
    {
        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            int digitValue;
            if (i == 9 && (characters[i] == 'X' || characters[i] == 'x'))
                digitValue = 10;
            else if (char.IsAsciiDigit(characters[i]))
                digitValue = characters[i] - '0';
            else
                return false;

            sum += digitValue * (10 - i);
        }

        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(char[] characters)
    {
        // Without this check, a checksum-valid but non-book EAN-13 (e.g. pasted from the wrong source) would be reported as a
        // valid ISBN.
        if (!IsBookland(characters))
            return false;

        var sum = 0;
        for (var i = 0; i < 13; i++)
        {
            if (!char.IsAsciiDigit(characters[i]))
                return false;

            var digitValue = characters[i] - '0';
            sum += i % 2 == 0 ? digitValue : digitValue * 3;
        }

        return sum % 10 == 0;
    }
}
