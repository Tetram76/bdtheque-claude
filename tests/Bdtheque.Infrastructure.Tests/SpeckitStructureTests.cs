using System.Globalization;
using System.Text.RegularExpressions;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The agent reads the .speckit through its map, never in full (gestion-projet/gouvernance/maintenance-du-speckit.md):
/// a concept missing from its index, without description, behind a broken link or with an outdated
/// <c>applies_to</c> would be a rule the agent never reads.
/// </summary>
public sealed partial class SpeckitStructureTests
{
    private static readonly string Speckit = Path.Combine(RepositoryFiles.Root, ".speckit");

    private static readonly string[] ConceptTypes =
    [
        "Gouvernance", "Périmètre", "Règle métier", "Modèle métier", "Contrainte technique", "Choix d'implémentation",
        "Plan d'implémentation", "Journal", "Référence visuelle",
    ];

    private static readonly string[] Tags = ["socle"];

    [Fact]
    public void Concepts_CarryTheirHeader()
    {
        var violations = new List<string>();
        foreach (var concept in Concepts())
        {
            var header = ReadHeader(concept);
            if (header is null)
            {
                violations.Add($"{Relative(concept)}: no front matter");
                continue;
            }

            if (!ConceptTypes.Contains(Value(header, "type")))
                violations.Add($"{Relative(concept)}: type '{Value(header, "type")}' is not one of the concept types");
            if (string.IsNullOrWhiteSpace(Value(header, "title")))
                violations.Add($"{Relative(concept)}: no title");
            if (string.IsNullOrWhiteSpace(Value(header, "description")))
                violations.Add($"{Relative(concept)}: no description");
            if (!GeneratedPattern().IsMatch(Value(header, "generated") ?? ""))
                violations.Add($"{Relative(concept)}: generated is not '{{ by: <actor>, at: <UTC instant> }}'");
            violations.AddRange(TagsOf(header).Where(tag => !Tags.Contains(tag)).Select(tag => $"{Relative(concept)}: unknown tag '{tag}'"));
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Indexes_ListEveryConceptAndFolderWithItsDescription()
    {
        var violations = new List<string>();
        foreach (var folder in Folders())
        {
            var index = Path.Combine(folder, "index.md");
            if (!File.Exists(index))
            {
                violations.Add($"{Relative(folder)}: no index.md");
                continue;
            }

            var entries = IndexEntryPattern().Matches(File.ReadAllText(index))
                .ToDictionary(entry => entry.Groups["path"].Value, entry => entry.Groups["description"].Value);
            foreach (var concept in Directory.GetFiles(folder, "*.md").Where(IsConcept))
            {
                var name = Path.GetFileName(concept);
                if (!entries.TryGetValue(name, out var description))
                    violations.Add($"{Relative(index)}: {name} is not listed");
                else if (description != Value(ReadHeader(concept) ?? [], "description"))
                    violations.Add($"{Relative(index)}: the entry of {name} does not give its description");
            }

            violations.AddRange(Directory.GetDirectories(folder).Where(ContainsMarkdown)
                .Select(sub => $"{Path.GetFileName(sub)}/index.md")
                .Where(sub => !entries.ContainsKey(sub))
                .Select(sub => $"{Relative(index)}: {sub} is not listed"));
            violations.AddRange(entries.Keys.Where(path => !File.Exists(Path.Combine(folder, path)))
                .Select(path => $"{Relative(index)}: lists {path}, which does not exist"));
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Indexes_HaveNoFrontMatterButTheVersionOfTheBundle()
    {
        var root = ReadHeader(Path.Combine(Speckit, "index.md"));
        Assert.NotNull(root);
        Assert.Equal("\"0.2\"", Value(root, "okf_version"));
        Assert.Single(root);

        var others = Folders().Where(folder => folder != Speckit).Select(folder => Path.Combine(folder, "index.md"));
        Assert.DoesNotContain(others, index => File.Exists(index) && ReadHeader(index) is not null);
    }

    [Fact]
    public void Links_DesignateExistingConceptsAndSections()
    {
        var violations = new List<string>();
        foreach (var file in Directory.GetFiles(Speckit, "*.md", SearchOption.AllDirectories))
        {
            // A link written as code is an example, not a renvoi.
            var text = InlineCodePattern().Replace(DoubleBacktickCodePattern().Replace(FencePattern().Replace(File.ReadAllText(file), ""), ""), "");
            foreach (Match link in LinkPattern().Matches(text))
            {
                var href = link.Groups["href"].Value;
                if (href.StartsWith("http:", StringComparison.Ordinal) || href.StartsWith("https:", StringComparison.Ordinal))
                    continue;

                var parts = href.Split('#', 2);
                var target = parts[0].Length == 0
                    ? file
                    : Path.GetFullPath(parts[0].StartsWith('/')
                        ? Path.Combine(Speckit, parts[0].TrimStart('/'))
                        : Path.Combine(Path.GetDirectoryName(file)!, parts[0]));
                if (!File.Exists(target) && !Directory.Exists(target))
                    violations.Add($"{Relative(file)}: {href} designates nothing");
                else if (parts.Length == 2 && File.Exists(target) && !Anchors(target).Contains(Uri.UnescapeDataString(parts[1])))
                    violations.Add($"{Relative(file)}: {href} designates no section");
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void AppliesTo_DesignatesFilesOfTheRepository()
    {
        var files = RepositoryFilePaths().ToList();
        var violations = Concepts()
            .SelectMany(concept => (ReadHeader(concept)?.GetValueOrDefault("applies_to") ?? [])
                .Skip(1)
                .Select(pattern => (Concept: concept, Pattern: Unquote(pattern))))
            .Where(item => !files.Any(Glob(item.Pattern).IsMatch))
            .Select(item => $"{Relative(item.Concept)}: applies_to '{item.Pattern}' designates no file");

        Assert.Empty(violations);
    }

    [Fact]
    public void Moments_DesignateExistingConcepts()
    {
        // The table of moments of AGENTS.md is what the hook of the repository reads to name the concepts of a moment.
        var concepts = AgentsConceptPattern().Matches(RepositoryFiles.Read("AGENTS.md"))
            .Select(link => link.Groups["path"].Value)
            .ToList();

        Assert.NotEmpty(concepts);
        Assert.All(concepts, path => Assert.True(File.Exists(Path.Combine(RepositoryFiles.Root, path)), $"{path} does not exist"));
    }

    [Fact]
    public void Log_GroupsItsEntriesByDateNewestFirst()
    {
        var dates = LogDatePattern().Matches(File.ReadAllText(Path.Combine(Speckit, "log.md")))
            .Select(date => DateOnly.ParseExact(date.Groups["date"].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture))
            .ToList();

        Assert.NotEmpty(dates);
        Assert.Equal(dates.OrderDescending(), dates);
    }

    private static bool IsConcept(string path) => Path.GetFileName(path) is not ("index.md" or "log.md");

    private static IEnumerable<string> Concepts() =>
        Directory.GetFiles(Speckit, "*.md", SearchOption.AllDirectories).Where(IsConcept);

    private static bool ContainsMarkdown(string folder) =>
        Directory.EnumerateFiles(folder, "*.md", SearchOption.AllDirectories).Any();

    private static IEnumerable<string> Folders() =>
        new[] { Speckit }.Concat(Directory.GetDirectories(Speckit, "*", SearchOption.AllDirectories)).Where(ContainsMarkdown);

    private static string Relative(string path) => Path.GetRelativePath(Speckit, path).Replace('\\', '/');

    // Front matter as key -> lines: the value of the key itself, then the items of a list under it.
    private static Dictionary<string, List<string>>? ReadHeader(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0 || lines[0] != "---")
            return null;

        var header = new Dictionary<string, List<string>>();
        List<string>? current = null;
        foreach (var line in lines.Skip(1).TakeWhile(line => line != "---"))
        {
            var key = HeaderKeyPattern().Match(line);
            if (key.Success)
                header[key.Groups["key"].Value] = current = [key.Groups["value"].Value];
            else if (current is not null && HeaderItemPattern().Match(line) is { Success: true } item)
                current.Add(item.Groups["value"].Value);
        }

        return header;
    }

    private static string? Value(Dictionary<string, List<string>> header, string key) =>
        header.TryGetValue(key, out var lines) ? Unquote(lines[0]) : null;

    private static string Unquote(string value) =>
        value.Length >= 2 && value[0] == '\'' && value[^1] == '\'' ? value[1..^1].Replace("''", "'") : value;

    private static string[] TagsOf(Dictionary<string, List<string>> header) =>
        (Value(header, "tags") ?? "").Trim('[', ']').Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    // Anchors as GitHub renders them, which the links of the bundle follow.
    private static HashSet<string> Anchors(string markdown) =>
        HeadingPattern().Matches(File.ReadAllText(markdown))
            .Select(heading => AnchorSpacePattern().Replace(AnchorDropPattern().Replace(heading.Groups["title"].Value.Trim().ToLowerInvariant(), ""), "-"))
            .ToHashSet();

    private static IEnumerable<string> RepositoryFilePaths()
    {
        string[] ignored = ["bin", "obj", ".git", "TestResults", "node_modules"];
        var pending = new Stack<string>([RepositoryFiles.Root]);
        while (pending.TryPop(out var directory))
        {
            foreach (var file in Directory.GetFiles(directory))
                yield return Path.GetRelativePath(RepositoryFiles.Root, file).Replace('\\', '/');
            foreach (var sub in Directory.GetDirectories(directory).Where(sub => !ignored.Contains(Path.GetFileName(sub))))
                pending.Push(sub);
        }
    }

    private static Regex Glob(string pattern) =>
        new("^" + Regex.Escape(pattern).Replace(@"\*\*", "\u0001").Replace(@"\*", "[^/]*").Replace("\u0001", ".*") + "$");

    [GeneratedRegex(@"^\{ by: (?:[\w.-]+/[\w.-]+|human:\S+|process:\S+), at: \d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z \}$")]
    private static partial Regex GeneratedPattern();

    [GeneratedRegex(@"^\* \[(?<title>[^\]]+)\]\((?<path>[^)]+)\)(?: - (?<description>.*?))?\r?$", RegexOptions.Multiline)]
    private static partial Regex IndexEntryPattern();

    [GeneratedRegex(@"\]\((?<href>[^)\s]+)\)")]
    private static partial Regex LinkPattern();

    [GeneratedRegex(@"^```.*?^```", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex FencePattern();

    [GeneratedRegex(@"``.+?``")]
    private static partial Regex DoubleBacktickCodePattern();

    // Removing the code span that names a link (``[`fonctionnel/`](…)``) leaves the link itself.
    [GeneratedRegex(@"`[^`\n]*`")]
    private static partial Regex InlineCodePattern();

    [GeneratedRegex(@"^(?<key>[a-z_]+):\s*(?<value>.*?)\s*$")]
    private static partial Regex HeaderKeyPattern();

    [GeneratedRegex(@"^\s+-\s+(?<value>.*?)\s*$")]
    private static partial Regex HeaderItemPattern();

    [GeneratedRegex(@"^#{1,6} (?<title>.+?)\r?$", RegexOptions.Multiline)]
    private static partial Regex HeadingPattern();

    [GeneratedRegex(@"[^\p{L}\p{N}\s_-]")]
    private static partial Regex AnchorDropPattern();

    [GeneratedRegex(@"\s")]
    private static partial Regex AnchorSpacePattern();

    [GeneratedRegex(@"\]\((?<path>\.speckit/[^)#]+)\)")]
    private static partial Regex AgentsConceptPattern();

    [GeneratedRegex(@"^## (?<date>\d{4}-\d{2}-\d{2})\r?$", RegexOptions.Multiline)]
    private static partial Regex LogDatePattern();
}
