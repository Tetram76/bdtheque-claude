using Bdtheque.Frontend.Api;
using Bdtheque.Frontend.Components.Controls;
using Bdtheque.Frontend.Components.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Controls;

/// <summary>
/// The error of the saving of a record, the same wherever it is entered: an entry form, a side panel, the row of a list
/// (visuel/listes/saisie-selon-l-entite.md, #411).
/// </summary>
public sealed class SaveErrorTests : BunitContext
{
    private const string ReloadHref = "/admin/authors?edit=1";

    public SaveErrorTests() => Services.AddLocalization();

    // A technical error bears the same mention wherever it occurs (fonctionnel/presentation-des-erreurs.md, #411): that of the
    // saving of a record is that of every other error box.
    [Fact]
    public void TechnicalError_BearsTheMentionOfEveryTechnicalError()
    {
        var saveError = RenderSaveError(ApiProblem.Technical(), staticForm: false);
        var errorMessage = Render<ErrorMessage>(parameters => parameters.Add(p => p.Problem, ApiProblem.Technical()));

        var label = saveError.Find("[data-error-label]").TextContent.Trim();
        Assert.Equal("Problème technique", label);
        Assert.Equal(errorMessage.Find("[data-error-label]").TextContent.Trim(), label);
    }

    // A form sent by the browser retries by sending itself again, with what was typed.
    [Fact]
    public void TechnicalError_OfAStaticForm_RetriesBySendingTheFormAgain()
    {
        var saveError = RenderSaveError(ApiProblem.Technical(), staticForm: true);

        var retry = Assert.Single(saveError.FindAll(".error-actions button"));
        Assert.Equal("submit", retry.GetAttribute("type"));
        Assert.Equal("Réessayer", retry.TextContent.Trim());
    }

    // The reload of a form sent by the browser reads the record anew at its address; what was typed stays in front of the user
    // until then, without « Voir mes changements ».
    [Theory]
    [InlineData(StatusCodes.Status409Conflict, "Conflit.")]
    [InlineData(StatusCodes.Status404NotFound, "Supprimée.")]
    public void FunctionalError_OfAStaticForm_ReloadsTheRecordByItsAddress(int status, string text)
    {
        var saveError = RenderSaveError(new ApiProblem(ProblemCategory.Functional, status, null, null), staticForm: true);

        Assert.Equal("Modifiée entre-temps", saveError.Find("[data-error-label]").TextContent.Trim());
        Assert.Equal(text, saveError.Find("[data-error-text]").TextContent.Trim());
        var reload = Assert.Single(saveError.FindAll(".error-actions a"));
        Assert.Equal(ReloadHref, reload.GetAttribute("href"));
        Assert.Equal("Recharger la fiche", reload.TextContent.Trim());
        Assert.Empty(saveError.FindAll(".error-actions button"));
    }

    // A new record has nothing to reload.
    [Fact]
    public void FunctionalError_OfANewRecord_OffersNoReload()
    {
        var saveError = RenderSaveError(
            new ApiProblem(ProblemCategory.Functional, StatusCodes.Status404NotFound, null, null), staticForm: true, canReload: false);

        Assert.Empty(saveError.FindAll(".error-actions a, .error-actions button"));
    }

    // An entry form, saved from its circuit, reloads the record or shows the changes typed since it was read.
    [Fact]
    public void FunctionalError_OfAnEntryForm_ReloadsOrShowsTheChanges()
    {
        var saveError = RenderSaveError(new ApiProblem(ProblemCategory.Functional, StatusCodes.Status409Conflict, null, null), staticForm: false);

        Assert.Equal(["Recharger la fiche", "Voir mes changements"],
            saveError.FindAll(".error-actions button").Select(button => button.TextContent.Trim()));
        Assert.Empty(saveError.FindAll(".error-actions a"));
    }

    private IRenderedComponent<SaveError> RenderSaveError(ApiProblem problem, bool staticForm, bool canReload = true) =>
        Render<SaveError>(parameters => parameters
            .Add(p => p.Problem, problem)
            .Add(p => p.Conflict, "Conflit.")
            .Add(p => p.NotFound, "Supprimée.")
            .Add(p => p.Technical, "Non enregistrée.")
            .Add(p => p.CanReload, canReload)
            .Add(p => p.StaticForm, staticForm)
            .Add(p => p.ReloadHref, staticForm ? ReloadHref : null));
}
