namespace ThemeProto.Theming;

public sealed class ThemeCatalog
{
    private readonly Dictionary<string, Theme> _themes;

    public ThemeCatalog(IEnumerable<Theme> themes, IEnumerable<string> allSlotKeys)
    {
        _themes = themes.ToDictionary(t => t.Id);
        // Fail at startup rather than on the first page that hits a missing block.
        var missing = _themes.Values.Where(t => t.Parent is null)
            .SelectMany(root => allSlotKeys.Where(key => !root.Binds(key)).Select(key => $"{root.Id}: {key}"))
            .ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException("Root themes must bind every slot. Missing: " + string.Join(", ", missing));
    }

    public IReadOnlyCollection<Theme> All => _themes.Values;

    public Theme? Find(string? id) => id is not null && _themes.TryGetValue(id, out var theme) ? theme : null;

    public Theme PickRandom() => _themes.Values.ElementAt(Random.Shared.Next(_themes.Count));
}
