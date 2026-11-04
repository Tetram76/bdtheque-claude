---
type: Contrainte technique
title: 'Architecture des tiers'
description: 'Socle de trois conteneurs (frontend, api, db), seul frontend exposé, seul api accède à db.'
applies_to:
  - 'docker-compose.yml'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Architecture des tiers

L'application repose sur un **socle de 3 conteneurs Docker**, fondation à laquelle d'autres conteneurs peuvent s'ajouter (ex. l'outil pour agents IA, cf. [Indépendance de l'outil pour agents IA](/contraintes-techniques/architecture-deploiement/independance-de-l-outil-pour-agents-ia.md)) :

| Conteneur | Rôle |
| --- | --- |
| `frontend` | UI et rendu des pages |
| `api` | API ASP.NET Core — logique métier, accès données, ML |
| `db` | PostgreSQL — persistance |

Les images de base des conteneurs sont des choix d'implémentation : cf. [Images de base des conteneurs](/choix-implementation/deploiement/images-de-base-des-conteneurs.md).

Seul le conteneur `frontend` est exposé : `api` n'est jamais exposé, le `frontend` l'appelle via HTTP interne (réseau Docker). Le conteneur `api` est le seul à accéder à `db`.
