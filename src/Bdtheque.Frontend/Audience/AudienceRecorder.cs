using Bdtheque.Contracts.Audience;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Visits;

namespace Bdtheque.Frontend.Audience;

/// <summary>
/// Records the screens displayed and the actions triggered (fonctionnel/mesure-audience.md), in the visit of the browser
/// (<see cref="VisitCookie"/>), whose first usage opens the visit of the measurement, kept by its cookie. Nothing is
/// recorded while the visitor opposes it (<see cref="AudienceOpposition"/>), nor without a visit: the root component of the
/// page, rendered first, continues it with the theme of the visit. A usage the API does not record never fails the page:
/// the measurement serves the administrator, not the user.
/// </summary>
public sealed class AudienceRecorder(
    IHttpContextAccessor httpContextAccessor, VisitCookie visitCookie, AudienceOpposition opposition, ApiClient api,
    ILogger<AudienceRecorder> logger)
{
    /// <summary>Records a screen displayed, with the record it presents, if any.</summary>
    public Task RecordScreenAsync(AudienceFunction screen, PresentedRecord? record = null, CancellationToken cancellationToken = default) =>
        screen.Kind == AudienceFunctionKind.Screen
            ? RecordAsync(new FunctionUse(screen.Key, record, Parameters: null), cancellationToken)
            : throw new ArgumentException($"{screen.Key} is not a screen.", nameof(screen));

    /// <summary>Records an action triggered, with its parameters, if any: never a secret (password, value of an access token).</summary>
    public Task RecordActionAsync(AudienceFunction action, string? parameters = null, CancellationToken cancellationToken = default) =>
        action.Kind == AudienceFunctionKind.Action
            ? RecordAsync(new FunctionUse(action.Key, Record: null, parameters), cancellationToken)
            : throw new ArgumentException($"{action.Key} is not an action.", nameof(action));

    private async Task RecordAsync(FunctionUse use, CancellationToken cancellationToken)
    {
        // The visit of the measurement is kept by the cookie of the response: an interactive circuit, which keeps the
        // request that opened it, could not keep it.
        var context = httpContextAccessor.HttpContext;
        if (context is null || context.Response.HasStarted)
            throw new InvalidOperationException("The audience measurement records a usage within a request whose response has not started.");
        if (opposition.IsOpposed || visitCookie.Current is not { } visit)
            return;

        try
        {
            // A visit of the measurement is that of the administrator or of a visitor from its opening: once the
            // authentication changed (login, logout, end of the session), the usage goes into another one, of its kind.
            var administrator = context.User.Identity?.IsAuthenticated == true;
            if (visit.AudienceVisit is { } current && current.OfAdministrator == administrator
                && await TryAddAsync(current.Id, use, cancellationToken))
                return;

            var device = AudienceDevice.Of(context.Request.Headers.UserAgent, context.Request.Cookies.ContainsKey(AudienceDevice.TouchScreenCookieName));
            var opened = await api.PostAsync<OpenVisitRequest, OpenedVisit>("/audience/visits", new OpenVisitRequest(
                administrator, device.DeviceType, device.RenderingEngine, device.EngineVersion, use), cancellationToken);
            visitCookie.Set(visit with { AudienceVisit = new MeasuredVisit(opened.VisitId, administrator) });
        }
        catch (ApiProblemException exception)
        {
            logger.LogWarning(exception, "The usage of {Function} could not be recorded by the audience measurement.", use.Function);
        }
    }

    // A visit no longer known (deleted at the end of its retention, or with the data of the deployment) is replaced by a new one.
    private async Task<bool> TryAddAsync(Guid visitId, FunctionUse use, CancellationToken cancellationToken)
    {
        try
        {
            await api.PostAsync($"/audience/visits/{visitId}/usages", use, cancellationToken);
            return true;
        }
        catch (ApiProblemException exception) when (exception.Problem is { Category: ProblemCategory.Functional, Status: 404 })
        {
            return false;
        }
    }
}
