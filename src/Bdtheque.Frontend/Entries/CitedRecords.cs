using Bdtheque.Contracts.Admin;
using Bdtheque.Contracts.Catalog;
using Bdtheque.Contracts.Enums;
using Bdtheque.Frontend.Components.Controls;

namespace Bdtheque.Frontend.Entries;

/// <summary>An author credited with a role on a record being entered.</summary>
public sealed record Credit(ContributionRole Role, RecordOption Author);

/// <summary>
/// The authors, genres and universes a record being entered cites (an album, a series: visuel/saisie-album/contenu.md,
/// visuel/saisie-serie/contenu.md), named for the form to present them, and those it added on the spot, created only with it
/// (choix-implementation/socle/frontend.md). Part of the entry persisted from the prerendering into the interactive circuit.
/// </summary>
public sealed class CitedRecords
{
    public List<RecordOption> Genres { get; set; } = [];

    /// <summary>Each named with its ancestors (« Univers Hergé › Moulinsart »).</summary>
    public List<RecordOption> Universes { get; set; } = [];

    public List<Credit> Credits { get; set; } = [];

    /// <summary>The authors added on the spot, referenced by their key in the credits.</summary>
    public List<NewAuthor> NewAuthors { get; set; } = [];

    /// <summary>The genres added on the spot, referenced by their key.</summary>
    public List<NewGenre> NewGenres { get; set; } = [];

    /// <summary>The universes added on the spot, referenced by their key.</summary>
    public List<NewUniverse> NewUniverses { get; set; } = [];

    /// <summary>The records a record cites, as the API reads them for its form, the authors of its contributions named.</summary>
    public static CitedRecords Of(
        IEnumerable<ContributionContent> contributions, IReadOnlyList<AuthorListItem> authors, IEnumerable<GenreListItem> genres,
        IEnumerable<UniverseInHierarchy> universes)
    {
        var named = authors.ToDictionary(a => a.Id);
        return new CitedRecords
        {
            Genres = [.. genres.Select(g => new RecordOption(g.Id, g.Label, null))],
            Universes = [.. universes.Select(Universe)],
            Credits = [.. contributions.Select(c => new Credit(c.Role, Author(named[c.AuthorId])))],
        };
    }

    public List<Guid> GenreIds() => [.. Genres.Select(g => g.Id)];

    public List<Guid> UniverseIds() => [.. Universes.Select(u => u.Id)];

    public List<ContributionContent> Contributions() => [.. Credits.Select(c => new ContributionContent(c.Author.Id, c.Role))];

    /// <summary>The records added on the spot that the record still cites, to create with it; <c>null</c> for none.</summary>
    public NewRecords? Added()
    {
        var added = new NewRecords(
            [.. NewAuthors.Where(a => Credits.Any(c => c.Author.Id == a.Key))],
            [.. NewGenres.Where(g => Genres.Any(o => o.Id == g.Key))],
            [.. NewUniverses.Where(u => Universes.Any(o => o.Id == u.Key))]);
        return added.Authors.Count + added.Genres.Count + added.Universes.Count == 0 ? null : added;
    }

    public IReadOnlyList<RecordOption> AuthorsOf(ContributionRole role) => [.. Credits.Where(c => c.Role == role).Select(c => c.Author)];

    /// <summary>Credits the author with the role, once.</summary>
    public void Credit(ContributionRole role, RecordOption author)
    {
        if (!Credits.Any(c => c.Role == role && c.Author.Id == author.Id))
            Credits.Add(new Credit(role, author));
    }

    public void Uncredit(ContributionRole role, RecordOption author) => Credits.RemoveAll(c => c.Role == role && c.Author.Id == author.Id);

    /// <summary>Credits the record with the authors a series gives it, in place of its own.</summary>
    public void CreditOnly(IEnumerable<SeriesAlbumContribution> contributions) =>
        Credits = [.. contributions.Select(c => new Credit(c.Role, Author(c.Author)))];

    public void AddGenre(RecordOption genre)
    {
        if (Genres.All(g => g.Id != genre.Id))
            Genres.Add(genre);
    }

    /// <summary>Adds the universe, once: whether it was not cited yet.</summary>
    public bool AddUniverse(RecordOption universe)
    {
        if (Universes.Any(u => u.Id == universe.Id))
            return false;
        Universes.Add(universe);
        return true;
    }

    /// <summary>Names the universe anew (with its ancestors, once read), if the record still cites it.</summary>
    public void Rename(RecordOption universe)
    {
        var index = Universes.FindIndex(u => u.Id == universe.Id);
        if (index >= 0)
            Universes[index] = universe;
    }

    /// <summary>The roles whose authors differ from those of the record as read.</summary>
    public IEnumerable<ContributionRole> RolesChangedSince(CitedRecords read) =>
        Enum.GetValues<ContributionRole>().Where(role => !Same(read.AuthorsOf(role), AuthorsOf(role)));

    public bool GenresChangedSince(CitedRecords read) => !Same(read.Genres, Genres);

    public bool UniversesChangedSince(CitedRecords read) => !Same(read.Universes, Universes);

    public static RecordOption Author(AuthorListItem author) =>
        new(author.Id, RecordSearch.AuthorName(author.LastName, author.FirstName, author.Pseudonym), null);

    /// <summary>A universe named with its ancestors, from the top of the hierarchy (visuel/saisie-album/contenu.md).</summary>
    public static RecordOption Universe(UniverseInHierarchy universe) =>
        new(universe.Universe.Id, string.Join(" › ", universe.Ancestors.Append(universe.Universe).Select(u => u.Name)), null);

    private static bool Same(IEnumerable<RecordOption> a, IEnumerable<RecordOption> b) =>
        a.Select(r => r.Id).Order().SequenceEqual(b.Select(r => r.Id).Order());
}
