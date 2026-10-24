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

    private string TextOf(ApiProblem problem) =>
        Render<ErrorMessage>(parameters => parameters.Add(p => p.Problem, problem)).Find("[data-error-text]").TextContent.Trim();
}
