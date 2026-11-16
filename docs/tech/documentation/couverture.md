---
title: 'Couverture'
description: 'Ce que la documentation décrit aujourd''hui, et ce qui reste à documenter, avec les tickets qui le portent ; pour savoir si une page existe avant de lire le code.'
---

# Couverture

La documentation technique est **partielle** : elle décrit la vue d'ensemble et un premier mécanisme transversal. Un sujet absent ici n'est pas documenté ; c'est le code, guidé par la [vue d'ensemble](../architecture/vue-d-ensemble.md) et les [projets de la solution](../architecture/projets-de-la-solution.md), qui le décrit. `Get-TechnicalDocumentationImpact.ps1` signale les fichiers modifiés qu'aucune page ne décrit.

## Documenté

| Sujet | Page |
| --- | --- |
| Conteneurs, échanges, frontières de sécurité | [Vue d'ensemble](../architecture/vue-d-ensemble.md) |
| Projets de la solution et leurs dépendances | [Projets de la solution](../architecture/projets-de-la-solution.md), [Dépendances des projets](../references/dependances-des-projets.md) (générée) |
| Déploiement, pré-versions, environnement de validation | [Déploiement et environnement de validation](../architecture/deploiement.md) |
| Authentification de l'administrateur, appels de `frontend` vers `api`, relais des médias | [Authentification et appels internes](../mecanismes/authentification-et-appels-internes.md) |
| Familles de routes et accès au contrat de l'API | [Contrat HTTP de l'API](../references/api-http.md) |
| La documentation technique elle-même | [Consulter, construire, vérifier et maintenir](consulter-et-maintenir.md) |

## À documenter

Par ordre de priorité : transversalité et risque de régression d'abord.

| Sujet | Ticket |
| --- | --- |
| Persistance : conventions EF Core, migrations au démarrage, concurrence et verrous, collation et unicités, suppression | [#495](https://github.com/Tetram76/bdtheque-claude/issues/495) |
| Erreurs, de la règle métier à leur présentation | [#496](https://github.com/Tetram76/bdtheque-claude/issues/496) |
| Frontend : rendu, fiches interactives, thèmes, internationalisation, mesure d'audience | [#497](https://github.com/Tetram76/bdtheque-claude/issues/497) |
| Recherche simple et avancée, visuels, journal des modifications | [#498](https://github.com/Tetram76/bdtheque-claude/issues/498) |
| Intégration continue et stratégie de tests | [#499](https://github.com/Tetram76/bdtheque-claude/issues/499) |

## Références à générer

| Référence | Raison de son absence | Ticket |
| --- | --- | --- |
| Contrat OpenAPI versionné | Consultable exact pour chaque commit en validation ; sa génération à la construction démarre l'hôte de l'API, à mettre au point. | [#493](https://github.com/Tetram76/bdtheque-claude/issues/493) |
| Schéma physique de la base | Exige une base PostgreSQL migrée par le CI, réservée aux changements du modèle pour ne pas peser sur les autres. | [#494](https://github.com/Tetram76/bdtheque-claude/issues/494) |
