using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Navigation;

public sealed class NavigationEntryTests
{
    [Theory]
    [InlineData("/albums", true)]
    [InlineData("/albums/0197a0b2-0000-7000-8000-000000000001", true)]
    [InlineData("/albums-of-the-year", false)]
    [InlineData("/series", false)]
    [InlineData("/", false)]
    public void Entry_IsCurrent_OnItsPage_AndOnThePagesUnderIt(string currentPath, bool expected) =>
        Assert.Equal(expected, new NavigationEntry("Albums", "/albums").IsCurrent(currentPath));

    [Theory]
    [InlineData("/", true)]
    [InlineData("/albums", false)]
    public void ExactEntry_IsCurrent_OnItsOwnPageOnly(string currentPath, bool expected) =>
        Assert.Equal(expected, new NavigationEntry("Home", "/", Exact: true).IsCurrent(currentPath));

    [Fact]
    public void EntryOfAPageNotYetImplemented_IsNeverCurrent() =>
        Assert.False(new NavigationEntry("Albums", AppPaths.NotYetImplemented).IsCurrent(AppPaths.NotYetImplemented));
}
