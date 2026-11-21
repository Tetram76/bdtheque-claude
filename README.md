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

Ce document présente l'application, son installation et son exploitation. Pour maintenir le projet ou rejoindre l'équipe (organisation du code, développement local, tests, validation fonctionnelle, spécifications), ou pour valider une version avant de l'installer, voir [`README.dev.md`](README.dev.md).

## Versions

Les versions publiées, leurs notes et leur paquet de déploiement sont sur la [page des releases](https://github.com/Tetram76/bdtheque-claude/releases). Jusqu'à la version 1.0.0, l'application est en **développement initial** : ses versions 0 ne sont pas destinées à la production, et rien n'est garanti d'une version à l'autre.

## Fonctionnalités

Disponibles :

- Consultation publique : tableau de bord de la collection avec l'édition « À la une », fiches d'un album, d'une série et d'un auteur, avec navigation de l'une à l'autre
- Administration, réservée à un compte unique : listes, recherche et saisie des albums, séries, éditions et visuels, intentions d'acquisition, auteurs, éditeurs et collections, genres et univers
- Trois thèmes, tirés au hasard à chaque visite, sur smartphone, tablette et grand écran
- Mesure d'audience respectueuse de la vie privée, avec droit d'opposition des visiteurs

Prévues :

- Recherche avancée et recherches enregistrées (multi-entités)
- Statistiques de la collection, détection des albums manquants dans une série
- Estimation de la valeur des éditions (Random Forest via ML.NET) et de la date de sortie du prochain tome d'une série
- Aide intégrée à l'application et documentation utilisateur

## Stack

| Composant | Technologie |
| --- | --- |
| Frontend | Blazor, rendu côté serveur (.NET 10) |
| Backend API | ASP.NET Core Minimal API (.NET 10) |
| ORM | EF Core 10 + Npgsql |
| Base de données | PostgreSQL 18 |
| ML | ML.NET, embarqué dans l'API (prévu) |
| Conteneurisation | Docker Compose |

## Architecture

L'application est déployée en **3 conteneurs Docker** :

- `frontend` — Blazor, rendu côté serveur (pages, interface utilisateur)
- `api` — Minimal API (logique métier, accès aux données)
- `db` — PostgreSQL (persistance)

Le `frontend` communique avec l'`api` via le réseau Docker interne. L'`api` est le seul tier à accéder à `db`.

Deux réseaux Docker isolent les tiers, chacun nommé d'après le conteneur auquel il donne accès : `db` (`db` ↔ `api`) et `api` (`api` ↔ `frontend`). Seul `frontend` publie un port sur l'hôte ; `api` et `db` ne sont jamais exposés à l'extérieur du réseau Docker.

## Déploiement

Le déploiement cible un **NAS Synology** via Docker Compose (Synology Container Manager). Il n'utilise pas les sources : les images de l'application sont téléchargées depuis un registre.

### Installer une version publiée

Une version publiée s'installe à partir de son **paquet de déploiement**, `bdtheque-<version>.zip`, joint à sa [release](https://github.com/Tetram76/bdtheque-claude/releases). Il contient :

- le `docker-compose.yml` de la version, qui vise ses images publiques sur `ghcr.io`, téléchargées sans authentification ;
- le `.env.example` des réglages à renseigner ;
- la procédure d'installation, `DEPLOIEMENT.md` ;
- les notes de la version, `NOTES-DE-VERSION.md`, suivies des changements des versions précédentes, pour qui en saute plusieurs.

Il suffit de suivre `DEPLOIEMENT.md`. Le HTTPS et le dossier des visuels sont détaillés ci-dessous.

**Passer à une autre version** : les notes de la nouvelle version, sur sa release comme dans son paquet, disent dans leur section « Compatibilité et montée de version » ce qui change pour l'application, son hébergement et son déploiement, depuis quelles versions la montée est directe, et les étapes à suivre. Une rupture de compatibilité y est signalée en tête, dans un encadré d'avertissement.

### Images publiées

| Image | Conteneur |
| --- | --- |
| [`tetram76/bdtheque-api`](https://hub.docker.com/r/tetram76/bdtheque-api) | `api` |
| [`tetram76/bdtheque-frontend`](https://hub.docker.com/r/tetram76/bdtheque-frontend) | `frontend` |

Les versions publiées le sont aussi, en images publiques et sous leur seul numéro, sur GitHub Container Registry : `ghcr.io/tetram76/bdtheque-api` et `ghcr.io/tetram76/bdtheque-frontend`.

Elles sont publiées pour l'architecture du NAS (`linux/amd64`), avec les tags suivants :

| Tag | Contenu |
| --- | --- |
| `nightly` | Publication quotidienne, depuis la tête de `main` |
| `<branche>` (ex. `main`) | Dernière publication à la demande de cette branche |
| `sha-<commit>` | Première publication à la demande d'un commit précis, immuable (une nouvelle publication du même commit ne déplace que le tag de branche) |
| `<version>` (ex. `0.1.0`) | Version publiée, depuis son tag (ex. `v0.1.0`), immuable |

Leur publication est décrite dans [`README.dev.md`](README.dev.md#publication-des-images).

Les dépôts Docker Hub sont privés : leur téléchargement exige une authentification (cf. procédure ci-dessous).

### Déployer depuis le dépôt (pré-versions)

Une pré-version, version candidate ou de développement (cf. [`README.dev.md`](README.dev.md#plusieurs-versions-sur-le-nas)), se déploie à partir des fichiers du dépôt et des images de Docker Hub. Le tag déployé est choisi par `BDTHEQUE_TAG` dans `.env` (`nightly` par défaut, ou `sha-<commit>`), et `docker-compose.yml` et `.env.example` sont pris dans le dépôt à ce même commit, car ils peuvent différer d'une version à l'autre.

1. Créer sur le NAS un dossier pour le projet (ex. `/volume1/docker/bdtheque`) et y copier [`docker-compose.yml`](docker-compose.yml) et [`.env.example`](.env.example), renommé en `.env`.
2. Renseigner `.env` : `POSTGRES_PASSWORD`, `INTERNAL_API_KEY` (ex. `openssl rand -base64 32`), le compte administrateur `ADMIN_LOGIN` et `ADMIN_PASSWORD` (un mot de passe long, ex. une phrase de plusieurs mots), `BDTHEQUE_TAG`, `FRONTEND_PORT`, `VISUELS_HOST_PATH` et, derrière le reverse proxy, `PUBLIC_URL` (ci-dessous).
3. Préparer le dossier des visuels (ci-dessous).
4. Les dépôts Docker Hub étant **privés**, authentifier le NAS auprès de Docker Hub avec le compte `tetram76` et un jeton d'accès en **lecture seule** (*Personal Access Token*, droits *Read-only*), sans quoi le téléchargement des images est refusé. En SSH : `sudo docker login -u tetram76` (le jeton tient lieu de mot de passe).
5. Dans Container Manager, créer un **projet** sur ce dossier, à partir de son `docker-compose.yml` : Container Manager télécharge les images et démarre les conteneurs. En ligne de commande (SSH), depuis ce dossier : `docker compose pull && docker compose up -d`.
6. Vérifier que l'application répond sur `http://<NAS>:<FRONTEND_PORT>/health`.

**Mise à jour d'une pré-version** : modifier `BDTHEQUE_TAG` si besoin, puis télécharger les nouvelles images et recréer les conteneurs, en SSH depuis le dossier du projet : `docker compose pull && docker compose up -d`. Le schéma de la base est mis à jour automatiquement au démarrage d'`api` ; les données (volume `db-data`) et les visuels sont conservés.

**Mise à jour de l'image PostgreSQL** : une nouvelle image `postgres` peut embarquer une autre version d'ICU, qui définit le tri et la comparaison des textes. Au démarrage, `api` le détecte et reconstruit alors tous les index de la base, ce que son journal signale par un avertissement (`The version of the collations … changed`). Si un index unique ne peut pas être reconstruit — deux libellés de genre devenus égaux sous la nouvelle définition —, `api` refuse de démarrer et son journal nomme l'index en cause (`IX_Genres_Label`). Il faut alors renommer l'un des genres en double, puis redémarrer `api`. En SSH, depuis le dossier du projet :

```bash
docker compose exec db psql -U bdtheque -d bdtheque -c 'SELECT array_agg("Id"), array_agg("Label") FROM "Genres" GROUP BY "Label" HAVING count(*) > 1'
```

liste les genres devenus égaux, avec leurs identifiants (utilisateur et base selon `POSTGRES_USER` et `POSTGRES_DB`). Renommer l'un d'eux par son identifiant — la comparaison des libellés ignorant la casse et les accents, une condition sur le libellé les viserait tous —, par `UPDATE "Genres" SET "Label" = '…' WHERE "Id" = '…'`, puis `docker compose restart api`.

### HTTPS : reverse proxy du NAS

Le `frontend` ne sert que du HTTP : le chiffrement (HTTPS, certificats) est assuré par le **reverse proxy du NAS** (DSM), qui reçoit les requêtes en HTTPS et les relaie en HTTP au port `FRONTEND_PORT` du NAS. La même image sert deux déploiements :

- **derrière le reverse proxy** : `PUBLIC_URL` donne, dans `.env`, l'adresse sous laquelle le navigateur atteint l'application (ex. `https://bdtheque.example.com`, port compris s'il n'est pas celui du schéma, sans chemin : l'application est servie à la racine). Le `frontend` construit ses redirections sur cette adresse et marque ses cookies `Secure`. Une adresse mal formée, avec un chemin ou un autre schéma que `http` ou `https`, empêche le démarrage du `frontend`, qui le signale dans son journal ;
- **en interne**, sans reverse proxy (versions à valider ou à tester, cf. [`README.dev.md`](README.dev.md#plusieurs-versions-sur-le-nas)) : `PUBLIC_URL` reste vide, et l'application est accessible directement sur `http://<NAS>:<FRONTEND_PORT>`, sans rien d'autre à configurer.

Le `frontend` ne redirige jamais vers HTTPS et n'envoie pas d'en-tête HSTS : une éventuelle politique HSTS se règle sur le reverse proxy.

### Dossier des visuels

Les visuels (couvertures, planches, etc.) sont stockés sur un dossier du NAS, dont le chemin hôte est configurable via la variable d'environnement `VISUELS_HOST_PATH`. Il n'est monté que dans le conteneur `api`, qui sert les visuels au `frontend`.

Ce dossier doit exister et être **accessible en écriture pour l'utilisateur sous lequel tourne le conteneur `api`** ; sans quoi `api` refuse de démarrer, en indiquant dans son journal le dossier en cause et l'utilisateur qui doit y écrire. Docker crée un dossier absent au nom de root : il faut donc le créer avant le premier démarrage, de l'une de ces deux façons.

- **Sans SSH** : créez le dossier depuis File Station, sous votre utilisateur du NAS, puis faites tourner `api` sous ce même utilisateur en renseignant `API_USER` dans le `.env`, sous la forme `uid:gid` (par exemple `1026:100`). Pour connaître ces deux nombres sans terminal, créez dans le Planificateur de tâches du Panneau de configuration une tâche « Script défini par l'utilisateur », exécutée sous votre utilisateur, dont le script est `id` ; exécutez-la, puis lisez son résultat (« Action », « Afficher le résultat ») : `uid=` et `gid=` donnent les deux nombres.
- **Sans `API_USER`** : `api` tourne sous l'utilisateur de son image (UID `1654`, qui n'est pas root), à qui il faut donner le dossier, par exemple en SSH :

  ```bash
  mkdir -p ./data/visuels && sudo chown 1654:1654 ./data/visuels
  ```

  Sur un NAS Synology, le droit peut aussi être accordé au dossier partagé par ses permissions (ACL).

`API_USER` reste facultatif, et ne concerne que `api`, seul conteneur qui écrit dans un dossier du NAS. Si aucun dossier de l'hôte n'est monté, `api` le signale par un avertissement : les visuels seraient perdus à chaque recréation du conteneur.
