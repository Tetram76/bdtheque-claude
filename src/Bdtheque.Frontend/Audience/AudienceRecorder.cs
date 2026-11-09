using Bdtheque.Contracts.Audience;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Visits;

namespace Bdtheque.Frontend.Audience;

/// <summary>
/// Records the screens displayed and the actions triggered (fonctionnel/mesure-audience.md), in the visit of the browser
/// (<see cref="VisitCookie"/>), whose first usage opens the visit of the measurement, kept by its cookie. Nothing is
/// recorded while the visitor opposes it (<see cref="AudienceOpposition"/>), nor without a visit: the root component of the
/// page, rendered first, continues it with the theme of the visit. An interactive circuit, which has no request of its own,
/// records its actions in the visit its prerendering recorded the screen in (<see cref="CircuitAudienceVisit"/>). A usage
/// the API does not record never fails the page: the measurement serves the administrator, not the user.
/// </summary>
public sealed class AudienceRecorder(
    IHttpContextAccessor httpContextAccessor, VisitCookie visitCookie, AudienceOpposition opposition,
    CircuitAudienceVisit circuitVisit, ApiClient api, ILogger<AudienceRecorder> logger)
{
    /// <summary>Records a screen displayed, with the record it presents, if any.</summary>
    public Task RecordScreenAsync(AudienceFunction screen, PresentedRecord? record = null, CancellationToken cancellationToken = default) =>
        screen.Kind == AudienceFunctionKind.Screen
            ? RecordAsync(new FunctionUse(screen.Key, record, Parameters: null), screen.Kind, cancellationToken)
            : throw new ArgumentException($"{screen.Key} is not a screen.", nameof(screen));

    /// <summary>Records an action triggered, with its parameters, if any: never a secret (password, value of an access token).</summary>
    public Task RecordActionAsync(AudienceFunction action, string? parameters = null, CancellationToken cancellationToken = default) =>
        action.Kind == AudienceFunctionKind.Action
            ? RecordAsync(new FunctionUse(action.Key, Record: null, parameters), action.Kind, cancellationToken)
            : throw new ArgumentException($"{action.Key} is not an action.", nameof(action));

    private async Task RecordAsync(FunctionUse use, AudienceFunctionKind kind, CancellationToken cancellationToken)
    {
        // The visit of the measurement is kept by the cookie of the response: an interactive circuit, whose request has
        // long started its response, records its actions in the visit of its prerendering.
        var context = httpContextAccessor.HttpContext;
        if (context is null || context.Response.HasStarted)
        {
            if (kind == AudienceFunctionKind.Screen)
                throw new InvalidOperationException("A screen is recorded by the request that renders it, whose response has not started.");
            await RecordInCircuitAsync(use, cancellationToken);
            return;
        }
        if (opposition.IsOpposed || visitCookie.Current is not { } visit)
            return;

        try
        {
            // A visit of the measurement is that of the administrator or of a visitor from its opening: once the
            // authentication changed (login, logout, end of the session), the usage goes into another one, of its kind.
            var administrator = context.User.Identity?.IsAuthenticated == true;
            if (visit.AudienceVisit is { } current && current.OfAdministrator == administrator
                && await TryAddAsync(current.Id, use, cancellationToken))
            {
                circuitVisit.VisitId = current.Id;
                return;
            }

            var device = AudienceDevice.Of(context.Request.Headers.UserAgent, context.Request.Cookies.ContainsKey(AudienceDevice.TouchScreenCookieName));
            var opened = await api.PostAsync<OpenVisitRequest, OpenedVisit>("/audience/visits", new OpenVisitRequest(
                administrator, device.DeviceType, device.RenderingEngine, device.EngineVersion, use), cancellationToken);
            visitCookie.Set(visit with { AudienceVisit = new MeasuredVisit(opened.VisitId, administrator) });
            circuitVisit.VisitId = opened.VisitId;
        }
        catch (ApiProblemException exception)
        {
            logger.LogWarning(exception, "The usage of {Function} could not be recorded by the audience measurement.", use.Function);
        }
    }

    // No visit can be opened without a response to keep it: a visit no longer known loses the action, as an API that does
    // not answer would.
    private async Task RecordInCircuitAsync(FunctionUse use, CancellationToken cancellationToken)
    {
        if (circuitVisit.VisitId is not { } visitId)
            return;

        try
        {
            if (!await TryAddAsync(visitId, use, cancellationToken))
                logger.LogWarning("The usage of {Function} could not be recorded: its visit is no longer known.", use.Function);
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
