using Bdtheque.Contracts.Admin;

namespace Bdtheque.Frontend.Api;

/// <summary>
/// The number of records of each type, which the menu of the administration presents on its every page and its home page
/// details (visuel/administration/): read once per request, whoever asks first.
/// </summary>
public sealed class AdminRecordCounts(ApiClient api)
{
    private Task<RecordCounts>? _counts;

    /// <exception cref="ApiProblemException">The API did not give them.</exception>
    public Task<RecordCounts> GetAsync(CancellationToken cancellationToken = default) =>
        _counts ??= api.GetAsync<RecordCounts>("/admin/record-counts", cancellationToken);

    /// <summary>
    /// The counts, or <c>null</c> when the API did not give them, for a page that goes on without them (e.g. the number of a
    /// list in its heading); the menu of the administration, which reads them on every page, logs the failure.
    /// </summary>
    public async Task<RecordCounts?> TryGetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetAsync(cancellationToken);
        }
        catch (ApiProblemException)
        {
            return null;
        }
    }
}
