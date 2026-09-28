using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// Guards the project-wide convention of persisting enums as their explicit int value
/// (see contraintes-techniques.md § Conventions de persistance): unlike a rename under
/// string persistence, reusing a retired member's numeric value would silently reinterpret
/// already-persisted rows without EF or the database ever noticing. This test is the
/// mechanical safety net that a compiler cannot provide on its own.
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
    public void EveryMember_HasAUniqueExplicitValue(Type enumType)
    {
        var values = Enum.GetValues(enumType).Cast<int>().ToArray();

        Assert.Equal(values.Length, values.Distinct().Count());
    }
}
