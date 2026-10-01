using System.Net;
using System.Text.Json;
using Bdtheque.Api.Deletion;
using Bdtheque.Api.Errors;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Errors;
using Bdtheque.Domain.Common;
using Bdtheque.Domain.Entities;
using Bdtheque.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Bdtheque.Api.Tests;

/// <summary>
/// Pins how every exception reaching the API's single exception handler is categorized
/// (choix-implementation.md § Erreurs métier, fonctionnelles et techniques). The handler is taken
/// from the API's own services, with the API's own ProblemDetails configuration.
/// </summary>
public sealed class ExceptionHandlingTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ExceptionHandlingTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task DomainRuleViolation_IsABusinessProblemCarryingItsRuleCode()
    {
        var problem = await HandleAsync(
            new DomainRuleViolationException(DomainRules.GenreLabelRequired, "log only"),
            HttpStatusCode.UnprocessableContent, ProblemTypes.Business);

        Assert.Equal(DomainRules.GenreLabelRequired, problem.GetProperty(ProblemTypes.RuleCodeExtension).GetString());
    }

    [Fact]
    public async Task RefusedDeletion_IsABusinessProblemCarryingItsImpact()
    {
        var impact = new DeletionImpact([new ImpactCount(EntityKind.Universe, 2)], [new ImpactCount(EntityKind.Album, 1)], [], "fingerprint");

        var problem = await HandleAsync(new DeletionRefusedException(impact), HttpStatusCode.UnprocessableContent, ProblemTypes.Business);

        Assert.Equal(DomainRules.DeletionBlockedByReferences, problem.GetProperty(ProblemTypes.RuleCodeExtension).GetString());
        ProblemAssert.SameImpact(impact, ProblemAssert.ImpactOf(problem));
    }

    [Fact]
    public async Task DeletionWhoseImpactChanged_IsAFunctionalProblemCarryingTheNewImpact()
    {
        var impact = new DeletionImpact([], [new ImpactCount(EntityKind.Series, 3)], [], "fingerprint");

        var problem = await HandleAsync(new DeletionImpactChangedException(impact), HttpStatusCode.Conflict, ProblemTypes.Functional);

        ProblemAssert.SameImpact(impact, ProblemAssert.ImpactOf(problem));
    }

    [Fact]
    public async Task UnknownEntity_IsAFunctionalProblem() =>
        await HandleAsync(new EntityNotFoundException(typeof(Album), Guid.CreateVersion7()), HttpStatusCode.NotFound, ProblemTypes.Functional);

    [Fact]
    public async Task ConcurrentModification_IsAFunctionalProblem() =>
        await HandleAsync(new DbUpdateConcurrencyException("stale"), HttpStatusCode.Conflict, ProblemTypes.Functional);

    [Fact]
    public async Task MappedUniqueIndexViolation_IsABusinessProblemCarryingTheIndexRuleCode()
    {
        var problem = await HandleAsync(
            DatabaseError(PostgresErrorCodes.UniqueViolation, "IX_Genres_Label"),
            HttpStatusCode.UnprocessableContent, ProblemTypes.Business);

        Assert.Equal(DomainRules.GenreLabelAlreadyUsed, problem.GetProperty(ProblemTypes.RuleCodeExtension).GetString());
    }

    [Fact]
    public async Task UnmappedUniqueViolation_IsATechnicalProblem() =>
        // Reveals a missing check rather than an input to correct.
        await HandleAsync(DatabaseError(PostgresErrorCodes.UniqueViolation, "PK_Albums"), HttpStatusCode.InternalServerError, ProblemTypes.Technical);

    [Fact]
    public async Task ForeignKeyViolation_IsAFunctionalProblem() =>
        // Default translation, for a creation or a modification: the referenced record was deleted in the meantime.
        await HandleAsync(
            DatabaseError(PostgresErrorCodes.ForeignKeyViolation, "FK_Editions_Publishers_PublisherId"),
            HttpStatusCode.NotFound, ProblemTypes.Functional);

    [Fact]
    public async Task TextTooLongForItsColumn_IsABusinessProblem()
    {
        // The user can shorten what was typed; the column lengths are the persistence limits of every entity.
        var problem = await HandleAsync(
            new DbUpdateException("Save failed", new PostgresException("value too long", "ERROR", "ERROR", PostgresErrorCodes.StringDataRightTruncation)),
            HttpStatusCode.UnprocessableContent, ProblemTypes.Business);

        Assert.Equal(DomainRules.TextTooLong, problem.GetProperty(ProblemTypes.RuleCodeExtension).GetString());
    }

    [Fact]
    public async Task OtherConstraintViolation_IsATechnicalProblem() =>
        await HandleAsync(DatabaseError(PostgresErrorCodes.CheckViolation, "CK_Genres_LabelNotBlank"), HttpStatusCode.InternalServerError, ProblemTypes.Technical);

    [Fact]
    public async Task UnreadableRequest_IsATechnicalProblemKeepingItsStatus() =>
        await HandleAsync(new BadHttpRequestException("Unreadable body", StatusCodes.Status400BadRequest), HttpStatusCode.BadRequest, ProblemTypes.Technical);

    [Fact]
    public async Task UnexpectedException_IsATechnicalProblemDisclosingNoInternalDetail()
    {
        var problem = await HandleAsync(new InvalidOperationException("connection string secret"), HttpStatusCode.InternalServerError, ProblemTypes.Technical);

        Assert.DoesNotContain("secret", problem.GetRawText());
    }

    private static DbUpdateException DatabaseError(string sqlState, string constraintName) =>
        new("Save failed", new PostgresException("violation", "ERROR", "ERROR", sqlState, constraintName: constraintName));

    private async Task<JsonElement> HandleAsync(Exception exception, HttpStatusCode status, string category)
    {
        using var scope = _factory.Services.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var body = new MemoryStream();
        context.Response.Body = body;

        var handler = Assert.Single(scope.ServiceProvider.GetServices<IExceptionHandler>());
        Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None));

        Assert.Equal((int)status, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType?.Split(';')[0]);
        var problem = JsonSerializer.Deserialize<JsonElement>(body.ToArray());
        Assert.Equal(category, problem.GetProperty("type").GetString());
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.IsType<ApiExceptionHandler>(handler);
        return problem;
    }
}
