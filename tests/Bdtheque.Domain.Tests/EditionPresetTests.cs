using Bdtheque.Domain.Entities;
using Bdtheque.Domain.Enums;

namespace Bdtheque.Domain.Tests;

/// <summary>
/// Values a new edition's form starts from (fonctionnel.md § Initialisation d'une nouvelle édition
/// depuis la série).
/// </summary>
public sealed class EditionPresetTests
{
    [Fact]
    public void For_AlbumWithoutSeries_ProposesTheDefaultValues()
    {
        // modele-metier.md § Édition: the values proposed on entry; no publisher nor category is.
        var preset = EditionPreset.For(new Album("Le Lotus bleu", null));

        Assert.Equal(
            new EditionPreset(
                null, null, null, EditionCondition.Excellent, BindingType.Hardcover, BookOrientation.Portrait,
                ReadingDirection.LeftToRight, EditionFormat.Standard, IsColor: true),
            preset);
    }

    [Fact]
    public void For_AlbumInASeries_TakesTheTemplateOfTheSeries()
    {
        var publisher = new Publisher("Dupuis");
        var collection = publisher.AddCollection("Repérages");
        var series = new Series("Spirou et Fantasio");
        series.SetTemplate(publisher, collection);
        series.SetTemplateEditionCategory(EditionCategory.SpecialEdition);
        series.SetTemplateCondition(EditionCondition.Good);
        series.SetTemplateBinding(BindingType.Paperback);
        series.SetTemplateOrientation(BookOrientation.Landscape);
        series.SetTemplateReadingDirection(ReadingDirection.RightToLeft);
        series.SetTemplateFormat(EditionFormat.Large);
        series.SetTemplateIsColor(false);

        var preset = EditionPreset.For(new Album(null, series));

        Assert.Equal(
            new EditionPreset(
                publisher.Id, collection.Id, EditionCategory.SpecialEdition, EditionCondition.Good, BindingType.Paperback,
                BookOrientation.Landscape, ReadingDirection.RightToLeft, EditionFormat.Large, IsColor: false),
            preset);
    }

    [Fact]
    public void For_AlbumInASeriesWithAnEmptyTemplate_ProposesNothing()
    {
        // The template of the series prevails: a field it leaves empty is not pre-filled, not even with
        // the default value. Colour, a yes/no field, keeps the default of the edition.
        var preset = EditionPreset.For(new Album(null, new Series("Spirou et Fantasio")));

        Assert.Equal(new EditionPreset(null, null, null, null, null, null, null, null, IsColor: true), preset);
    }
}
