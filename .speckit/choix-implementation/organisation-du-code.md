---
type: Choix d'implémentation
title: 'Organisation du code'
description: 'Projets de la solution et leur rôle, projets de tests ; à lire pour ajouter un projet ou placer du code.'
applies_to:
  - 'Bdtheque.slnx'
  - 'src/*/*.csproj'
  - 'tests/*/*.csproj'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Organisation du code

La solution est découpée en **projets par responsabilité** (`src/`), chacun ne référençant que ce dont il a besoin : une dépendance interdite ne compile pas, au lieu d'être une convention à surveiller.

| Projet | Rôle et dépendances voulues |
| --- | --- |
| `Bdtheque.Domain` | Modèle métier et ses règles, sans EF Core ni framework web : les règles se testent sans base ni serveur. |
| `Bdtheque.Infrastructure` | Persistance EF Core / Npgsql. |
| `Bdtheque.Contracts` | Contrats d'échange de l'API, découplés des entités : les clients de l'API (`frontend`, `mcp`) ne dépendent pas du domaine, qui évolue sans casser leurs contrats. |
| `Bdtheque.Api` | Conteneur `api` : endpoints et règles applicatives. |
| `Bdtheque.Frontend` | Conteneur `frontend` : l'interface web, cliente de l'API par `Bdtheque.Contracts`. |
| `Bdtheque.Mcp` | Conteneur `mcp` (Phase 6) : [Outil pour agents IA](agents-ia.md), client de l'API par `Bdtheque.Contracts`, sans référence à `Bdtheque.Api` ni à `Bdtheque.Domain`. |
| `Bdtheque.HealthProbe` | Sonde de santé des images publiées, qui n'ont ni shell ni `curl` ([Images de base des conteneurs](deploiement/images-de-base-des-conteneurs.md)). |
| `Bdtheque.ValidationData` | Données de l'[Environnement de validation fonctionnelle](deploiement/environnement-de-validation-fonctionnelle.md), chargées par l'API d'administration (seulement `Bdtheque.Contracts`) ; jamais déployé. |

Chaque projet source a son miroir de tests sous `tests/` (ex. `Bdtheque.Api.Tests`) dès que son contenu en justifie ([Qualité du code](../gestion-projet/gouvernance/qualite-du-code.md), non-régression). S'y ajoutent les tests de bout en bout de l'application complète (`Bdtheque.EndToEnd.Tests`) et la bibliothèque que partagent les tests (`Bdtheque.Testing`), cf. [Outillage de développement .NET](outillage.md).
