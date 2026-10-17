using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The change log is never purged: the JSON form of its labels and values must read back forever.
/// Each kind is pinned here, so that renaming a member of the domain records — which name their JSON
/// properties — fails a test instead of silently making the entries already written unreadable.
/// </summary>
public sealed class ChangeLogJsonTests
{
    public static TheoryData<ChangeLogValue, string> Values() => new()
    {
        { new TextValue("Tintin"), """{"kind":"text","value":"Tintin"}""" },
        { new NumberValue(5), """{"kind":"number","value":5}""" },
        { new FlagValue(true), """{"kind":"flag","value":true}""" },
        { new DateValue(new DateOnly(1986, 3, 14)), """{"kind":"date","value":"1986-03-14"}""" },
        { new PartialDateValue(1934, 12), """{"kind":"partialDate","year":1934,"month":12}""" },
        { new PartialDateValue(1934, null), """{"kind":"partialDate","year":1934}""" },
        { new AmountValue(45.5m, "FRF"), """{"kind":"amount","amount":45.5,"currency":"FRF"}""" },
        { new EnumValue(2), """{"kind":"enum","value":2}""" },
        {
            new ReferenceValue(Guid.Parse("0199c0de-0000-7000-8000-000000000001"), new NameLabel("Tintin")),
            """{"kind":"reference","id":"0199c0de-0000-7000-8000-000000000001","label":{"kind":"name","name":"Tintin"}}"""
        },
        {
            new ContributionValue(
                Guid.Parse("0199c0de-0000-7000-8000-000000000002"), new AuthorLabel("Remi", "Georges", "Hergé"), ContributionRole.Scenarist),
            """{"kind":"contribution","authorId":"0199c0de-0000-7000-8000-000000000002","author":{"lastName":"Remi","firstName":"Georges","pseudonym":"Hergé"},"role":1}"""
        },
        {
            new VisualValue(Guid.Parse("0199c0de-0000-7000-8000-000000000003"), VisualType.BackCover, 2, "a.jpg"),
            """{"kind":"visual","id":"0199c0de-0000-7000-8000-000000000003","type":5,"displayOrder":2,"mediaReference":"a.jpg"}"""
        },
        {
            new SearchCriteriaValue("""{"kind":"group","operator":"And","conditions":[]}"""),
            """{"kind":"searchCriteria","criteria":"{\"kind\":\"group\",\"operator\":\"And\",\"conditions\":[]}"}"""
        },
        {
            new SearchVariablesValue([new SearchVariableValue("Titre", "/values/0", "Tintin")]),
            """{"kind":"searchVariables","variables":[{"name":"Titre","location":"/values/0","defaultValue":"Tintin"}]}"""
        },
    };

    public static TheoryData<RecordLabel, string> Labels()
    {
        var album = new AlbumLabel("Gaston", "Gaston", AlbumType.Omnibus, true, 1, 2, 3);
        const string albumJson =
            """{"kind":"album","title":"Gaston","seriesTitle":"Gaston","type":2,"isSpecialIssue":true,"volumeNumber":1,"startVolumeNumber":2,"endVolumeNumber":3}""";
        // Nested where only an album fits, it carries no kind.
        const string nestedAlbumJson =
            """{"title":"Gaston","seriesTitle":"Gaston","type":2,"isSpecialIssue":true,"volumeNumber":1,"startVolumeNumber":2,"endVolumeNumber":3}""";
        var edition = new EditionLabel(album, "Dupuis", "Gaston", 1985, "2-8001-0000-0");
        var editionJson =
            $$"""{"kind":"edition","album":{{nestedAlbumJson}},"publisherName":"Dupuis","publisherCollectionName":"Gaston","publicationYear":1985,"isbn":"2-8001-0000-0"}""";
        var nestedEditionJson =
            $$"""{"album":{{nestedAlbumJson}},"publisherName":"Dupuis","publisherCollectionName":"Gaston","publicationYear":1985,"isbn":"2-8001-0000-0"}""";

        return new TheoryData<RecordLabel, string>
        {
            { album, albumJson },
            { edition, editionJson },
            { new PurchaseIntentLabel(album, null), $$"""{"kind":"purchaseIntent","album":{{nestedAlbumJson}}}""" },
            { new PurchaseIntentLabel(null, edition), $$"""{"kind":"purchaseIntent","edition":{{nestedEditionJson}}}""" },
            { new AuthorLabel(null, null, "Moebius"), """{"kind":"author","pseudonym":"Moebius"}""" },
            { new PublisherCollectionLabel("Lucky Luke", "Dargaud"), """{"kind":"publisherCollection","name":"Lucky Luke","publisherName":"Dargaud"}""" },
            { new NameLabel("Aventure"), """{"kind":"name","name":"Aventure"}""" },
        };
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void Value_HasItsPinnedJsonForm(ChangeLogValue value, string json)
    {
        var change = new ChangeLogChange(ChangeLogAttribute.Title, null, value);
        var expected = $$"""[{"attribute":1,"after":{{json}}}]""";

        Assert.Equal(expected, ChangeLogJson.SerializeChanges([change]));
        Assert.Equal([change], ChangeLogJson.DeserializeChanges(expected));
    }

    [Theory]
    [MemberData(nameof(Labels))]
    public void Label_HasItsPinnedJsonForm(RecordLabel label, string json)
    {
        Assert.Equal(json, ChangeLogJson.SerializeLabel(label));
        Assert.Equal(label, ChangeLogJson.DeserializeLabel(json));
    }

    [Fact]
    public void ReadingBack_DoesNotDependOnTheOrderOfTheKeys()
    {
        // jsonb stores the keys of an object shortest first, then in byte order: an entry read back
        // must be the entry written, whatever the order of its keys, so that its message is always the same.
        const string storedChanges =
            """[{"after": {"id": "0199c0de-0000-7000-8000-000000000001", "kind": "reference", "label": {"kind": "name", "name": "Tintin"}}, "attribute": 9}]""";
        const string storedLabel = """{"kind": "edition", "isbn": null, "album": {"type": 1, "title": "Gaston"}, "publisherName": "Dupuis"}""";
        var change = new ChangeLogChange(
            ChangeLogAttribute.Series, null, new ReferenceValue(Guid.Parse("0199c0de-0000-7000-8000-000000000001"), new NameLabel("Tintin")));
        var label = new EditionLabel(new AlbumLabel("Gaston", null, AlbumType.Regular, false, null, null, null), "Dupuis", null, null, null);

        Assert.Equal([change], ChangeLogJson.DeserializeChanges(storedChanges));
        Assert.Equal(label, ChangeLogJson.DeserializeLabel(storedLabel));
        Assert.Equal(ChangeLogJson.SerializeChanges([change]), ChangeLogJson.SerializeChanges(ChangeLogJson.DeserializeChanges(storedChanges)));
        Assert.Equal(ChangeLogJson.SerializeLabel(label), ChangeLogJson.SerializeLabel(ChangeLogJson.DeserializeLabel(storedLabel)));
    }

    [Fact]
    public void EveryKindOfValueAndLabel_IsPinned()
    {
        Assert.Equal(
            KindsOf<ChangeLogValue>(),
            Values().Select(row => ((ChangeLogValue)row[0]).GetType()).ToHashSet());
        Assert.Equal(
            KindsOf<RecordLabel>(),
            Labels().Select(row => ((RecordLabel)row[0]).GetType()).ToHashSet());
    }

    private static HashSet<Type> KindsOf<TBase>() =>
        typeof(TBase).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(TBase)) && !t.IsAbstract).ToHashSet();
}
