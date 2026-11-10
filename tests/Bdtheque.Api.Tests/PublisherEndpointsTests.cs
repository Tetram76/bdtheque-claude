using System.Net;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Errors;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Tests;

/// <summary>Administration of the publishers (<c>/admin/publishers</c>), including their deletion.</summary>
public sealed class PublisherEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PublisherEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateApiClient();
    }

    [Fact]
    public async Task Create_ReturnsThePublisherAsStored()
    {
        var response = await _client.PostAsJsonAsync("/admin/publishers", new PublisherContent(" Dargaud ", " https://www.dargaud.com ", []));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PublisherForm>();
        Assert.Equal(("Dargaud", "https://www.dargaud.com"), (created!.Name, created.Website));
        Assert.Equal($"/admin/publishers/{created.Id}", response.Headers.Location?.OriginalString);
        SameForm(created, await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{created.Id}"));
    }

    [Fact]
    public async Task Create_WithBlankName_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/publishers", new PublisherContent(" ", null, []));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherNameRequired);
    }

    [Fact]
    public async Task Create_WithANameLongerThanTheColumn_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/publishers", new PublisherContent(new string('A', 301), null, []));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.TextTooLong);
    }

    [Fact]
    public async Task Create_WithAWebsiteLongerThanTheColumn_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync(
            "/admin/publishers", new PublisherContent("Éditeur au site interminable", "https://example.com/" + new string('a', 2040), []));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.TextTooLong);
    }

    [Fact]
    public async Task Update_WithANameLongerThanTheColumn_IsABusinessError()
    {
        var publisher = await CreateAsync("Éditeur à rallonger");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(new PublisherContent(new string('B', 301), null, []), publisher.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.TextTooLong);
    }

    [Fact]
    public async Task Create_WithAnInvalidWebsite_IsABusinessError()
    {
        var response = await _client.PostAsJsonAsync("/admin/publishers", new PublisherContent("Glénat", "pas une url", []));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherWebsiteInvalid);
    }

    [Fact]
    public async Task Create_WithANameAlreadyUsed_IsABusinessError()
    {
        await CreateAsync("Casterman");

        var response = await _client.PostAsJsonAsync("/admin/publishers", new PublisherContent("Casterman", null, []));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherNameAlreadyUsed);
    }

    [Fact]
    public async Task Create_WithANameDifferingOnlyByCase_IsAccepted()
    {
        // modele-metier/editeur.md: unique as typed, 'Dargaud' and 'dargaud' remain distinct.
        await CreateAsync("Delcourt");

        var response = await _client.PostAsJsonAsync("/admin/publishers", new PublisherContent("delcourt", null, []));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownPublisher_IsAFunctionalError()
    {
        var response = await _client.GetAsync($"/admin/publishers/{Guid.CreateVersion7()}");

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Update_ReturnsTheNewStateAndVersion()
    {
        var publisher = await CreateAsync("Dupuis");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(new PublisherContent("Dupuis BD", "https://www.dupuis.com", []), publisher.Version));

        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<PublisherForm>())!;
        Assert.Equal(("Dupuis BD", "https://www.dupuis.com"), (updated.Name, updated.Website));
        Assert.NotEqual(publisher.Version, updated.Version);
        SameForm(updated, await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{publisher.Id}"));
    }

    [Fact]
    public async Task Update_ToAnotherPublishersName_IsABusinessError()
    {
        await CreateAsync("Lombard");
        var publisher = await CreateAsync("Le Lombard");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(new PublisherContent("Lombard", null, []), publisher.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherNameAlreadyUsed);
    }

    [Fact]
    public async Task Update_FromAStaleVersion_IsAFunctionalError()
    {
        var publisher = await CreateAsync("Soleil");
        await UpdateAsync(publisher, "Soleil Productions");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(new PublisherContent("Soleil", null, []), publisher.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Update_AfterACollectionWasAdded_IsAFunctionalError()
    {
        // The publisher is the aggregate root of its collections: a write on one changes its version.
        var publisher = await CreateAsync("Bamboo");
        await CreateCollectionAsync(publisher, "Grand Angle");

        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(new PublisherContent("Bamboo Édition", null, []), publisher.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
    }

    [Fact]
    public async Task Delete_OfAPublisherUsedByEditionsAndSeriesTemplates_IsRefusedWithTheImpactNamingThem()
    {
        var publisher = await CreateAsync("Futuropolis");
        await SeedEditionAndSeriesTemplateOfAsync(publisher.Id);
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/publishers/{publisher.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(publisher, impact!.Fingerprint));

        Assert.Equal([new ImpactCount(EntityKind.Series, 1), new ImpactCount(EntityKind.Edition, 1)], impact.BlockedBy);
        var problem = await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.DeletionBlockedByReferences);
        ProblemAssert.SameImpact(impact, ProblemAssert.ImpactOf(problem));
        SameForm(publisher, await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{publisher.Id}"));
    }

    [Fact]
    public async Task Delete_AsConfirmed_RemovesThePublisherWithItsCollections()
    {
        var publisher = await CreateAsync("Vents d'Ouest");
        var first = await CreateCollectionAsync(publisher, "Éclipse");
        await CreateCollectionAsync(publisher with { Version = first.PublisherVersion }, "Étincelle");
        var current = await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{publisher.Id}");
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/publishers/{publisher.Id}/deletion-impact");

        var response = await _client.DeleteAsync(DeleteUri(current!, impact!.Fingerprint));

        Assert.Equal([new ImpactCount(EntityKind.PublisherCollection, 2)], impact.DeletedWith);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        Assert.False(await context.Publishers.AnyAsync(p => p.Id == publisher.Id));
        Assert.False(await context.Set<PublisherCollection>().AnyAsync(c => c.PublisherId == publisher.Id));
    }

    [Fact]
    public async Task Delete_WhoseImpactChangedSinceConfirmation_IsAFunctionalErrorAndKeepsThePublisher()
    {
        var publisher = await CreateAsync("Dargaud Benelux");
        var impact = await _client.GetFromJsonAsync<DeletionImpact>($"/admin/publishers/{publisher.Id}/deletion-impact");
        var newVersion = (await CreateCollectionAsync(publisher, "Nouvelle")).PublisherVersion;

        var response = await _client.DeleteAsync(
            $"/admin/publishers/{publisher.Id}?version={newVersion}&fingerprint={Uri.EscapeDataString(impact!.Fingerprint)}");

        var problem = await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Conflict, ProblemTypes.Functional);
        Assert.Equal([new ImpactCount(EntityKind.PublisherCollection, 1)], ProblemAssert.ImpactOf(problem).DeletedWith);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/admin/publishers/{publisher.Id}")).StatusCode);
    }

    [Fact]
    public async Task Create_WithCollections_ReturnsThemByName_NothingReferencingThem()
    {
        var response = await _client.PostAsJsonAsync("/admin/publishers", new PublisherContent(
            "Casterman Jeunesse", "https://www.casterman.com", [new(null, " Écritures "), new(null, "Âge d'or")]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<PublisherForm>())!;
        Assert.Equal([("Âge d'or", 0, 0), ("Écritures", 0, 0)], created.Collections.Select(c => (c.Name, c.EditionCount, c.SeriesCount)));
        Assert.NotEqual(default, created.CreatedAt);
        Assert.Equal(created.CreatedAt, created.ModifiedAt);
        SameForm(created, await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{created.Id}"));
    }

    [Fact]
    public async Task Get_CountsWhatReferencesEachCollection()
    {
        // visuel/saisie-editeur/contenu.md: « 58 éditions · 3 séries », an edition targeted by an intent included.
        var publisher = await CreateAsync("Glénat", "Treize étrange", "Vécu");
        var thirteen = publisher.Collections.Single(c => c.Name == "Treize étrange");
        await SeedReferencesAsync(publisher.Id, thirteen.Id, editions: 2, series: 1);

        var form = await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{publisher.Id}");

        Assert.Equal([("Treize étrange", 2, 1), ("Vécu", 0, 0)], form!.Collections.Select(c => (c.Name, c.EditionCount, c.SeriesCount)));
    }

    [Fact]
    public async Task Update_RenamesAddsAndRemovesItsCollections_UnderTheVersionOfThePublisher()
    {
        var publisher = await CreateAsync("Delcourt Jeunesse", "Encrages", "Machination", "Contrebande");
        var encrages = publisher.Collections.Single(c => c.Name == "Encrages");
        var contrebande = publisher.Collections.Single(c => c.Name == "Contrebande");

        var response = await _client.PutAsJsonAsync($"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(
            new PublisherContent("Delcourt Jeunesse", null, [new(encrages.Id, "Encrages (poche)"), new(contrebande.Id, "Contrebande"), new(null, "Shampooing")]),
            publisher.Version));

        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<PublisherForm>())!;
        Assert.Equal(
            [(contrebande.Id, "Contrebande"), (encrages.Id, "Encrages (poche)")],
            updated.Collections.Where(c => c.Name != "Shampooing").Select(c => (c.Id, c.Name)));
        Assert.Equal(3, updated.Collections.Count);
        Assert.NotEqual(publisher.Version, updated.Version);
        SameForm(updated, await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{publisher.Id}"));
    }

    [Fact]
    public async Task Update_RemovingACollectionAnEditionOrASeriesReferences_IsABusinessError_AndChangesNothing()
    {
        // fonctionnel/regles-metier/suppression-des-entites.md: a collection referenced cannot be removed.
        foreach (var (editions, series) in new[] { (1, 0), (0, 1) })
        {
            var publisher = await CreateAsync($"Dupuis {editions}{series}", "Repérages", "Aire libre");
            var used = publisher.Collections.Single(c => c.Name == "Repérages");
            var free = publisher.Collections.Single(c => c.Name == "Aire libre");
            await SeedReferencesAsync(publisher.Id, used.Id, editions, series);

            var response = await _client.PutAsJsonAsync($"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(
                new PublisherContent(publisher.Name, null, [new(free.Id, free.Name)]), publisher.Version));

            await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherCollectionInUse);
            var form = await _client.GetFromJsonAsync<PublisherForm>($"/admin/publishers/{publisher.Id}");
            Assert.Equal([free.Id, used.Id], form!.Collections.Select(c => c.Id));
            Assert.Equal(publisher.Version, form.Version);
        }
    }

    [Fact]
    public async Task Update_RenamingACollectionToTheNameOfOneRemoved_OrAddingOneNamedSo_Saves()
    {
        var publisher = await CreateAsync("Paquet", "Cairn", "Calamar", "Discover");
        var cairn = publisher.Collections.Single(c => c.Name == "Cairn");
        var calamar = publisher.Collections.Single(c => c.Name == "Calamar");

        var response = await _client.PutAsJsonAsync($"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(
            new PublisherContent(publisher.Name, null, [new(cairn.Id, "Calamar"), new(null, "Discover")]), publisher.Version));

        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<PublisherForm>())!;
        Assert.Equal(["Calamar", "Discover"], updated.Collections.Select(c => c.Name));
        Assert.Equal(cairn.Id, updated.Collections[0].Id);
        Assert.DoesNotContain(calamar.Id, updated.Collections.Select(c => c.Id));
    }

    [Fact]
    public async Task Update_WithANewCollectionNamedAsAnother_IsABusinessError()
    {
        var publisher = await CreateAsync("Kana", "Big Kana");

        var response = await _client.PutAsJsonAsync($"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(
            new PublisherContent("Kana", null, [new(publisher.Collections[0].Id, "Big Kana"), new(null, "Big Kana")]), publisher.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherCollectionNameAlreadyUsed);
    }

    [Fact]
    public async Task Update_WithABlankCollectionName_IsABusinessError()
    {
        var publisher = await CreateAsync("Pika");

        var response = await _client.PutAsJsonAsync($"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(
            new PublisherContent("Pika", null, [new(null, " ")]), publisher.Version));

        await ProblemAssert.IsBusinessProblemAsync(response, DomainRules.PublisherCollectionNameRequired);
    }

    [Fact]
    public async Task Update_WithACollectionOfAnotherPublisher_IsAFunctionalError()
    {
        var other = await CreateAsync("Ankama", "Label 619");
        var publisher = await CreateAsync("Ki-oon");

        var response = await _client.PutAsJsonAsync($"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(
            new PublisherContent("Ki-oon", null, [new(other.Collections[0].Id, "Label 619")]), publisher.Version));

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound, ProblemTypes.Functional);
    }

    private static void SameForm(PublisherForm expected, PublisherForm? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected with { Collections = [] }, actual with { Collections = [] });
        Assert.Equal(expected.Collections, actual.Collections);
    }

    private static string DeleteUri(PublisherForm publisher, string fingerprint) =>
        $"/admin/publishers/{publisher.Id}?version={publisher.Version}&fingerprint={Uri.EscapeDataString(fingerprint)}";

    private async Task<PublisherForm> CreateAsync(string name, params string[] collections)
    {
        var response = await _client.PostAsJsonAsync(
            "/admin/publishers", new PublisherContent(name, null, [.. collections.Select(c => new PublisherCollectionContent(null, c))]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublisherForm>())!;
    }

    private async Task UpdateAsync(PublisherForm publisher, string name)
    {
        var response = await _client.PutAsJsonAsync(
            $"/admin/publishers/{publisher.Id}", new UpdatePublisherRequest(new PublisherContent(name, null, []), publisher.Version));
        response.EnsureSuccessStatusCode();
    }

    private async Task<PublisherCollectionForm> CreateCollectionAsync(PublisherForm publisher, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/admin/publishers/{publisher.Id}/collections", new CreatePublisherCollectionRequest(name, publisher.Version));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublisherCollectionForm>())!;
    }

    // Editions of albums of their own, and series whose new editions are proposed the collection.
    private async Task SeedReferencesAsync(Guid publisherId, Guid collectionId, int editions, int series)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var publisher = await context.Publishers.Include(p => p.Collections).SingleAsync(p => p.Id == publisherId);
        var collection = publisher.Collections.Single(c => c.Id == collectionId);
        for (var i = 0; i < editions; i++)
        {
            var album = new Album($"Album {Guid.NewGuid():N}", null);
            var edition = new Edition(album, publisher);
            edition.SetPublisher(publisher, collection);
            context.AddRange(album, edition);
        }

        for (var i = 0; i < series; i++)
        {
            var template = new Series($"Série {Guid.NewGuid():N}");
            template.SetTemplate(publisher, collection);
            context.Add(template);
        }

        await context.SaveChangesAsync();
    }

    private async Task SeedEditionAndSeriesTemplateOfAsync(Guid publisherId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>();
        var publisher = await context.Publishers.SingleAsync(p => p.Id == publisherId);
        var album = new Album("Les Cités obscures", null);
        var series = new Series("Aldébaran");
        series.SetTemplate(publisher, null);
        context.AddRange(album, series, new Edition(album, publisher));
        await context.SaveChangesAsync();
    }
}
