namespace Bdtheque.Domain.Common;

/// <summary>
/// Validates the check digit of an ISBN-10 or ISBN-13 to flag likely typos on input.
/// This check is advisory only (fonctionnel.md § Validation de l'ISBN): some publishers have
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
