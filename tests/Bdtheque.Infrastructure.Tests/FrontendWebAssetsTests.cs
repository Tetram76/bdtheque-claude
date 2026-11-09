using System.Xml.Linq;

namespace Bdtheque.Infrastructure.Tests;

/// <summary>
/// The script of Blazor (<c>_framework/blazor.web.js</c>) in the image of <c>frontend</c>, without which no interactive component
/// works: its package is restored by the SDK only when the project requires the web assets of ASP.NET Core, which it infers
/// from the <c>.razor</c> files present when the project is evaluated. The Dockerfile restores the project from its
/// <c>.csproj</c> alone, before copying its sources (choix-implementation/deploiement/images-de-base-des-conteneurs.md): the
/// project declares it, rather than leaving it to the files present.
/// </summary>
[Trait("Category", RepositoryFiles.Category)]
public sealed class FrontendWebAssetsTests
{
    [Fact]
    public void Frontend_RequiresTheWebAssetsOfAspNetCore_WhateverTheFilesPresentAtRestore()
    {
        var project = XDocument.Load(Path.Combine(RepositoryFiles.Root, "src", "Bdtheque.Frontend", "Bdtheque.Frontend.csproj"));

        Assert.Equal("true", project.Descendants("RequiresAspNetWebAssets").SingleOrDefault()?.Value);
    }
}
