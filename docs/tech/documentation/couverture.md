---
title: 'Couverture'
description: 'Ce que la documentation décrit aujourd''hui, et ce qui reste à documenter, avec les tickets qui le portent ; pour savoir si une page existe avant de lire le code.'
---

# Couverture

La documentation technique est **partielle** : elle décrit la vue d'ensemble, l'authentification et les appels internes, la persistance, le parcours des erreurs, le frontend, la recherche, les visuels, le journal des modifications, l'intégration continue et la stratégie de tests. Un sujet absent ici n'est pas documenté ; c'est le code, guidé par la [vue d'ensemble](../architecture/vue-d-ensemble.md) et les [projets de la solution](../architecture/projets-de-la-solution.md), qui le décrit. `Get-TechnicalDocumentationImpact.ps1` signale les fichiers modifiés qu'aucune page ne décrit.

## Documenté

| Sujet | Page |
| --- | --- |
| Conteneurs, échanges, frontières de sécurité | [Vue d'ensemble](../architecture/vue-d-ensemble.md) |
| Projets de la solution et leurs dépendances | [Projets de la solution](../architecture/projets-de-la-solution.md), [Dépendances des projets](../references/dependances-des-projets.md) (générée) |
| Déploiement, pré-versions, environnement de validation | [Déploiement et environnement de validation](../architecture/deploiement.md) |
| Authentification de l'administrateur, appels de `frontend` vers `api`, relais des médias | [Authentification et appels internes](../mecanismes/authentification-et-appels-internes.md) |
| Persistance : contexte EF Core et conventions, schéma et migrations, démarrage d'`api`, base des tests | [Persistance : modèle et schéma](../mecanismes/persistance-modele-et-schema.md) |
| Persistance : transaction, verrous et version d'un agrégat, enregistrement, suppression, échecs de la base | [Persistance : écriture d'un agrégat](../mecanismes/persistance-ecriture-d-un-agregat.md) |
| Erreurs : classement par `api`, ProblemDetails, champ en cause, lecture et présentation par `frontend` | [Erreurs, de la règle à leur présentation](../mecanismes/erreurs.md) |
| Frontend : requête d'une page, rendu statique et îlots interactifs, fiches de saisie, scripts du navigateur, tests | [Frontend : rendu des pages et fiches de saisie](../mecanismes/frontend-rendu-et-saisie.md) |
| Frontend : écran d'accueil, cookies, visite et thème, couleurs du jour, culture et fuseau, mesure d'audience | [Frontend : visite, thème, culture et mesure d'audience](../mecanismes/frontend-visite-theme-et-culture.md) |
| Recherche des listes, recherche avancée et recherches enregistrées | [Recherche simple et avancée](../mecanismes/recherche.md) |
| Visuels : téléversement, zone d'attente, stockage, médias, réconciliation | [Visuels](../mecanismes/visuels.md) |
| Journal des modifications : capture, correspondance des attributs, stockage, lecture | [Journal des modifications](../mecanismes/journal-des-modifications.md) |
| Workflow CI : classement des fichiers modifiés, jobs et leurs conditions, tests de bout en bout par thème, analyse SonarQube Cloud, analyse des images | [Intégration continue](../integration-et-tests/integration-continue.md) |
| Projets de tests et leurs ressources, tests du dépôt, horloge et thème imposés, API interdites, captures de référence | [Stratégie de tests](../integration-et-tests/strategie-de-tests.md) |
| Contrôle qualité hebdomadaire, contrôle de sécurité quotidien, relevé des versions | [Contrôles périodiques](../integration-et-tests/controles-periodiques.md) |
| Familles de routes, contrat OpenAPI de l'API et son accès | [Contrat HTTP de l'API](../references/api-http.md), contrat [`openapi-v1.json`](../references/openapi-v1.json) (généré) |
| La documentation technique elle-même | [Consulter, construire, vérifier et maintenir](consulter-et-maintenir.md) |

## Références à générer

| Référence | Raison de son absence | Ticket |
| --- | --- | --- |
| Schéma physique de la base | Exige une base PostgreSQL migrée par le CI, réservée aux changements du modèle pour ne pas peser sur les autres. | [#494](https://github.com/Tetram76/bdtheque-claude/issues/494) |
