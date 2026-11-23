using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Audience;
using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Audience;

/// <summary>
/// The criteria of the indicators of the audience measurement, carried by the address of their page: read back as written, an
/// address written by hand taking the default for what it cannot read; the indicators of the API they read; the action they
/// record.
/// </summary>
public sealed class AudienceIndicatorsQueryTests
{
    private static readonly DateOnly November = new(2026, 11, 1);

    [Fact]
    public void Default_ATwelveMonthsOfEveryone_WithoutParameters()
    {
        var query = AudienceIndicatorsQuery.Parse("");

        Assert.Equal(new AudienceIndicatorsQuery(AudiencePeriod.TwelveMonths, AudienceUsers.All, null, null), query);
        Assert.Equal(AppPaths.AdminStatistics, query.Href());
        Assert.Null(query.Action);
    }

    [Fact]
    public void Criteria_ReadBackFromTheirAddress()
    {
        var query = new AudienceIndicatorsQuery(AudiencePeriod.All, AudienceUsers.Visitors, DeviceType.Smartphone, RenderingEngine.WebKit);

        Assert.Equal("/admin/statistics?period=All&users=Visitors&device=Smartphone&engine=WebKit", query.Href());
        Assert.Equal(query, AudienceIndicatorsQuery.Parse(new Uri(new Uri("http://frontend"), query.Href()).Query));
    }

    [Theory]
    [InlineData("?period=Decade&users=Robots&device=Fridge&engine=Trident")]
    [InlineData("?period=1&users=2&device=3&engine=1")]
    [InlineData("?device=&engine=%20")]
    public void ValueItCannotRead_TakesTheDefault(string address) =>
        Assert.Equal(new AudienceIndicatorsQuery(), AudienceIndicatorsQuery.Parse(address));

    [Theory]
    [InlineData(AudiencePeriod.CurrentMonth, "/admin/audience/indicators?from=2026-11-01")]
    [InlineData(AudiencePeriod.ThreeMonths, "/admin/audience/indicators?from=2026-09-01")]
    [InlineData(AudiencePeriod.TwelveMonths, "/admin/audience/indicators?from=2025-12-01")]
    [InlineData(AudiencePeriod.All, "/admin/audience/indicators")]
    public void Period_UpToTheCurrentMonth(AudiencePeriod period, string expected) =>
        Assert.Equal(expected, new AudienceIndicatorsQuery(period).ApiPath(November));

    [Theory]
    [InlineData(AudienceUsers.Visitors, "administrator=false")]
    [InlineData(AudienceUsers.Administrator, "administrator=true")]
    public void Users_TheVisitorsOrTheAdministrator(AudienceUsers users, string expected) =>
        Assert.Equal($"/admin/audience/indicators?{expected}", new AudienceIndicatorsQuery(AudiencePeriod.All, users).ApiPath(November));

    [Fact]
    public void DeviceAndEngine_ByTheirNames()
    {
        var query = new AudienceIndicatorsQuery(AudiencePeriod.CurrentMonth, AudienceUsers.All, DeviceType.Tablet, RenderingEngine.Gecko);

        Assert.Equal("/admin/audience/indicators?from=2026-11-01&deviceType=Tablet&renderingEngine=Gecko", query.ApiPath(November));
    }

    [Fact]
    public void OtherCriteria_RecordTheActionOfTheFilter_WithTheCriteria()
    {
        var query = new AudienceIndicatorsQuery(AudiencePeriod.ThreeMonths, AudienceUsers.Administrator);

        Assert.Equal((AudienceFunctions.AdminStatisticsFilter, "period=ThreeMonths&users=Administrator"), query.Action);
    }

    [Fact]
    public void Page_KeptWithTheCriteria_AndRecordedAsAPage()
    {
        var query = AudienceIndicatorsQuery.Parse("?users=Visitors&page=3");

        Assert.Equal(new AudienceIndicatorsQuery(Users: AudienceUsers.Visitors, Page: 3), query);
        Assert.Equal("/admin/statistics?users=Visitors&page=3", query.Href());
        Assert.Equal("/admin/statistics?users=Visitors", query.PageHref(1));
        Assert.Equal((AudienceFunctions.AdminStatisticsPage, "users=Visitors&page=3"), query.Action);
        // The page is no criterion of the API, which gives every function.
        Assert.Equal("/admin/audience/indicators?from=2025-12-01&administrator=false", query.ApiPath(November));
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page=-2")]
    [InlineData("?page=two")]
    public void PageItCannotRead_TheFirst(string address) =>
        Assert.Equal(1, AudienceIndicatorsQuery.Parse(address).Page);

    [Theory]
    [InlineData("2026-11-30T23:59:59+00:00", "2026-11-01")]
    // The months are those of universal time, whatever the offset of the instant.
    [InlineData("2026-12-01T00:30:00+01:00", "2026-11-01")]
    public void MonthOf_AnInstant_InUniversalTime(string instant, string month) =>
        Assert.Equal(DateOnly.Parse(month, System.Globalization.CultureInfo.InvariantCulture),
            AudienceIndicatorsQuery.MonthOf(DateTimeOffset.Parse(instant, System.Globalization.CultureInfo.InvariantCulture)));
}
