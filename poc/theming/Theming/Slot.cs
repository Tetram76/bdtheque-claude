using Microsoft.AspNetCore.Components;

namespace ThemeProto.Theming;

/// <summary>A place in a page that a theme fills with a block receiving <typeparamref name="TData"/>.</summary>
public sealed record Slot<TData>(string Key)
{
    // The constraint is the point: a theme cannot bind a block that expects other data than the slot provides.
    public SlotBinding Use<TBlock>() where TBlock : Block<TData> => new(Key, typeof(TBlock));
}

public sealed record SlotBinding(string Key, Type BlockType);

/// <summary>Base class of every themed block: it only receives the raw data of its slot.</summary>
public abstract class Block<TData> : ComponentBase
{
    [Parameter, EditorRequired] public TData Data { get; set; } = default!;
}
