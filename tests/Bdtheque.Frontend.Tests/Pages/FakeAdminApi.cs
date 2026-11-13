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
/// The endpoints the pages of the administration read, each answering what a test sets for its path with its query, or else
/// for its path alone, or the error status it sets; the audience ones, relayed to the audience measurement.
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

    /// <summary>
    /// What a write (POST, PUT, DELETE) answers, from its method, path and body; none, a not found. A multipart form is given as an
    /// object of its fields, a file as its name and length; a deletion, as the query of its address.
    /// </summary>
    public Func<HttpMethod, string, JsonElement, HttpResponseMessage>? Write { get; set; }

    /// <summary>What a request waits for before it answers, from its path and body (none for a read): a response that arrives late.</summary>
    public Func<string, JsonElement, Task>? Hold { get; set; }

    /// <summary>The writes sent, with their body.</summary>
    public List<(HttpMethod Method, string Path, JsonElement Body)> Written { get; } = [];

    /// <summary>A response with the given contract, as the API answers.</summary>
    public static HttpResponseMessage Json(object content, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(content, content.GetType()) };

    /// <summary>A ProblemDetails of the given category, as the API answers an error, with the fields a business error designates.</summary>
    public static HttpResponseMessage Problem(string type, HttpStatusCode status, string? ruleCode = null, params string[] fields)
    {
        var body = new Dictionary<string, object> { ["type"] = type, ["status"] = (int)status };
        if (ruleCode is not null)
            body[ProblemTypes.RuleCodeExtension] = ruleCode;
        if (fields.Length > 0)
            body[ProblemTypes.FieldsExtension] = fields;
        return new(status)
        {
            Content = JsonContent.Create(
                body, mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json")),
        };
    }

    /// <summary>
    /// A business error, as the API answers it, designating <paramref name="fields"/> to correct and the
    /// <paramref name="related"/> fields their values clash with.
    /// </summary>
    public static HttpResponseMessage BusinessProblem(string ruleCode, string[] fields, string[] related) =>
        new(HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(
                new Dictionary<string, object>
                {
                    ["type"] = ProblemTypes.Business, ["status"] = 422, [ProblemTypes.RuleCodeExtension] = ruleCode,
                    [ProblemTypes.FieldsExtension] = fields, [ProblemTypes.RelatedFieldsExtension] = related,
                },
                mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json")),
        };

    /// <summary>The services a page of the administration needs, on this API, for the given request.</summary>
    public void AddServices(IServiceCollection services, TestRequest request)
    {
        audience.AddServices(services, request, new ApiClient(new HttpClient(this) { BaseAddress = new Uri("http://api") }));
        services.AddSingleton(new ApiUploadClient(new HttpClient(this) { BaseAddress = new Uri("http://api") }));
        services.AddLocalization();
        services.AddLogging();
        services.AddScoped<AdminRecordCounts>();
        services.AddScoped(_ => new BrowserTimeZone { Name = "Europe/Paris" });
        services.AddScoped<DisplayFormatter>();
        services.AddSingleton<IsbnFormatter>().AddSingleton<RecordLabels>();
        services.AddSingleton<ValueLabels>();
    }

    private static async Task<JsonElement> BodyOfAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        switch (request.Content)
        {
            case null:
                return JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["query"] = request.RequestUri!.Query });
            case MultipartFormDataContent form:
                var fields = new Dictionary<string, object>();
                foreach (var part in form)
                {
                    var name = part.Headers.ContentDisposition!.Name!.Trim('"');
                    if (part.Headers.ContentDisposition.FileName is { } fileName)
                    {
                        await using var file = await part.ReadAsStreamAsync(cancellationToken);
                        var length = 0L;
                        var buffer = new byte[81920];
                        int read;
                        while ((read = await file.ReadAsync(buffer, cancellationToken)) > 0)
                            length += read;
                        fields[name] = new Dictionary<string, object> { ["name"] = fileName.Trim('"'), ["length"] = length };
                    }
                    else
                    {
                        fields[name] = await part.ReadAsStringAsync(cancellationToken);
                    }
                }

                return JsonSerializer.SerializeToElement(fields);
            default:
                return await request.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (path.StartsWith("/audience/", StringComparison.Ordinal))
            return await _audience.SendAsync(request, cancellationToken);

        if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete)
        {
            var body = await BodyOfAsync(request, cancellationToken);
            Written.Add((request.Method, path, body));
            if (Hold is not null)
                await Hold(path, body);
            return Write?.Invoke(request.Method, path, body) ?? new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        Requested.Add(request.RequestUri.PathAndQuery);
        if (Hold is not null)
            await Hold(path, default);
        if (Failures.TryGetValue(path, out var status))
            return Problem(ProblemTypes.Technical, status);

        return Answers.TryGetValue(request.RequestUri.PathAndQuery, out var answer) || Answers.TryGetValue(path, out answer)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(answer, answer.GetType()) }
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
