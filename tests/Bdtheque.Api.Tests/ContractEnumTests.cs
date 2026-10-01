using System.Reflection;
using System.Text.Json;
using ContractSeriesStatus = Bdtheque.Contracts.Enums.SeriesStatus;
using DomainSeriesStatus = Bdtheque.Domain.Enums.SeriesStatus;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The contracts duplicate the domain enums so that the frontend never references the domain
/// (choix-implementation.md § Organisation de l'API): the copies must stay identical, member by
/// member, or a value would silently change meaning between the API and the frontend.
/// </summary>
public sealed class ContractEnumTests
{
    // Any enum of each side would do: it only locates its assembly and enum namespace.
    private static readonly Type[] DomainEnums = EnumsOf(typeof(DomainSeriesStatus).Assembly, typeof(DomainSeriesStatus).Namespace!);

    private static readonly Type[] ContractEnums = EnumsOf(typeof(ContractSeriesStatus).Assembly, typeof(ContractSeriesStatus).Namespace!);

    [Fact]
    public void EveryDomainEnum_HasAContractCopy_AndNothingElse()
    {
        Assert.NotEmpty(DomainEnums);
        Assert.Equal(DomainEnums.Select(t => t.Name).Order(), ContractEnums.Select(t => t.Name).Order());
    }

    [Fact]
    public void ContractEnums_HaveTheSameMembersAndValuesAsTheDomain()
    {
        foreach (var domainEnum in DomainEnums)
        {
            var contractEnum = ContractEnums.Single(t => t.Name == domainEnum.Name);

            Assert.Equal(Members(domainEnum), Members(contractEnum));
        }
    }

    [Fact]
    public void ContractEnums_AreSerializedByName_WhateverTheSerializerOptions()
    {
        // Carried by the enum types themselves, so that the frontend reads them back without having
        // to mirror the API's serializer configuration.
        foreach (var contractEnum in ContractEnums)
        {
            var member = Enum.GetValues(contractEnum).GetValue(0)!;

            var json = JsonSerializer.Serialize(member, contractEnum, JsonSerializerOptions.Web);

            Assert.Equal($"\"{member}\"", json);
            Assert.Equal(member, JsonSerializer.Deserialize(json, contractEnum, JsonSerializerOptions.Web));
        }
    }

    private static Type[] EnumsOf(Assembly assembly, string @namespace) =>
        assembly.GetTypes().Where(t => t.IsEnum && t.IsPublic && t.Namespace == @namespace).ToArray();

    private static List<(string Name, long Value)> Members(Type enumType) =>
        Enum.GetNames(enumType)
            .Select(name => (name, Convert.ToInt64(Enum.Parse(enumType, name))))
            .OrderBy(m => m.name, StringComparer.Ordinal)
            .ToList();
}
