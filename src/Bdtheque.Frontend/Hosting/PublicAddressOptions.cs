namespace Bdtheque.Frontend.Hosting;

/// <summary>
/// Address under which the browser reaches the application behind the reverse proxy of the NAS, which terminates TLS
/// and relays in HTTP (choix-implementation/deploiement.md § Frontend derrière le reverse proxy du NAS). Supplied via
/// the <c>PublicAddress__Url</c> environment variable; absent for a deployment reached directly in HTTP.
/// </summary>
public sealed class PublicAddressOptions
{
    public const string SectionName = "PublicAddress";

    public string? Url { get; set; }

    /// <summary>
    /// The scheme and host of <see cref="Url"/>, port included, or <see langword="null"/> without public address.
    /// </summary>
    /// <remarks>
    /// The application is served at the root: a path would be lost by every link the application builds from the root.
    /// </remarks>
    public Uri? ParsedUrl => string.IsNullOrWhiteSpace(Url) ? null : Parse(Url);

    public bool IsValid() => string.IsNullOrWhiteSpace(Url) || Parse(Url) is not null;

    private static Uri? Parse(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.UserInfo.Length == 0
        && uri.AbsolutePath == "/"
        && uri.Query.Length == 0
        && uri.Fragment.Length == 0
            ? uri
            : null;
}
