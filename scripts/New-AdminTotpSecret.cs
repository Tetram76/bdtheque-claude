#:package QRCoder
// A file-based app is published with native AOT by default, which restores the ILCompiler package; this script is only
// ever run, never published.
#:property PublishAot=false

// The secret of the second factor of the administrator account
// (contraintes-techniques/architecture-deploiement/authentification.md), shown as a QR code that an authenticator
// application scans. Run with the .NET SDK:
//   dotnet run scripts/New-AdminTotpSecret.cs                  creates a new secret, for ADMIN_TOTP_SECRET of the .env
//   dotnet run scripts/New-AdminTotpSecret.cs -- <current key>  shows the secret already declared, to register an
//                                                              authenticator again (new or second phone), nothing else
//                                                              changing
// The secret is only written to the console. A new one revokes every session opened before (AdminSessions); showing the
// current one revokes nothing.

using System.Security.Cryptography;
using QRCoder;

const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
// 160 bits, the length that RFC 4226 recommends and that the application requires (AdminSecondFactor): 32 symbols.
const int secretBytes = 20;
const int secretSymbols = secretBytes * 8 / 5;

var existing = args.Length > 0 ? Normalize(string.Concat(args)) : null;
if (existing is not null && (existing.Length < secretSymbols || existing.Any(symbol => !alphabet.Contains(symbol))))
{
    Console.Error.WriteLine($"Not a key of the second factor: {secretSymbols} letters A-Z or digits 2-7 at least, as ADMIN_TOTP_SECRET holds it.");
    return 1;
}

var secret = existing ?? Base32(RandomNumberGenerator.GetBytes(secretBytes));

// The key URI of authenticator applications: SHA-1, six digits and thirty seconds are their defaults, and those of the
// application, so the URI does not repeat them.
var uri = $"otpauth://totp/{Uri.EscapeDataString("BDthèque:admin")}?secret={secret}&issuer={Uri.EscapeDataString("BDthèque")}";

using var data = QRCodeGenerator.GenerateQrCode(uri, QRCodeGenerator.ECCLevel.M);
Console.WriteLine(new AsciiQRCode(data).GetGraphicSmall());
Console.WriteLine();
Console.WriteLine("Scan this QR code with the authenticator application of the administrator,");
Console.WriteLine("or type the key by hand (time-based, 6 digits, 30 seconds):");
Console.WriteLine();
Console.WriteLine($"ADMIN_TOTP_SECRET={secret}");
Console.WriteLine();
Console.WriteLine(existing is null
    ? "This is a new secret: set this line in the .env of the deployment, recreate the frontend container, then clear this console."
    : "This is the secret already declared: nothing to change in the .env. Clear this console.");
return 0;

// As authenticators and the .env show a key: in any case, in groups separated by spaces, padding optional.
static string Normalize(string key) => key.Replace(" ", string.Empty, StringComparison.Ordinal).TrimEnd('=').ToUpperInvariant();

static string Base32(byte[] bytes)
{
    var text = new System.Text.StringBuilder((bytes.Length * 8 + 4) / 5);
    int buffer = 0, bits = 0;
    foreach (var value in bytes)
    {
        buffer = (buffer << 8) | value;
        bits += 8;
        while (bits >= 5)
        {
            bits -= 5;
            text.Append(alphabet[(buffer >> bits) & 0x1F]);
        }
    }

    if (bits > 0)
        text.Append(alphabet[(buffer << (5 - bits)) & 0x1F]);
    return text.ToString();
}
