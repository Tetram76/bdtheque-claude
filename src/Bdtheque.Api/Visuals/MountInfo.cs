namespace Bdtheque.Api.Visuals;

/// <summary>
/// Whether a path of the container lies on a mount (Linux, <c>/proc/self/mountinfo</c>): the visuals
/// volume must be mounted from outside the container, or its files are lost whenever the container
/// is recreated.
/// </summary>
internal static class MountInfo
{
    private const string MountInfoPath = "/proc/self/mountinfo";

    /// <summary>
    /// Whether <paramref name="path"/> lies on a mount of the container, <c>null</c> when the system
    /// does not tell (not Linux, e.g. a development machine).
    /// </summary>
    public static bool? IsOnMount(string path) =>
        File.Exists(MountInfoPath) ? IsOnMount(Path.GetFullPath(path), MountPoints(File.ReadLines(MountInfoPath))) : null;

    /// <summary>
    /// Whether <paramref name="path"/> is one of <paramref name="mountPoints"/> or lies below one,
    /// the root of the container (<c>/</c>) excepted: a path only on the root is in the container's
    /// own file system.
    /// </summary>
    public static bool IsOnMount(string path, IEnumerable<string> mountPoints)
    {
        var normalized = path.TrimEnd('/');
        return mountPoints
            .Select(m => m.TrimEnd('/'))
            .Where(m => m.Length > 0)
            .Any(m => normalized == m || normalized.StartsWith(m + "/", StringComparison.Ordinal));
    }

    /// <summary>
    /// The mount points listed by <paramref name="mountInfoLines"/>: the fifth field of each line,
    /// whose spaces and other special characters are escaped in octal (<c>\040</c>).
    /// </summary>
    public static IEnumerable<string> MountPoints(IEnumerable<string> mountInfoLines) =>
        mountInfoLines
            .Select(line => line.Split(' '))
            .Where(fields => fields.Length > 4)
            .Select(fields => Unescape(fields[4]));

    private static string Unescape(string field)
    {
        var result = new System.Text.StringBuilder(field.Length);
        for (var i = 0; i < field.Length; i++)
        {
            if (field[i] == '\\' && i + 3 < field.Length && !field.AsSpan(i + 1, 3).ContainsAnyExcept("01234567"))
            {
                result.Append((char)Convert.ToInt32(field.Substring(i + 1, 3), 8));
                i += 3;
            }
            else
            {
                result.Append(field[i]);
            }
        }

        return result.ToString();
    }
}
