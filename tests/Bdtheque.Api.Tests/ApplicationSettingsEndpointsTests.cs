using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The setting of the application (<c>/catalog/settings</c>, <c>/admin/settings</c>), present from the first
/// startup, read by the consultation and modified by the administration under its version.
/// </summary>
/// <remarks>The setting being a single record, each test reads the version it modifies.</remarks>
public sealed class ApplicationSettingsEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApplicationSettingsEndpointsTests(ApiWebApplicationFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Read_OnAFreshDeployment_GivesTheDefaults()
    {
        // Run on the database of this class, which no other test modifies before it reads (each test of the class
        // that modifies the setting writes back the default it found).
        var settings = await _client.GetFromJsonAsync<ApplicationSettings>("/catalog/settings");

        Assert.Equal(new ApplicationSettings(AlbumLabelFormat.TitleSeriesVolume, DisplayCulture.FrenchFrance), settings);
    }

    [Fact]
    public async Task Update_AtTheCurrentVersion_SavesAndReturnsTheNewVersion()
    {
        var form = await ReadFormAsync();

        var response = await _client.PutAsJsonAsync(
            "/admin/settings", new UpdateApplicationSettingsRequest(AlbumLabelFormat.VolumeTitleSeries, DisplayCulture.FrenchFrance, form.Version));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<ApplicationSettingsForm>())!;
        Assert.Equal(AlbumLabelFormat.VolumeTitleSeries, updated.AlbumLabelFormat);
        Assert.NotEqual(form.Version, updated.Version);
        Assert.Equal(updated, await ReadFormAsync());
        Assert.Equal(
            new ApplicationSettings(AlbumLabelFormat.VolumeTitleSeries, DisplayCulture.FrenchFrance),
            await _client.GetFromJsonAsync<ApplicationSettings>("/catalog/settings"));

        await RestoreAsync(form, updated.Version);
    }

    [Fact]
    public async Task Update_AtAStaleVersion_IsAFunctionalError()
    {
        var form = await ReadFormAsync();
        var current = await UpdateAsync(form.AlbumLabelFormat, form.Version);

        var response = await _client.PutAsJsonAsync(
            "/admin/settings", new UpdateApplicationSettingsRequest(AlbumLabelFormat.VolumeTitleSeries, DisplayCulture.FrenchFrance, form.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(current, await ReadFormAsync());
    }

    [Fact]
    public async Task Update_IsRecordedInTheChangeLog()
    {
        var form = await ReadFormAsync();
        var updated = await UpdateAsync(AlbumLabelFormat.VolumeTitleSeries, form.Version);
        await RestoreAsync(form, updated.Version);

        var entries = (await _client.GetFromJsonAsync<Page<ChangeLogEntryItem>>("/admin/change-log?pageSize=2"))!.Items;

        // The most recent first: the restoration, then the modification.
        Assert.All(entries, e => Assert.Equal((RecordType.ApplicationSettings, ChangeLogOperation.Modification, (RecordLabel)new ApplicationSettingsLabel()),
            (e.RecordType, e.Operation, e.Label)));
        Assert.Equal(
            new ChangeLogChange(
                ChangeLogAttribute.AlbumLabelFormat,
                new EnumValue(nameof(AlbumLabelFormat), nameof(AlbumLabelFormat.TitleSeriesVolume)),
                new EnumValue(nameof(AlbumLabelFormat), nameof(AlbumLabelFormat.VolumeTitleSeries))),
            Assert.Single(entries[1].Changes));
    }

    private async Task<ApplicationSettingsForm> ReadFormAsync() =>
        (await _client.GetFromJsonAsync<ApplicationSettingsForm>("/admin/settings"))!;

    private async Task<ApplicationSettingsForm> UpdateAsync(AlbumLabelFormat format, uint version)
    {
        var response = await _client.PutAsJsonAsync("/admin/settings", new UpdateApplicationSettingsRequest(format, DisplayCulture.FrenchFrance, version));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApplicationSettingsForm>())!;
    }

    private Task<ApplicationSettingsForm> RestoreAsync(ApplicationSettingsForm original, uint version) => UpdateAsync(original.AlbumLabelFormat, version);
}
