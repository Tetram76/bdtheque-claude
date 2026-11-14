using Bdtheque.Frontend.Components.Layout;
using Microsoft.Extensions.DependencyInjection;

namespace Bdtheque.Frontend.Tests.Layout;

/// <summary>
/// The dialog of the reconnection of an interactive page, each of whose states the script of Blazor shows by a class:
/// the failure of the reconnection, and that of the resumption of the session, are technical errors, presented by the
/// single component of the errors (choix-implementation/socle/frontend.md, Présentation); waiting, or a session paused by
/// the server, is none.
/// </summary>
public sealed class ReconnectModalTests : BunitContext
{
    public ReconnectModalTests() => Services.AddLocalization();

    [Theory]
    [InlineData(".components-reconnect-failed-visible", "#components-reconnect-button")]
    [InlineData(".components-resume-failed-visible", "[data-reconnect-resume]")]
    public void Failure_IsATechnicalError_WithItsAction(string state, string action)
    {
        var modal = Render<ReconnectModal>();

        var alert = modal.Find($"{state} [role=alert]");
        Assert.Equal("technical", alert.GetAttribute("data-error-category"));
        Assert.NotNull(alert.QuerySelector($".error-actions {action}"));
    }

    [Theory]
    [InlineData(".components-reconnect-first-attempt-visible")]
    [InlineData(".components-reconnect-repeated-attempt-visible")]
    [InlineData(".components-pause-visible")]
    public void WaitingOrPause_IsNoError(string state)
    {
        var modal = Render<ReconnectModal>();

        Assert.NotEmpty(modal.FindAll(state));
        Assert.All(modal.FindAll(state), element => Assert.Null(element.QuerySelector("[role=alert]")));
    }

    // Paused by the server, the session is resumed by the same action as after a failed resumption.
    [Fact]
    public void Pause_OffersToResume()
    {
        var modal = Render<ReconnectModal>();

        Assert.NotNull(modal.Find(".components-pause-visible [data-reconnect-resume]"));
    }
}
