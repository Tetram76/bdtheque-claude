using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using SkiaSharp;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The media of the visuals, which the browser obtains from <c>frontend</c> only: <c>frontend</c> relays those of
/// <c>api</c>, which alone reads the volume (choix-implementation/domaine.md § Visuels : stockage et traitement).
/// </summary>
[Collection(ApplicationCollection.Name)]
public sealed class VisualMediaRelayTests(ApplicationHost application)
{
    [Fact]
    public async Task Media_IsRelayedFromTheApi_WithItsCacheHeaders()
    {
        var visual = await UploadVisualAsync();
        using var api = application.CreateApiClient();
        var expected = await api.GetByteArrayAsync(VisualMediaRoutes.Original(visual));

        using var browser = Browser();
        using var original = await browser.GetAsync(VisualMediaRoutes.Original(visual));
        using var display = await browser.GetAsync(VisualMediaRoutes.Display(visual));

        Assert.Equal(HttpStatusCode.OK, original.StatusCode);
        Assert.Equal(expected, await original.Content.ReadAsByteArrayAsync());
        Assert.Equal("image/png", original.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, display.StatusCode);
        Assert.Equal("image/webp", display.Content.Headers.ContentType?.MediaType);
        Assert.True(display.Headers.CacheControl is { Public: true, MaxAge: not null }
                    && display.Headers.CacheControl.Extensions.Any(extension => extension.Name == "immutable"));
    }

    [Fact]
    public async Task MediaOfAnUnknownVisual_IsNotFound()
    {
        using var browser = Browser();

        using var response = await browser.GetAsync(VisualMediaRoutes.Display(Guid.CreateVersion7()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // What a browser sends: no internal key, which frontend adds on its own.
    private HttpClient Browser() => new() { BaseAddress = application.BaseAddress };

    private async Task<Guid> UploadVisualAsync()
    {
        using var api = application.CreateApiClient();
        var publisher = await PostAsync<CreatePublisherRequest, PublisherForm>(api, "/admin/publishers",
            new($"Éditeur {Guid.NewGuid():N}", null));
        var album = await PostAsync<AlbumContent, AlbumForm>(api, "/admin/albums",
            new("Album relayé", null, null, AlbumType.Regular, false, null, null, null, 2020, null, null, null, null, [], [], []));
        var edition = await PostAsync<CreateEditionRequest, EditionForm>(api, $"/admin/albums/{album.Id}/editions", new(
            new(publisher.Id, null, 2020, null, null, null, null, null, null, null, false, true, null, AcquisitionMode.Purchase,
                false, null, null, null, false, null, null, null, null),
            album.Version));

        var file = new ByteArrayContent(Png());
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        using var form = new MultipartFormDataContent
        {
            { file, UploadVisualFields.File, "cover.png" },
            { new StringContent(nameof(VisualType.Cover)), UploadVisualFields.Type },
            { new StringContent(edition.AlbumVersion.ToString(CultureInfo.InvariantCulture)), UploadVisualFields.AlbumVersion },
        };
        using var response = await api.PostAsync($"/admin/albums/{album.Id}/editions/{edition.Id}/visuals", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EditionVisualsForm>())!.Visuals.Single().Id;
    }

    private static async Task<TResponse> PostAsync<TRequest, TResponse>(HttpClient api, string path, TRequest request)
    {
        using var response = await api.PostAsJsonAsync(path, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    private static byte[] Png()
    {
        using var bitmap = new SKBitmap(30, 40);
        bitmap.Erase(SKColors.SteelBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
