using System.Globalization;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.Contracts.Search;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Formatting;

/// <summary>
/// The texts of the entries of the change log, built when they are presented (modele-metier/entree-du-journal-des-modifications.md):
/// every type of record, operation and attribute named in the display culture, and each kind of value as the record holds it.
/// </summary>
public sealed class ChangeLogTextsTests : IDisposable
{
    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;
    private readonly CultureInfo _previousUi = CultureInfo.CurrentUICulture;
    private readonly ServiceProvider _services;
    private readonly ChangeLogTexts _texts;

    public ChangeLogTextsTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
        _services = new ServiceCollection().AddLogging().AddLocalization()
            .AddSingleton<ValueLabels>().AddSingleton<IsbnFormatter>().AddSingleton<RecordLabels>()
            .AddScoped(_ => new BrowserTimeZone { Name = "Europe/Paris" }).AddScoped<DisplayFormatter>().AddScoped<ChangeLogTexts>()
            .BuildServiceProvider();
        _texts = _services.CreateScope().ServiceProvider.GetRequiredService<ChangeLogTexts>();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _previous;
        CultureInfo.CurrentUICulture = _previousUi;
        _services.Dispose();
    }

    public static TheoryData<RecordType> RecordTypes => [.. Enum.GetValues<RecordType>()];

    public static TheoryData<ChangeLogOperation> Operations => [.. Enum.GetValues<ChangeLogOperation>()];

    public static TheoryData<ChangeLogAttribute> Attributes => [.. Enum.GetValues<ChangeLogAttribute>()];

    // A text not found reads as its key.
    [Theory]
    [MemberData(nameof(RecordTypes))]
    public void EveryTypeOfRecord_IsNamed(RecordType type) => Assert.NotEqual($"RecordType.{type}", _texts.Of(type));

    [Theory]
    [MemberData(nameof(Operations))]
    public void EveryOperation_IsNamed(ChangeLogOperation operation) => Assert.NotEqual($"Operation.{operation}", _texts.Of(operation));

    [Theory]
    [MemberData(nameof(Attributes))]
    public void EveryAttribute_IsNamed(ChangeLogAttribute attribute) => Assert.NotEqual($"Attribute.{attribute}", _texts.Of(attribute));

    public static TheoryData<ChangeLogValue, string> Values => new()
    {
        { new TextValue("Le Lotus bleu"), "Le Lotus bleu" },
        { new NumberValue(1234), "1 234" },
        { new FlagValue(true), "Oui" },
        { new FlagValue(false), "Non" },
        { new DateValue(new DateOnly(2026, 11, 25)), "25/11/2026" },
        { new PartialDateValue(1986, 5), "mai 1986" },
        { new PartialDateValue(1986, null), "1986" },
        { new AmountValue(12.5m, "EUR"), "12,50 €" },
        { new EnumValue(nameof(BindingType), nameof(BindingType.Hardcover)), "Cartonnée" },
        // A member retired since the entry was written, given by its number.
        { new EnumValue(nameof(BindingType), "7"), "7" },
        { new ReferenceValue(Guid.Empty, new AlbumLabel("Tintin au Tibet", "Tintin", AlbumType.Regular, false, 20, null, null)), "Tintin au Tibet (Tintin - T. 20)" },
        { new ContributionValue(Guid.Empty, new AuthorLabel("Remi", "Georges", "Hergé"), ContributionRole.Scenarist), "Hergé (Scénario)" },
        { new VisualValue(Guid.Empty, VisualType.Cover, 0), "Couverture" },
        { new VisualValue(Guid.Empty, VisualType.Cover, 1), "Couverture 2" },
        { new VisualValue(Guid.Empty, VisualType.Plate, 0), "Planche 1" },
        { new SearchCriteriaValue(new SearchGroup(SearchOperator.And, [new SearchNot(new SearchLink("series")), new SearchLink("genres")])), "2 critères" },
        { new SearchVariablesValue([new SavedSearchVariable("Titre", "/conditions/0/value", "Astérix")]), "Titre" },
        { new SearchVariablesValue([]), "Aucune variable" },
    };

    [Theory]
    [MemberData(nameof(Values))]
    public void Value_AsTheRecordHoldsIt(ChangeLogValue value, string text) =>
        Assert.Equal(text, _texts.Value(value, AlbumLabelFormat.TitleSeriesVolume).Replace(' ', ' ').Replace(' ', ' '));

    [Fact]
    // An edition by itself, its album on the line of its type; any other record by its label, under its type.
    public void Edition_ByItself_ItsAlbumWithItsType()
    {
        var album = new AlbumLabel("L'Iris blanc", "Astérix", AlbumType.Regular, false, 40, null, null);
        var edition = new EditionLabel(album, "Albert René", null, 2023, null);

        Assert.Equal(("Albert René [2023]", "Édition de L'Iris blanc (Astérix - T. 40)"),
            (_texts.LabelOf(edition, AlbumLabelFormat.TitleSeriesVolume), _texts.TypeOf(edition, RecordType.Edition, AlbumLabelFormat.TitleSeriesVolume)));
        Assert.Equal(("L'Iris blanc (Astérix - T. 40)", "Album"),
            (_texts.LabelOf(album, AlbumLabelFormat.TitleSeriesVolume), _texts.TypeOf(album, RecordType.Album, AlbumLabelFormat.TitleSeriesVolume)));
    }
}
