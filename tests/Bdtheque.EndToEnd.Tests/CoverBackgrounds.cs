using System.Text.RegularExpressions;

namespace Bdtheque.EndToEnd.Tests;

/// <summary>
/// The background images a theme paints a cover without image with (--cover-generic, --cover-blank, themes/base.css), as the
/// browser computes them: what tells the generic visual and the blank card apart from a frame that wiped them out.
/// </summary>
internal static partial class CoverBackgrounds
{
    /// <summary>The grey diagonal stripes of the generic visual of an edition without cover.</summary>
    [GeneratedRegex("^repeating-linear-gradient")]
    public static partial Regex GenericVisual();

    /// <summary>The plain grey card of a place of a pile without volume.</summary>
    [GeneratedRegex("^linear-gradient")]
    public static partial Regex BlankCard();
}
