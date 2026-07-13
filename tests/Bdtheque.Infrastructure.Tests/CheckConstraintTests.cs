using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Verifies that the database-level CHECK constraint on <c>Authors</c> is enforced.
/// These tests bypass the domain model (which also enforces the rule) and write raw SQL,
/// ensuring that the constraint acts as a genuine defence-in-depth layer.
/// </summary>
public sealed class CheckConstraintTests : IDisposable
{
    // Each test gets its own isolated database so that a failing insert in one test
    // cannot affect the state seen by another.
    private readonly BdthequeDbContextFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AuthorCheckConstraint_NullLastNameAndPseudonym_ThrowsAtDatabase()
    {
        // Insert directly via raw SQL to bypass the domain-layer guard
        var id = Guid.CreateVersion7();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _fixture.Context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Authors\" (\"Id\", \"LastName\", \"Pseudonym\") VALUES ({0}, NULL, NULL)",
                id));
    }

    [Fact]
    public async Task AuthorCheckConstraint_LastNameProvided_Succeeds()
    {
        var id = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Authors\" (\"Id\", \"LastName\", \"Pseudonym\") VALUES ({0}, 'Dupont', NULL)",
            id);

        var count = await _fixture.Context.Authors.CountAsync(a => a.Id == id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AuthorCheckConstraint_PseudonymProvided_Succeeds()
    {
        var id = Guid.CreateVersion7();
        await _fixture.Context.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"Authors\" (\"Id\", \"LastName\", \"Pseudonym\") VALUES ({0}, NULL, 'Moebius')",
            id);

        var count = await _fixture.Context.Authors.CountAsync(a => a.Id == id);
        Assert.Equal(1, count);
    }
}
