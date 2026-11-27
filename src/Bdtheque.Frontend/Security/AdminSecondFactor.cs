using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Bdtheque.Frontend.Hosting;
using Microsoft.Extensions.Options;

namespace Bdtheque.Frontend.Security;

/// <summary>
/// The second factor of the administrator account (contraintes-techniques/architecture-deploiement/authentification.md):
/// a time-based one-time code (RFC 6238) with the defaults of authenticator applications, HMAC-SHA1, six digits and a
/// step of thirty seconds, the only parameters every one of them reads from a key typed by hand.
/// </summary>
public sealed class AdminSecondFactor
{
    public const int Digits = 6;

    // RFC 4226, § 4 (R6): a shared secret of at least 128 bits, 160 recommended, the length of an HMAC-SHA1 key.
    public const int MinimumSecretBytes = 20;

    public static readonly TimeSpan Step = TimeSpan.FromSeconds(30);

    // RFC 6238, § 5.2: one step either way, for the clock of the phone and the time it takes to type the code.
    private const int ToleratedSteps = 1;

    private readonly byte[]? _secret;
    private readonly TimeProvider _clock;
    private readonly Lock _lock = new();
    private long _lastAcceptedStep = long.MinValue;

    public AdminSecondFactor(IOptions<AdminAccountOptions> options, TimeProvider clock)
    {
        _secret = string.IsNullOrWhiteSpace(options.Value.TotpSecret) ? null : DecodeBase32(options.Value.TotpSecret);
        _clock = clock;
    }

    /// <summary>Whether the login asks for a code: only with a secret, which an HTTPS public address requires.</summary>
    public bool IsRequired => _secret is not null;

    /// <summary>
    /// Whether <paramref name="code"/> is that of a step around now, accepted once only (RFC 6238, § 5.2): a code seen
    /// over the shoulder, or caught on its way, no longer signs in once used, nor does any code of a step before it.
    /// </summary>
    public bool IsValid(string? code)
    {
        if (_secret is null || Normalize(code) is not { } typed)
            return false;

        var now = _clock.GetUtcNow().ToUnixTimeSeconds() / (long)Step.TotalSeconds;
        lock (_lock)
        {
            // Every step of the window computed and compared, whatever an earlier one gives: the time of a refusal
            // tells nothing of which step was close.
            long? matched = null;
            for (var step = now - ToleratedSteps; step <= now + ToleratedSteps; step++)
            {
                if (CryptographicOperations.FixedTimeEquals(typed, Encoding.ASCII.GetBytes(Code(_secret, step))) && step > _lastAcceptedStep)
                    matched = step;
            }

            if (matched is not { } accepted)
                return false;

            _lastAcceptedStep = accepted;
            return true;
        }
    }

    /// <summary>
    /// Whether the settings of the account can start the application: a secret, if any, of at least 160 bits in
    /// Base32, and one always on a deployment exposed in HTTPS, so that no forgotten setting leaves it to the password
    /// alone.
    /// </summary>
    public static bool AreSettingsValid(AdminAccountOptions account, PublicAddressOptions publicAddress)
    {
        if (string.IsNullOrWhiteSpace(account.TotpSecret))
            return publicAddress.ParsedUrl?.Scheme != Uri.UriSchemeHttps;
        return DecodeBase32(account.TotpSecret) is { Length: >= MinimumSecretBytes };
    }

    // RFC 4226, § 5.3 (HOTP), over the counter of the step (RFC 6238, § 4.2).
    private static string Code(byte[] secret, long step)
    {
        Span<byte> counter = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);
        Span<byte> hash = stackalloc byte[HMACSHA1.HashSizeInBytes];
        HMACSHA1.HashData(secret, counter, hash);
        var offset = hash[^1] & 0x0F;
        var binary = BinaryPrimitives.ReadInt32BigEndian(hash[offset..]) & 0x7FFFFFFF;
        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    // Authenticators show the code in two groups of three digits.
    private static byte[]? Normalize(string? code)
    {
        var digits = code?.Replace(" ", string.Empty, StringComparison.Ordinal);
        return digits is { Length: Digits } && digits.All(char.IsAsciiDigit) ? Encoding.ASCII.GetBytes(digits) : null;
    }

    // RFC 4648, § 6, as authenticators show a key: in any case, in groups separated by spaces, padding optional.
    private static byte[]? DecodeBase32(string text)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var symbols = text.Replace(" ", string.Empty, StringComparison.Ordinal).TrimEnd('=').ToUpperInvariant();
        var bytes = new List<byte>(symbols.Length * 5 / 8);
        int buffer = 0, bits = 0;
        foreach (var symbol in symbols)
        {
            var value = alphabet.IndexOf(symbol, StringComparison.Ordinal);
            if (value < 0)
                return null;
            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)(buffer >> bits));
                buffer &= (1 << bits) - 1;
            }
        }

        return bytes.Count == 0 ? null : [.. bytes];
    }
}
