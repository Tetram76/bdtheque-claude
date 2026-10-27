namespace Bdtheque.Domain.Common;

/// <summary>An owned edition that may be put in the spotlight of a day, with what the draw weighs.</summary>
/// <param name="SpotlightCount">How many days the edition was in the spotlight (modele-metier.md § Édition).</param>
/// <param name="IsRecent">Whether it was in the spotlight within the <see cref="SpotlightDraw.RepetitionWindow"/> of the day.</param>
/// <param name="HasCover">Whether one of its visuals is a cover.</param>
public sealed record SpotlightCandidate(Guid EditionId, int SpotlightCount, bool IsRecent, bool HasCover);

/// <summary>
/// The draw of the edition in the spotlight of a day (fonctionnel/page-accueil.md § Page d'accueil, À la une): it avoids
/// close repetitions without forbidding them, prefers the editions with a cover, then favours the least often
/// spotlighted.
/// </summary>
/// <remarks>
/// The random number is the caller's: the rules hold for any roll, which a test sets to check the edition each
/// one leads to.
/// </remarks>
public static class SpotlightDraw
{
    /// <summary>
    /// The days around <paramref name="day"/> on which a spotlight makes an edition recent: back in the spotlight ten
    /// days later at the earliest. Both ways, the day being that of the visitor: a time zone ahead may have drawn
    /// the next days already.
    /// </summary>
    public static (DateOnly From, DateOnly To) RepetitionWindow(DateOnly day) => (day.AddDays(-9), day.AddDays(9));

    /// <summary>Draws the edition in the spotlight among <paramref name="candidates"/>, every owned edition.</summary>
    /// <param name="roll">The random number, from 0 included to 1 excluded.</param>
    /// <exception cref="ArgumentException">No candidate: a technical error, the collection being empty.</exception>
    public static Guid Pick(IReadOnlyList<SpotlightCandidate> candidates, double roll)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(roll);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(roll, 1.0);
        if (candidates.Count == 0)
            throw new ArgumentException("An edition is drawn among the owned editions, of which there is none.", nameof(candidates));

        // Unless every edition was spotlighted within the window; then, among those kept, the editions with a cover
        // unless none has one.
        var kept = OrAll(candidates.Where(c => !c.IsRecent).ToList(), candidates);
        kept = OrAll(kept.Where(c => c.HasCover).ToList(), kept);

        // A chance in proportion to 1 / (1 + n)²: four times as many for an edition never spotlighted as for one
        // spotlighted once, nine times as many as for one spotlighted twice.
        var weights = kept.Select(c => 1.0 / ((1.0 + c.SpotlightCount) * (1.0 + c.SpotlightCount))).ToList();
        var target = roll * weights.Sum();
        var cumulated = 0.0;
        for (var i = 0; i < kept.Count - 1; i++)
        {
            cumulated += weights[i];
            if (target < cumulated)
                return kept[i].EditionId;
        }
        // The last one, whatever the rounding of the sum.
        return kept[^1].EditionId;
    }

    private static IReadOnlyList<SpotlightCandidate> OrAll(IReadOnlyList<SpotlightCandidate> some, IReadOnlyList<SpotlightCandidate> all) =>
        some.Count > 0 ? some : all;
}
