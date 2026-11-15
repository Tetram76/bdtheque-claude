using Bdtheque.Contracts.Deletion;
using Microsoft.Extensions.Localization;

namespace Bdtheque.Frontend.Components.Errors;

/// <summary>
/// Texts of the business rules (<c>RuleMessages.resx</c>), keyed by the rule codes the API reports: a published
/// code is never renamed, so its key never changes
/// (choix-implementation/ecritures/erreurs-metier-fonctionnelles-et-techniques.md).
/// </summary>
public sealed class RuleMessages;

/// <summary>
/// Names of the entity types (<c>EntityNames.resx</c>), singular (<c>{kind}.One</c>) and plural
/// (<c>{kind}.Other</c>), each taking its count: those of a deletion's impact, for instance.
/// </summary>
public sealed class EntityNames;

/// <summary>
/// Texts of the business rules under the field in cause (<c>FieldMessages.resx</c>), shorter than those of
/// <see cref="RuleMessages"/>, which the error above the form gives, and keyed by the same rule codes. A text may take the
/// subject of the form (<c>{0}</c>, e.g. the name of the publisher as typed).
/// </summary>
public sealed class FieldMessages;

/// <summary>
/// A number of records of an entity type, in the singular or the plural, and the form (<c>One</c>, <c>Other</c>) that
/// suffixes the key of every text agreeing with it: those of <see cref="EntityNames"/>, or what to do about records
/// blocking a deletion.
/// </summary>
public static class EntityCounts
{
    public static string Form(int count) => count == 1 ? "One" : "Other";

    /// <summary>« 2 éditions », « 1 intention d'achat ».</summary>
    public static string Of(this IStringLocalizer<EntityNames> names, ImpactCount count) =>
        names[$"{count.Kind}.{Form(count.Count)}", count.Count];
}
