namespace Bdtheque.Frontend.Components.Controls;

/// <summary>The names of a new author proposed from a name typed in one piece (a search, the choice of an author).</summary>
public static class AuthorNames
{
    /// <summary>
    /// « Jean Van Hamme »: the first word the first name, the rest the last name; a single word, the last name. The user
    /// corrects them, or turns them into a pseudonym (choix-implementation/socle/frontend.md).
    /// </summary>
    public static (string? FirstName, string LastName) Split(string name)
    {
        var words = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words is [var first, var rest] ? (first, rest) : (null, name.Trim());
    }
}
