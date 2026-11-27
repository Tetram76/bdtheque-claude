using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Bdtheque.Frontend.Security;

/// <summary>
/// The sessions of the administrator (choix-implementation/socle/transverse/authentification-mise-en-oeuvre.md): how
/// long one lasts, and how all of them are revoked, by a change of the settings of the account.
/// </summary>
public sealed class AdminSessions(IOptions<AdminAccountOptions> options, TimeProvider clock)
{
    /// <summary>Without a request for that long, the session ends: a forgotten browser does not stay signed in.</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(2);

    /// <summary>However active, a session ends after that long, and the administrator signs in again.</summary>
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromHours(12);

    private const string StampClaim = "bdtheque:account-stamp";
    private const string SignedInClaim = "bdtheque:signed-in";

    // A digest of the settings the session was opened with: a new password, or a new secret for a lost phone, and the
    // container restarted, and every session opened before is no longer that of the account.
    private readonly string _stamp = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join('\n', options.Value.Login, options.Value.Password, options.Value.TotpSecret ?? string.Empty))));

    public ClaimsPrincipal SignIn() =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, options.Value.Login),
                new Claim(StampClaim, _stamp),
                new Claim(SignedInClaim, clock.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme));

    /// <summary>Whether the session of <paramref name="principal"/> is still one of the account, and still running.</summary>
    public bool IsCurrent(ClaimsPrincipal principal) =>
        principal.FindFirstValue(StampClaim) is { } stamp
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(stamp), Encoding.UTF8.GetBytes(_stamp))
        && long.TryParse(principal.FindFirstValue(SignedInClaim), NumberStyles.None, CultureInfo.InvariantCulture, out var signedIn)
        && clock.GetUtcNow() - DateTimeOffset.FromUnixTimeSeconds(signedIn) < MaximumLifetime;
}
