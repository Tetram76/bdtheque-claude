using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure;

/// <summary>
/// Provisions the database with the setting of the application (choix-implementation/domaine.md § Paramétrage de
/// l'application : mise en œuvre), a record always present that the application never creates.
/// </summary>
public static class ApplicationSettingsProvisioning
{
    internal const string Table = "ApplicationSettings";

    /// <summary>
    /// Inserts the setting with its defaults, dated by the application clock, unless the database already has it: run
    /// at every startup of <c>api</c>, it keeps the setting as the administration left it.
    /// </summary>
    /// <remarks>
    /// Written outside <c>SaveChangesAsync</c>, which would record a creation in the change log: the setting is part
    /// of the provisioned database, like its schema, and no write of the user creates it.
    /// </remarks>
    public static async Task ProvisionApplicationSettingsAsync(this BdthequeDbContext context, CancellationToken cancellationToken = default)
    {
        var now = context.Now();
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "ApplicationSettings" ("Id", "AlbumLabelFormat", "DisplayCulture", "CreatedAt", "ModifiedAt")
            VALUES ({ApplicationSettings.SingletonId}, {(int)ApplicationSettings.DefaultAlbumLabelFormat},
                    {(int)ApplicationSettings.DefaultDisplayCulture}, {now}, {now})
            ON CONFLICT ("Id") DO NOTHING
            """,
            cancellationToken);
    }
}
