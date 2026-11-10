namespace Bdtheque.ValidationData.Tests;

/// <summary>
/// The arguments of the program that loads the validation data, passed by scripts/Start-Validation.ps1
/// (choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md).
/// </summary>
public sealed class CommandLineTests
{
    [Fact]
    public void WithoutArgument_ThePaleCoverIsNotPutInTheSpotlight() =>
        Assert.False(ValidationDataLoader.PaleCoverInTheSpotlight([]));

    [Fact]
    public void WithItsArgument_ThePaleCoverIsPutInTheSpotlight() =>
        Assert.True(ValidationDataLoader.PaleCoverInTheSpotlight([ValidationDataLoader.PaleCoverSpotlightArgument]));

    // A mistyped or unknown argument stops the start rather than loading data other than those asked for.
    [Theory]
    [InlineData("--pale-cover")]
    [InlineData("--pale-cover-spotlight", "--pale-cover-spotlight")]
    public void AnyOtherArgument_IsRefused(params string[] arguments) =>
        Assert.Throws<ArgumentException>(() => ValidationDataLoader.PaleCoverInTheSpotlight(arguments));
}
