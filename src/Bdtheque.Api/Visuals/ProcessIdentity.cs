namespace Bdtheque.Api.Visuals;

/// <summary>
/// The user and group the process runs as (Linux, <c>/proc/self/status</c>), named when the visuals
/// volume cannot be written: the deployment may run <c>api</c> as another user than the one of its
/// image (<c>API_USER</c>), so the image's own variables do not tell who needs the access.
/// </summary>
internal static class ProcessIdentity
{
    private const string StatusPath = "/proc/self/status";

    /// <summary>
    /// The effective user and group of the process, <c>null</c> when the system does not tell (not
    /// Linux, e.g. a development machine).
    /// </summary>
    public static string? Describe() => File.Exists(StatusPath) ? Describe(File.ReadLines(StatusPath)) : null;

    /// <summary>
    /// The effective user and group listed by <paramref name="statusLines"/>: the second id of the
    /// <c>Uid:</c> and <c>Gid:</c> lines, which give the real, effective, saved and file system ids.
    /// </summary>
    public static string? Describe(IEnumerable<string> statusLines)
    {
        string? uid = null, gid = null;
        foreach (var line in statusLines)
        {
            if (line.StartsWith("Uid:", StringComparison.Ordinal))
                uid = EffectiveId(line);
            else if (line.StartsWith("Gid:", StringComparison.Ordinal))
                gid = EffectiveId(line);
        }

        return uid is null || gid is null ? null : $"UID {uid}, GID {gid}";
    }

    private static string? EffectiveId(string line)
    {
        var fields = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
        return fields.Length > 2 ? fields[2] : null;
    }
}
