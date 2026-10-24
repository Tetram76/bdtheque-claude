namespace Bdtheque.Frontend.Components.Errors;

/// <summary>
/// Texts of the business rules (<c>RuleMessages.resx</c>), keyed by the rule codes the API reports: a published
/// code is never renamed, so its key never changes (choix-implementation/ecritures.md § Erreurs métier,
/// fonctionnelles et techniques).
/// </summary>
public sealed class RuleMessages;

/// <summary>
/// Names of the entity types (<c>EntityNames.resx</c>), singular (<c>{kind}.One</c>) and plural
/// (<c>{kind}.Other</c>), each taking its count: those of a deletion's impact, for instance.
/// </summary>
public sealed class EntityNames;
