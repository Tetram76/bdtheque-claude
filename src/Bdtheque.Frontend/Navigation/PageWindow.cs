namespace Bdtheque.Frontend.Navigation;

/// <summary>
/// The pages a pagination offers: all of them when they are few, otherwise the first and the last, and those around the
/// current page, a gap (<see langword="null"/>) standing for the others. Near an end, the pages next to it are shown
/// rather than a gap, so that the pagination keeps about the same width wherever the current page is.
/// </summary>
public static class PageWindow
{
    private const int AllShownUpTo = 7;

    // The pages shown at an end when the current page is near it: as many as the first, the gap and the three around.
    private const int EndSpan = 5;

    public static IReadOnlyList<int?> Of(int current, int count)
    {
        if (count <= AllShownUpTo)
            return [.. Enumerable.Range(1, count).Select(page => (int?)page)];

        var shown = new SortedSet<int> { 1, count, current - 1, current, current + 1 };
        if (current < EndSpan)
            shown.UnionWith(Enumerable.Range(1, EndSpan));
        if (current > count - EndSpan + 1)
            shown.UnionWith(Enumerable.Range(count - EndSpan + 1, EndSpan));
        shown.RemoveWhere(page => page < 1 || page > count);

        var window = new List<int?>();
        foreach (var page in shown)
        {
            if (window.Count > 0 && page > window[^1] + 1)
                window.Add(null);
            window.Add(page);
        }

        return window;
    }
}
