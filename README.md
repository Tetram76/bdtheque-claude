# BDthèque

[![CI](https://github.com/Tetram76/bdtheque-claude/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Tetram76/bdtheque-claude/actions/workflows/ci.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Tetram76_bdtheque-claude&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Tetram76_bdtheque-claude)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Tetram76_bdtheque-claude&metric=coverage)](https://sonarcloud.io/component_measures?id=Tetram76_bdtheque-claude&metric=coverage)

[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=Tetram76_bdtheque-claude&metric=reliability_rating)](https://sonarcloud.io/component_measures?id=Tetram76_bdtheque-claude&metric=reliability_rating)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=Tetram76_bdtheque-claude&metric=security_rating)](https://sonarcloud.io/component_measures?id=Tetram76_bdtheque-claude&metric=security_rating)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=Tetram76_bdtheque-claude&metric=sqale_rating)](https://sonarcloud.io/component_measures?id=Tetram76_bdtheque-claude&metric=sqale_rating)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=Tetram76_bdtheque-claude&metric=bugs)](https://sonarcloud.io/project/issues?id=Tetram76_bdtheque-claude&types=BUG&resolved=false)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=Tetram76_bdtheque-claude&metric=vulnerabilities)](https://sonarcloud.io/project/issues?id=Tetram76_bdtheque-claude&types=VULNERABILITY&resolved=false)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=Tetram76_bdtheque-claude&metric=code_smells)](https://sonarcloud.io/project/issues?id=Tetram76_bdtheque-claude&types=CODE_SMELL&resolved=false)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=Tetram76_bdtheque-claude&metric=duplicated_lines_density)](https://sonarcloud.io/component_measures?id=Tetram76_bdtheque-claude&metric=duplicated_lines_density)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=Tetram76_bdtheque-claude&metric=sqale_index)](https://sonarcloud.io/component_measures?id=Tetram76_bdtheque-claude&metric=sqale_index)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=Tetram76_bdtheque-claude&metric=ncloc)](https://sonarcloud.io/component_measures?id=Tetram76_bdtheque-claude&metric=ncloc)

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
├── global.json                    # Version minimale du SDK .NET
├── docker-compose.yml           # Déploiement (images Docker Hub) et build local
├── docker/
│   ├── api/Dockerfile
│   ├── frontend/Dockerfile
│   ├── validation/                # Environnement de validation fonctionnelle (surcharge Compose, valeurs)
│   └── validation-data/Dockerfile # Chargement des données de validation
├── scripts/                       # Relevé des dépendances, lancement et arrêt de la validation
├── src/
│   ├── Bdtheque.Domain/           # Entités, enums, value objects — aucune dépendance externe
│   ├── Bdtheque.Contracts/        # DTOs échangés entre api et frontend
│   ├── Bdtheque.Infrastructure/   # DbContext EF Core, configurations, migrations
│   ├── Bdtheque.Api/              # Conteneur api : endpoints, sécurité, règles applicatives
│   ├── Bdtheque.Frontend/         # Conteneur frontend : composants Blazor, auth cookie
│   ├── Bdtheque.HealthProbe/      # Sonde du bilan de santé des images (sans shell ni curl)
│   └── Bdtheque.ValidationData/   # Données fictives de l'environnement de validation, chargées par l'API
├── tests/
│   ├── Bdtheque.Domain.Tests/         # Tests unitaires du domaine
│   ├── Bdtheque.Infrastructure.Tests/ # Tests de persistance (migrations, contraintes) sur PostgreSQL
│   ├── Bdtheque.Api.Tests/            # Tests d'intégration de l'API (WebApplicationFactory + PostgreSQL)
│   ├── Bdtheque.Frontend.Tests/       # Tests des composants Blazor (bUnit)
│   ├── Bdtheque.EndToEnd.Tests/       # Tests de bout en bout dans Chromium (Playwright), application complète
│   ├── Bdtheque.ValidationData.Tests/ # Chargement des données de validation par l'API
│   └── Bdtheque.Testing/              # Support partagé : conteneur PostgreSQL de test, hôte de l'API
└── .speckit/                      # Source de vérité du projet (voir ci-dessous)
```

## Démarrage local

### Prérequis

- [.NET SDK 10](https://dotnet.microsoft.com/download) (version minimale fixée par [`global.json`](global.json))
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (pour `docker compose`)

### Build et tests

Docker doit être démarré : les tests de persistance, d'API et de bout en bout s'exécutent sur un conteneur PostgreSQL éphémère (Testcontainers). Les tests de bout en bout installent eux-mêmes, au premier lancement, le navigateur Chromium de Playwright qu'ils pilotent.

```bash
dotnet restore Bdtheque.slnx
dotnet build Bdtheque.slnx --configuration Release
dotnet test Bdtheque.slnx --configuration Release
```

### Captures de référence des écrans

Chaque écran est capturé dans chacun des thèmes et comparé, pixel par pixel, à sa capture de référence (`tests/Bdtheque.EndToEnd.Tests/Captures/References`). Le rendu des polices n'étant identique que dans un même environnement, la comparaison n'a lieu que sous Linux (CI) : ailleurs, ces tests sont ignorés, et le script suivant les exécute dans un conteneur Linux, sur la copie de travail. Une capture en écart est déposée, avec son image des différences, dans `tests/Bdtheque.EndToEnd.Tests/TestResults/captures`.

```bash
./scripts/Test-Captures.ps1
```

Un changement de rendu voulu met à jour les références, à relire avant de les committer :

```bash
./scripts/Test-Captures.ps1 -Update
```

### Lancer la stack complète depuis les sources (Docker Compose)

```bash
cp .env.example .env   # puis renseigner POSTGRES_PASSWORD et INTERNAL_API_KEY
mkdir -p ./data/visuels && sudo chown 1654:1654 ./data/visuels   # cf. « Dossier des visuels »
docker compose up -d --build
```

`--build` construit les images depuis les sources (sous les mêmes noms que les images publiées, cf. ci-dessous) au lieu de les télécharger.

L'application est ensuite accessible sur `http://localhost:8080` (port configurable via `FRONTEND_PORT` dans `.env`). L'état de chaque conteneur peut être vérifié via son endpoint `/health` (exposé uniquement en interne pour `api`, et sur le port publié pour `frontend`).

## Validation fonctionnelle

Les écrans d'une Pull Request sont validés sur le poste de l'utilisateur, avant sa revue, dans un environnement de validation : l'application complète, construite depuis les sources de la Pull Request, avec des données fictives qui couvrent les cas des écrans.

### Prérequis

- [PowerShell 7.3](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) ou plus récent (`pwsh`) ;
- git, avec ce dépôt cloné (le dépôt étant public, aucune authentification) ;
- un service Docker démarré : [Docker Desktop](https://www.docker.com/products/docker-desktop/) ou [Rancher Desktop](https://rancherdesktop.io/) (moteur `dockerd`).

Ni SDK .NET, ni fichier à éditer, ni secret, ni dossier à préparer : les images sont construites par Docker, et l'environnement a ses propres valeurs ([`docker/validation/validation.env`](docker/validation/validation.env)). Les scripts sont ceux de `main` : mettre à jour le dépôt (`git pull`) avant une validation.

### Lancer la version d'une Pull Request

Depuis la racine du dépôt, avec le numéro de la Pull Request :

```bash
pwsh ./scripts/Start-Validation.ps1 -PullRequest 245
```

Le premier lancement construit les images, en quelques minutes ; les suivants ne reconstruisent que ce que la Pull Request a changé. Une fois les conteneurs démarrés et les données chargées, le script affiche :

- l'application : `http://localhost:8180` ;
- la documentation de l'API (Scalar), pour information : `http://localhost:8181/scalar/v1`. Pour y essayer un appel, lui ajouter l'en-tête `X-Internal-Api-Key` avec la valeur de `INTERNAL_API_KEY` de `validation.env`.

Ces ports ne sont ouverts qu'au poste lui-même.

### Options

| Option | Effet |
| --- | --- |
| `-Empty` | Sans les données de validation : écran d'accueil d'une collection vide. |
| `-KeepData` | Conserve les données du lancement précédent (arrêté lui aussi avec `-KeepData`), au lieu de repartir d'une base vide. |
| `-Ref <commit>` | Au lieu d'une Pull Request, n'importe quel commit du dépôt : branche, tag, empreinte. Sans `-PullRequest` ni `-Ref`, le commit courant (`HEAD`), sans les modifications non commitées. |

### Remise à zéro et arrêt

Chaque lancement repart d'une base vide (sauf `-KeepData`) : relancer la commande remet l'environnement à zéro. Pour l'arrêter et supprimer ses conteneurs, ses réseaux, ses données (base et visuels) et les sources extraites pour lui :

```bash
pwsh ./scripts/Stop-Validation.ps1
```

Les images construites restent sur le poste, pour accélérer la validation suivante ; `docker image rm bdtheque-validation-api bdtheque-validation-frontend bdtheque-validation-data` les supprime.

## Déploiement

Le déploiement cible un **NAS Synology** via Docker Compose (Synology Container Manager). Il n'utilise pas les sources : les images de l'application sont téléchargées depuis **Docker Hub**.

### Images publiées

| Image | Conteneur |
| --- | --- |
| [`tetram76/bdtheque-api`](https://hub.docker.com/r/tetram76/bdtheque-api) | `api` |
| [`tetram76/bdtheque-frontend`](https://hub.docker.com/r/tetram76/bdtheque-frontend) | `frontend` |

Elles sont publiées par le workflow [`docker-publish.yml`](.github/workflows/docker-publish.yml), pour l'architecture du NAS (`linux/amd64`), avec les tags suivants :

| Tag | Contenu |
| --- | --- |
| `nightly` | Publication quotidienne, depuis la tête de `main` |
| `<branche>` (ex. `main`) | Dernière publication à la demande de cette branche |
| `sha-<commit>` | Première publication à la demande d'un commit précis, immuable (une nouvelle publication du même commit ne déplace que le tag de branche) |

Une publication à la demande se lance depuis l'onglet **Actions** du dépôt GitHub : workflow **Docker publish**, **Run workflow**, en choisissant la branche à construire.

Les dépôts sont privés : leur téléchargement exige une authentification (cf. procédure). Le tag déployé est choisi par `BDTHEQUE_TAG` dans `.env` (`nightly` par défaut).

### Procédure (Synology Container Manager)

1. Créer sur le NAS un dossier pour le projet (ex. `/volume1/docker/bdtheque`) et y copier [`docker-compose.yml`](docker-compose.yml) et [`.env.example`](.env.example), renommé en `.env`.
2. Renseigner `.env` : `POSTGRES_PASSWORD`, `INTERNAL_API_KEY` (ex. `openssl rand -base64 32`), `BDTHEQUE_TAG`, `FRONTEND_PORT`, `VISUELS_HOST_PATH` et, derrière le reverse proxy, `PUBLIC_URL` (ci-dessous).
3. Préparer le dossier des visuels (ci-dessous).
4. Les dépôts Docker Hub étant **privés**, authentifier le NAS auprès de Docker Hub avec le compte `tetram76` et un jeton d'accès en **lecture seule** (*Personal Access Token*, droits *Read-only*), sans quoi le téléchargement des images est refusé. En SSH : `sudo docker login -u tetram76` (le jeton tient lieu de mot de passe).
5. Dans Container Manager, créer un **projet** sur ce dossier, à partir de son `docker-compose.yml` : Container Manager télécharge les images et démarre les conteneurs. En ligne de commande (SSH), depuis ce dossier : `docker compose pull && docker compose up -d`.
6. Vérifier que l'application répond sur `http://<NAS>:<FRONTEND_PORT>/health`.

**Mise à jour** : modifier `BDTHEQUE_TAG` si besoin, puis télécharger les nouvelles images et recréer les conteneurs, en SSH depuis le dossier du projet : `docker compose pull && docker compose up -d`. Le schéma de la base est mis à jour automatiquement au démarrage d'`api` ; les données (volume `db-data`) et les visuels sont conservés.

**Mise à jour de l'image PostgreSQL** : une nouvelle image `postgres` peut embarquer une autre version d'ICU, qui définit le tri et la comparaison des textes. Au démarrage, `api` le détecte et reconstruit alors tous les index de la base, ce que son journal signale par un avertissement (`The version of the collations … changed`). Si un index unique ne peut pas être reconstruit — deux libellés de genre devenus égaux sous la nouvelle définition —, `api` refuse de démarrer et son journal nomme l'index en cause (`IX_Genres_Label`). Il faut alors renommer l'un des genres en double, puis redémarrer `api`. En SSH, depuis le dossier du projet :

```bash
docker compose exec db psql -U bdtheque -d bdtheque -c 'SELECT array_agg("Id"), array_agg("Label") FROM "Genres" GROUP BY "Label" HAVING count(*) > 1'
```

liste les genres devenus égaux, avec leurs identifiants (utilisateur et base selon `POSTGRES_USER` et `POSTGRES_DB`). Renommer l'un d'eux par son identifiant — la comparaison des libellés ignorant la casse et les accents, une condition sur le libellé les viserait tous —, par `UPDATE "Genres" SET "Label" = '…' WHERE "Id" = '…'`, puis `docker compose restart api`.

### Plusieurs versions sur le NAS

Le NAS fait tourner jusqu'à trois versions en même temps : la production, la version candidate (RC) et une version alpha ou bêta. Chacune est un **projet distinct**, déployé selon la procédure ci-dessus dans **son propre dossier** (ex. `/volume1/docker/bdtheque`, `/volume1/docker/bdtheque-rc`, `/volume1/docker/bdtheque-beta`), avec le même `docker-compose.yml` et son propre `.env` :

- `BDTHEQUE_TAG` : le numéro de la version en production (ex. `0.1.0`), `sha-<commit>` pour une pré-version ;
- `FRONTEND_PORT` : un port différent pour chaque version ;
- `VISUELS_HOST_PATH` : un dossier différent pour chaque version ;
- `PUBLIC_URL` : en production seulement ; les pré-versions sont atteintes en interne, sur `http://<NAS>:<FRONTEND_PORT>`.

Le nom du projet (celui du dossier) distingue les conteneurs, les réseaux et le volume `db-data` de chaque version : chacune a sa propre base. Une pré-version ne doit **jamais** partager la base ni le dossier des visuels de la production : elle migrerait la base de production à son démarrage, et sa réconciliation supprimerait les visuels qu'elle ne connaît pas. Pour la tester sur des données réelles, on lui donne une **copie** des données de production.

### HTTPS : reverse proxy du NAS

Le `frontend` ne sert que du HTTP : le chiffrement (HTTPS, certificats) est assuré par le **reverse proxy du NAS** (DSM), qui reçoit les requêtes en HTTPS et les relaie en HTTP au port `FRONTEND_PORT` du NAS. La même image sert deux déploiements :

- **derrière le reverse proxy** : `PUBLIC_URL` donne, dans `.env`, l'adresse sous laquelle le navigateur atteint l'application (ex. `https://bdtheque.example.com`, port compris s'il n'est pas celui du schéma, sans chemin : l'application est servie à la racine). Le `frontend` construit ses redirections sur cette adresse et marque ses cookies `Secure`. Une adresse mal formée, avec un chemin ou un autre schéma que `http` ou `https`, empêche le démarrage du `frontend`, qui le signale dans son journal ;
- **en interne**, sans reverse proxy (versions à valider ou à tester) : `PUBLIC_URL` reste vide, et l'application est accessible directement sur `http://<NAS>:<FRONTEND_PORT>`, sans rien d'autre à configurer.

Le `frontend` ne redirige jamais vers HTTPS et n'envoie pas d'en-tête HSTS : une éventuelle politique HSTS se règle sur le reverse proxy.

### Dossier des visuels

Les visuels (couvertures, planches, etc.) sont stockés sur un dossier du NAS, dont le chemin hôte est configurable via la variable d'environnement `VISUELS_HOST_PATH`. Il n'est monté que dans le conteneur `api`, qui sert les visuels au `frontend`.

Ce dossier doit exister et être **accessible en écriture pour l'utilisateur du conteneur `api`** (UID `1654`, qui n'est pas root) ; sans quoi `api` refuse de démarrer, en indiquant le dossier en cause dans son journal. Docker crée un dossier absent au nom de root : il faut donc le créer et lui donner ce droit avant le premier démarrage, par exemple :

```bash
mkdir -p ./data/visuels && sudo chown 1654:1654 ./data/visuels
```

Sur un NAS Synology, le droit peut aussi être accordé au dossier partagé par ses permissions (ACL). Si aucun dossier de l'hôte n'est monté, `api` le signale par un avertissement : les visuels seraient perdus à chaque recréation du conteneur.

## Spécifications

Le projet est piloté en **développement piloté par les spécifications** (*Spec-Driven Development*, SDD) : toute exigence est d'abord écrite dans la spécification, puis découpée en chantiers, puis implémentée ; le code n'en est que la mise en œuvre.

Les spécifications du projet sont dans le dossier [`.speckit/`](.speckit/) constituent la **source de vérité absolue** du projet : toute décision d'architecture, de fonctionnel ou de gestion doit s'y conformer, et son contenu prime sur toute autre source (historique de discussion, suppositions, etc.).

Le développement est mené par un agent IA disposant d'une **autonomie gouvernée** : il décide librement, dans les limites des règles et contraintes imposées par le `.speckit/`. Il décide seul des choix techniques ; sur un point fonctionnel ou métier, il ne fait qu'appliquer ce que la spécification établit, l'utilisateur décidant de tout changement d'exigence et de toute ambiguïté. Les fichiers `.speckit/` sont son unique canal d'expression des besoins et exigences.

| Fichier | Contenu |
| --- | --- |
| [`fonctionnel/`](.speckit/fonctionnel/index.md) | Fonctionnalités, règles métier, flux |
| [`modele-metier.md`](.speckit/modele-metier.md) | Entités, attributs, relations |
| [`contraintes-techniques/`](.speckit/contraintes-techniques/index.md) | Stack, architecture, déploiement |
| [`choix-implementation/`](.speckit/choix-implementation/index.md) | Choix techniques retenus par l'agent de sa propre initiative |
| [`gestion-projet/`](.speckit/gestion-projet/index.md) | Branches, commits, CI/CD |
| [`suivi-implementation.md`](.speckit/suivi-implementation.md) | Avancement du plan d'implémentation (phases, PR, statut) |
