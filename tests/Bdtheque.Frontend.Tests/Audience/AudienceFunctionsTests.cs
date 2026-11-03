using System.Reflection;
using Bdtheque.Frontend.Audience;

namespace Bdtheque.Frontend.Tests.Audience;

/// <summary>
/// The list of the functions the audience measurement follows, so that a function never used appears with null
/// indicators (fonctionnel/mesure-audience.md, Indicateurs): every function declared is in it, under its own key.
/// </summary>
public sealed class AudienceFunctionsTests
{
    [Fact]
    public void EveryFunctionDeclared_IsFollowed()
    {
        var declared = typeof(AudienceFunctions).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(AudienceFunction))
            .Select(field => (AudienceFunction)field.GetValue(null)!);

        Assert.Equal(declared.OrderBy(f => f.Key), AudienceFunctions.Followed.OrderBy(f => f.Key));
    }

    [Fact]
    public void EveryFunction_HasItsOwnKey()
    {
        Assert.Equal(AudienceFunctions.Followed.Count, AudienceFunctions.Followed.Select(f => f.Key).Distinct(StringComparer.Ordinal).Count());
    }
}
