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
}
