using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>The setting of the application (modele-metier.md § Paramétrage de l'application).</summary>
public sealed class ApplicationSettingsTests
{
    [Fact]
    public void Defaults_AreTheDefaultFormatAndTheOnlyCulture()
    {
        var settings = new ApplicationSettings();

        Assert.Equal(AlbumLabelFormat.TitleSeriesVolume, settings.AlbumLabelFormat);
        Assert.Equal(DisplayCulture.FrenchFrance, settings.DisplayCulture);
        Assert.Equal(ApplicationSettings.SingletonId, settings.Id);
    }

    [Fact]
    public void Set_KeepsTheValues()
    {
        var settings = new ApplicationSettings();

        settings.Set(AlbumLabelFormat.VolumeTitleSeries, DisplayCulture.FrenchFrance);

        Assert.Equal(AlbumLabelFormat.VolumeTitleSeries, settings.AlbumLabelFormat);
        Assert.Equal(DisplayCulture.FrenchFrance, settings.DisplayCulture);
    }

    [Fact]
    public void Set_UndefinedFormat_Throws()
    {
        var settings = new ApplicationSettings();

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Set((AlbumLabelFormat)99, DisplayCulture.FrenchFrance));
        Assert.Equal(AlbumLabelFormat.TitleSeriesVolume, settings.AlbumLabelFormat);
    }

    [Fact]
    public void Set_UndefinedCulture_Throws()
    {
        var settings = new ApplicationSettings();

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.Set(AlbumLabelFormat.VolumeTitleSeries, (DisplayCulture)99));
        Assert.Equal(AlbumLabelFormat.TitleSeriesVolume, settings.AlbumLabelFormat);
    }
}
