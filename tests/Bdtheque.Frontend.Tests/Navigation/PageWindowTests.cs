using Bdtheque.Frontend.Navigation;

namespace Bdtheque.Frontend.Tests.Navigation;

public sealed class PageWindowTests
{
    [Theory]
    [InlineData(1, 1, new[] { 1 })]
    [InlineData(2, 3, new[] { 1, 2, 3 })]
    [InlineData(7, 7, new[] { 1, 2, 3, 4, 5, 6, 7 })]
    public void FewPages_AreAllShown(int current, int count, int[] expected) =>
        Assert.Equal(expected.Select(page => (int?)page), PageWindow.Of(current, count));

    [Theory]
    [InlineData(1, new int[] { 1, 2, 3, 4, 5, 0, 10 })]
    [InlineData(4, new int[] { 1, 2, 3, 4, 5, 0, 10 })]
    [InlineData(5, new int[] { 1, 0, 4, 5, 6, 0, 10 })]
    [InlineData(6, new int[] { 1, 0, 5, 6, 7, 0, 10 })]
    [InlineData(7, new int[] { 1, 0, 6, 7, 8, 9, 10 })]
    [InlineData(10, new int[] { 1, 0, 6, 7, 8, 9, 10 })]
    public void ManyPages_ShowTheEnds_AndAroundTheCurrentOne_WithAGapForTheOthers(int current, int[] expected) =>
        // 0 stands for a gap.
        Assert.Equal(expected.Select(page => page == 0 ? (int?)null : page), PageWindow.Of(current, 10));

    [Fact]
    public void NoPage_ShowsNothing() => Assert.Empty(PageWindow.Of(1, 0));
}
