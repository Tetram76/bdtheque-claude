namespace Bdtheque.Domain.Tests;

public sealed class RgbColorTests
{
    [Fact]
    public void FromComponents_WritesItsHexNotation()
    {
        var color = new RgbColor(0x2F, 0x5B, 0xA8);

        Assert.Equal("#2F5BA8", color.ToString());
        Assert.Equal(0x2F5BA8, color.Value);
    }

    [Fact]
    public void FromValue_KeepsTheComponents()
    {
        var color = RgbColor.FromValue(0xD8342B);

        Assert.Equal((0xD8, 0x34, 0x2B), (color.R, color.G, color.B));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0x1000000)]
    public void FromValue_BeyondTwentyFourBits_Throws(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RgbColor.FromValue(value));
    }
}
