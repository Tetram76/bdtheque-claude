# BDthèque — développement

Ce document s'adresse à qui maintient le projet ou souhaite rejoindre l'équipe, ou veut valider une version avant de l'installer en production (§ Plusieurs versions sur le NAS). La présentation de l'application, son installation et son exploitation sont dans le [`README.md`](README.md).

## Spécifications

Le projet est piloté en **développement piloté par les spécifications** (*Spec-Driven Development*, SDD) : toute exigence est d'abord écrite dans la spécification, puis découpée en chantiers, puis implémentée ; le code n'en est que la mise en œuvre.

Les spécifications du projet, dans le dossier [`.speckit/`](.speckit/), constituent la **source de vérité absolue** du projet : toute décision d'architecture, de fonctionnel ou de gestion doit s'y conformer, et son contenu prime sur toute autre source (historique de discussion, suppositions, etc.).

Le développement est mené par un agent IA disposant d'une **autonomie gouvernée** : il décide librement, dans les limites des règles et contraintes imposées par le `.speckit/`. Il décide seul des choix techniques ; sur un point fonctionnel ou métier, il ne fait qu'appliquer ce que la spécification établit, l'utilisateur décidant de tout changement d'exigence et de toute ambiguïté. Les fichiers `.speckit/` sont son unique canal d'expression des besoins et exigences.

Le `.speckit/` est écrit au format [OKF](https://github.com/GoogleCloudPlatform/open-knowledge-format/blob/main/SPEC.md) (*Open Knowledge Format*) : des **concepts**, un par fichier Markdown, chacun avec un en-tête qui le décrit, et une **carte**, l'`index.md` de chaque dossier, qui liste ses concepts avec leur description. L'agent lit la carte et les règles qui valent pour tout chantier, puis les seuls concepts qu'exige le chantier en cours ([`AGENTS.md`](AGENTS.md)). `SpeckitStructureTests` vérifie la structure du bundle, et [`scripts/Get-SpeckitConcepts.ps1`](scripts/Get-SpeckitConcepts.ps1) liste les concepts qui régissent les fichiers modifiés d'une branche.

| Dossier | Contenu |
| --- | --- |
| [`fonctionnel/`](.speckit/fonctionnel/index.md) | Fonctionnalités, règles métier, flux |
| [`modele-metier/`](.speckit/modele-metier/index.md) | Entités, attributs, relations |
| [`contraintes-techniques/`](.speckit/contraintes-techniques/index.md) | Stack, architecture, déploiement |
| [`choix-implementation/`](.speckit/choix-implementation/index.md) | Choix techniques retenus par l'agent de sa propre initiative |
| [`gestion-projet/`](.speckit/gestion-projet/index.md) | Branches, commits, CI/CD |
| [`suivi-implementation/`](.speckit/suivi-implementation/index.md) | Avancement du plan d'implémentation (phases, PR, statut) |
| [`visuel/`](.speckit/visuel/index.md) | Charte graphique, présentation des écrans, maquettes |

## Documentation technique

La spécification dit ce que l'application doit être ; la **documentation technique**, dans [`docs/tech/`](docs/tech/index.md), décrit comment elle est réellement construite : architecture, mécanismes, déploiement, références générées depuis le code, avec leurs diagrammes (Mermaid, que GitHub affiche). Elle se lit à partir de sa [carte](docs/tech/index.md), et se tient à jour dans la même Pull Request que le code qu'elle décrit ; sa forme, ses contrôles et leurs limites sont dans [Consulter, construire, vérifier et maintenir](docs/tech/documentation/consulter-et-maintenir.md).

Son site (navigation, recherche, diagrammes), celui de `main`, est publié à chaque fusion sur <https://tetram76.github.io/bdtheque-claude/>. Celui de la copie de travail se construit et se consulte localement, sur `http://localhost:8182` :

```bash
pwsh ./scripts/Build-TechnicalDocumentation.ps1 -Serve
```

Avant de pousser, la liste des pages que les changements de la branche peuvent rendre fausses :

```bash
pwsh ./scripts/Get-TechnicalDocumentationImpact.ps1
```

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
│   ├── validation-data/Dockerfile # Chargement des données de validation
│   └── validation-docs/Dockerfile # Site de la documentation technique, en validation
├── docs/tech/                     # Documentation technique (cf. § Documentation technique)
├── scripts/                       # Relevé des dépendances, validation, documentation technique
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
│   ├── Bdtheque.Documentation.Tests/  # Site de la documentation technique, rendu de ses diagrammes, scripts
│   └── Bdtheque.Testing/              # Support partagé : conteneur PostgreSQL de test, hôte de l'API
└── .speckit/                      # Source de vérité du projet (cf. § Spécifications)
```

## Démarrage local

### Prérequis

- [.NET SDK 10](https://dotnet.microsoft.com/download) (version minimale fixée par [`global.json`](global.json))
- un service Docker compatible, avec `docker compose` : [Docker Desktop](https://www.docker.com/products/docker-desktop/), [Rancher Desktop](https://rancherdesktop.io/) (moteur `dockerd`), Docker Engine sous Linux ou toute autre implémentation compatible

### Build et tests

Docker doit être démarré : les tests de persistance, d'API et de bout en bout s'exécutent sur un conteneur PostgreSQL éphémère (Testcontainers). Les tests de bout en bout installent eux-mêmes, au premier lancement, le navigateur Chromium de Playwright qu'ils pilotent.

```bash
dotnet restore Bdtheque.slnx
dotnet build Bdtheque.slnx --configuration Release --no-restore
dotnet test Bdtheque.slnx --configuration Release --no-build
```

Chaque commande reprend ce que la précédente a produit, comme le CI : la compilation ne refait pas la restauration (`--no-restore`), et les tests ne refont ni la compilation ni la restauration (`--no-build`). Lancé seul, sans ces options, `dotnet test` restaure et compile lui-même.

Un test de bout en bout s'exécute dans chacun des thèmes proposés, jamais dans celui que tirerait l'application. La variable d'environnement `BDTHEQUE_TEST_THEME` limite une exécution à un seul thème, comme le fait chaque job de bout en bout du CI : le filtre `Themes=Each` n'en garde alors que les tests de ce thème, les autres (tirage du thème, tests propres à un thème) s'exécutant sans lui.

```bash
BDTHEQUE_TEST_THEME=planche dotnet test tests/Bdtheque.EndToEnd.Tests --configuration Release --filter "Themes=Each"
```

### Captures de référence des écrans

Chaque écran est capturé dans chacun des thèmes et comparé, pixel par pixel, à sa capture de référence (`tests/Bdtheque.EndToEnd.Tests/Captures/References`). Le rendu des polices n'étant identique que dans un même environnement, la comparaison n'a lieu que sous Linux (CI) : ailleurs, ces tests sont ignorés, et le script suivant les exécute dans un conteneur Linux, sur la copie de travail. Une capture en écart est déposée, avec son image des différences, dans `tests/Bdtheque.EndToEnd.Tests/TestResults/captures`.

```bash
./scripts/Test-Captures.ps1
```

Un changement de rendu voulu met à jour les références, à relire avant de les committer ; si la construction ou un test échoue, aucune n'est remplacée, et le script le dit :

```bash
./scripts/Test-Captures.ps1 -Update
```

L'une comme l'autre peut se limiter aux captures dont le nom du test contient l'un des textes donnés (un écran, une classe de captures, un thème), les autres références restant telles quelles :

```bash
./scripts/Test-Captures.ps1 -Update -Filter admin-logout, AdminHome
```

Le script est borné : un test bloqué plus d'une minute arrête le passage, et le conteneur est arrêté s'il n'a pas fini en six minutes (un passage complet en dure près de quatre sur quatre processeurs) ; un tel arrêt signale d'abord un moteur Docker à vérifier.

Derrière un proxy qui signe de nouveau le HTTPS (session cloud de Claude Code, proxy d'entreprise), le conteneur doit faire confiance à son certificat pour restaurer les packages NuGet. Il suffit que la variable `SSL_CERT_FILE` du poste désigne le fichier des autorités de certification auxquelles il fait confiance, celle du proxy comprise, ce qu'une session cloud fait déjà : le script le transmet au conteneur. Sans cette variable, rien ne change.

### Lancer la stack complète depuis les sources (Docker Compose)

```bash
cp .env.example .env   # puis renseigner POSTGRES_PASSWORD, INTERNAL_API_KEY, ADMIN_LOGIN et ADMIN_PASSWORD
mkdir -p ./data/visuels && sudo chown 1654:1654 ./data/visuels   # cf. README.md, « Dossier des visuels »
docker compose up -d --build
```

`--build` construit les images depuis les sources (sous les mêmes noms que les images publiées, cf. [`README.md`](README.md#images-publiées)) au lieu de les télécharger.

L'application est ensuite accessible sur `http://localhost:8080` (port configurable via `FRONTEND_PORT` dans `.env`). L'état de chaque conteneur peut être vérifié via son endpoint `/health` (exposé uniquement en interne pour `api`, et sur le port publié pour `frontend`).

## Validation fonctionnelle

Les écrans d'une Pull Request sont validés sur le poste de l'utilisateur, avant sa revue, dans un environnement de validation : l'application complète, construite depuis les sources de la Pull Request, avec des données fictives qui couvrent les cas des écrans.

### Prérequis de la validation

- [PowerShell 7.4](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) ou plus récent (`pwsh`) ;
- git, avec ce dépôt cloné (le dépôt étant public, aucune authentification) ;
- un service Docker compatible démarré, avec `docker compose` : [Docker Desktop](https://www.docker.com/products/docker-desktop/), [Rancher Desktop](https://rancherdesktop.io/) (moteur `dockerd`), Docker Engine sous Linux ou toute autre implémentation compatible.

Ni SDK .NET, ni fichier à éditer, ni secret, ni dossier à préparer : les images sont construites par Docker, et l'environnement a ses propres valeurs ([`docker/validation/validation.env`](docker/validation/validation.env)). Les scripts sont ceux de `main` : mettre à jour le dépôt (`git pull`) avant une validation.

### Lancer la version d'une Pull Request

Depuis la racine du dépôt, avec le numéro de la Pull Request :

```bash
pwsh ./scripts/Start-Validation.ps1 -PullRequest 245
```

Le premier lancement construit les images, en quelques minutes ; les suivants ne reconstruisent que ce que la Pull Request a changé. Une fois les conteneurs démarrés et les données chargées, le script affiche :

- l'application : `http://localhost:8180` ;
- la documentation de l'API (Scalar), pour information : `http://localhost:8181/scalar/v1`. Pour y essayer un appel, lui ajouter l'en-tête `X-Internal-Api-Key` avec la valeur de `INTERNAL_API_KEY` de `validation.env` ;
- la documentation technique du commit validé, dont l'empreinte figure au pied de chaque page : `http://localhost:8182/`.

Ces ports ne sont ouverts qu'au poste lui-même.

L'administration s'ouvre avec le compte fictif de `validation.env` : identifiant `admin`, mot de passe `validation`, que le script affiche aussi.

Le thème, tiré au hasard à chaque visite en production, se choisit ici par l'adresse : `?theme=base`, `?theme=planche`, `?theme=gazette` ou `?theme=couverture` (ex. `http://localhost:8180/?theme=planche`). Il reste celui de la visite pour les pages suivantes, jusqu'au prochain choix.

### Options

| Option | Effet |
| --- | --- |
| `-Empty` | Sans les données de validation : écran d'accueil d'une collection vide. |
| `-KeepData` | Conserve les données du lancement précédent (arrêté lui aussi avec `-KeepData`), au lieu de repartir d'une base vide. |
| `-PaleCoverSpotlight` | Met à la une la couverture pâle des données de validation, cas le plus exigeant du thème `couverture`, au lieu de l'édition que tire l'application au premier affichage de la journée parmi toutes les éditions possédées. Incompatible avec `-Empty` et `-KeepData`, qui ne chargent pas les données. |
| `-Ref <commit>` | Au lieu d'une Pull Request, n'importe quel commit du dépôt : branche, tag, empreinte. Sans `-PullRequest` ni `-Ref`, le commit courant (`HEAD`), sans les modifications non commitées. |

### Un seul environnement sur le poste

Le poste n'a qu'un environnement de validation, quel que soit le clone ou le worktree du dépôt depuis lequel il est lancé : ses ports, ses images et ses sources extraites sont les mêmes pour tous. Pour que deux commandes ne se mélangent pas :

- un lancement ou un arrêt est refusé tant qu'un autre lancement ou arrêt est en cours ; le message dit lequel, depuis quel dossier et depuis quand, et il suffit de relancer la commande une fois celui-ci terminé ;
- un lancement est refusé tant que l'environnement exécute une autre version (une autre Pull Request, ou un autre commit avec `-Ref`) ; le message la nomme. Il faut d'abord l'arrêter, une fois sa validation terminée, puis relancer la commande.

### Remise à zéro et arrêt

Chaque lancement repart d'une base vide (sauf `-KeepData`) : relancer la commande pour la même Pull Request, même après de nouveaux commits, remet l'environnement à zéro. Pour l'arrêter et supprimer ses conteneurs, ses réseaux, ses données (base et visuels) et les sources extraites pour lui :

```bash
pwsh ./scripts/Stop-Validation.ps1
```

Le script affiche la version qu'il a arrêtée, qui peut avoir été lancée depuis un autre clone ou worktree.

Les images construites restent sur le poste, pour accélérer la validation suivante ; `docker image rm bdtheque-validation-api bdtheque-validation-frontend bdtheque-validation-data bdtheque-validation-docs` les supprime.

## Publication des images

Les images de l'application sont publiées sur Docker Hub par le workflow [`docker-publish.yml`](.github/workflows/docker-publish.yml) de deux façons : chaque nuit, sous le tag `nightly`, si `main` a changé depuis la publication précédente ; ou à la demande, sous les tags décrits dans le [`README.md`](README.md#images-publiées). Une publication à la demande se lance depuis l'onglet **Actions** du dépôt GitHub : workflow **Docker publish**, **Run workflow**, en choisissant la branche à construire, ou le tag d'une version (`v<version>`, ex. `v0.1.0`) pour publier cette version : les images reçoivent alors son numéro (`0.1.0`) au lieu d'un tag de branche. Comme `sha-<commit>`, ce numéro est immuable : une nouvelle publication de la même version le laisse tel quel. Le workflow refuse tout autre tag, de même que le tag d'une version `0.0.x`, qui n'a pas d'images.

Publiée sur le tag d'une version, la paire d'images est aussi poussée sur GitHub Container Registry (`ghcr.io/tetram76/bdtheque-api`, `ghcr.io/tetram76/bdtheque-frontend`), sous le seul numéro de la version, et le paquet de déploiement `bdtheque-<version>.zip` est joint à la release de la version, qui doit donc exister avant cette publication. Le paquet est construit par [`New-DeploymentPackage.ps1`](.github/scripts/New-DeploymentPackage.ps1), qui peut se lancer sur le poste pour l'examiner, avec un fichier de notes de release : `pwsh .github/scripts/New-DeploymentPackage.ps1 -Version 0.1.0 -ReleaseNotes ./notes.md -OutputDirectory ./paquet`. Le paquet reprend les notes de la release telles que publiées : une modification ultérieure des notes demande de relancer la publication sur le tag, qui remplace le paquet. Un paquet ghcr.io est créé privé à sa première publication : il est rendu public une fois pour toutes, sur sa page de réglages (*Package settings*, *Change visibility*).

## Publication d'une version

Une version est publiable quand tous les tickets de son jalon sont fermés ; elle n'est publiée que publiable, et sur la décision du propriétaire du dépôt. Publier une version qui ne l'est pas demande de redéfinir d'abord le contenu de son jalon, en reportant ses tickets ouverts à une autre version. Les règles qui suivent sont celles de [Versions et releases](.speckit/gestion-projet/depot/versions-et-releases.md). Les étapes se suivent dans cet ordre, chacune ne commençant qu'une fois la précédente réussie. Les exemples publient `v0.1.0`, dont la version précédente est `v0.0.3`.

1. **Vérifier que la version est publiable**, et que le CI de `main` est vert sur le commit à publier :

   ```powershell
   gh issue list --milestone v0.1.0 --state open
   gh run list --branch main --limit 3
   ```

2. **Poser le tag**, annoté et signé avec la clé du dépôt ([Signature des commits](.speckit/gestion-projet/depot/signature-des-commits.md)), sur le commit de `main` qui complète la version, puis le pousser :

   ```powershell
   git fetch origin
   git tag -s v0.1.0 -m "v0.1.0 — Phase 3, partie 1 : éléments de base du frontend" origin/main
   git push origin v0.1.0
   ```

   Sur GitHub, le tag doit apparaître « Verified ».

3. **Créer la release**, d'abord en brouillon. Une version `0.0.x` est une pré-release (`--prerelease`) ; une version `0.x` à partir de `0.1.0` n'en est pas une, mais reste une version 0. Les notes, écrites dans un fichier `notes.md`, suivent l'ordre que fixe [Versions et releases](.speckit/gestion-projet/depot/versions-et-releases.md) :
   - la présentation de la version, qui annonce une version 0 et toute rupture de compatibilité ;
   - l'encadré `> [!WARNING]` d'une rupture ;
   - la compatibilité fonctionnelle, d'hébergement et de déploiement avec les versions précédentes, et la montée de version, d'après ce qui a changé depuis le tag de la version précédente (`git diff v0.0.3 v0.1.0 -- docker-compose.yml .env.example .github/deployment docker` et migrations de la base pour l'hébergement et le déploiement) ;
   - la liste des changements, rédigée à partir des commits de la version (`git log --first-parent --format=%s v0.0.3..v0.1.0`), regroupée par nature, les changements qui vont ensemble réunis en un point (une Pull Request qui ajuste un point d'une précédente de la même version ne fait qu'une description avec elle), sans citer ni ticket ni Pull Request, et limitée à ce que peuvent voir l'utilisateur final, de l'application, et celui qui la déploie, de son hébergement et de son déploiement, dans une partie à part, sans le fonctionnement interne (spécification, outillage, CI, tests…) ;
   - l'historique : les notes des releases précédentes, de la plus récente à la plus ancienne (`gh release view v0.0.3 --json body --jq .body`), pour qui saute plusieurs versions.

   ```powershell
   gh release create v0.1.0 --draft --verify-tag --title "v0.1.0 — Phase 3, partie 1" --notes-file notes.md
   ```

   Relire les notes du brouillon, qui ne doivent mentionner Codex nulle part ([Budget IA](.speckit/gestion-projet/gouvernance/budget-ia.md)), et les soumettre à la validation du propriétaire du dépôt. Une fois qu'il les a validées, publier la release : `gh release edit v0.1.0 --draft=false`.

4. **Relancer l'analyse SonarQube Cloud de `main`**, pour que la période du nouveau code commence au commit publié : `gh workflow run ci.yml --ref main`.

5. **Publier les images de la version** sur Docker Hub et sur `ghcr.io`, ce qui joint aussi le paquet de déploiement à la release : `gh workflow run docker-publish.yml --ref v0.1.0`. À la toute première publication sur `ghcr.io`, chaque paquet (`bdtheque-api`, `bdtheque-frontend`) est créé privé : le rendre public sur sa page de réglages GitHub (*Package settings*, *Change visibility*), une fois pour toutes. La release doit alors proposer `bdtheque-0.1.0.zip`.

6. **Fermer le jalon** de la version : `gh api -X PATCH repos/Tetram76/bdtheque-claude/milestones/<numéro> -f state=closed`.

Le déploiement de la version reste ensuite une décision distincte.

**Version corrective d'une version dépassée** : le projet suit le modèle *trunk-based* ([Stratégie de branches](.speckit/gestion-projet/depot/strategie-de-branches.md)). Si `main` a déjà avancé au-delà de la version à corriger, une branche `release/<majeur>.<mineur>` est créée depuis son tag (`git switch -c release/0.1 v0.1.0`), une seule fois. Le correctif est fait sur `main` par sa Pull Request, puis reporté sur la branche (`git cherry-pick <commit>`), qui reçoit le tag de la version corrective (`v0.1.1`) ; les étapes ci-dessus s'appliquent ensuite, la liste des changements se calculant entre `v0.1.0` et `v0.1.1`.

## Plusieurs versions sur le NAS

Le NAS fait tourner jusqu'à trois versions en même temps : la production, la version candidate (RC) et une version de développement (alpha ou bêta). Chacune est un **projet distinct**, déployé selon la procédure du [`README.md`](README.md#procédure-synology-container-manager) dans **son propre dossier** (ex. `/volume1/docker/bdtheque`, `/volume1/docker/bdtheque-rc`, `/volume1/docker/bdtheque-dev`), avec le `docker-compose.yml` de **sa** version (celui du dépôt au tag de la version, ou au commit de `sha-<commit>`), qui peut différer d'une version à l'autre, et son propre `.env` :

- `BDTHEQUE_TAG` : le numéro de la version en production (ex. `0.1.0`), `sha-<commit>` pour une pré-version ;
- `FRONTEND_PORT` : un port différent pour chaque version ;
- `VISUELS_HOST_PATH` : un dossier différent pour chaque version ;
- `PUBLIC_URL` : en production seulement ; les pré-versions sont atteintes en interne, sur `http://<NAS>:<FRONTEND_PORT>`.

Le nom du projet (celui du dossier) distingue les conteneurs, les réseaux et le volume `db-data` de chaque version : chacune a sa propre base. Une pré-version ne doit **jamais** utiliser comme les siens la base ni le dossier des visuels de la production (au plus y lire, pour en récupérer une copie) : elle migrerait la base de production à son démarrage, et sa réconciliation supprimerait les visuels qu'elle ne connaît pas. Pour la tester sur des données réelles, on lui donne une **copie** des données de production (récupération automatisée à venir, #267).
