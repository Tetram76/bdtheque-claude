using System.Text.Json;
using Bdtheque.Contracts.Audience;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Components.Audience;
using Bdtheque.Frontend.Visits;
using Microsoft.AspNetCore.Http;

namespace Bdtheque.Frontend.Tests.Audience;

/// <summary>The display of a screen, recorded once per display, by the request that renders it.</summary>
public sealed class AudienceScreenTests : BunitContext
{
    private static readonly BrowserVisit Visit = new(new DateOnly(2026, 11, 3), "base", new DateOnly(2026, 11, 3));
    private static readonly AudienceFunction Screen = new("screen", AudienceFunctionKind.Screen);

    private readonly FakeAudienceApi _api = new();
    private readonly TestRequest _request = new(new Dictionary<string, string> { [BrowserVisit.CookieName] = Visit.Format() });

    public AudienceScreenTests() => _api.AddServices(Services, _request);

    [Fact]
    public void Screen_DisplayedByARequest_IsRecorded_WithTheRecordItPresents()
    {
        var record = new PresentedRecord(RecordType.Series, Guid.CreateVersion7());

        Render<AudienceScreen>(parameters => parameters
            .Add(p => p.Function, Screen)
            .Add(p => p.Record, record)
            .AddCascadingValue(_request.Context));

        var (_, opened) = Assert.Single(_api.Sent);
        Assert.Equal(new FunctionUse("screen", record, null),
            opened.Deserialize<OpenVisitRequest>(JsonSerializerOptions.Web)!.FirstUse);
    }

    [Fact]
    public void Screen_RenderedForASubmittedForm_IsNotRecordedAgain()
    {
        _request.Context.Request.Method = HttpMethods.Post;

        Render<AudienceScreen>(parameters => parameters.Add(p => p.Function, Screen).AddCascadingValue(_request.Context));

        Assert.Empty(_api.Sent);
    }

    [Fact]
    public void Screen_RenderedInteractively_AfterItsPrerendering_IsNotRecordedAgain()
    {
        Render<AudienceScreen>(parameters => parameters.Add(p => p.Function, Screen));

        Assert.Empty(_api.Sent);
    }
}
