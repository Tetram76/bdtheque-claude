using Bdtheque.Api.Deletion;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Errors;
using Bdtheque.Domain.Common;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bdtheque.Api.Errors;

/// <summary>
/// Single point turning an exception into an error response, categorized as business, functional
/// or technical (fonctionnel.md § Présentation des erreurs; choix-implementation.md § Erreurs
/// métier, fonctionnelles et techniques). No endpoint builds an error response itself.
/// </summary>
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, type, ruleCode) = Categorize(exception);

        // Since .NET 10 the exception handler middleware no longer logs what a handler reports as
        // handled: the cause of a technical error would otherwise appear nowhere, as the response
        // discloses no internal detail.
        if (type == ProblemTypes.Technical)
            logger.LogError(exception, "Technical error while processing the request.");
        else
            logger.LogInformation("Request refused ({ProblemType}, rule {RuleCode}): {Reason}", type, ruleCode, exception.Message);

        var problem = new ProblemDetails { Status = status, Type = type };
        if (ruleCode is not null)
            problem.Extensions[ProblemTypes.RuleCodeExtension] = ruleCode;
        // What the message has to announce beyond the category: the records concerned by the deletion.
        if (ImpactOf(exception) is { } impact)
            problem.Extensions[ProblemTypes.ImpactExtension] = impact;

        httpContext.Response.StatusCode = status;
        // The exception is deliberately not passed on: nothing internal reaches the response.
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = problem });
    }

    private static DeletionImpact? ImpactOf(Exception exception) => exception switch
    {
        DeletionRefusedException refused => refused.Impact,
        DeletionImpactChangedException changed => changed.Impact,
        _ => null,
    };

    private static (int Status, string Type, string? RuleCode) Categorize(Exception exception) => exception switch
    {
        DomainRuleViolationException violation =>
            (StatusCodes.Status422UnprocessableEntity, ProblemTypes.Business, violation.Rule),

        DeletionRefusedException =>
            (StatusCodes.Status422UnprocessableEntity, ProblemTypes.Business, DomainRules.DeletionBlockedByReferences),

        DeletionImpactChangedException => (StatusCodes.Status409Conflict, ProblemTypes.Functional, null),

        EntityNotFoundException => (StatusCodes.Status404NotFound, ProblemTypes.Functional, null),

        // Before DbUpdateException, from which it derives.
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, ProblemTypes.Functional, null),

        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } violation }
            when violation.ConstraintName is not null && UniqueIndexRules.ByIndexName.TryGetValue(violation.ConstraintName, out var rule) =>
            (StatusCodes.Status422UnprocessableEntity, ProblemTypes.Business, rule),

        // A creation or a modification: the referenced record was deleted in the meantime, as for an
        // unknown identifier. A deletion never raises this code: references are ON DELETE RESTRICT,
        // reported as RestrictViolation, which the deletion translates itself, being the only one
        // able to recompute the impact that blocks it.
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
            (StatusCodes.Status404NotFound, ProblemTypes.Functional, null),

        // Only thrown outside Production (RouteHandlerOptions.ThrowOnBadRequest): a request the
        // frontend built wrongly, whatever the user typed.
        BadHttpRequestException badRequest => (badRequest.StatusCode, ProblemTypes.Technical, null),

        _ => (StatusCodes.Status500InternalServerError, ProblemTypes.Technical, null),
    };
}
