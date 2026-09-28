using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// Guards the project-wide convention of persisting enums as their explicit int value
/// (see contraintes-techniques.md § Conventions de persistance). Checking uniqueness among only
/// the currently declared members is not enough: it would miss a member removed and a later,
/// unrelated member reusing its retired value, which would silently reinterpret already-persisted
/// rows without EF or the database ever noticing. These tests cross-check every current member
/// against <see cref="EnumValueLedger"/>, an append-only record that keeps retired values reserved.
/// </summary>
public sealed class EnumValueUniquenessTests
{
    public static TheoryData<Type> DomainEnumTypes()
    {
        var data = new TheoryData<Type>();

        foreach (var type in typeof(AlbumType).Assembly.GetTypes())
        {
            if (type.IsEnum && type.Namespace == typeof(AlbumType).Namespace)
                data.Add(type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(DomainEnumTypes))]
    public void EveryCurrentMember_MatchesItsLedgerEntry(Type enumType)
    {
        var ledgerEntries = EnumValueLedger.Entries[enumType];
        var currentMembers = Enum.GetValues(enumType)
            .Cast<Enum>()
            .Select(value => (Name: value.ToString()!, Value: Convert.ToInt32(value)))
            .ToArray();

        foreach (var member in currentMembers)
        {
            Assert.Contains(member, ledgerEntries);
        }
    }

    [Theory]
    [MemberData(nameof(DomainEnumTypes))]
    public void LedgerValues_AreUniquePerEnum(Type enumType)
    {
        var values = EnumValueLedger.Entries[enumType].Select(entry => entry.Value).ToArray();

        Assert.Equal(values.Length, values.Distinct().Count());
    }
}
