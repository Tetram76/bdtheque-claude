using Bdtheque.Domain.Common;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class EnumGuardTests
{
    [Fact]
    public void EnsureDefined_DefinedValue_DoesNotThrow()
    {
        EnumGuard.EnsureDefined(SeriesStatus.InProgress, "value");
    }

    [Fact]
    public void EnsureDefined_UndefinedValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EnumGuard.EnsureDefined((SeriesStatus)42, "value"));
    }
}
