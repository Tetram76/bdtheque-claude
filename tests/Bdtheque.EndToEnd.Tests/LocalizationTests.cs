using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The display culture is a setting of the application, French of France the only one offered
/// (fonctionnel/presentation.md § Langue et culture d'affichage): never the language the browser asks for.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class LocalizationTests(ApplicationHost application)
{
    [Fact]
    public void EveryRequest_IsServedInFrenchOfFrance_WhateverTheBrowserAsks()
    {
        var options = application.FrontendServices.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;

        Assert.Equal("fr-FR", options.DefaultRequestCulture.Culture.Name);
        Assert.Equal("fr-FR", options.DefaultRequestCulture.UICulture.Name);
        Assert.Empty(options.RequestCultureProviders);
    }

    [Fact]
    public async Task Page_AskedInEnglish_IsInFrench()
    {
        var context = await application.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = application.BaseAddress.ToString(),
            Locale = "en-US",
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync("/no-such-page");

        await Assertions.Expect(page.Locator("main h1")).ToHaveTextAsync("Page introuvable");
    }
}
