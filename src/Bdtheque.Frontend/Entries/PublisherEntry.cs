using System.Text.Json;
using Bdtheque.Contracts.Admin;

namespace Bdtheque.Frontend.Entries;

/// <summary>
/// A publisher as its entry form holds it while it is entered (visuel/saisie-editeur/contenu.md): its fields, and its collections
/// as the form lists them until the saving, removed or added ones included. Persisted from the prerendering into the interactive
/// circuit, which does not read it again. It applies no rule of the publisher: the domain is their only source
/// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md), and reports what to correct when the publisher
/// is saved.
/// </summary>
public sealed class PublisherEntry
{
    /// <summary>The publisher; <c>null</c> for a new one, not saved yet.</summary>
    public Guid? Id { get; set; }

    /// <summary>The version of the publisher the form was read at, sent back with its modification.</summary>
    public uint Version { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public string? Name { get; set; }

    public string? Website { get; set; }

    /// <summary>Its collections as read, by name, then those the form added, in the order they were added.</summary>
    public List<CollectionEntry> Collections { get; set; } = [];

    /// <summary>The publisher as the API gives it.</summary>
    public static PublisherEntry Of(PublisherForm form) => new()
    {
        Id = form.Id,
        Version = form.Version,
        CreatedAt = form.CreatedAt,
        ModifiedAt = form.ModifiedAt,
        Name = form.Name,
        Website = form.Website,
        Collections = [.. form.Collections.Select(c => new CollectionEntry
        {
            Key = c.Id, Id = c.Id, Name = c.Name, EditionCount = c.EditionCount, SeriesCount = c.SeriesCount,
        })],
    };

    /// <summary>
    /// Adds an empty collection, created with the saving of the publisher. Its key, the form's own, tells it apart from the
    /// other collections until it is saved and receives its identifier.
    /// </summary>
    public CollectionEntry AddCollection()
    {
        var collection = new CollectionEntry { Key = Guid.NewGuid() };
        Collections.Add(collection);
        return collection;
    }

    /// <summary>
    /// Removes a collection at the saving: one the form added leaves the list at once; one read stays in it, struck, until the
    /// saving, and can be restored.
    /// </summary>
    public void Remove(CollectionEntry collection)
    {
        if (collection.Id is null)
            Collections.Remove(collection);
        else
            collection.Removed = true;
    }

    /// <summary>The collections the publisher keeps at the saving, removed ones left out.</summary>
    public IEnumerable<CollectionEntry> Kept => Collections.Where(c => !c.Removed);

    /// <summary>The content to save: the whole form, its collections replaced by those kept.</summary>
    public PublisherContent ToContent() => new(
        Name ?? "",
        Website,
        [.. Kept.Select(c => new PublisherCollectionContent(c.Id, c.Name ?? ""))]);

    /// <summary>What tells two states of the form apart: the content it would save.</summary>
    public string Fingerprint() => JsonSerializer.Serialize(ToContent());

    public PublisherEntry Copy() => JsonSerializer.Deserialize<PublisherEntry>(JsonSerializer.Serialize(this))!;
}

/// <summary>A collection of the publisher, as its form lists it.</summary>
public sealed class CollectionEntry
{
    /// <summary>What tells it apart in the form: its identifier, or a key of the form's own for a collection it added.</summary>
    public Guid Key { get; set; }

    /// <summary>The collection; <c>null</c> for one the form added, not saved yet.</summary>
    public Guid? Id { get; set; }

    public string? Name { get; set; }

    /// <summary>The editions of the collection, when it was read.</summary>
    public int EditionCount { get; set; }

    /// <summary>The series whose new editions are proposed the collection, when it was read.</summary>
    public int SeriesCount { get; set; }

    /// <summary>Removed at the saving: a collection read, struck until then.</summary>
    public bool Removed { get; set; }

    /// <summary>
    /// Referenced when it was read: it cannot be removed (fonctionnel/regles-metier/suppression-des-entites.md), which the
    /// saving would refuse anyway.
    /// </summary>
    public bool InUse => EditionCount > 0 || SeriesCount > 0;
}
