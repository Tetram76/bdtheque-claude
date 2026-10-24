using System.Net;
using System.Text;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Frontend.Api;

namespace Bdtheque.Frontend.Tests.Api;

public sealed class ApiClientTests
{
    [Fact]
    public async Task GetAsync_Success_ReadsTheContract()
    {
        var client = ClientAnswering(HttpStatusCode.OK, "application/json",
            """{"items":[{"id":"0199b6a4-0000-7000-8000-000000000001","label":"Aventure"}],"number":1,"size":50,"totalCount":1}""");

        var page = await client.GetAsync<Page<GenreListItem>>("/catalog/genres");

        Assert.Equal("Aventure", Assert.Single(page.Items).Label);
    }

    [Fact]
    public async Task BusinessProblem_CarriesItsRuleCode()
    {
        var client = ClientAnswering(HttpStatusCode.UnprocessableContent, "application/problem+json",
            $$"""{"type":"{{ProblemTypes.Business}}","status":422,"ruleCode":"Genre.LabelRequired"}""");

        var problem = await ProblemOf(() => client.GetAsync<object>("/admin/genres"));

        Assert.Equal(new ApiProblem(ProblemCategory.Business, 422, "Genre.LabelRequired", null), problem);
    }

    [Fact]
    public async Task RefusedDeletion_CarriesItsImpact()
    {
        var client = ClientAnswering(HttpStatusCode.UnprocessableContent, "application/problem+json",
            $$$"""
            {"type":"{{{ProblemTypes.Business}}}","status":422,"ruleCode":"Deletion.BlockedByReferences",
             "impact":{"blockedBy":[{"kind":"Album","count":12}],"associationsRemoved":[],"deletedWith":[],"leavingCollection":[],"fingerprint":"abc"}}
            """);

        var problem = await ProblemOf(() => client.DeleteAsync("/admin/genres/1"));

        var count = Assert.Single(problem.Impact!.BlockedBy);
        Assert.Equal((EntityKind.Album, 12), (count.Kind, count.Count));
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, 409)]
    [InlineData(HttpStatusCode.NotFound, 404)]
    public async Task FunctionalProblem_CarriesItsStatus(HttpStatusCode status, int expected)
    {
        var client = ClientAnswering(status, "application/problem+json", $$"""{"type":"{{ProblemTypes.Functional}}","status":{{expected}}}""");

        var problem = await ProblemOf(() => client.GetAsync<object>("/admin/genres/1"));

        Assert.Equal(new ApiProblem(ProblemCategory.Functional, expected, null, null), problem);
    }

    [Theory]
    [InlineData("application/problem+json", "{\"type\":\"urn:bdtheque:problem:technical\",\"status\":500}")]
    [InlineData("application/problem+json", "{\"type\":\"https://tools.ietf.org/html/rfc9110#section-15.6.1\"}")]
    [InlineData("application/problem+json", "not json")]
    [InlineData("text/html", "<html>Bad gateway</html>")]
    public async Task AnyOtherFailure_IsTechnical(string mediaType, string body)
    {
        var client = ClientAnswering(HttpStatusCode.InternalServerError, mediaType, body);

        var problem = await ProblemOf(() => client.GetAsync<object>("/catalog/genres"));

        Assert.Equal(ProblemCategory.Technical, problem.Category);
        Assert.Null(problem.RuleCode);
    }

    [Fact]
    public async Task UnreachableApi_IsTechnical()
    {
        var client = new ApiClient(new HttpClient(new StubHandler(_ => throw new HttpRequestException("refused")))
        {
            BaseAddress = new Uri("http://api"),
        });

        var problem = await ProblemOf(() => client.GetAsync<object>("/catalog/genres"));

        Assert.Equal(ProblemCategory.Technical, problem.Category);
    }

    [Fact]
    public async Task UnreadableSuccess_IsTechnical()
    {
        var client = ClientAnswering(HttpStatusCode.OK, "application/json", "{ not json");

        var problem = await ProblemOf(() => client.GetAsync<Page<GenreListItem>>("/catalog/genres"));

        Assert.Equal(ProblemCategory.Technical, problem.Category);
    }

    private static ApiClient ClientAnswering(HttpStatusCode status, string mediaType, string body) =>
        new(new HttpClient(new StubHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, mediaType),
        }))
        {
            BaseAddress = new Uri("http://api"),
        });

    private static async Task<ApiProblem> ProblemOf(Func<Task> call) =>
        (await Assert.ThrowsAsync<ApiProblemException>(call)).Problem;

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(answer(request));
    }
}
