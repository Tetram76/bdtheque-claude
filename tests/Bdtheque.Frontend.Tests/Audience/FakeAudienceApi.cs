using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Errors;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bdtheque.Frontend.Tests.Audience;

/// <summary>
/// The recording endpoints of <c>api</c> (<c>/audience</c>), answering as a test chooses, which keep what they received.
/// </summary>
internal sealed class FakeAudienceApi : HttpMessageHandler
{
    public Guid OpenedVisit { get; } = Guid.CreateVersion7();

    public HttpStatusCode VisitsAnswer { get; init; } = HttpStatusCode.Created;

    public HttpStatusCode UsagesAnswer { get; init; } = HttpStatusCode.NoContent;

    public List<(string Path, JsonElement Content)> Sent { get; } = [];

    /// <summary>The functions whose usages were sent, in their order.</summary>
    public IEnumerable<string> Functions => Sent.Select(sent => sent.Path == "/audience/visits"
        ? sent.Content.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!.FirstUse.Function
        : sent.Content.Deserialize<FunctionUse>(JsonSerializerOptions.Web)!.Function);

    public ApiClient Client() => new(new HttpClient(this) { BaseAddress = new Uri("http://api") });

    /// <summary>The services of the audience measurement, for the given request, on this API.</summary>
    public void AddServices(IServiceCollection services, TestRequest request)
    {
        services.AddSingleton(request.Accessor);
        services.AddSingleton(Client());
        services.AddSingleton<ILogger<AudienceRecorder>>(NullLogger<AudienceRecorder>.Instance);
        services.AddScoped<VisitCookie>();
        services.AddScoped<AudienceOpposition>();
        services.AddScoped<AudienceRecorder>();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        Sent.Add((path, await request.Content!.ReadFromJsonAsync<JsonElement>(cancellationToken)));
        var status = path == "/audience/visits" ? VisitsAnswer : UsagesAnswer;
        if ((int)status >= 400)
            return new HttpResponseMessage(status)
            {
                Content = JsonContent.Create(
                    new { type = status == HttpStatusCode.NotFound ? ProblemTypes.Functional : ProblemTypes.Technical, status = (int)status },
                    mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json")),
            };
        return status == HttpStatusCode.Created
            ? new HttpResponseMessage(status) { Content = JsonContent.Create(new OpenedVisit(OpenedVisit)) }
            : new HttpResponseMessage(status);
    }
}
