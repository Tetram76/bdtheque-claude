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
| Frontend | Blazor, rendu côté serveur (.NET 10) |
| Backend API | ASP.NET Core Minimal API (.NET 10) |
| ORM | EF Core 10 + Npgsql |
| Base de données | PostgreSQL 18 |
| ML | ML.NET (embarqué dans l'API) |
| Conteneurisation | Docker Compose |

## Architecture

L'application est déployée en **3 conteneurs Docker** :

- `frontend` — Blazor, rendu côté serveur (pages, interface utilisateur)
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
├── docker-compose.yml           # Déploiement (images Docker Hub) et build local
├── docker/
│   ├── api/Dockerfile
│   └── frontend/Dockerfile
├── src/
│   ├── Bdtheque.Domain/           # Entités, enums, value objects — aucune dépendance externe
│   ├── Bdtheque.Contracts/        # DTOs échangés entre api et frontend
│   ├── Bdtheque.Infrastructure/   # DbContext EF Core, configurations, migrations
│   ├── Bdtheque.Api/              # Conteneur api : endpoints, sécurité, règles applicatives
│   └── Bdtheque.Frontend/         # Conteneur frontend : composants Blazor, auth cookie
├── tests/
│   ├── Bdtheque.Domain.Tests/         # Tests unitaires du domaine
│   ├── Bdtheque.Infrastructure.Tests/ # Tests de persistance (migrations, contraintes) sur PostgreSQL
│   ├── Bdtheque.Api.Tests/            # Tests d'intégration de l'API (WebApplicationFactory + PostgreSQL)
│   └── Bdtheque.Testing/              # Support partagé : conteneur PostgreSQL de test (Testcontainers)
└── .speckit/                      # Source de vérité du projet (voir ci-dessous)
```

## Démarrage local

### Prérequis

- [.NET SDK 10](https://dotnet.microsoft.com/download) (version pilotée par [`global.json`](global.json))
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (pour `docker compose`)

### Build et tests

Docker doit être démarré : les tests de persistance et d'API s'exécutent sur un conteneur PostgreSQL éphémère (Testcontainers).

```bash
dotnet restore Bdtheque.slnx
dotnet build Bdtheque.slnx --configuration Release
dotnet test Bdtheque.slnx --configuration Release
```

### Lancer la stack complète depuis les sources (Docker Compose)

```bash
cp .env.example .env   # puis renseigner POSTGRES_PASSWORD et INTERNAL_API_KEY
mkdir -p ./data/visuels && sudo chown 1654:1654 ./data/visuels   # cf. « Dossier des visuels »
docker compose up -d --build
```

`--build` construit les images depuis les sources (sous les mêmes noms que les images publiées, cf. ci-dessous) au lieu de les télécharger.

L'application est ensuite accessible sur `http://localhost:8080` (port configurable via `FRONTEND_PORT` dans `.env`). L'état de chaque conteneur peut être vérifié via son endpoint `/health` (exposé uniquement en interne pour `api`, et sur le port publié pour `frontend`).

## Déploiement

Le déploiement cible un **NAS Synology** via Docker Compose (Synology Container Manager). Il n'utilise pas les sources : les images de l'application sont téléchargées depuis **Docker Hub**.

### Images publiées

| Image | Conteneur |
| --- | --- |
| [`tetram76/bdtheque-api`](https://hub.docker.com/r/tetram76/bdtheque-api) | `api` |
| [`tetram76/bdtheque-frontend`](https://hub.docker.com/r/tetram76/bdtheque-frontend) | `frontend` |

Elles sont publiées par le workflow [`docker-publish.yml`](.github/workflows/docker-publish.yml), pour les architectures `linux/amd64` et `linux/arm64`, avec les tags suivants :

| Tag | Contenu |
| --- | --- |
| `latest` | Dernière release |
| `X.Y.Z`, `X.Y` | Release `vX.Y.Z` |
| `main` | État courant de la branche `main` (hors release) |
| `sha-<commit>` | Commit précis, immuable |

Les dépôts sont privés : leur téléchargement exige une authentification (cf. procédure). Le tag déployé est choisi par `BDTHEQUE_TAG` dans `.env` (`latest` par défaut). Tant qu'aucune release n'a été publiée, seul `main` existe.

### Procédure (Synology Container Manager)

1. Créer sur le NAS un dossier pour le projet (ex. `/volume1/docker/bdtheque`) et y copier [`docker-compose.yml`](docker-compose.yml) et [`.env.example`](.env.example), renommé en `.env`.
2. Renseigner `.env` : `POSTGRES_PASSWORD`, `INTERNAL_API_KEY` (ex. `openssl rand -base64 32`), `BDTHEQUE_TAG`, `FRONTEND_PORT` et `VISUELS_HOST_PATH`.
3. Préparer le dossier des visuels (ci-dessous).
4. Les dépôts Docker Hub étant **privés**, authentifier le NAS auprès de Docker Hub avec le compte `tetram76` et un jeton d'accès en **lecture seule** (*Personal Access Token*, droits *Read-only*), sans quoi le téléchargement des images est refusé. En SSH : `sudo docker login -u tetram76` (le jeton tient lieu de mot de passe).
5. Dans Container Manager, créer un **projet** sur ce dossier, à partir de son `docker-compose.yml` : Container Manager télécharge les images et démarre les conteneurs. En ligne de commande (SSH), depuis ce dossier : `docker compose pull && docker compose up -d`.
6. Vérifier que l'application répond sur `http://<NAS>:<FRONTEND_PORT>/health`.

**Mise à jour** : modifier `BDTHEQUE_TAG` si besoin, puis télécharger les nouvelles images et recréer les conteneurs, en SSH depuis le dossier du projet : `docker compose pull && docker compose up -d`. Le schéma de la base est mis à jour automatiquement au démarrage d'`api` ; les données (volume `db-data`) et les visuels sont conservés.

### Dossier des visuels

Les visuels (couvertures, planches, etc.) sont stockés sur un dossier du NAS monté dans les conteneurs, dont le chemin hôte est configurable via la variable d'environnement `VISUELS_HOST_PATH`.

Ce dossier doit exister et être **accessible en écriture pour l'utilisateur du conteneur `api`** (UID `1654`, qui n'est pas root) ; sans quoi `api` refuse de démarrer, en indiquant le dossier en cause dans son journal. Docker crée un dossier absent au nom de root : il faut donc le créer et lui donner ce droit avant le premier démarrage, par exemple :

```bash
mkdir -p ./data/visuels && sudo chown 1654:1654 ./data/visuels
```

Sur un NAS Synology, le droit peut aussi être accordé au dossier partagé par ses permissions (ACL). Si aucun dossier de l'hôte n'est monté, `api` le signale par un avertissement : les visuels seraient perdus à chaque recréation du conteneur.

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
