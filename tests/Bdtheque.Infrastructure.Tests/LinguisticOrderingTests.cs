using Bdtheque.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// Pins that text sorted by the database follows French linguistic order (fonctionnel.md §
/// Langue et culture d'affichage, § Tri et navigation par initiale): case-insensitive at the
/// first level, accented letters next to their base letter — not byte order, which puts every
/// lowercase and accented initial after "Z".
/// </summary>
public sealed class LinguisticOrderingTests : IAsyncLifetime
{
    private static readonly string[] FrenchOrder = ["ange", "Astérix", "Eden", "Épervier", "Fox", "Zorro"];

    private readonly BdthequeDbContextFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task SeriesSortKey_OrderedByDatabase_FollowsFrenchOrder()
    {
        _fixture.Context.Series.AddRange(Shuffled().Select(title => new Series(title)));
        await _fixture.Context.SaveChangesAsync();

        var ordered = await _fixture.Context.Series.OrderBy(s => s.SortKey).Select(s => s.SortKey).ToListAsync();

        Assert.Equal(FrenchOrder, ordered);
    }

    [Fact]
    public async Task AlbumSortKey_OrderedByDatabase_FollowsFrenchOrder()
    {
        _fixture.Context.Albums.AddRange(Shuffled().Select(title => new Album(title, null)));
        await _fixture.Context.SaveChangesAsync();

        var ordered = await _fixture.Context.Albums.OrderBy(a => a.SortKey).Select(a => a.SortKey).ToListAsync();

        Assert.Equal(FrenchOrder, ordered);
    }

    [Fact]
    public async Task ReferentialName_OrderedByDatabase_FollowsFrenchOrder()
    {
        // Referential lists (publishers, genres, universes, authors…) are displayed sorted too.
        _fixture.Context.Publishers.AddRange(Shuffled().Select(name => new Publisher(name)));
        await _fixture.Context.SaveChangesAsync();

        var ordered = await _fixture.Context.Publishers.OrderBy(p => p.Name).Select(p => p.Name).ToListAsync();

        Assert.Equal(FrenchOrder, ordered);
    }

    // Inserted out of order so that insertion order can never make the assertion pass by chance.
    private static IEnumerable<string> Shuffled() => FrenchOrder.Reverse();
}
