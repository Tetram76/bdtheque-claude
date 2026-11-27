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

    /// <summary>
    /// The records added on the spot that the record still cites, to create with it, with those it cites otherwise (the series
    /// of an album, the publisher of a series and its collection, added on the spot and still chosen); <c>null</c> for none.
    /// </summary>
    public NewRecords? Added(
        IReadOnlyList<NewSeries>? series = null, IReadOnlyList<NewPublisher>? publishers = null,
        IReadOnlyList<NewPublisherCollection>? collections = null)
    {
        var added = new NewRecords(
            [.. NewAuthors.Where(a => Credits.Any(c => c.Author.Id == a.Key))],
            [.. NewGenres.Where(g => Genres.Any(o => o.Id == g.Key))],
            [.. NewUniverses.Where(u => Universes.Any(o => o.Id == u.Key))],
            series ?? [],
            publishers ?? [],
            collections ?? []);
        return added.IsEmpty() ? null : added;
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

    /// <summary>The key of the authors of a role among the data of an entry form (<see cref="EntryDatum{TEntry}"/>).</summary>
    public static string RoleKey(ContributionRole role) => $"role-{role}";

    public const string GenresKey = "genres";

    public const string UniversesKey = "universes";

    /// <summary>
    /// The data of the authors of an entry form, a line per role in the order of the form, each compared on the authors it
    /// credits, whatever their order.
    /// </summary>
    public static IEnumerable<EntryDatum<TEntry>> AuthorData<TEntry>(Func<TEntry, CitedRecords> cited, Func<ContributionRole, string> roleLabel) =>
        Enum.GetValues<ContributionRole>().Select(role => new EntryDatum<TEntry>(
            RoleKey(role), roleLabel(role), e => Identities(cited(e).AuthorsOf(role)), e => EntryChange.Names(cited(e).AuthorsOf(role)),
            (from, to) => cited(to).TakeAuthorsOf(role, cited(from))));

    /// <summary>The data of the genres and the universes of an entry form, each compared on the records it cites.</summary>
    public static IEnumerable<EntryDatum<TEntry>> ClassificationData<TEntry>(Func<TEntry, CitedRecords> cited, string genres, string universes) =>
    [
        new EntryDatum<TEntry>(GenresKey, genres, e => Identities(cited(e).Genres), e => EntryChange.Names(cited(e).Genres),
            (from, to) => cited(to).TakeGenres(cited(from))),
        new EntryDatum<TEntry>(UniversesKey, universes, e => Identities(cited(e).Universes), e => EntryChange.Names(cited(e).Universes),
            (from, to) => cited(to).TakeUniverses(cited(from))),
    ];
    /// <summary>Credits the role with the authors another entry credits it with, those it added on the spot included.</summary>
    public void TakeAuthorsOf(ContributionRole role, CitedRecords from)
    {
        Credits.RemoveAll(c => c.Role == role);
        Credits.AddRange(from.Credits.Where(c => c.Role == role));
        NewAuthors.AddRange(from.NewAuthors.Where(a => NewAuthors.All(o => o.Key != a.Key)));
    }

    /// <summary>Cites the genres another entry cites, those it added on the spot included.</summary>
    public void TakeGenres(CitedRecords from)
    {
        Genres = [.. from.Genres];
        NewGenres.AddRange(from.NewGenres.Where(g => NewGenres.All(o => o.Key != g.Key)));
    }

    /// <summary>Cites the universes another entry cites, those it added on the spot included.</summary>
    public void TakeUniverses(CitedRecords from)
    {
        Universes = [.. from.Universes];
        NewUniverses.AddRange(from.NewUniverses.Where(u => NewUniverses.All(o => o.Key != u.Key)));
    }

    // The records a list cites, whatever their order or their names.
    private static string Identities(IEnumerable<RecordOption> records) => string.Join(',', records.Select(r => r.Id).Order());

    public static RecordOption Author(AuthorListItem author) =>
        new(author.Id, RecordSearch.AuthorName(author.LastName, author.FirstName, author.Pseudonym), null);

    /// <summary>A universe named with its ancestors, from the top of the hierarchy (visuel/saisie-album/contenu.md).</summary>
    public static RecordOption Universe(UniverseInHierarchy universe) =>
        new(universe.Universe.Id, string.Join(" › ", universe.Ancestors.Append(universe.Universe).Select(u => u.Name)), null);

}
