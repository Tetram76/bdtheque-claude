using Microsoft.Extensions.Options;

namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// Downloads the ranges of the International ISBN Agency from its own site, at the start of the application then every day,
/// for <see cref="IsbnFormatter"/>: the agency offers its range message for public use from its site, and forbids
/// republishing it, so that neither the repository nor the images carry it (choix-implementation/socle/frontend.md,
/// présentation). A download that fails keeps the last ranges obtained, and is tried again an hour later.
/// </summary>
public sealed partial class IsbnRangesRefresher(
    IHttpClientFactory clients, IsbnFormatter formatter, IOptions<IsbnRangesOptions> options, TimeProvider clock, ILogger<IsbnRangesRefresher> logger)
    : BackgroundService
{
    /// <summary>The name of its HTTP client.</summary>
    public const string HttpClientName = "IsbnRanges";

    /// <summary>The agency gives new ranges to publishers as they ask: a day is soon enough for a collection.</summary>
    public static readonly TimeSpan RefreshDelay = TimeSpan.FromDays(1);

    /// <summary>A site that does not answer is tried again soon, without loading it.</summary>
    public static readonly TimeSpan RetryDelay = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Value.Source is not { } source)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = await TryDownloadAsync(source, stoppingToken) ? RefreshDelay : RetryDelay;
            try
            {
                await Task.Delay(delay, clock, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task<bool> TryDownloadAsync(Uri source, CancellationToken cancellationToken)
    {
        try
        {
            // A client per download, from the factory, which renews its connections: never one kept by a service that lives as
            // long as the application (documentation of .NET, IHttpClientFactory).
            using var client = clients.CreateClient(HttpClientName);
            using var response = await client.GetAsync(source, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var message = await response.Content.ReadAsStreamAsync(cancellationToken);
            formatter.Ranges = IsbnRanges.Parse(message);
            return true;
        }
        // A message the application no longer reads (its format changed): the ISBN are no longer formatted by new ranges until
        // the application follows it, which the warning reports.
        catch (InvalidDataException exception)
        {
            LogFormatNotRecognized(logger, source, exception);
            return false;
        }
        // Any other failure, network or not: the ranges only format the ISBN, which are presented as entered without them, so
        // that no failure of their download may stop the application, as an exception left to a background service would.
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogDownloadFailed(logger, source, exception);
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The ISBN ranges could not be downloaded from {Source}: the last ones obtained are kept.")]
    private static partial void LogDownloadFailed(ILogger logger, Uri source, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "The ISBN ranges downloaded from {Source} are not in the format the application reads: the last ones obtained are kept, and the ISBN not formatted without them.")]
    private static partial void LogFormatNotRecognized(ILogger logger, Uri source, Exception exception);
}
