extern alias frontend;

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Enums;
using Bdtheque.EndToEnd.Tests.Controls;
using frontend::Bdtheque.Frontend.Navigation;
using SkiaSharp;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The list of the albums of the administration (visuel/liste-albums/contenu.md), on the application shared by the tests:
/// each test names its albums with a marker of its own, and searches it.
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class AdminAlbumsTests(ApplicationHost application) : BrowserTest<ApplicationHost>(application)
{
    private readonly string _marker = Guid.NewGuid().ToString("N")[..8];

    [Theory, InEachTheme]
    public async Task Menu_LeadsToTheList_WhoseFilDAraneLeadsBackToTheAdministration(string theme)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, AppPaths.AdminHome);

        await page.GetByRole(AriaRole.Button, new() { Name = "Menu" }).ClickAsync();
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Plan de l'administration" })
            .GetByRole(AriaRole.Link, new() { Name = "Albums" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbums));
        await Assertions.Expect(page.Locator("h1")).ToContainTextAsync("Albums");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Fil d'Ariane" }).GetByRole(AriaRole.Link, new() { Name = "Administration" }).ClickAsync();
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminHome));
    }

    [Theory, InEachTheme]
    public async Task Search_ThenTheNavigationByInitial_NarrowTheList(string theme)
    {
        await CreateAlbumAsync($"Astérix {_marker}");
        await CreateAlbumAsync($"Le Lotus bleu {_marker}");
        var page = await OpenAsync(theme, AppPaths.AdminAlbums);

        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher un album" }).FillAsync(_marker);
        await page.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher un album" }).PressAsync("Enter");

        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Astérix {_marker}", $"Le Lotus bleu {_marker}"]);
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Navigation par initiale" }).GetByRole(AriaRole.Link, new() { Name = "L", Exact = true }).ClickAsync();
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Le Lotus bleu {_marker}"]);
        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbums, $"?q={_marker}&entry=L"));
    }

    [Theory, InEachTheme]
    public async Task Filter_ChosenByTypingItsName_NarrowsTheList_AndItsTagRemovesIt(string theme)
    {
        var genre = await PostAsync<GenreForm>("/admin/genres", new CreateGenreRequest($"Genre {_marker}"));
        await CreateAlbumAsync($"Avec le genre {_marker}", [genre.Id]);
        await CreateAlbumAsync($"Sans le genre {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminAlbums}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = "Filtres" }).ClickAsync();
        await ControlsPage.WaitUntilInteractiveAsync(page);
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Genre" }).PressSequentiallyAsync($"Genre {_marker}");
        await page.GetByRole(AriaRole.Option, new() { Name = $"Genre {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Filtrer" }).ClickAsync();

        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Avec le genre {_marker}"]);
        await page.GetByRole(AriaRole.Link, new() { Name = $"Retirer « Genre : Genre {_marker} »" }).ClickAsync();
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"Avec le genre {_marker}", $"Sans le genre {_marker}"]);
    }

    [Theory, InEachTheme]
    public async Task Deletion_ConfirmedInAModalDialog_TakesTheAlbumOutOfTheList(string theme)
    {
        await CreateAlbumAsync($"À supprimer {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminAlbums}?q={_marker}");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur À supprimer {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();

        var dialog = page.GetByRole(AriaRole.Alertdialog, new() { Name = "Supprimer cet album ?" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        Assert.True(await dialog.EvaluateAsync<bool>("dialog => dialog.matches(':modal')"));
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Supprimer définitivement" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbums, $"?q={_marker}"));
        await Assertions.Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync($"Aucun album ne correspond à « {_marker} ».");
    }

    [Theory, InEachTheme]
    public async Task Deletion_Escape_ClosesTheDialogWithoutDeleting(string theme)
    {
        await CreateAlbumAsync($"À garder {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminAlbums}?q={_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur À garder {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Supprimer…" }).ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Alertdialog)).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminAlbums, $"?q={_marker}"));
        await Assertions.Expect(Labels(page)).ToHaveTextAsync([$"À garder {_marker}"]);
    }

    [Theory, InEachTheme]
    public async Task Merge_SearchesTheOtherAlbum_AndContinuesWithTheOneChosen(string theme)
    {
        var album = await CreateAlbumAsync($"Barracuda {_marker}");
        var other = await CreateAlbumAsync($"Barracuda bis {_marker}");
        var page = await OpenAsync(theme, $"{AppPaths.AdminAlbums}?q={_marker}");
        await page.GetByRole(AriaRole.Button, new() { Name = $"Autres actions sur Barracuda {_marker}" }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Fusionner avec un autre album…" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Fusionner deux albums" });

        await dialog.GetByRole(AriaRole.Searchbox, new() { Name = "Rechercher l'autre album" }).FillAsync(_marker);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Rechercher" }).ClickAsync();
        await Assertions.Expect(dialog.GetByRole(AriaRole.Radio)).ToHaveCountAsync(1);
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Continuer" }).ClickAsync();

        await Assertions.Expect(page).ToHaveURLAsync(PageAddress.Of(AppPaths.AdminMergeAlbums, $"?other={other.Id}&album={album.Id}"));
    }

    [Theory, InEachTheme]
    public async Task Cover_WhateverItsFormat_IsHuggedByTheFrameOfTheTheme(string theme)
    {
        // Narrower than the slot of a cover (3:4): the white frame of « La planche encrée » once showed in the gap.
        var album = await CreateAlbumAsync($"Couverture étroite {_marker}");
        var publisher = await PostAsync<PublisherForm>("/admin/publishers", new PublisherContent($"Éditeur {_marker}", null, []));
        var edition = await PostAsync<EditionForm>($"/admin/albums/{album.Id}/editions", new CreateEditionRequest(
            new EditionContent(publisher.Id, null, null, null, null, null, null, null, null, null, false, true, null,
                AcquisitionMode.Purchase, false, null, null, null, false, null, null, null, null),
            album.Version));
        await UploadCoverAsync(edition, 300, 420);
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = 1440, Height = 900 });
        await ApplicationHost.SignInAsync(page, $"{AppPaths.AdminAlbums}?q={_marker}");
        await PageReadiness.WaitAsync(page);

        var cover = page.Locator(".album-row img.cover");
        await Assertions.Expect(cover).ToHaveJSPropertyAsync("complete", true);
        var size = await cover.EvaluateAsync<double[]>("image => { const box = image.getBoundingClientRect(); return [box.width, box.height]; }");
        var border = await cover.EvaluateAsync<string>("image => getComputedStyle(image).borderTopWidth");

        // The frame, if the theme draws one, is the border of the image itself, whose box has the proportions of the cover: no
        // gap can show.
        var frame = 2 * double.Parse(border.Replace("px", "", StringComparison.Ordinal), System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(300.0 / 420, (size[0] - frame) / (size[1] - frame), 2);
    }

    [Theory]
    [InEachTheme(414)]
    [InEachTheme(768)]
    [InEachTheme(1024)]
    [InEachTheme(1180)]
    [InEachTheme(1440)]
    public async Task Rows_AtEveryWidth_LeaveTheLabelItsRoom_WithoutTheListOverflowing(string theme, int width)
    {
        // A label long enough to wrap: squeezed by the columns, it once stood one letter per line on a tablet.
        await CreateAlbumAsync($"L'Horloger d'Ombrelune et les veilleurs du port {_marker}");
        var page = await NewPageInThemeAsync(theme, new ViewportSize { Width = width, Height = 900 });
        await ApplicationHost.SignInAsync(page, $"{AppPaths.AdminAlbums}?q={_marker}");
        await PageReadiness.WaitAsync(page);

        var label = await page.Locator(".row-entry").EvaluateAsync<double>("label => label.getBoundingClientRect().width");
        var overflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.True(label >= 120, $"{theme} at {width}px: the label is {label}px wide.");
        Assert.False(overflow, $"{theme} at {width}px: the page overflows sideways.");
        // At the width of the mockups' desktop, the table has all its columns, the last modification included.
        if (width == 1440)
            await Assertions.Expect(page.GetByRole(AriaRole.Columnheader, new() { Name = "Modifié" })).ToBeVisibleAsync();
    }

    private async Task UploadCoverAsync(EditionForm edition, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.SteelBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        var file = new ByteArrayContent(png.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var form = new MultipartFormDataContent
        {
            { file, UploadVisualFields.File, "cover.png" },
            { new StringContent(nameof(VisualType.Cover)), UploadVisualFields.Type },
            { new StringContent(edition.AlbumVersion.ToString(CultureInfo.InvariantCulture)), UploadVisualFields.AlbumVersion },
        };
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsync($"/admin/albums/{edition.AlbumId}/editions/{edition.Id}/visuals", form);
        response.EnsureSuccessStatusCode();
    }

    private async Task<IPage> OpenAsync(string theme, string path)
    {
        var page = await NewPageAsync(theme);
        await ApplicationHost.SignInAsync(page, path);
        await PageReadiness.WaitAsync(page);
        return page;
    }

    private static ILocator Labels(IPage page) => page.GetByRole(AriaRole.Table, new() { Name = "Albums" }).Locator(".row-entry");

    private Task<AlbumForm> CreateAlbumAsync(string title, Guid[]? genreIds = null) =>
        PostAsync<AlbumForm>("/admin/albums", new AlbumContent(
            title, null, null, AlbumType.Regular, false, null, null, null, null, null, null, null, null, genreIds ?? [], [], []));

    private async Task<TForm> PostAsync<TForm>(string path, object request)
    {
        using var api = Application.CreateApiClient();
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TForm>())!;
    }
}
