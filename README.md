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
| --- | --- |
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

Deux réseaux Docker isolent les tiers : `backend` (`db` ↔ `api`) et `frontend-net` (`api` ↔ `frontend`). Seul `frontend` publie un port sur l'hôte ; `api` et `db` ne sont jamais exposés à l'extérieur du réseau Docker.

## Organisation du code

```text
.
├── Bdtheque.slnx                  # Solution .NET
├── Directory.Build.props          # Propriétés MSBuild communes (TFM, nullable, analyzers…)
├── Directory.Packages.props       # Gestion centralisée des versions NuGet (CPM)
├── global.json                    # Version du SDK .NET
├── docker-compose.yml
├── docker/
│   ├── api/Dockerfile
│   └── frontend/Dockerfile
├── src/
│   ├── Bdtheque.Domain/           # Entités, enums, value objects — aucune dépendance externe
│   ├── Bdtheque.Contracts/        # DTOs échangés entre api et frontend
│   ├── Bdtheque.Infrastructure/   # DbContext EF Core, configurations, migrations
│   ├── Bdtheque.Api/              # Conteneur api : endpoints, sécurité, règles applicatives
│   └── Bdtheque.Frontend/         # Conteneur frontend : composants Blazor Server, auth cookie
├── tests/
│   └── Bdtheque.Api.Tests/        # Tests d'intégration (WebApplicationFactory + SQLite)
└── .speckit/                      # Source de vérité du projet (voir ci-dessous)
```

## Démarrage local

### Prérequis

- [.NET SDK 10](https://dotnet.microsoft.com/download) (version pilotée par [`global.json`](global.json))
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (pour `docker compose`)

### Build et tests

```bash
dotnet restore Bdtheque.slnx
dotnet build Bdtheque.slnx --configuration Release
dotnet test Bdtheque.slnx --configuration Release
```

### Lancer la stack complète (Docker Compose)

```bash
cp .env.example .env   # puis renseigner POSTGRES_PASSWORD et INTERNAL_API_KEY
docker compose up -d --build
```

L'application est ensuite accessible sur `http://localhost:8080` (port configurable via `FRONTEND_PORT` dans `.env`). L'état de chaque conteneur peut être vérifié via son endpoint `/health` (exposé uniquement en interne pour `api`, et sur le port publié pour `frontend`).

## Déploiement

Le déploiement cible un **NAS Synology** via Docker Compose (compatible Synology Container Manager). Les visuels (couvertures, planches, etc.) sont stockés sur un volume monté sur le NAS, dont le chemin hôte est configurable via la variable d'environnement `VISUELS_HOST_PATH`.

## Spécifications

Les spécifications du projet sont dans le dossier [`.speckit/`](.speckit/) constituent la **source de vérité absolue** du projet : toute décision d'architecture, de fonctionnel ou de gestion doit s'y conformer, et son contenu prime sur toute autre source (historique de discussion, suppositions, etc.).

Le développement est mené par un agent IA disposant d'une **autonomie totale** sur les choix techniques et fonctionnels ; les fichiers `.speckit/` sont son unique canal d'expression des besoins et exigences.

| Fichier | Contenu |
| --- | --- |
| [`fonctionnel.md`](.speckit/fonctionnel.md) | Fonctionnalités, règles métier, flux |
| [`modele-metier.md`](.speckit/modele-metier.md) | Entités, attributs, relations |
| [`contraintes-techniques.md`](.speckit/contraintes-techniques.md) | Stack, architecture, déploiement |
| [`choix-implementation.md`](.speckit/choix-implementation.md) | Choix techniques retenus par l'agent de sa propre initiative |
| [`gestion-projet.md`](.speckit/gestion-projet.md) | Branches, commits, CI/CD |
| [`suivi-implementation.md`](.speckit/suivi-implementation.md) | Avancement du plan d'implémentation (phases, PR, statut) |
