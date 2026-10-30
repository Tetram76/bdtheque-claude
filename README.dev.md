# BDthèque — développement

Ce document s'adresse à qui maintient le projet ou souhaite rejoindre l'équipe. La présentation de l'application, son installation et son exploitation sont dans le [`README.md`](README.md).

## Spécifications

Le projet est piloté en **développement piloté par les spécifications** (*Spec-Driven Development*, SDD) : toute exigence est d'abord écrite dans la spécification, puis découpée en chantiers, puis implémentée ; le code n'en est que la mise en œuvre.

Les spécifications du projet, dans le dossier [`.speckit/`](.speckit/), constituent la **source de vérité absolue** du projet : toute décision d'architecture, de fonctionnel ou de gestion doit s'y conformer, et son contenu prime sur toute autre source (historique de discussion, suppositions, etc.).

Le développement est mené par un agent IA disposant d'une **autonomie gouvernée** : il décide librement, dans les limites des règles et contraintes imposées par le `.speckit/`. Il décide seul des choix techniques ; sur un point fonctionnel ou métier, il ne fait qu'appliquer ce que la spécification établit, l'utilisateur décidant de tout changement d'exigence et de toute ambiguïté. Les fichiers `.speckit/` sont son unique canal d'expression des besoins et exigences.

| Fichier | Contenu |
| --- | --- |
| [`fonctionnel/`](.speckit/fonctionnel/index.md) | Fonctionnalités, règles métier, flux |
| [`modele-metier.md`](.speckit/modele-metier.md) | Entités, attributs, relations |
| [`contraintes-techniques/`](.speckit/contraintes-techniques/index.md) | Stack, architecture, déploiement |
| [`choix-implementation/`](.speckit/choix-implementation/index.md) | Choix techniques retenus par l'agent de sa propre initiative |
| [`gestion-projet/`](.speckit/gestion-projet/index.md) | Branches, commits, CI/CD |
| [`suivi-implementation.md`](.speckit/suivi-implementation.md) | Avancement du plan d'implémentation (phases, PR, statut) |

## Organisation du code

```text
.
├── Bdtheque.slnx                  # Solution .NET
├── Directory.Build.props          # Propriétés MSBuild communes (TFM, nullable, analyzers…)
├── Directory.Packages.props       # Gestion centralisée des versions NuGet (CPM)
├── global.json                    # Version minimale du SDK .NET
├── docker-compose.yml             # Déploiement (images Docker Hub) et build local
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
└── .speckit/                      # Source de vérité du projet (cf. § Spécifications)
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
mkdir -p ./data/visuels && sudo chown 1654:1654 ./data/visuels   # cf. README.md, « Dossier des visuels »
docker compose up -d --build
```

`--build` construit les images depuis les sources (sous les mêmes noms que les images publiées, cf. [`README.md`](README.md#images-publiées)) au lieu de les télécharger.

L'application est ensuite accessible sur `http://localhost:8080` (port configurable via `FRONTEND_PORT` dans `.env`). L'état de chaque conteneur peut être vérifié via son endpoint `/health` (exposé uniquement en interne pour `api`, et sur le port publié pour `frontend`).

## Validation fonctionnelle

Les écrans d'une Pull Request sont validés sur le poste de l'utilisateur, avant sa revue, dans un environnement de validation : l'application complète, construite depuis les sources de la Pull Request, avec des données fictives qui couvrent les cas des écrans.

### Prérequis de la validation

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

## Publication des images

Les images de l'application (cf. [`README.md`](README.md#images-publiées)) sont publiées sur Docker Hub par le workflow [`docker-publish.yml`](.github/workflows/docker-publish.yml) : chaque nuit sous le tag `nightly`, si `main` a changé, et à la demande. Une publication à la demande se lance depuis l'onglet **Actions** du dépôt GitHub : workflow **Docker publish**, **Run workflow**, en choisissant la branche à construire ; lancée sur le tag d'une version, elle pose en outre le numéro de cette version.
