using System.ComponentModel.DataAnnotations;

namespace Bdtheque.Frontend.Security;

/// <summary>
/// The single administrator account (contraintes-techniques/architecture-deploiement/authentification.md), supplied by the
/// <c>AdminAccount__Login</c> and <c>AdminAccount__Password</c> environment variables
/// (choix-implementation/socle/transverse/authentification-mise-en-oeuvre.md). Missing, it prevents the start.
/// </summary>
public sealed class AdminAccountOptions
{
    public const string SectionName = "AdminAccount";

    [Required(AllowEmptyStrings = false)]
    public string Login { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The secret of the second factor, in Base32 as authenticator applications take it (<c>AdminAccount__TotpSecret</c>),
    /// required on a deployment exposed in HTTPS (<see cref="AdminSecondFactor.AreSettingsValid"/>).
    /// </summary>
    public string? TotpSecret { get; set; }
}
