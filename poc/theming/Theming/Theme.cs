namespace ThemeProto.Theming;

/// <summary>
/// A theme only declares what it changes; anything else is resolved on its parent, down to the
/// root theme, which must bind every slot.
/// </summary>
public sealed class Theme
{
    private readonly Dictionary<string, Type> _blocks;

    public Theme(string id, string name, Theme? parent, string? fontsUrl, params SlotBinding[] bindings)
    {
        Id = id;
        Name = name;
        Parent = parent;
        _fontsUrl = fontsUrl;
        _blocks = bindings.ToDictionary(b => b.Key, b => b.BlockType);
    }

    private readonly string? _fontsUrl;

    public string Id { get; }
    public string Name { get; }
    public Theme? Parent { get; }
    public string? FontsUrl => _fontsUrl ?? Parent?.FontsUrl;

    public bool Binds(string slotKey) => _blocks.ContainsKey(slotKey);

    public Type Resolve(string slotKey) =>
        _blocks.TryGetValue(slotKey, out var type) ? type
        : Parent?.Resolve(slotKey) ?? throw new InvalidOperationException($"No block bound to slot '{slotKey}'.");

    /// <summary>Stylesheets from the root theme to this one, so that each theme overrides its parent.</summary>
    public IEnumerable<string> Stylesheets => (Parent?.Stylesheets ?? []).Append($"themes/{Id}.css");
}
