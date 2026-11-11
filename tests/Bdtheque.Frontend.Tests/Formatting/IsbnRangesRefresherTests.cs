using System.Net;
using System.Text;
using Bdtheque.Frontend.Formatting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Bdtheque.Frontend.Tests.Formatting;

/// <summary>
/// The ranges of the International ISBN Agency, downloaded from its site by the application, at its start then every day,
/// the last ones obtained kept when a download fails (choix-implementation/socle/frontend.md, présentation).
/// </summary>
public sealed class IsbnRangesRefresherTests : IAsyncDisposable
{
    private static readonly Uri Source = new("https://ranges.example/export_rangemessage.xml");
    private static readonly DateTimeOffset Start = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Start);
    private readonly IsbnFormatter _formatter = new();
    private readonly Queue<Func<HttpResponseMessage>> _answers = new();
    private readonly List<Uri> _requested = [];
    private readonly CapturingLogger _logger = new();
    private readonly StubHandler _handler;
    private readonly List<(DateTimeOffset WakeUp, DateTimeOffset? NextAttempt)> _armedWaits = [];
    private readonly IsbnRangesRefresher _refresher;

    public IsbnRangesRefresherTests()
    {
        _handler = new StubHandler(request =>
        {
            lock (_requested)
                _requested.Add(request.RequestUri!);
            return _answers.Dequeue()();
        });
        _refresher = new IsbnRangesRefresher(
            new StubClients(_handler), _formatter, Options.Create(new IsbnRangesOptions { Source = Source }),
            new ObservedClock(_clock, wakeUp =>
            {
                lock (_armedWaits)
                    _armedWaits.Add((wakeUp, _refresher!.NextAttempt));
            }),
            _logger);
    }

    public async ValueTask DisposeAsync()
    {
        await _refresher.StopAsync(CancellationToken.None);
        _refresher.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task Ranges_AreDownloadedAtTheStart_ThenAgainADayLater()
    {
        _answers.Enqueue(RangeMessage);

        await _refresher.StartAsync(CancellationToken.None);
        await NextAttemptAsync(Start + IsbnRangesRefresher.RefreshDelay);

        Assert.Equal([Source], _requested);
        Assert.Equal("978-2-7234-4567-1", _formatter.Format("9782723445671"));
    }

    [Fact]
    public async Task Download_ThatFails_IsTriedAgainAnHourLater_TheIsbnAsEnteredMeanwhile()
    {
        _answers.Enqueue(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        _answers.Enqueue(RangeMessage);

        await _refresher.StartAsync(CancellationToken.None);
        await NextAttemptAsync(Start + IsbnRangesRefresher.RetryDelay);
        Assert.Equal("9782723445671", _formatter.Format("9782723445671"));
        _clock.Advance(IsbnRangesRefresher.RetryDelay);
        await NextAttemptAsync(Start + IsbnRangesRefresher.RetryDelay + IsbnRangesRefresher.RefreshDelay);

        Assert.Equal(2, _requested.Count);
        Assert.Equal("978-2-7234-4567-1", _formatter.Format("9782723445671"));
    }

    [Fact]
    public async Task Download_ThatFailsAfterwards_KeepsTheLastRanges()
    {
        _answers.Enqueue(RangeMessage);
        _answers.Enqueue(() => throw new HttpRequestException("unreachable"));

        await _refresher.StartAsync(CancellationToken.None);
        await NextAttemptAsync(Start + IsbnRangesRefresher.RefreshDelay);
        _clock.Advance(IsbnRangesRefresher.RefreshDelay);
        await NextAttemptAsync(Start + IsbnRangesRefresher.RefreshDelay + IsbnRangesRefresher.RetryDelay);

        Assert.Equal(2, _requested.Count);
        Assert.Equal("978-2-7234-4567-1", _formatter.Format("9782723445671"));
    }

    [Fact]
    public async Task Message_WhoseFormatChanged_IsReportedAsAWarning_TheLastRangesKept()
    {
        _answers.Enqueue(RangeMessage);
        _answers.Enqueue(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(IsbnRangesTests.RangeMessage.Replace("<Range>", "<Interval>").Replace("</Range>", "</Interval>"),
                Encoding.UTF8, "text/xml"),
        });

        await _refresher.StartAsync(CancellationToken.None);
        await NextAttemptAsync(Start + IsbnRangesRefresher.RefreshDelay);
        _clock.Advance(IsbnRangesRefresher.RefreshDelay);
        await NextAttemptAsync(Start + IsbnRangesRefresher.RefreshDelay + IsbnRangesRefresher.RetryDelay);

        // The application goes on, the ISBN formatted by the last ranges, and the change of format is reported.
        Assert.Equal("978-2-7234-4567-1", _formatter.Format("9782723445671"));
        var warning = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Contains("format", warning.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidDataException>(warning.Exception);
    }

    // Whatever the failure, the application goes on, every ISBN as entered, and the failure is reported. Named by the case,
    // which the test explorer lists one by one, an exception not being serializable.
    [Theory]
    [InlineData("network")]
    [InlineData("unexpected")]
    public async Task Download_ThatFails_IsReportedAsAWarning_TheIsbnAsEntered(string failureCase)
    {
        Exception failure = failureCase == "network" ? new HttpRequestException("unreachable") : new InvalidOperationException("unexpected");
        _answers.Enqueue(() => throw failure);

        await _refresher.StartAsync(CancellationToken.None);
        await NextAttemptAsync(Start + IsbnRangesRefresher.RetryDelay);

        var warning = Assert.Single(_logger.Entries);
        Assert.Equal((LogLevel.Warning, failure), (warning.Level, warning.Exception));
        Assert.Equal("9782723445671", _formatter.Format("9782723445671"));
    }

    // Announced before its wait was armed, the next attempt let a test move the clock on before the wait started, which then
    // ended a whole delay later and left the loop stuck (#375): each wait is armed while the previous attempt is still the
    // one announced.
    [Fact]
    public async Task NextAttempt_IsAnnounced_OnlyOnceItsWaitIsArmedOnTheClock()
    {
        _answers.Enqueue(RangeMessage);
        _answers.Enqueue(() => throw new HttpRequestException("unreachable"));

        await _refresher.StartAsync(CancellationToken.None);
        await NextAttemptAsync(Start + IsbnRangesRefresher.RefreshDelay);
        _clock.Advance(IsbnRangesRefresher.RefreshDelay);
        await NextAttemptAsync(Start + IsbnRangesRefresher.RefreshDelay + IsbnRangesRefresher.RetryDelay);

        lock (_armedWaits)
            Assert.Equal(
                [
                    (Start + IsbnRangesRefresher.RefreshDelay, null),
                    (Start + IsbnRangesRefresher.RefreshDelay + IsbnRangesRefresher.RetryDelay, Start + IsbnRangesRefresher.RefreshDelay),
                ],
                _armedWaits);
    }

    // A bounded wait on the background loop, until it awaits its next attempt at the time given: the fake clock alone moves
    // it on.
    private async Task NextAttemptAsync(DateTimeOffset expected)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (_refresher.NextAttempt != expected)
            await Task.Delay(10, timeout.Token);
    }

    private static HttpResponseMessage RangeMessage() =>
        new(HttpStatusCode.OK) { Content = new StringContent(IsbnRangesTests.RangeMessage, Encoding.UTF8, "text/xml") };

    // A client per download, as the factory gives it, on the same handler, which it leaves to the test.
    private sealed class StubClients(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(answer(request));
    }

    // The fake clock, which tells the time each wait armed on it is to wake up, as it is armed.
    private sealed class ObservedClock(FakeTimeProvider clock, Action<DateTimeOffset> armed) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => clock.GetUtcNow();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            armed(clock.GetUtcNow() + dueTime);
            return clock.CreateTimer(callback, state, dueTime, period);
        }
    }

    // What the refresher writes to the journal, with its level and its exception.
    private sealed class CapturingLogger : ILogger<IsbnRangesRefresher>
    {
        public System.Collections.Concurrent.ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Enqueue((logLevel, formatter(state, exception), exception));
    }
}
