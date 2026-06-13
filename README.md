# BDthèque

Application web de gestion de collection de bandes dessinées — réécriture d'une application client lourd (Delphi / Firebird) en architecture n-tiers.

## Fonctionnalités principales

- Catalogue d'albums, séries et auteurs avec navigation inter-entités
- Gestion de la collection personnelle : éditions, modes d'acquisition, visuels
- Dashboard public avec statistiques de la collection
- Recherche simple et avancée (multi-entités)
- Détection des albums manquants dans une série
- Estimation de la valeur des éditions (Random Forest via ML.NET)
- Estimation de la date de sortie du prochain tome d'une série
- Gestion des intentions d'achat
- Interface d'administration (compte unique, authentification par cookie)

## Stack

| Composant | Technologie |
|---|---|
| Frontend | Blazor Server (.NET 10) |
| Backend API | ASP.NET Core Minimal API (.NET 10) |
| ORM | EF Core 10 + Npgsql |
| Base de données | PostgreSQL 17 |
| ML | ML.NET (embarqué dans l'API) |
| Conteneurisation | Docker Compose |

## Architecture

L'application est déployée en **3 conteneurs Docker** :

- `frontend` — Blazor Server (rendu des pages, interface utilisateur)
- `api` — Minimal API (logique métier, accès données, modèle ML)
- `db` — PostgreSQL (persistance)

Le `frontend` communique avec l'`api` via le réseau Docker interne. L'`api` est le seul tier à accéder à `db`.

## Déploiement

Le déploiement cible un **NAS Synology** via Docker Compose (compatible Synology Container Manager). Les visuels (couvertures, planches, etc.) sont stockés sur un volume Docker monté sur le NAS, configurable via variable d'environnement.

> **Note :** les fichiers Docker (`Dockerfile`, `docker-compose.yml`) ne sont pas encore dans le dépôt — cette section sera complétée lors de la mise en place de l'infrastructure.

## Spécifications

Les spécifications du projet sont dans le dossier [`.speckit/`](.speckit/) :

| Fichier | Contenu |
|---|---|
| [`fonctionnel.md`](.speckit/fonctionnel.md) | Fonctionnalités, règles métier, flux |
| [`modele-metier.md`](.speckit/modele-metier.md) | Entités, attributs, relations |
| [`contraintes-techniques.md`](.speckit/contraintes-techniques.md) | Stack, architecture, déploiement |
| [`gestion-projet.md`](.speckit/gestion-projet.md) | Branches, commits, CI/CD |
