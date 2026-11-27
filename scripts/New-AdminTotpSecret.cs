#:package QRCoder

// Creates the secret of the second factor of the administrator account
// (contraintes-techniques/architecture-deploiement/authentification.md), and shows it as a QR code that an authenticator
// application scans. Run with the .NET SDK: dotnet run scripts/New-AdminTotpSecret.cs -- [account name]
//
// The secret is only written to the console: it goes into ADMIN_TOTP_SECRET of the .env of the deployment, and nowhere
// else. A new one, for a lost phone, revokes every session opened before (AdminSessions).

using System.Security.Cryptography;
using QRCoder;

const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
// 160 bits, the length that RFC 4226 recommends and that the application requires (AdminSecondFactor).
const int secretBytes = 20;

var account = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0] : "admin";
var secret = Base32(RandomNumberGenerator.GetBytes(secretBytes));

// The key URI of authenticator applications: SHA-1, six digits and thirty seconds are their defaults, and those of the
// application, so the URI does not repeat them.
var uri = $"otpauth://totp/{Uri.EscapeDataString("BDthèque:" + account)}?secret={secret}&issuer={Uri.EscapeDataString("BDthèque")}";

using var data = QRCodeGenerator.GenerateQrCode(uri, QRCodeGenerator.ECCLevel.M);
Console.WriteLine(new AsciiQRCode(data).GetGraphicSmall());
Console.WriteLine();
Console.WriteLine("Scan this QR code with the authenticator application of the administrator,");
Console.WriteLine("or type the key by hand (time-based, 6 digits, 30 seconds):");
Console.WriteLine();
Console.WriteLine($"ADMIN_TOTP_SECRET={secret}");
Console.WriteLine();
Console.WriteLine("Then set this line in the .env of the deployment, recreate the frontend container, and clear this console.");

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
