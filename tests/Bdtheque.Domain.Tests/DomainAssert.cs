using Bdtheque.Domain.Common;

namespace Bdtheque.Domain.Tests;

internal static class DomainAssert
{
    /// <summary>
    /// Asserts that <paramref name="action"/> is rejected as a business rule violation — not a
    /// technical error — reporting exactly <paramref name="rule"/>.
    /// </summary>
    /// <remarks>The rule concerns no value of a record of its own: the violation designates none.</remarks>
    public static void Violates(string rule, Action action)
    {
        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal(rule, exception.Rule);
        Assert.Null(exception.Record);
        Assert.Null(exception.Attribute);
    }

    /// <summary>
    /// Asserts that <paramref name="action"/> is rejected as a violation of <paramref name="rule"/> that designates
    /// <paramref name="attribute"/> of a <typeparamref name="TRecord"/>: the value the user is to correct
    /// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md).
    /// </summary>
    public static void Violates<TRecord>(string rule, string attribute, Action action)
    {
        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal(rule, exception.Rule);
        Assert.IsType<TRecord>(exception.Record);
        Assert.Equal(attribute, exception.Attribute);
    }
}
