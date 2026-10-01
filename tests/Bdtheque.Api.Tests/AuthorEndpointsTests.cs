using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Errors;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Api.Tests;

/// <summary>Administration of the authors (<c>/admin/authors</c>), including their deletion.</summary>
public sealed class AuthorEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthorEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Create_ReturnsTheAuthorAsStored()
    {
        var response = await _client.PostAsJsonAsync(
            "/admin/authors", new CreateAuthorRequest(" Remi ", " Georges ", " Hergé ", " Créateur de Tintin ", " belge "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AuthorForm>();
        Assert.Equal(("Remi", "Georges", "Hergé", "Créateur de Tintin", "belge"),
            (created!.LastName, created.FirstName, created.Pseudonym, created.Biography, created.Nationality));
        Assert.Equal($"/admin/authors/{created.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(created, await _client.GetFromJsonAsync<AuthorForm>($"/admin/authors/{created.Id}"));
    }

    [Fact]
    public async Task Create_WithPseudonymOnly_IsAccepted()
    {
        var response = await _client.PostAsJsonAsync("/admin/authors", new CreateAuthorRequest(null, null, "Moebius", null, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutLastNameNorPseudonym_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/authors", new CreateAuthorRequest(" ", "Jean", null, null, null));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AuthorLastNameOrPseudonymRequired);
    }

    [Fact]
    public async Task Create_WithATextLongerThanItsColumn_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync(
            "/admin/authors", new CreateAuthorRequest(new string('x', 5000), null, null, null, null));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.TextTooLong);
    }

    [Fact]
    public async Task Get_UnknownAuthor_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/authors/{Guid.CreateVersion7()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Update_AtTheCurrentVersion_SavesAllFieldsAndReturnsTheNewVersion()
    {
        var author = await CreateAsync(UniqueName());

        var response = await _client.PutAsJsonAsync(
            $"/admin/authors/{author.Id}", new UpdateAuthorRequest("Giraud", "Jean", "Moebius", "Bio", "française", author.Version));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AuthorForm>();
        Assert.Equal(("Giraud", "Jean", "Moebius", "Bio", "française"),
            (updated!.LastName, updated.FirstName, updated.Pseudonym, updated.Biography, updated.Nationality));
        Assert.NotEqual(author.Version, updated.Version);
        Assert.Equal(updated, await _client.GetFromJsonAsync<AuthorForm>($"/admin/authors/{author.Id}"));
    }

    [Fact]
    public async Task Update_ClearingBothLastNameAndPseudonym_IsABusinessErrorAndKeepsTheAuthor()
    {
        var author = await CreateAsync(UniqueName());

        var response = await _client.PutAsJsonAsync(
            $"/admin/authors/{author.Id}", new UpdateAuthorRequest(null, "Jean", null, null, null, author.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.AuthorLastNameOrPseudonymRequired);
        Assert.Equal(author, await _client.GetFromJsonAsync<AuthorForm>($"/admin/authors/{author.Id}"));
    }

    [Fact]
    public async Task Update_FromAStaleVersion_IsAFunctionalErrorAndKeepsTheOtherModification()
    {
        var author = await CreateAsync(UniqueName());
        var otherTab = UniqueName();
        await _client.PutAsJsonAsync($"/admin/authors/{author.Id}", new UpdateAuthorRequest(otherTab, null, null, null, null, author.Version));

        var response = await _client.PutAsJsonAsync(
            $"/admin/authors/{author.Id}", new UpdateAuthorRequest(UniqueName(), null, null, null, null, author.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal(otherTab, (await _client.GetFromJsonAsync<AuthorForm>($"/admin/authors/{author.Id}"))!.LastName);
    }

    [Fact]
    public async Task Update_UnknownAuthor_IsAFunctionalError()
    {
        var response = await _client.PutAsJsonAsync(
            $"/admin/authors/{Guid.CreateVersion7()}", new UpdateAuthorRequest(UniqueName(), null, null, null, null, 1));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task DeletionImpact_CountsTheAlbumsAndSeriesCreditingTheAuthorOnceEach()
    {
        var author = await CreateAsync(UniqueName());
        // Two roles on the same record count as one record: the impact counts records, not credits.
        await SeedCreditsAsync(author.Id, albums: 2, series: 1, rolesPerRecord: 2);

        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/authors/{author.Id}/deletion-impact");

        Assert.Equal([new ImpactCount(EntityKind.Album, 2), new ImpactCount(EntityKind.Series, 1)], impact!.BlockedBy);
        Assert.Empty(impact.AssociationsRemoved);
        Assert.Empty(impact.DeletedWith);
        Assert.NotEmpty(impact.Fingerprint);
    }

    [Fact]
    public async Task DeletionImpact_OfAnUnknownAuthor_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/authors/{Guid.CreateVersion7()}/deletion-impact");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_OfAnAuthorWithoutCredits_Succeeds()
    {
        var author = await CreateAsync(UniqueName());
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/authors/{author.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(author, impact!.Fingerprint));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await ProblemAssert.IsProblemAsync(
            await _client.GetAsync($"/admin/authors/{author.Id}"), HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_OfACreditedAuthor_IsRefusedWithTheBlockingImpact()
    {
        var author = await CreateAsync(UniqueName());
        await SeedCreditsAsync(author.Id, albums: 1, series: 1, rolesPerRecord: 1);
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/authors/{author.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(author, impact!.Fingerprint));

        var problem = await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.DeletionBlockedByReferences);
        ProblemAssert.SameImpact(impact, ProblemAssert.ImpactOf(problem));
        Assert.Equal(author, await _client.GetFromJsonAsync<AuthorForm>($"/admin/authors/{author.Id}"));
    }

    [Fact]
    public async Task Delete_FromAStaleVersion_IsAFunctionalError()
    {
        var author = await CreateAsync(UniqueName());
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/authors/{author.Id}/deletion-impact");
        await _client.PutAsJsonAsync($"/admin/authors/{author.Id}", new UpdateAuthorRequest(UniqueName(), null, null, null, null, author.Version));

        var response = await _client.DeleteAsync(DeleteUri(author, impact!.Fingerprint));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    private static string UniqueName() => $"Auteur {Guid.NewGuid():N}";

    private static string DeleteUri(AuthorForm author, string fingerprint) =>
        $"/admin/authors/{author.Id}?version={author.Version}&fingerprint={Uri.EscapeDataString(fingerprint)}";

    private async Task<AuthorForm> CreateAsync(string lastName)
    {
        var response = await _client.PostAsJsonAsync("/admin/authors", new CreateAuthorRequest(lastName, null, null, null, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthorForm>())!;
    }

    private async Task SeedCreditsAsync(Guid authorId, int albums, int series, int rolesPerRecord)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var author = await context.Authors.SingleAsync(a => a.Id == authorId);
        ContributionRole[] roles = [ContributionRole.Scenarist, ContributionRole.Illustrator, ContributionRole.Colorist];
        for (var i = 0; i < albums; i++)
        {
            var album = new Album(UniqueName(), null);
            context.Albums.Add(album);
            foreach (var role in roles.Take(rolesPerRecord))
                context.Contributions.Add(Contribution.ForAlbum(album, author, role));
        }

        for (var i = 0; i < series; i++)
        {
            var oneSeries = new Series(UniqueName());
            context.Series.Add(oneSeries);
            foreach (var role in roles.Take(rolesPerRecord))
                context.Contributions.Add(Contribution.ForSeriesTemplate(oneSeries, author, role));
        }

        await context.SaveChangesAsync();
    }
}
