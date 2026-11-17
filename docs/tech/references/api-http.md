---
title: 'Contrat HTTP de l''API'
description: 'Familles de routes, contrat OpenAPI versionné et généré depuis les endpoints, son exploration interactive (Scalar) ; pour connaître un endpoint, relire ce qu''un changement fait au contrat, ou l''essayer en validation.'
code:
  - 'src/Bdtheque.Api/Program.cs'
  - 'src/Bdtheque.Api/Endpoints/**'
  - 'src/Bdtheque.Api/Hosting/**'
  - 'src/Bdtheque.Contracts/**'
  - 'tests/Bdtheque.Api.Tests/OpenApiContractTests.cs'
---

# Contrat HTTP de l'API

Le contrat de l'API n'est écrit qu'une fois : par ses endpoints eux-mêmes. ASP.NET Core en produit la description OpenAPI à l'exécution, sans bibliothèque tierce ; sa copie versionnée, [`openapi-v1.json`](openapi-v1.json), en est générée (ci-dessous). Choix et raisons : [Documentation de l'API](../../../.speckit/choix-implementation/socle/transverse/documentation-de-l-api.md), [Organisation de l'API](../../../.speckit/choix-implementation/api/organisation-de-l-api.md).

## Familles de routes

Déclarées par [`RouteGroups`](../../../src/Bdtheque.Api/Endpoints/RouteGroups.cs), chacune avec les réponses d'erreur qu'elle peut rendre (ProblemDetails) ; chaque ressource y ajoute ses endpoints, dans [`src/Bdtheque.Api/Endpoints/`](../../../src/Bdtheque.Api/Endpoints/).

| Famille | Usage | Appelée par |
| --- | --- | --- |
| `/admin` | Saisie et administration : fiches, listes, visuels en attente, paramétrage, journal des modifications, lecture de l'audience. | `frontend`, pour l'administrateur authentifié ; données de validation |
| `/catalog` | Consultation publique : fiches, listes, recherche avancée, médias des visuels, dashboard. | `frontend`, pour tout visiteur |
| `/audience` | Enregistrement de la mesure d'audience. | `frontend`, pour tout visiteur |

Les contrats JSON échangés sont les types de [`src/Bdtheque.Contracts/`](../../../src/Bdtheque.Contracts/), partagés par `api` et ses clients.

## Consulter le contrat

| Où | Quoi | Adresse |
| --- | --- | --- |
| Environnement de validation | Exploration interactive (Scalar) du commit validé | `http://localhost:8181/scalar/v1` |
| Environnement de validation | Description OpenAPI (JSON) du commit validé | `http://localhost:8181/openapi/v1.json` |
| Développement local (`Development`) | Les mêmes, sur le port de l'API lancée | `/scalar/v1`, `/openapi/v1.json` |
| Production | Ni l'une ni l'autre : `api` n'y est pas joignable | — |

Ces adresses sont servies par [`Program.cs` de l'API](../../../src/Bdtheque.Api/Program.cs) dans les seuls environnements `Development` et [`Validation`](../../../src/Bdtheque.Api/Hosting/ApiEnvironments.cs), et exemptées du secret interne. Pour essayer un appel dans Scalar, lui ajouter l'en-tête `X-Internal-Api-Key` avec la valeur `INTERNAL_API_KEY` de [`validation.env`](../../../docker/validation/validation.env).

La documentation technique, servie à côté en validation ([Déploiement et environnement de validation](../architecture/deploiement.md)), n'en présente que le fichier versionné (ci-dessous), sans exploration : celle-ci reste Scalar.

## Contrat versionné

[`openapi-v1.json`](openapi-v1.json) est le document OpenAPI 3.1 que produit l'API, tel que le sert `/openapi/v1.json`, sans sa liste `servers`, qui reprend l'adresse de la requête : il ne se modifie pas à la main. Un changement du contrat se relit ainsi dans le diff de sa Pull Request.

- **Génération** : [`OpenApiContractTests`](../../../tests/Bdtheque.Api.Tests/OpenApiContractTests.cs) démarre l'API dans son hôte de test, sur une base PostgreSQL migrée, et lui demande le document (`IOpenApiDocumentProvider`, document `v1`), sans requête HTTP. [`Update-TechnicalReferences.ps1`](../../../scripts/Update-TechnicalReferences.ps1) exécute ce test en lui faisant écrire le fichier : Docker est donc requis pour le régénérer, et `-NoOpenApiContract` l'omet.
- **Ordre** : celui que donne ASP.NET Core, sans tri ajouté : chemins dans l'ordre de déclaration des endpoints, schémas par nom, sans distinction de casse. Ni date, ni adresse, ni secret n'y figure ; deux générations donnent le même fichier.
- **Vérification** : le même test compare le document produit au fichier versionné et échoue s'ils diffèrent, ou si le fichier manque. Il s'exécute avec les tests de l'API, dans le job `Build & tests (.NET)`, dès qu'une source change ; une Pull Request qui ne modifie que ce fichier les déclenche aussi ([Intégration continue](../integration-et-tests/integration-continue.md#classement-des-fichiers-modifiés)). `Update-TechnicalReferences.ps1 -Check`, sans base, ne le compare pas.
