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
            new StubClients(_handler), _formatter, Options.Create(new IsbnRangesOptions { Source = Source }), _clock,
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

    public static TheoryData<Exception> Failures => [new HttpRequestException("unreachable"), new InvalidOperationException("unexpected")];

    // Whatever the failure, the application goes on, every ISBN as entered, and the failure is reported.
    [Theory]
    [MemberData(nameof(Failures))]
    public async Task Download_ThatFails_IsReportedAsAWarning_TheIsbnAsEntered(Exception failure)
    {
        _answers.Enqueue(() => throw failure);

        await _refresher.StartAsync(CancellationToken.None);
        await NextAttemptAsync(Start + IsbnRangesRefresher.RetryDelay);

        var warning = Assert.Single(_logger.Entries);
        Assert.Equal((LogLevel.Warning, failure), (warning.Level, warning.Exception));
        Assert.Equal("9782723445671", _formatter.Format("9782723445671"));
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
