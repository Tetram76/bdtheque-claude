---
title: 'Contrat HTTP de l''API'
description: 'Cette page présente les familles de routes de l''API, son contrat OpenAPI versionné, généré depuis les endpoints, et la façon de l''explorer avec Scalar ; à lire pour connaître un endpoint, relire ce qu''un changement fait au contrat, ou essayer un appel en validation.'
code:
  - 'src/Bdtheque.Api/Program.cs'
  - 'src/Bdtheque.Api/Endpoints/**'
  - 'src/Bdtheque.Api/Hosting/**'
  - 'src/Bdtheque.Contracts/**'
  - 'tests/Bdtheque.Api.Tests/OpenApiContractTests.cs'
---

# Contrat HTTP de l'API

Le contrat HTTP de l'API, c'est-à-dire la liste de ses routes, de leurs paramètres et de leurs réponses, n'est écrit qu'à un seul endroit : dans ses endpoints eux-mêmes. ASP.NET Core en produit à l'exécution une description au format OpenAPI, le standard de description des API HTTP, sans bibliothèque tierce. La copie versionnée de cette description, [`openapi-v1.json`](openapi-v1.json), en est générée, comme l'explique la dernière section. Les raisons de ces choix sont données dans [Documentation de l'API](../../../.speckit/choix-implementation/socle/transverse/documentation-de-l-api.md) et [Organisation de l'API](../../../.speckit/choix-implementation/api/organisation-de-l-api.md).

## Les familles de routes

Les routes de l'API se répartissent en trois familles, déclarées par [`RouteGroups`](../../../src/Bdtheque.Api/Endpoints/RouteGroups.cs). Chaque famille déclare les réponses d'erreur qu'elle peut rendre, au format ProblemDetails, et chaque ressource y ajoute ses endpoints, dans [`src/Bdtheque.Api/Endpoints/`](../../../src/Bdtheque.Api/Endpoints/).

| Famille | À quoi elle sert | Qui l'appelle |
| --- | --- | --- |
| `/admin` | Elle sert à la saisie et à l'administration : fiches, listes, visuels en attente, paramétrage, journal des modifications et lecture de l'audience. | `frontend` l'appelle pour l'administrateur authentifié, et le chargement des données de validation l'utilise aussi. |
| `/catalog` | Elle sert à la consultation publique : fiches, listes, recherche avancée, médias des visuels et dashboard. | `frontend` l'appelle pour tout visiteur. |
| `/audience` | Elle sert à enregistrer la mesure d'audience. | `frontend` l'appelle pour tout visiteur. |

Les données échangées en JSON sont décrites par les types de [`src/Bdtheque.Contracts/`](../../../src/Bdtheque.Contracts/), un projet que partagent `api` et ses clients, si bien que les deux côtés emploient les mêmes définitions.

## Consulter le contrat

L'API sert elle-même sa description OpenAPI et une interface d'exploration interactive, Scalar, qui permet de parcourir les endpoints et d'en essayer les appels. Elle ne les sert que hors production :

| Environnement | Ce qui est servi | Adresse |
| --- | --- | --- |
| Environnement de validation | L'exploration interactive par Scalar, pour le commit validé. | `http://localhost:8181/scalar/v1` |
| Environnement de validation | La description OpenAPI, en JSON, du commit validé. | `http://localhost:8181/openapi/v1.json` |
| Développement local, environnement `Development` | Les mêmes, sur le port de l'API lancée. | `/scalar/v1`, `/openapi/v1.json` |
| Production | Ni l'une ni l'autre, puisque `api` n'y est pas joignable de l'extérieur. | — |

C'est [`Program.cs` de l'API](../../../src/Bdtheque.Api/Program.cs) qui sert ces adresses, dans les seuls environnements `Development` et [`Validation`](../../../src/Bdtheque.Api/Hosting/ApiEnvironments.cs), et qui les dispense du secret interne qu'`api` exige de toute autre requête. Pour essayer un appel dans Scalar, il faut en revanche ajouter à la requête l'en-tête `X-Internal-Api-Key`, avec la valeur `INTERNAL_API_KEY` du fichier [`validation.env`](../../../docker/validation/validation.env).

La documentation technique, servie à côté en validation ([Déploiement et environnement de validation](../architecture/deploiement.md)), ne présente que le fichier versionné décrit ci-dessous, sans l'explorer : l'exploration reste le rôle de Scalar.

## Contrat versionné

[`openapi-v1.json`](openapi-v1.json) est le document OpenAPI 3.1 que produit l'API, identique à celui que sert `/openapi/v1.json`, à une exception près : il ne contient pas la liste `servers`, qui reprend l'adresse de la requête et changerait d'une machine à l'autre. Il ne se modifie pas à la main. Son intérêt est de rendre visible tout changement du contrat : une Pull Request qui modifie une route modifie aussi ce fichier, et le changement se relit dans son diff.

Le fichier est produit par [`OpenApiContractTests`](../../../tests/Bdtheque.Api.Tests/OpenApiContractTests.cs). Ce test démarre l'API dans son hôte de test, sur une base PostgreSQL qui a reçu les migrations, et lui demande directement le document `v1` par `IOpenApiDocumentProvider`, sans passer par une requête HTTP. [`Update-TechnicalReferences.ps1`](../../../scripts/Update-TechnicalReferences.ps1) exécute ce test en lui demandant d'écrire le fichier. Il faut donc Docker pour régénérer le contrat ; l'option `-NoOpenApiContract` permet de l'omettre.

Le document suit l'ordre que donne ASP.NET Core, sans tri ajouté : les chemins apparaissent dans l'ordre de déclaration des endpoints, et les schémas par nom, sans distinction de casse. Il ne contient ni date, ni adresse, ni secret, si bien que deux générations successives donnent exactement le même fichier.

Le même test vérifie le contrat : il compare le document produit au fichier versionné, et échoue s'ils diffèrent ou si le fichier manque. Il s'exécute avec les tests de l'API, dans le job `Build & tests (.NET)` du CI, dès qu'une source change. Le fichier du contrat compte lui-même comme une source, de sorte qu'une Pull Request qui ne modifie que lui déclenche aussi la comparaison ([Intégration continue](../integration-et-tests/integration-continue.md#classement-des-fichiers-modifiés)). En revanche, `Update-TechnicalReferences.ps1 -Check`, qui s'exécute sans base, ne le compare pas.
