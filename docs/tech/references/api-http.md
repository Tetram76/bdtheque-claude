---
title: 'Contrat HTTP de l''API'
description: 'Familles de routes, production du contrat OpenAPI et accès à son exploration interactive (Scalar) ; pour connaître un endpoint ou l''essayer en validation.'
code:
  - 'src/Bdtheque.Api/Program.cs'
  - 'src/Bdtheque.Api/Endpoints/**'
  - 'src/Bdtheque.Api/Hosting/**'
  - 'src/Bdtheque.Contracts/**'
---

# Contrat HTTP de l'API

Le contrat de l'API n'est écrit qu'une fois : par ses endpoints eux-mêmes. ASP.NET Core en produit la description OpenAPI à l'exécution, sans bibliothèque tierce ; aucune copie n'en est versionnée ici. Choix et raisons : [Documentation de l'API](../../../.speckit/choix-implementation/socle/transverse/documentation-de-l-api.md), [Organisation de l'API](../../../.speckit/choix-implementation/api/organisation-de-l-api.md).

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

La documentation technique, servie à côté en validation, ne reproduit pas ce contrat : elle y renvoie ([Déploiement et environnement de validation](../architecture/deploiement.md)).

## Non généré à ce jour

Une copie versionnée du contrat OpenAPI, régénérée et comparée par le CI, permettrait de relire dans une Pull Request ce qu'un changement fait au contrat. Elle n'est pas encore en place : [#493](https://github.com/Tetram76/bdtheque-claude/issues/493).
