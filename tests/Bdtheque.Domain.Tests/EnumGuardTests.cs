using Bdtheque.Domain.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class EnumGuardTests
{
    [Fact]
    public void EnsureDefined_DefinedValue_DoesNotThrow()
    {
        EnumGuard.EnsureDefined(AlbumType.Omnibus, "value");
        EnumGuard.EnsureDefined(SeriesStatus.InProgress, "value");
        EnumGuard.EnsureDefined(ContributionRole.Illustrator, "value");
    }

    [Fact]
    public void EnsureDefined_UndefinedValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EnumGuard.EnsureDefined((AlbumType)42, "value"));
        Assert.Throws<ArgumentOutOfRangeException>(() => EnumGuard.EnsureDefined((SeriesStatus)42, "value"));
        Assert.Throws<ArgumentOutOfRangeException>(() => EnumGuard.EnsureDefined((ContributionRole)42, "value"));
    }
}
