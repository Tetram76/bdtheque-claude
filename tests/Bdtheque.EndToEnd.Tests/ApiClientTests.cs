extern alias frontend;

using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using frontend::Bdtheque.Frontend.Api;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>The client of <c>frontend</c>, as configured by the application, against the real API.</summary>
[Collection(ApplicationCollection.Name)]
public sealed class ApiClientTests(ApplicationHost application)
{
    [Fact]
    public async Task Call_ReachesTheApi_WithTheInternalKey()
    {
        var page = await Client().GetAsync<Page<GenreListItem>>("/catalog/genres");

        Assert.NotNull(page.Items);
    }

    [Fact]
    public async Task BrokenRule_IsABusinessProblem_WithItsCode()
    {
        var exception = await Assert.ThrowsAsync<ApiProblemException>(() =>
            Client().PostAsync<CreateGenreRequest, GenreForm>("/admin/genres", new CreateGenreRequest("")));

        Assert.Equal((ProblemCategory.Business, "Genre.LabelRequired"), (exception.Problem.Category, exception.Problem.RuleCode));
    }

    [Fact]
    public async Task UnknownRecord_IsAFunctionalProblem()
    {
        var exception = await Assert.ThrowsAsync<ApiProblemException>(() =>
            Client().GetAsync<GenreForm>($"/admin/genres/{Guid.CreateVersion7()}"));

        Assert.Equal((ProblemCategory.Functional, 404), (exception.Problem.Category, exception.Problem.Status));
    }

    private ApiClient Client() => application.FrontendServices.GetRequiredService<ApiClient>();
}
