using System.Globalization;
using System.Text;
using Bdtheque.Contracts.Admin;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using ContractEnums = Bdtheque.Contracts.Enums;

namespace Bdtheque.Api.Visuals;

/// <summary>The fields of an upload form (<see cref="UploadVisualFields"/>), once read.</summary>
internal sealed record VisualUpload(byte[] File, ContractEnums.VisualType Type, uint AlbumVersion);

/// <summary>
/// Reads an upload form as a stream (choix-implementation.md § Visuels : stockage et traitement): the
/// file is read into memory up to the maximum weight, and refused as soon as it exceeds it. Nothing is
/// buffered beforehand, as the framework's form binding would (temporary file of the whole request).
/// </summary>
internal static class VisualUploadReader
{
    // The text fields are a few characters long: anything longer is no request the frontend builds.
    private const int MaxFieldLength = 64;

    // Room for the boundaries, headers and text fields of the form around the file.
    private const long FormAllowance = 64 * 1024;

    /// <exception cref="Domain.Common.DomainRuleViolationException">The file weighs more than <paramref name="maxFileBytes"/>.</exception>
    /// <exception cref="BadHttpRequestException">The request is not a complete upload form: a technical error.</exception>
    public static async Task<VisualUpload> ReadAsync(HttpRequest request, long maxFileBytes, CancellationToken cancellationToken)
    {
        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType)
            || !string.Equals(contentType.MediaType.Value, "multipart/form-data", StringComparison.OrdinalIgnoreCase)
            || HeaderUtilities.RemoveQuotes(contentType.Boundary).Value is not { Length: > 0 } boundary)
            throw new BadHttpRequestException("A visual is uploaded as a multipart form.", StatusCodes.Status415UnsupportedMediaType);

        // A form heavier than any valid one carries a file too heavy: refused for that, without reading
        // anything — the server would otherwise refuse it first, as a technical error (413).
        var maxFormBytes = maxFileBytes + FormAllowance;
        if (request.ContentLength > maxFormBytes)
            throw VisualImage.FileTooLarge(maxFileBytes);
        // The transport stays bounded (a form sent without its length), above any valid form: the
        // reading below refuses the file for its weight first.
        if (request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } bodyLimit)
            bodyLimit.MaxRequestBodySize = maxFormBytes;

        byte[]? file = null;
        string? type = null, albumVersion = null;
        var reader = new MultipartReader(boundary, request.Body);
        while (await reader.ReadNextSectionAsync(cancellationToken) is { } section)
        {
            var name = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition)
                ? HeaderUtilities.RemoveQuotes(disposition.Name).Value
                : null;
            switch (name)
            {
                case UploadVisualFields.File:
                    file = await ReadBoundedAsync(section.Body, maxFileBytes, cancellationToken)
                           ?? throw VisualImage.FileTooLarge(maxFileBytes);
                    break;
                case UploadVisualFields.Type:
                    type = await ReadFieldAsync(section.Body, cancellationToken);
                    break;
                case UploadVisualFields.AlbumVersion:
                    albumVersion = await ReadFieldAsync(section.Body, cancellationToken);
                    break;
            }
        }

        if (file is null || type is null || albumVersion is null)
            throw new BadHttpRequestException("The upload form misses its file, the type of the visual or the version of the album.");
        if (!Enum.TryParse<ContractEnums.VisualType>(type, out var visualType))
            throw new BadHttpRequestException($"'{type}' is not a type of visual.");
        if (!uint.TryParse(albumVersion, NumberStyles.None, CultureInfo.InvariantCulture, out var version))
            throw new BadHttpRequestException($"'{albumVersion}' is not a version.");

        return new VisualUpload(file, visualType, version);
    }

    private static async Task<string> ReadFieldAsync(Stream body, CancellationToken cancellationToken) =>
        Encoding.UTF8.GetString(
            await ReadBoundedAsync(body, MaxFieldLength, cancellationToken)
            ?? throw new BadHttpRequestException("A field of the upload form is too long."));

    /// <summary>Reads <paramref name="body"/> in full, or returns <c>null</c> as soon as it exceeds <paramref name="maxBytes"/>.</summary>
    private static async Task<byte[]?> ReadBoundedAsync(Stream body, long maxBytes, CancellationToken cancellationToken)
    {
        using var content = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (content.Length + read > maxBytes)
                return null;
            content.Write(chunk, 0, read);
        }

        return content.ToArray();
    }
}
