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

    /// <summary>
    /// The screen a function belongs to, by its name: an action is named after its screen, followed by what it does there
    /// (« Liste des albums · recherche »), a screen by itself alone. An action that no screen has, done from any page
    /// (« Administration · déconnexion »), gives its own group.
    /// </summary>
    public static string ScreenOf(string name) => name.Split(Separator, 2)[0];

    private const string Separator = " · ";

    /// <summary>Whether a function has no name: what the tests check of every function followed.</summary>
    public bool ResourceNotFound(AudienceFunction function) => texts[function.Key].ResourceNotFound;
}
