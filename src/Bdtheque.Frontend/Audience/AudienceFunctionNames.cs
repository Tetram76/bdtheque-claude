using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Audience;

/// <summary>
/// The names of the functions the audience measurement follows, as the administration presents their indicators, in the display
/// culture (<c>AudienceFunctionNames.resx</c>, under the key of each function): a screen by what it presents, an action by its
/// screen and what it does there.
/// </summary>
public sealed class AudienceFunctionNames(IStringLocalizer<AudienceFunctionNames> texts)
{
    /// <summary>
    /// The name of a function by its key; a function the frontend no longer follows, whose usages the API still counts, keeps
    /// its key: its key is never given to another function.
    /// </summary>
    public string Of(string key) => texts[key] is { ResourceNotFound: false } name ? name.Value : key;

    /// <summary>Whether a function has no name: what the tests check of every function followed.</summary>
    public bool ResourceNotFound(AudienceFunction function) => texts[function.Key].ResourceNotFound;
}
