using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Errors;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Formatting;
using Bdtheque.Frontend.Tests.Audience;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Pages;

/// <summary>
/// The endpoints the pages of the administration read, each answering what a test sets for its path (without its query),
/// or the error status it sets; the audience ones, relayed to the audience measurement.
/// </summary>
internal sealed class FakeAdminApi(FakeAudienceApi audience) : HttpMessageHandler
{
    public static readonly RecordCounts Counts = new(2847, 214, 2913, 412, 28, 18, 1106, 96, 214, 24, 37, 9);

    private readonly HttpMessageInvoker _audience = new(audience);

    public Dictionary<string, object> Answers { get; } = new()
    {
        ["/admin/record-counts"] = Counts,
        ["/admin/checks"] = new AdminChecks(7),
        ["/admin/change-log/latest-records"] = Array.Empty<ChangeLogEntryItem>(),
        ["/catalog/settings"] = new ApplicationSettings(AlbumLabelFormat.TitleSeriesVolume, Contracts.Enums.DisplayCulture.FrenchFrance),
    };

    /// <summary>The paths that fail, with their status.</summary>
    public Dictionary<string, HttpStatusCode> Failures { get; } = [];

    public List<string> Requested { get; } = [];

    /// <summary>What a write (POST, PUT) answers, from its method, path and body; none, a not found.</summary>
    public Func<HttpMethod, string, JsonElement, HttpResponseMessage>? Write { get; set; }

    /// <summary>What a write waits for before it answers, from its path and body: a response that arrives late.</summary>
    public Func<string, JsonElement, Task>? Hold { get; set; }

    /// <summary>The writes sent, with their body.</summary>
    public List<(HttpMethod Method, string Path, JsonElement Body)> Written { get; } = [];

    /// <summary>A response with the given contract, as the API answers.</summary>
    public static HttpResponseMessage Json(object content, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(content, content.GetType()) };

    /// <summary>A ProblemDetails of the given category, as the API answers an error.</summary>
    public static HttpResponseMessage Problem(string type, HttpStatusCode status, string? ruleCode = null) =>
        new(status)
        {
            Content = JsonContent.Create(
                ruleCode is null ? new Dictionary<string, object> { ["type"] = type, ["status"] = (int)status }
                    : new Dictionary<string, object> { ["type"] = type, ["status"] = (int)status, [ProblemTypes.RuleCodeExtension] = ruleCode },
                mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json")),
        };

    /// <summary>The services a page of the administration needs, on this API, for the given request.</summary>
    public void AddServices(IServiceCollection services, TestRequest request)
    {
        audience.AddServices(services, request, new ApiClient(new HttpClient(this) { BaseAddress = new Uri("http://api") }));
        services.AddLocalization();
        services.AddLogging();
        services.AddScoped<AdminRecordCounts>();
        services.AddScoped(_ => new BrowserTimeZone { Name = "Europe/Paris" });
        services.AddScoped<DisplayFormatter>();
        services.AddSingleton<RecordLabels>();
        services.AddSingleton<ValueLabels>();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (path.StartsWith("/audience/", StringComparison.Ordinal))
            return await _audience.SendAsync(request, cancellationToken);

        if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put)
        {
            var body = await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken);
            Written.Add((request.Method, path, body));
            if (Hold is not null)
                await Hold(path, body);
            return Write?.Invoke(request.Method, path, body) ?? new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        Requested.Add(request.RequestUri.PathAndQuery);
        if (Failures.TryGetValue(path, out var status))
            return Problem(ProblemTypes.Technical, status);

        return Answers.TryGetValue(path, out var answer)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(answer, answer.GetType()) }
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
