using System.Reflection;
using Bdtheque.Api.Errors;
using Bdtheque.Domain.Common;
using Bdtheque.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Api.Tests;

/// <summary>
/// A unique index the database enforces is a business rule the domain cannot check alone
/// (choix-implementation.md § Erreurs métier, fonctionnelles et techniques): each one must be
/// translated into a business error with its rule code, never left as a technical error.
/// </summary>
public sealed class UniqueIndexRulesTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public UniqueIndexRulesTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public void EveryUniqueIndexOfTheModel_IsMapped_AndOnlyThem()
    {
        using var scope = _factory.Services.CreateScope();
        var uniqueIndexes = scope.ServiceProvider.GetRequiredService<BdthequeDbContext>().Model
            .GetEntityTypes()
            .SelectMany(t => t.GetIndexes())
            .Where(i => i.IsUnique)
            .Select(i => i.GetDatabaseName()!)
            .Order(StringComparer.Ordinal);

        Assert.Equal(uniqueIndexes, UniqueIndexRules.ByIndexName.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryMappedCode_IsADeclaredRule()
    {
        var declared = typeof(DomainRules).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();

        Assert.All(UniqueIndexRules.ByIndexName.Values, code => Assert.Contains(code, declared));
    }
}
