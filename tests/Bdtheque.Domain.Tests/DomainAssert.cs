using Bdtheque.Domain.Common;

namespace Bdtheque.Domain.Tests;

internal static class DomainAssert
{
    /// <summary>
    /// Asserts that <paramref name="action"/> is rejected as a business rule violation — not a
    /// technical error — reporting exactly <paramref name="rule"/>.
    /// </summary>
    public static void Violates(string rule, Action action)
    {
        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal(rule, exception.Rule);
    }
}
