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
        var exception = Throws<DomainRuleViolationException>(action);
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
        var exception = Throws<DomainRuleViolationException>(action);
        Assert.Equal(rule, exception.Rule);
        Assert.IsType<TRecord>(exception.Record);
        Assert.Equal(attribute, exception.Attribute);
    }

    /// <summary>
    /// Asserts that <paramref name="action"/> throws exactly <typeparamref name="TException"/>, with a message of its own: the
    /// journal never lacks the information of an error (gestion-projet/gouvernance/qualite-du-code.md, the journal).
    /// </summary>
    /// <remarks>
    /// An argument exception appends its parameter, and its value, to its own message: an empty message of its own leaves a
    /// text that starts with that separating space, hence the first character checked rather than the whole text.
    /// </remarks>
    public static TException Throws<TException>(Action action) where TException : Exception
    {
        var exception = Assert.Throws<TException>(action);
        Assert.False(string.IsNullOrWhiteSpace(exception.Message) || char.IsWhiteSpace(exception.Message[0]),
            $"{typeof(TException).Name} thrown without a message of its own: '{exception.Message}'.");
        return exception;
    }
}
