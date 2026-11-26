namespace Bdtheque.Domain.Tests;

internal static class DomainAssert
{
    /// <summary>
    /// Asserts that <paramref name="action"/> is rejected as a business rule violation — not a
    /// technical error — reporting exactly <paramref name="rule"/>.
    /// </summary>
    /// <remarks>The rule concerns no value of a record of its own: the violation designates none.</remarks>
    public static void Violates(string rule, Action action) =>
        ViolatesWithoutRecord(rule, Throws<DomainRuleViolationException>(action));

    /// <inheritdoc cref="Violates(string, Action)"/>
    /// <remarks>For code that yields a value (e.g. a constructor), handed to xunit as by its own overload.</remarks>
    public static void Violates(string rule, Func<object?> testCode) =>
        ViolatesWithoutRecord(rule, Throws<DomainRuleViolationException>(testCode));

    /// <summary>
    /// Asserts that <paramref name="action"/> is rejected as a violation of <paramref name="rule"/> that designates
    /// <paramref name="attribute"/> of a <typeparamref name="TRecord"/>: the value the user is to correct
    /// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md).
    /// </summary>
    public static void Violates<TRecord>(string rule, string attribute, Action action) =>
        ViolatesOn<TRecord>(rule, attribute, Throws<DomainRuleViolationException>(action));

    /// <inheritdoc cref="Violates{TRecord}(string, string, Action)"/>
    /// <remarks>For code that yields a value (e.g. a constructor), handed to xunit as by its own overload.</remarks>
    public static void Violates<TRecord>(string rule, string attribute, Func<object?> testCode) =>
        ViolatesOn<TRecord>(rule, attribute, Throws<DomainRuleViolationException>(testCode));

    private static void ViolatesWithoutRecord(string rule, DomainRuleViolationException exception)
    {
        Assert.Equal(rule, exception.Rule);
        Assert.Null(exception.Record);
        Assert.Null(exception.Attribute);
    }

    private static void ViolatesOn<TRecord>(string rule, string attribute, DomainRuleViolationException exception)
    {
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
    public static TException Throws<TException>(Action action) where TException : Exception =>
        WithMessage(Assert.Throws<TException>(action));

    /// <summary>
    /// As <see cref="Throws{TException}(Action)"/>, for code that yields a value (e.g. a constructor): the value is
    /// handed to xunit, which reports it if no exception is thrown, as its own overload does.
    /// </summary>
    public static TException Throws<TException>(Func<object?> testCode) where TException : Exception =>
        WithMessage(Assert.Throws<TException>(testCode));

    private static TException WithMessage<TException>(TException exception) where TException : Exception
    {
        Assert.False(string.IsNullOrWhiteSpace(exception.Message) || char.IsWhiteSpace(exception.Message[0]),
            $"{typeof(TException).Name} thrown without a message of its own: '{exception.Message}'.");
        return exception;
    }
}
