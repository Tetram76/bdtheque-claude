using System.Reflection;
using Bdtheque.Domain.Common;

namespace Bdtheque.Domain.Tests;

public sealed class DomainRulesTests
{
    [Fact]
    public void Codes_AreUnique()
    {
        // Each code is a localization key on the frontend: two rules sharing one would show the
        // user the explanation of the wrong rule.
        var codes = typeof(DomainRules)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(codes);
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }
}
