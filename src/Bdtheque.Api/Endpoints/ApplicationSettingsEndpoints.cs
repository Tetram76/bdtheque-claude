using Bdtheque.Contracts.Admin;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using CatalogSettings = Bdtheque.Contracts.Catalog.ApplicationSettings;
using ContractEnums = Bdtheque.Contracts.Enums;
using DomainEnums = Bdtheque.Domain.Enums;

namespace Bdtheque.Api.Endpoints;

/// <summary>
/// The setting of the application (modele-metier.md § Paramétrage de l'application): read by the consultation
/// (<c>/catalog/settings</c>) and by the administration, which modifies it (<c>/admin/settings</c>). A single record,
/// always present, addressed without identifier and neither created nor deleted.
/// </summary>
internal static class ApplicationSettingsEndpoints
{
    public static void MapApplicationSettings(this RouteGroupBuilder admin)
    {
        admin.MapGet("/settings", GetFormAsync);
        admin.MapPut("/settings", UpdateAsync);
    }

    public static void MapApplicationSettingsCatalog(this RouteGroupBuilder catalog) =>
        catalog.MapGet("/settings", GetAsync);

    private static async Task<CatalogSettings> GetAsync(BdthequeDbContext context, CancellationToken cancellationToken)
    {
        var settings = await context.ApplicationSettings.AsNoTracking().SingleAsync(cancellationToken);
        return new CatalogSettings(FormatOf(settings), CultureOf(settings));
    }

    private static async Task<ApplicationSettingsForm> GetFormAsync(BdthequeDbContext context, CancellationToken cancellationToken) =>
        ToForm(context, await context.ApplicationSettings.SingleAsync(cancellationToken));

    private static async Task<ApplicationSettingsForm> UpdateAsync(
        UpdateApplicationSettingsRequest request, BdthequeDbContext context, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var settings = await context.LoadAggregateForWriteAsync<ApplicationSettings>(
            ApplicationSettings.SingletonId, request.Version, cancellationToken: cancellationToken);
        settings.Set(
            EnumMapping.Map<DomainEnums.AlbumLabelFormat>(request.AlbumLabelFormat)!.Value,
            EnumMapping.Map<DomainEnums.DisplayCulture>(request.DisplayCulture)!.Value);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToForm(context, settings);
    }

    private static ApplicationSettingsForm ToForm(BdthequeDbContext context, ApplicationSettings settings) =>
        new(FormatOf(settings), CultureOf(settings), context.VersionOf(settings));

    private static ContractEnums.AlbumLabelFormat FormatOf(ApplicationSettings settings) =>
        EnumMapping.Map<ContractEnums.AlbumLabelFormat>(settings.AlbumLabelFormat)!.Value;

    private static ContractEnums.DisplayCulture CultureOf(ApplicationSettings settings) =>
        EnumMapping.Map<ContractEnums.DisplayCulture>(settings.DisplayCulture)!.Value;
}
