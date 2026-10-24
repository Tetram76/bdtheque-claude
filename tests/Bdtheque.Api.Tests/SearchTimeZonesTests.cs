using Bdtheque.Api.Search;
using Microsoft.AspNetCore.Http;

namespace Bdtheque.Api.Tests;

/// <summary>The time zone of a search, and the day a date and time falls on in it.</summary>
public sealed class SearchTimeZonesTests
{
    [Fact]
    public void Resolve_WithoutName_IsUniversalTime() => Assert.Same(TimeZoneInfo.Utc, SearchTimeZones.Resolve(null));

    [Fact]
    public void Resolve_IanaName_IsThatTimeZone() => Assert.Equal("Europe/Paris", SearchTimeZones.Resolve("Europe/Paris").Id);

    [Theory]
    [InlineData("Nowhere/Atlantis")]
    // A Windows name, which .NET knows but PostgreSQL does not.
    [InlineData("Romance Standard Time")]
    public void Resolve_AnyOtherName_IsRefused(string name) => Assert.Throws<BadHttpRequestException>(() => SearchTimeZones.Resolve(name));

    [Fact]
    public void StartOfADay_IsItsMidnightInTheTimeZone() =>
        Assert.Equal(
            new DateTimeOffset(2025, 12, 31, 23, 0, 0, TimeSpan.Zero),
            InstantField.StartOf(new DateOnly(2026, 1, 1), SearchTimeZones.Resolve("Europe/Paris")));

    [Fact]
    public void StartOfADay_ThatBeginsInAGap_IsTheEndOfTheGap() =>
        // Santiago moves to summer time at midnight: 7 September 2025 begins at 01:00 (UTC-3).
        Assert.Equal(
            new DateTimeOffset(2025, 9, 7, 4, 0, 0, TimeSpan.Zero),
            InstantField.StartOf(new DateOnly(2025, 9, 7), SearchTimeZones.Resolve("America/Santiago")));
}
