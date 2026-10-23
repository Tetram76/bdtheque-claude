using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

public sealed class AudienceTests
{
    private static readonly PresentedRecord Album = new(RecordType.Album, Guid.CreateVersion7());

    [Fact]
    public void Open_StartsTheVisitWithItsFirstUsage()
    {
        var first = AudienceVisit.Open(true, DeviceType.Tablet, RenderingEngine.WebKit, " 605.1 ", new FunctionUse("catalog.album", Album, null));

        Assert.True(first.Visit!.IsAdministrator);
        Assert.Equal(DeviceType.Tablet, first.Visit!.DeviceType);
        Assert.Equal(RenderingEngine.WebKit, first.Visit!.RenderingEngine);
        Assert.Equal("605.1", first.Visit!.EngineVersion);
        Assert.Equal(first.Visit!.Id, first.VisitId);
        Assert.Equal("catalog.album", first.Function);
        Assert.Equal(Album, first.PresentedRecord);
        Assert.False(first.IsRepeat);
    }

    [Fact]
    public void Open_LeavesABlankEngineVersionUnknown()
    {
        var first = AudienceVisit.Open(false, DeviceType.Computer, RenderingEngine.Other, "  ", new FunctionUse("dashboard", null, null));

        Assert.Null(first.Visit!.EngineVersion);
    }

    [Fact]
    public void Open_RefusesAnUndefinedDeviceOrEngine()
    {
        var use = new FunctionUse("dashboard", null, null);

        Assert.Throws<ArgumentOutOfRangeException>(() => AudienceVisit.Open(false, (DeviceType)99, RenderingEngine.Blink, null, use));
        Assert.Throws<ArgumentOutOfRangeException>(() => AudienceVisit.Open(false, DeviceType.Computer, (RenderingEngine)99, null, use));
    }

    [Fact]
    public void Follow_RecordsTheNextUsageOfTheSameVisit()
    {
        var first = AudienceVisit.Open(false, DeviceType.Smartphone, RenderingEngine.Blink, null, new FunctionUse("dashboard", null, null));

        var next = first.Follow(new FunctionUse("catalog.search", null, "{\"q\":\"tintin\"}"));

        Assert.Equal(first.VisitId, next.VisitId);
        Assert.Null(next.Visit);
        Assert.Equal("catalog.search", next.Function);
        Assert.Equal("{\"q\":\"tintin\"}", next.Parameters);
        Assert.False(next.IsRepeat);
    }

    [Theory]
    [InlineData("catalog.album", true, null, true)]
    [InlineData("admin.album", true, null, false)]
    [InlineData("catalog.album", false, null, false)]
    [InlineData("catalog.search", false, "{\"q\":\"a\"}", true)]
    [InlineData("catalog.search", false, "{\"q\":\"b\"}", false)]
    [InlineData("dashboard", false, null, true)]
    public void Follow_RepeatsThePreviousUsage_OnlyForTheSameFunctionRecordAndParameters(
        string function, bool sameRecord, string? parameters, bool repeats)
    {
        var previousUse = function switch
        {
            "catalog.search" => new FunctionUse(function, null, "{\"q\":\"a\"}"),
            "dashboard" => new FunctionUse(function, null, null),
            _ => new FunctionUse("catalog.album", Album, null),
        };
        var previous = AudienceVisit.Open(false, DeviceType.Computer, RenderingEngine.Gecko, "128", previousUse);
        var record = parameters is null && function != "dashboard"
            ? sameRecord ? Album : new PresentedRecord(RecordType.Album, Guid.CreateVersion7())
            : null;

        var next = previous.Follow(new FunctionUse(function, record, parameters));

        Assert.Equal(repeats, next.IsRepeat);
    }

    [Fact]
    public void FunctionUse_RequiresAFunction()
    {
        Assert.Throws<ArgumentException>(() => new FunctionUse(" ", null, null));
    }

    [Fact]
    public void FunctionUse_CarriesARecordOrParameters_NeverBoth()
    {
        Assert.Throws<ArgumentException>(() => new FunctionUse("admin.album.save", Album, "{}"));
    }

    [Fact]
    public void FunctionUse_TrimsTheFunction_AndLeavesBlankParametersOut()
    {
        var use = new FunctionUse(" dashboard ", null, " ");

        Assert.Equal("dashboard", use.Function);
        Assert.Null(use.Parameters);
    }

    [Fact]
    public void PresentedRecord_RefusesAnUndefinedType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PresentedRecord((RecordType)99, Guid.CreateVersion7()));
    }

    [Fact]
    public void Counter_KeepsItsFigures()
    {
        var month = new DateOnly(2026, 3, 1);
        var last = new DateTimeOffset(2026, 4, 2, 10, 0, 0, TimeSpan.Zero);
        var objects = new[] { new AudienceCounterObject(Album, null, 2), new AudienceCounterObject(null, "{}", 1) };

        var counter = new AudienceCounter("catalog.album", month, false, DeviceType.Computer, RenderingEngine.Blink, "130", 3, 2, 1, last, objects);

        Assert.Equal("catalog.album", counter.Function);
        Assert.Equal(month, counter.Month);
        Assert.Equal("130", counter.EngineVersion);
        Assert.Equal((3, 2, 1, last), (counter.UsageCount, counter.VisitCount, counter.RepeatedUsageCount, counter.LastUsage));
        Assert.Equal(objects, counter.Objects);
    }

    [Fact]
    public void Counter_RefusesAMonthNotStartingOnItsFirstDay()
    {
        Assert.Throws<ArgumentException>(() => new AudienceCounter(
            "dashboard", new DateOnly(2026, 3, 2), false, DeviceType.Computer, RenderingEngine.Blink, null, 1, 1, 0,
            DateTimeOffset.UtcNow, []));
    }

    [Theory]
    [InlineData(0, 0, 0)] // a counter counts at least one usage
    [InlineData(2, 3, 0)] // more visits than usages
    [InlineData(2, 2, 1)] // the first usage of a visit repeats nothing
    [InlineData(2, 0, 0)] // a usage belongs to a visit
    public void Counter_RefusesInconsistentFigures(int usages, int visits, int repeated)
    {
        Assert.Throws<ArgumentException>(() => new AudienceCounter(
            "dashboard", new DateOnly(2026, 3, 1), false, DeviceType.Computer, RenderingEngine.Blink, null, usages, visits, repeated,
            DateTimeOffset.UtcNow, []));
    }

    [Fact]
    public void CounterObject_IsARecordOrParameters_UsedAtLeastOnce()
    {
        Assert.Throws<ArgumentException>(() => new AudienceCounterObject(null, null, 1));
        Assert.Throws<ArgumentException>(() => new AudienceCounterObject(Album, "{}", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AudienceCounterObject(Album, null, 0));
    }
}
