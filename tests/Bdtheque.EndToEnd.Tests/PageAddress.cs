using System.Text.RegularExpressions;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The address a page is expected to reach, asserted by <c>Assertions.Expect(page).ToHaveURLAsync</c>, which the driver
/// polls in a single call: on failure, it tells the address reached. Never through the waits of the client that check what
/// they received before subscribing to what comes, and lose what arrives in between (BannedSymbols.txt).
/// </summary>
internal static class PageAddress
{
    /// <summary>The address of <paramref name="path"/> with exactly <paramref name="query"/>, none by default.</summary>
    public static Regex Of(string path, string query = "") => new($@"^[a-z]+://[^/]+{Regex.Escape(path + query)}$");

    /// <summary>The address of <paramref name="path"/>, whatever its query.</summary>
    public static Regex OfPath(string path) => new($@"^[a-z]+://[^/]+{Regex.Escape(path)}(\?.*)?$");
}
