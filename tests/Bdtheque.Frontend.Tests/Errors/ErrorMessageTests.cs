using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Errors;

public sealed class ErrorMessageTests : BunitContext
{
    public ErrorMessageTests() => Services.AddLocalization();

    [Theory]
    [InlineData(ProblemCategory.Business, "business")]
    [InlineData(ProblemCategory.Functional, "functional")]
    [InlineData(ProblemCategory.Technical, "technical")]
    public void EachCategory_IsTellableApartAtFirstGlance(ProblemCategory category, string expected)
    {
        var message = Render<ErrorMessage>(parameters => parameters.Add(p => p.Problem, new ApiProblem(category, null, null, null)));

        var alert = message.Find("[role=alert]");
        Assert.Equal(expected, alert.GetAttribute("data-error-category"));
        Assert.False(string.IsNullOrWhiteSpace(alert.QuerySelector("[data-error-label]")?.TextContent));
    }

    // Each with its own icon besides its name (visuel/saisie-album/contenu.md): to correct, to reload, the application's.
    [Theory]
    [InlineData(ProblemCategory.Business, "M12 7v6M12 17h.01")]
    [InlineData(ProblemCategory.Functional, "M4 12a8 8 0 0 1 14-5.3M20 4v4h-4M20 12a8 8 0 0 1-14 5.3M4 20v-4h4")]
    [InlineData(ProblemCategory.Technical, "M14.7 6.3a4 4 0 0 0-5.4 5.4L4 17v3h3l5.3-5.3a4 4 0 0 0 5.4-5.4l-2.5 2.5-2.4-.6-.6-2.4z")]
    public void EachCategory_HasItsOwnIcon(ProblemCategory category, string path)
    {
        var message = Render<ErrorMessage>(parameters => parameters.Add(p => p.Problem, new ApiProblem(category, null, null, null)));

        Assert.Equal(path, message.Find(".error-icon path").GetAttribute("d"));
    }

    [Fact]
    public void ErrorFoundByTheFrontend_IsNamedAndWordedByIt()
    {
        var message = Render<ErrorMessage>(parameters => parameters
            .Add(p => p.Problem, new ApiProblem(ProblemCategory.Functional, null, null, null))
            .Add(p => p.Label, "Connexion suspendue")
            .Add(p => p.Message, "Réessayez dans quelques minutes."));

        Assert.Equal("Connexion suspendue", message.Find("[data-error-label]").TextContent.Trim());
        Assert.Equal("Réessayez dans quelques minutes.", message.Find("[data-error-text]").TextContent.Trim());
        Assert.Equal("functional", message.Find("[role=alert]").GetAttribute("data-error-category"));
    }

    [Fact]
    public void BusinessError_ShowsTheTextOfItsRule_NotItsCode()
    {
        var text = TextOf(new ApiProblem(ProblemCategory.Business, 422, "Genre.LabelAlreadyUsed", null));

        Assert.DoesNotContain("Genre.LabelAlreadyUsed", text);
        Assert.NotEqual(TextOf(new ApiProblem(ProblemCategory.Business, 422, null, null)), text);
    }

    [Fact]
    public void BusinessError_OfAnUnknownRule_ShowsAGenericText()
    {
        var text = TextOf(new ApiProblem(ProblemCategory.Business, 422, "No.SuchRule", null));

        Assert.DoesNotContain("No.SuchRule", text);
        Assert.Equal(TextOf(new ApiProblem(ProblemCategory.Business, 422, null, null)), text);
    }

    [Fact]
    public void FunctionalError_TellsAModificationFromADeletionInTheMeantime()
    {
        var modified = TextOf(new ApiProblem(ProblemCategory.Functional, 409, null, null));
        var deleted = TextOf(new ApiProblem(ProblemCategory.Functional, 404, null, null));

        Assert.NotEqual(modified, deleted);
    }

    [Fact]
    public void RefusedDeletion_ListsTheRecordsThatBlockIt_ByEntityType()
    {
        var impact = new DeletionImpact([new ImpactCount(EntityKind.Album, 12), new ImpactCount(EntityKind.Series, 1)], [], [], [], "x");

        var message = Render<ErrorMessage>(parameters => parameters.Add(p => p.Problem,
            new ApiProblem(ProblemCategory.Business, 422, "Deletion.BlockedByReferences", impact)));

        var items = message.FindAll("[data-error-impact] li").Select(item => item.TextContent.Trim()).ToList();
        Assert.Equal(["12 albums", "1 série"], items);
    }

    [Fact]
    public void TechnicalError_DisclosesNoDetail()
    {
        var message = Render<ErrorMessage>(parameters => parameters.Add(p => p.Problem,
            new ApiProblem(ProblemCategory.Technical, 500, "Leaked.Code", null)));

        Assert.DoesNotContain("Leaked.Code", message.Markup);
        Assert.DoesNotContain("500", message.Markup);
    }

    // An error presented in a control (a list of suggestions, the line of an upload, under a field): the same category, icon
    // and frame, at the scale of the control (choix-implementation/socle/frontend.md, Présentation, #438).
    [Fact]
    public void CompactError_KeepsItsCategory_AtTheScaleOfAControl()
    {
        var message = Render<ErrorMessage>(parameters => parameters
            .Add(p => p.Problem, ApiProblem.Technical())
            .Add(p => p.Compact, true));

        var alert = message.Find("[role=alert]");
        Assert.True(alert.HasAttribute("data-error-compact"));
        Assert.Equal("technical", alert.GetAttribute("data-error-category"));
        Assert.Equal("Erreur technique", alert.QuerySelector("[data-error-label]")!.TextContent.Trim());
        Assert.NotNull(alert.QuerySelector(".error-icon path"));
    }

    [Fact]
    public void Error_IsFullSized_UnlessCompact()
    {
        var message = Render<ErrorMessage>(parameters => parameters.Add(p => p.Problem, ApiProblem.Technical()));

        Assert.False(message.Find("[role=alert]").HasAttribute("data-error-compact"));
    }

    // The generic text of a technical error, said for the action of a control that failed (e.g. its search); an error of
    // another category keeps its own text, which says what to do about it.
    [Theory]
    [InlineData(ProblemCategory.Technical, "La recherche n'a pas abouti.")]
    [InlineData(ProblemCategory.Functional, "Cette fiche n'existe plus : elle a été supprimée entre-temps.")]
    public void TechnicalText_WordsOnlyATechnicalError(ProblemCategory category, string expected)
    {
        var message = Render<ErrorMessage>(parameters => parameters
            .Add(p => p.Problem, new ApiProblem(category, 404, null, null))
            .Add(p => p.TechnicalText, "La recherche n'a pas abouti."));

        Assert.Equal(expected, message.Find("[data-error-text]").TextContent.Trim());
    }

    private string TextOf(ApiProblem problem) =>
        Render<ErrorMessage>(parameters => parameters.Add(p => p.Problem, problem)).Find("[data-error-text]").TextContent.Trim();
}
