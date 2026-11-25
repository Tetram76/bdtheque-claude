using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// A title or a name cited in a message of the application, between the quotation marks of the display culture
/// (<c>Citation.resx</c>: « », separated from it by no-break spaces, so that a line never breaks between them,
/// fonctionnel/presentation/noms-cites-dans-un-message.md): « « Gotlib » sera crédité… ». A title or a name shown alone, as a
/// value, is not cited.
/// </summary>
public sealed class Citation(IStringLocalizer<Citation> texts)
{
    public string Of(string name) => texts["Cited", name].Value;
}
