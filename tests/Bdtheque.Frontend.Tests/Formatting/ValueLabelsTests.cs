using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Formatting;

/// <summary>The values of the enumerations a page presents, each named in the display culture.</summary>
public sealed class ValueLabelsTests
{
    private readonly ValueLabels _labels =
        new ServiceCollection().AddLogging().AddLocalization().AddSingleton<ValueLabels>()
            .BuildServiceProvider().GetRequiredService<ValueLabels>();

    public static TheoryData<Enum> Values =>
    [
        .. Enum.GetValues<ContributionRole>().Cast<Enum>(),
        .. Enum.GetValues<VisualType>().Cast<Enum>(),
        .. Enum.GetValues<AcquisitionMode>().Cast<Enum>(),
        .. Enum.GetValues<EditionCondition>().Cast<Enum>(),
        .. Enum.GetValues<EditionFormat>().Cast<Enum>(),
        .. Enum.GetValues<EditionCategory>().Cast<Enum>(),
        .. Enum.GetValues<BindingType>().Cast<Enum>(),
        .. Enum.GetValues<BookOrientation>().Cast<Enum>(),
        .. Enum.GetValues<ReadingDirection>().Cast<Enum>(),
        .. Enum.GetValues<AlbumRating>().Cast<Enum>(),
        .. Enum.GetValues<SeriesStatus>().Cast<Enum>(),
    ];

    [Theory]
    [MemberData(nameof(Values))]
    public void EveryValue_HasALabel(Enum value) =>
        Assert.False(_labels.ResourceNotFound(value), $"{value.GetType().Name}.{value}");

    [Fact]
    public void Value_IsNamedAsTheModelNamesIt()
    {
        Assert.Equal("Scénario", _labels.Of(ContributionRole.Scenarist));
        Assert.Equal("Très bien", _labels.Of(AlbumRating.VeryGood));
        Assert.Equal("Cartonnée", _labels.Of(BindingType.Hardcover));
        Assert.Equal("Excellent (état neuf)", _labels.Of(EditionCondition.Excellent));
        Assert.Equal("Grand (> A4)", _labels.Of(EditionFormat.Large));
    }
}
