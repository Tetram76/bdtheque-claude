using Bdtheque.Domain.Common;

namespace Bdtheque.Domain.Entities;

/// <summary>
/// The three colours drawn from a visual (visuel/dashboard/la-couverture-du-jour.md), which the theme « La couverture
/// du jour » paints a page with when the visual is the cover of the edition in the spotlight, and the spine of an
/// album with when it is its cover. Technical data of the visual (modele-metier/visuel-d-edition.md), computed once
/// from its image.
/// </summary>
/// <param name="Dominant">The colour that most stands out on the image.</param>
/// <param name="Deep">A deep version of <paramref name="Dominant"/>, dark enough to carry white text.</param>
/// <param name="Accent">A colour of the image that sets itself apart from <paramref name="Dominant"/>.</param>
public sealed record VisualPalette(RgbColor Dominant, RgbColor Deep, RgbColor Accent);
