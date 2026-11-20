using System.Text.RegularExpressions;
using Bdtheque.Frontend.Components.Pages.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Tests.Lists;

/// <summary>
/// A line break never separates a number from what it counts: the column of the numbers only wraps between two of
/// them (visuel/listes/principes-communs.md, Nombres). The page tests read their texts with plain spaces, so only these
/// tests pin the non-breaking space of the texts themselves.
/// </summary>
public sealed partial class ListCountTextsTests
{
    private static readonly IServiceProvider Services = new ServiceCollection().AddLogging().AddLocalization().BuildServiceProvider();

    public static TheoryData<Type> Lists() =>
        [typeof(AdminAlbums), typeof(AdminSeries), typeof(AdminAuthors), typeof(AdminPublishers), typeof(AdminGenres), typeof(AdminUniverses)];

    [Theory]
    [MemberData(nameof(Lists))]
    public void EveryNumber_IsGluedToWhatItCounts(Type list)
    {
        var localizer = (IStringLocalizer)Services.GetRequiredService(typeof(IStringLocalizer<>).MakeGenericType(list));
        var texts = localizer.GetAllStrings(includeParentCultures: true).ToList();

        Assert.Contains(texts, text => GluedNumber().IsMatch(text.Value));
        Assert.Empty(texts.Where(text => BreakableNumber().IsMatch(text.Value)).Select(text => $"{text.Name}: {text.Value}"));
    }

    // A number, written or to come, followed by a word.
    [GeneratedRegex(@"(\{\d+\}|\b\d+) (?=\p{L})")]
    private static partial Regex BreakableNumber();

    [GeneratedRegex(@"(\{\d+\}|\b\d+) (?=\p{L})")]
    private static partial Regex GluedNumber();
}
