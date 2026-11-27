---
title: 'Couverture'
description: 'Cette page dit quels sujets la documentation décrit aujourd''hui et à quelle page ; à lire pour savoir si une page existe sur un sujet avant d''en lire le code.'
---

# Couverture

La documentation technique ne couvre pas encore toute l'application. Elle décrit aujourd'hui la vue d'ensemble, l'authentification et les appels internes, la persistance, le parcours des erreurs, le frontend, la recherche, les visuels, le journal des modifications, l'intégration continue et la stratégie de tests. Un sujet qui n'apparaît pas dans le tableau ci-dessous n'est pas documenté : c'est alors le code qui le décrit, et la [vue d'ensemble](../architecture/vue-d-ensemble.md) ainsi que les [projets de la solution](../architecture/projets-de-la-solution.md) aident à s'y orienter. Le script `Get-TechnicalDocumentationImpact.ps1` signale de son côté les fichiers modifiés qu'aucune page ne décrit.

## Les sujets documentés

Le tableau suivant associe chaque sujet documenté à la page, ou aux pages, qui le décrivent. Une page marquée « générée » est produite par un script depuis une source qui fait autorité, et ne se modifie pas à la main.

| Sujet | Page |
| --- | --- |
| Les conteneurs de l'application, leurs échanges et les frontières de sécurité. | [Vue d'ensemble](../architecture/vue-d-ensemble.md) |
| Les projets de la solution et leurs dépendances. | [Projets de la solution](../architecture/projets-de-la-solution.md), [Dépendances des projets](../references/dependances-des-projets.md) (générée) |
| Le déploiement, les pré-versions et l'environnement de validation. | [Déploiement et environnement de validation](../architecture/deploiement.md) |
| L'authentification de l'administrateur, les appels de `frontend` vers `api` et le relais des médias. | [Authentification et appels internes](../mecanismes/authentification-et-appels-internes.md) |
| Le contexte EF Core et ses conventions, le schéma et les migrations, le démarrage d'`api` et la base des tests. | [Persistance : modèle et schéma](../mecanismes/persistance-modele-et-schema.md), [Schéma de la base](../references/schema-de-la-base.md) (générée) |
| La transaction, les verrous et la version d'un agrégat, l'enregistrement, la suppression et les échecs de la base. | [Persistance : écriture d'un agrégat](../mecanismes/persistance-ecriture-d-un-agregat.md) |
| Le classement des erreurs par `api`, les réponses ProblemDetails, le champ en cause, et la lecture et la présentation des erreurs par `frontend`. | [Erreurs, de la règle à leur présentation](../mecanismes/erreurs.md) |
| Le traitement d'une requête de page par le frontend, le rendu statique et les îlots interactifs, les fiches de saisie, les scripts du navigateur et leurs tests. | [Frontend : rendu des pages et fiches de saisie](../mecanismes/frontend-rendu-et-saisie.md) |
| L'écran d'accueil, les cookies, la visite et son thème, les couleurs du jour, la culture et le fuseau horaire, et la mesure d'audience. | [Frontend : visite, thème, culture et mesure d'audience](../mecanismes/frontend-visite-theme-et-culture.md) |
| La recherche des listes, la recherche avancée et les recherches enregistrées. | [Recherche simple et avancée](../mecanismes/recherche.md) |
| Le téléversement des visuels, leur zone d'attente, leur stockage, leurs médias et leur réconciliation. | [Visuels](../mecanismes/visuels.md) |
| La capture du journal des modifications, la correspondance des attributs, son stockage, sa lecture filtrée et son écran dans l'administration. | [Journal des modifications](../mecanismes/journal-des-modifications.md) |
| Le workflow CI : classement des fichiers modifiés, jobs et leurs conditions, tests de bout en bout par thème, analyse SonarQube Cloud et analyse des images. | [Intégration continue](../integration-et-tests/integration-continue.md) |
| Les projets de tests et leurs ressources, les tests du dépôt, l'horloge et le thème imposés, les API interdites et les captures de référence. | [Stratégie de tests](../integration-et-tests/strategie-de-tests.md) |
| Le contrôle qualité hebdomadaire, le contrôle de sécurité quotidien, le relevé des versions et la dette au fil de l'eau d'une Pull Request. | [Contrôles périodiques](../integration-et-tests/controles-periodiques.md) |
| Les familles de routes de l'API, son contrat OpenAPI et la façon de le consulter. | [Contrat HTTP de l'API](../references/api-http.md), contrat [`openapi-v1.json`](../references/openapi-v1.json) (généré) |
| La documentation technique elle-même. | [Consulter, construire, vérifier et maintenir](consulter-et-maintenir.md) |
