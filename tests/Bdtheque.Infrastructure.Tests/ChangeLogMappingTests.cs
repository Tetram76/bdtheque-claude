using Bdtheque.Domain.Entities.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The change log records every attribute of every record (modele-metier.md § Entrée du journal des
/// modifications): a property added to an entity must be either recorded or deliberately left out,
/// never silently ignored.
/// </summary>
public sealed class ChangeLogMappingTests : IAsyncLifetime
{
    private readonly BdthequeDbContextFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public void EveryRecordType_HasItsMapping()
    {
        Assert.Equal(
            Enum.GetValues<RecordType>().Order(),
            ChangeLogMappings.Records.Values.Select(r => r.RecordType).Order());
    }

    [Fact]
    public void EveryPropertyOfARecord_IsRecordedOrDeliberatelyLeftOut()
    {
        string[] common = [nameof(EntityBase.Id), nameof(EntityBase.CreatedAt), nameof(EntityBase.ModifiedAt), BdthequeDbContext.VersionProperty];

        foreach (var (clrType, mapping) in ChangeLogMappings.Records)
        {
            var properties = _fixture.Context.Model.FindEntityType(clrType)!.GetProperties()
                .Select(p => p.Name)
                .Except(common)
                .Order(StringComparer.Ordinal);
            // A property may be read by several attributes (e.g. the criteria of a saved search, which
            // give its variables their default values).
            var covered = mapping.Attributes.SelectMany(a => a.Properties)
                .Concat(mapping.LeftOut)
                .Distinct()
                .Order(StringComparer.Ordinal);

            Assert.Equal(properties, covered);
        }
    }

    [Fact]
    public void EachAttribute_IsRecordedOncePerRecordType()
    {
        foreach (var mapping in ChangeLogMappings.Records.Values)
        {
            var attributes = mapping.Attributes.Select(a => a.Attribute).ToList();
            Assert.Equal(attributes.Count, attributes.Distinct().Count());
        }
    }
}
