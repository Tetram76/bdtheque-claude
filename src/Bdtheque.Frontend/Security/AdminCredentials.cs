using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Bdtheque.Frontend.Security;

/// <summary>The check of the credentials of the administrator account, exactly as configured.</summary>
public sealed class AdminCredentials(IOptions<AdminAccountOptions> options)
{
    private readonly byte[] _login = Digest(options.Value.Login);
    private readonly byte[] _password = Digest(options.Value.Password);

    /// <summary>
    /// Compared in a constant time, over digests of the same length, so that the time of a refusal tells nothing of how
    /// much of a value, or of its length, was right.
    /// </summary>
    public bool AreValid(string? login, string? password)
    {
        // Both compared whatever the first one gives, before being combined: no shortcut that would tell the login apart
        // from the password.
        var loginMatches = CryptographicOperations.FixedTimeEquals(Digest(login), _login);
        var passwordMatches = CryptographicOperations.FixedTimeEquals(Digest(password), _password);
        return loginMatches && passwordMatches;
    }

    private static byte[] Digest(string? value) => SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
}
