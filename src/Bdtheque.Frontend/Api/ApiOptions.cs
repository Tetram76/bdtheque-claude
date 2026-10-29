using System.ComponentModel.DataAnnotations;

namespace Bdtheque.Frontend.Api;

/// <summary>Where <c>api</c> is reached, over the internal Docker network. Supplied via the <c>Api__BaseUrl</c> environment variable.</summary>
public sealed class ApiOptions
{
    public const string SectionName = "Api";

    [Required(AllowEmptyStrings = false), Url]
    public string BaseUrl { get; set; } = string.Empty;
}
