using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Bdtheque.Api.Security;
using Bdtheque.Contracts.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Bdtheque.Api.Tests;

/// <summary>
/// The logs of production (choix-implementation.md § Journalisation): what serves the operation of
/// the application, without the database statements, and without a user's input mistake looking
/// like an error.
/// </summary>
public sealed class ProductionLoggingTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ProductionLoggingTests(ApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task DatabaseStatements_AreNotLogged()
    {
        var (logs, client) = CapturingLogs();

        (await client.PostAsJsonAsync("/admin/genres", new CreateGenreRequest($"Genre {Guid.NewGuid():N}"))).EnsureSuccessStatusCode();

        Assert.DoesNotContain(logs.Entries, e => e.Category == "Microsoft.EntityFrameworkCore.Database.Command");
    }

    [Fact]
    public async Task BusinessError_IsNotLoggedAsAnError()
    {
        // A label already used breaks a unique index: a mistake the user corrects, reported once by
        // the exception handler as a refused request — the database layer must not log it again as
        // an error.
        var (logs, client) = CapturingLogs();
        var label = $"Genre {Guid.NewGuid():N}";
        (await client.PostAsJsonAsync("/admin/genres", new CreateGenreRequest(label))).EnsureSuccessStatusCode();
        // Only what the refused request logs: the startup may legitimately warn (e.g. a visuals
        // volume not mounted from the host, as on a Linux test machine).
        var before = logs.Entries.Count;

        var response = await client.PostAsJsonAsync("/admin/genres", new CreateGenreRequest(label));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.DoesNotContain(logs.Entries.Skip(before), e => e.Level >= LogLevel.Warning);
    }

    private (CapturingLoggerProvider Logs, HttpClient Client) CapturingLogs()
    {
        var logs = new CapturingLoggerProvider();
        var client = _factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddProvider(logs))).CreateClient();
        client.DefaultRequestHeaders.Add(InternalApiKeyOptions.HeaderName, ApiWebApplicationFactory.InternalApiKey);
        return (logs, client);
    }

    private sealed record LogEntry(string Category, LogLevel Level, string Message);

    // Subject to the log levels of the configuration, as every provider of the application is.
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<LogEntry> _entries = new();

        public IReadOnlyCollection<LogEntry> Entries => _entries;

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                entries.Enqueue(new LogEntry(category, logLevel, formatter(state, exception)));
        }
    }
}
