---
type: Choix d'implémentation
title: 'Organisation du code'
description: 'Projets de la solution et leur rôle, projets de tests ; à lire pour ajouter un projet ou placer du code.'
applies_to:
  - 'Bdtheque.slnx'
  - 'src/*/*.csproj'
  - 'tests/*/*.csproj'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Organisation du code

La solution .NET est découpée en projets par responsabilité, sous `src/` :

| Projet | Rôle |
| --- | --- |
| `Bdtheque.Domain` | Entités du modèle métier, enums, value objects. Aucune dépendance à EF Core ni à un framework web. |
| `Bdtheque.Infrastructure` | Implémentation EF Core / Npgsql : `DbContext`, configurations d'entités, migrations. |
| `Bdtheque.Contracts` | Contrats d'échange (DTOs) exposés par l'API et consommés par ses clients internes (`frontend`, `mcp`). Découplés des entités du domaine. |
| `Bdtheque.Api` | Conteneur `api` : endpoints Minimal API, règles applicatives, service de taux de change, estimation ML.NET. |
| `Bdtheque.Frontend` | Conteneur `frontend` : composants Blazor (rendu côté serveur), authentification cookie, appels HTTP vers `Bdtheque.Api`. |
| `Bdtheque.Mcp` | Conteneur `mcp` (Phase 6) : outils MCP pour agents IA, appels HTTP vers `Bdtheque.Api` via `Bdtheque.Contracts` ([Outil pour agents IA](/choix-implementation/agents-ia.md)). Ne référence ni `Bdtheque.Api` ni `Bdtheque.Domain`. |
| `Bdtheque.HealthProbe` | Sonde du bilan de santé des images publiées (`HEALTHCHECK`), qui n'ont ni shell ni `curl` ([Images de base des conteneurs](/choix-implementation/deploiement/images-de-base-des-conteneurs.md)) : interroge une URL et sort en `0` sur un succès, en `1` sinon. |
| `Bdtheque.ValidationData` | Chargement des données de l'environnement de validation fonctionnelle, au travers de l'API d'administration ([Environnement de validation fonctionnelle](/choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md)). Ne référence que `Bdtheque.Contracts` ; jamais déployé. |

Chaque projet source a vocation à avoir son miroir sous `tests/` (ex. `Bdtheque.Api.Tests`), créé dès que son contenu justifie des tests — proportionnalité définie dans la règle de non-régression de `gestion-projet/`. S'y ajoutent les tests de bout en bout de l'application complète (`Bdtheque.EndToEnd.Tests`) et la bibliothèque que partagent les tests (`Bdtheque.Testing`), cf. [Outillage de développement .NET](/choix-implementation/outillage.md).
