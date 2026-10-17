using Bdtheque.Api.Endpoints;
using Bdtheque.Contracts.Deletion;
using Bdtheque.Contracts.Search;
using Bdtheque.Domain.Entities;
using C = Bdtheque.Contracts.Enums;
using D = Bdtheque.Domain.Enums;
using EntityKind = Bdtheque.Contracts.Enums.EntityKind;

namespace Bdtheque.Api.Search;

/// <summary>
/// What the advanced search queries (fonctionnel.md § Recherche avancée): every entity of the consultation,
/// every data of the business model — public, of management or deduced —, and every link of the model, in
/// both directions (modele-metier.md § Relations et cardinalités).
/// </summary>
/// <remarks>
/// The keys are a contract, which saved searches will store: a key is never renamed nor reused for other
/// data. The amounts are only queried for being set, and the data computed by later phases (amounts in
/// euros, estimated value, missing volumes, estimated release) join the search with them
/// (suivi-implementation.md).
/// </remarks>
internal static class AdvancedSearchSchema
{
    private static readonly Dictionary<EntityKind, SearchEntityDefinition> Entities = new SearchEntityDefinition[]
    {
        new SearchEntityBuilder<Album>(EntityKind.Album)
            .Text("title", a => a.Title)
            // That of the series standing in for an album without a title of its own (modele-metier.md § Album).
            .Text("sortKey", a => a.SortKey ?? a.Series!.SortKey, isOptional: false)
            .Boolean("manualSortKey", a => a.IsManualSortKey)
            .Text("initial", a => a.NavigationEntry ?? a.Series!.NavigationEntry, isOptional: false)
            .Enumeration<D.AlbumType, C.AlbumType>("type", a => a.Type, isOptional: false)
            .Boolean("specialIssue", a => a.IsSpecialIssue)
            .Number("volumeNumber", a => a.VolumeNumber)
            .Number("startVolumeNumber", a => a.StartVolumeNumber)
            .Number("endVolumeNumber", a => a.EndVolumeNumber)
            .PartialDate("firstPublicationDate", a => a.FirstPublicationYear, a => a.FirstPublicationMonth)
            .Text("summary", a => a.Summary)
            .Text("personalNotes", a => a.PersonalNotes)
            .Enumeration<D.AlbumRating, C.AlbumRating>("rating", a => a.Rating)
            .Boolean("inCollection", CatalogExpressions.Expand((Album a) => (bool?)a.Editions.Any(e => e.IsOwned())))
            .Number("editionCount", a => a.Editions.Count(), isOptional: false)
            .One("series", EntityKind.Series, a => a.Series)
            .Many("editions", EntityKind.Edition, a => a.Editions)
            .Many("contributions", EntityKind.Contribution, a => a.Contributions)
            .Many("genres", EntityKind.Genre, a => a.Genres)
            .Attached("displayedGenres", EntityKind.Genre, SearchAttachments.GenresOf<Album>(a => a.Genres, a => a.Series!.Genres))
            .Attached("universes", EntityKind.Universe, SearchAttachments.UniversesOf<Album>(a => a.Universes))
            .Attached("displayedUniverses", EntityKind.Universe, SearchAttachments.UniversesOf<Album>(a => a.Universes, a => a.Series!.Universes))
            // The intent on the album itself: those on its editions carry its identifier too, but target them
            // (modele-metier.md § Intention d'achat — relations), and are reached through them.
            .ZeroOrOne("purchaseIntent", EntityKind.PurchaseIntent, a => a.PurchaseIntents.Where(p => p.EditionId == null))
            .Build(),

        new SearchEntityBuilder<Series>(EntityKind.Series)
            .Text("title", s => s.Title, isOptional: false)
            .Text("sortKey", s => s.SortKey, isOptional: false)
            .Boolean("manualSortKey", s => s.IsManualSortKey)
            .Text("initial", s => s.NavigationEntry, isOptional: false)
            .Enumeration<D.SeriesStatus, C.SeriesStatus>("status", s => s.Status)
            .Number("theoreticalVolumeCount", s => s.TheoreticalVolumeCount)
            .Boolean("complete", s => s.IsComplete)
            .Boolean("excludeFromMissingVolumes", s => s.ExcludeFromMissingVolumes)
            .Boolean("excludeFromReleaseEstimates", s => s.ExcludeFromReleaseEstimates)
            .Text("summary", s => s.Summary)
            .Text("personalNotes", s => s.PersonalNotes)
            .Enumeration<D.BindingType, C.BindingType>("templateBinding", s => s.TemplateBinding)
            .Enumeration<D.BookOrientation, C.BookOrientation>("templateOrientation", s => s.TemplateOrientation)
            .Enumeration<D.ReadingDirection, C.ReadingDirection>("templateReadingDirection", s => s.TemplateReadingDirection)
            .Enumeration<D.EditionFormat, C.EditionFormat>("templateFormat", s => s.TemplateFormat)
            .Enumeration<D.EditionCategory, C.EditionCategory>("templateCategory", s => s.TemplateEditionCategory)
            .Enumeration<D.EditionCondition, C.EditionCondition>("templateCondition", s => s.TemplateCondition)
            .Boolean("templateColor", s => s.TemplateIsColor, isOptional: true)
            .Queried<Album>("albums", EntityKind.Album, scope => s => scope.Context.Albums.Where(a => a.SeriesId == s.Id))
            .Many("contributions", EntityKind.Contribution, s => s.TemplateContributions)
            .One("publisher", EntityKind.Publisher, s => s.TemplatePublisher)
            .One("publisherCollection", EntityKind.PublisherCollection, s => s.TemplatePublisherCollection)
            .Many("genres", EntityKind.Genre, s => s.Genres)
            .Attached("universes", EntityKind.Universe, SearchAttachments.UniversesOf<Series>(s => s.Universes))
            .Build(),

        new SearchEntityBuilder<Edition>(EntityKind.Edition)
            .PartialDate("publicationYear", e => e.PublicationYear)
            // An ISBN is entered as printed, with or without separators: ignored on both sides, as by the lists.
            .Text("isbn", e => e.Isbn!.Replace("-", "").Replace(" ", ""), normalize: EditionEndpoints.WithoutIsbnSeparators)
            .Enumeration<D.BindingType, C.BindingType>("binding", e => e.Binding)
            .Enumeration<D.BookOrientation, C.BookOrientation>("orientation", e => e.Orientation)
            .Enumeration<D.ReadingDirection, C.ReadingDirection>("readingDirection", e => e.ReadingDirection)
            .Enumeration<D.EditionFormat, C.EditionFormat>("format", e => e.Format)
            .Number("pageCount", e => e.PageCount)
            .Enumeration<D.EditionCategory, C.EditionCategory>("category", e => e.Category)
            .Boolean("dedicated", e => e.IsDedicated)
            .Boolean("color", e => e.IsColor)
            .Enumeration<D.EditionCondition, C.EditionCondition>("condition", e => e.Condition)
            .Enumeration<D.AcquisitionMode, C.AcquisitionMode>("acquisitionMode", e => e.AcquisitionMode)
            .Boolean("secondHand", e => e.IsSecondHand)
            .Day("acquisitionDate", e => e.AcquisitionDate)
            .Amount("acquisitionPrice", e => e.AcquisitionAmount)
            .Boolean("free", e => e.IsFree)
            .Amount("initialValue", e => e.InitialValueAmount)
            .Instant("enteredCollectionAt", e => e.EnteredCollectionAt)
            .Text("personalReference", e => e.PersonalReference)
            .Text("personalNotes", e => e.PersonalNotes)
            .Boolean("inCollection", CatalogExpressions.Expand((Edition e) => (bool?)e.IsOwned()))
            .One("album", EntityKind.Album, e => e.Album)
            .One("publisher", EntityKind.Publisher, e => e.Publisher)
            .One("publisherCollection", EntityKind.PublisherCollection, e => e.PublisherCollection)
            .Many("visuals", EntityKind.EditionVisual, e => e.Visuals)
            // The intents of an album carry its identifier, those on its editions included.
            .ZeroOrOne("purchaseIntent", EntityKind.PurchaseIntent, e => e.Album.PurchaseIntents.Where(p => p.EditionId == e.Id))
            .Build(),

        new SearchEntityBuilder<Author>(EntityKind.Author)
            .Text("lastName", a => a.LastName)
            .Text("firstName", a => a.FirstName)
            .Text("pseudonym", a => a.Pseudonym)
            .Text("biography", a => a.Biography)
            .Text("nationality", a => a.Nationality)
            .Text("sortKey", a => a.SortKey, isOptional: false)
            .Text("initial", a => a.NavigationEntry, isOptional: false)
            .Queried<Contribution>("contributions", EntityKind.Contribution, scope => a => scope.Context.Contributions.Where(c => c.AuthorId == a.Id))
            .Build(),

        new SearchEntityBuilder<Publisher>(EntityKind.Publisher)
            .Text("name", p => p.Name, isOptional: false)
            .Text("website", p => p.Website)
            .Many("collections", EntityKind.PublisherCollection, p => p.Collections)
            .Queried<Edition>("editions", EntityKind.Edition, scope => p => scope.Context.Editions.Where(e => e.PublisherId == p.Id))
            .Queried<Series>("series", EntityKind.Series, scope => p => scope.Context.Series.Where(s => s.TemplatePublisherId == p.Id))
            .Build(),

        new SearchEntityBuilder<PublisherCollection>(EntityKind.PublisherCollection)
            .Text("name", c => c.Name, isOptional: false)
            .One("publisher", EntityKind.Publisher, c => c.Publisher)
            .Queried<Edition>("editions", EntityKind.Edition, scope => c => scope.Context.Editions.Where(e => e.PublisherCollectionId == c.Id))
            .Queried<Series>("series", EntityKind.Series, scope => c => scope.Context.Series.Where(s => s.TemplatePublisherCollectionId == c.Id))
            .Build(),

        new SearchEntityBuilder<Genre>(EntityKind.Genre)
            .Text("label", g => g.Label, isOptional: false)
            // The reverse of the association of an album, as its link `genres`: an album showing the genre of its
            // series is reached through the series (`series`, then `albums`), a path of its own.
            .Attached("albums", EntityKind.Album, SearchAttachments.RecordsOfGenre<Album>(a => a.Genres))
            .Attached("series", EntityKind.Series, SearchAttachments.RecordsOfGenre<Series>(s => s.Genres))
            .Build(),

        new SearchEntityBuilder<Universe>(EntityKind.Universe)
            .Text("name", u => u.Name, isOptional: false)
            .Text("description", u => u.Description)
            .One("parent", EntityKind.Universe, u => u.Parent)
            .Many("children", EntityKind.Universe, u => u.Children)
            // Attached to the universe or one of its descendants (fonctionnel.md § Hiérarchie des univers): the reverse
            // of the link `universes` of an album, those of its series being reached through the series.
            .Attached("albums", EntityKind.Album, SearchAttachments.RecordsOfUniverse<Album>(a => a.Universes))
            .Attached("series", EntityKind.Series, SearchAttachments.RecordsOfUniverse<Series>(s => s.Universes))
            .Build(),

        new SearchEntityBuilder<EditionVisual>(EntityKind.EditionVisual)
            .Enumeration<D.VisualType, C.VisualType>("type", v => v.Type, isOptional: false)
            .Number("displayOrder", v => v.DisplayOrder, isOptional: false)
            // The reference of the stored media, its "file" in the model (modele-metier.md § Visuel d'édition).
            .Text("mediaReference", v => v.MediaReference, isOptional: false)
            .One("edition", EntityKind.Edition, v => v.Edition)
            .Build(),

        new SearchEntityBuilder<PurchaseIntent>(EntityKind.PurchaseIntent)
            .Boolean("targetsEdition", p => p.EditionId != null)
            // The album targeted, only by an intent on the album itself: that of an edition is reached through it.
            .QueriedZeroOrOne<Album>("album", EntityKind.Album, scope => p => scope.Context.Albums.Where(a => a.Id == p.AlbumId && p.EditionId == null))
            .One("edition", EntityKind.Edition, p => p.Edition)
            .Build(),

        new SearchEntityBuilder<Contribution>(EntityKind.Contribution)
            .Enumeration<D.ContributionRole, C.ContributionRole>("role", c => c.Role, isOptional: false)
            .One("author", EntityKind.Author, c => c.Author)
            .One("album", EntityKind.Album, c => c.Album)
            .One("series", EntityKind.Series, c => c.Series)
            .Build(),
    }.ToDictionary(e => e.Kind);

    /// <summary>The description of the schema, for the clients to build the form of the search.</summary>
    public static readonly SearchSchema Description = new(Entities.Values.Select(e => e.Describe()).ToList());

    /// <exception cref="ArgumentException">
    /// An entity the search never returns (the saved searches): a technical error, the client building the
    /// search from the schema.
    /// </exception>
    public static SearchEntityDefinition Of(EntityKind kind) =>
        Entities.GetValueOrDefault(kind) ?? throw new ArgumentException($"The advanced search does not return {kind}.", nameof(kind));

    /// <summary>The entity of the records of <paramref name="clrType"/>; <c>null</c> for an entity the search never returns.</summary>
    public static EntityKind? KindOf(Type clrType) => Entities.Values.SingleOrDefault(e => e.ClrType == clrType)?.Kind;
}
