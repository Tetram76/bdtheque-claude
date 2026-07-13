# Contraintes Techniques

Ce fichier décrit les choix et contraintes techniques de l'application et de son déploiement.
Il ne concerne PAS les aspects gestion de projet (repo, branches, outillage dev, etc.) — ceux-ci relèvent de `gestion-projet.md`.

---

## Application existante (référence)

| Élément | Valeur |
| --- | --- |
| Langage | Delphi (version inconnue) |
| Base de données | Firebird 1.5 |
| Type | Client lourd (desktop) |

Cette application est la **référence fonctionnelle** : le périmètre de la réécriture web doit couvrir ses fonctionnalités.

## Stack cible

| Élément | Valeur |
| --- | --- |
| Langage | C# / .NET 10 (LTS) |
| Frontend | Blazor Server (ASP.NET Core) |
| Backend API | ASP.NET Core Minimal API |
| ORM | EF Core 10 + Npgsql |
| Base de données | PostgreSQL |
| ML / estimation de valeur | ML.NET (embarqué dans le conteneur `api`) |

## Architecture des tiers

L'application est découpée en **3 conteneurs Docker** :

| Conteneur | Rôle | Image de base |
| --- | --- | --- |
| `frontend` | Blazor Server — UI et rendu des pages | `mcr.microsoft.com/dotnet/aspnet:10.0` |
| `api` | ASP.NET Core Minimal API — logique métier, accès données, ML | `mcr.microsoft.com/dotnet/aspnet:10.0` |
| `db` | PostgreSQL — persistance | `postgres:17-alpine` |

Le conteneur `frontend` appelle `api` via HTTP interne (réseau Docker). Le conteneur `api` est le seul à accéder à `db`.

## Déploiement

- Architecture **n-tiers avec isolation stricte** : chaque tier est déployé dans un **conteneur Docker dédié**.
- Un tier = un conteneur (pas de cohabitation de responsabilités dans un même conteneur).
- Orchestration via **Docker Compose**, compatible avec Synology Container Manager.

## Licences

- **Aucune licence payante** autorisée, pour quelque composant que ce soit (frameworks, bibliothèques, outils, bases de données, images Docker, etc.).
- Licences acceptées : **open source** (MIT, Apache 2.0, GPL, etc.) et **Community Edition** gratuites.
- Pour les projets open source : n'utiliser que des projets **reconnus, activement maintenus et largement adoptés** par la communauté. Exclure les projets confidentiels, abandonnés ou à faible adoption.

## Migration des données

- Des données existantes sont stockées dans la base Firebird 1.5 de l'application client lourd.
- Une **migration de données** depuis Firebird vers la base cible devra être possible lorsque la nouvelle application sera suffisamment mature.
- La conception du modèle de données cible doit tenir compte de cette migration future : préserver la sémantique des données existantes et ne pas rendre la migration inutilement complexe.
- La migration pourra être **incrémentale** (imports successifs et partiels) : l'outil de migration devra **fusionner** les données importées avec les données déjà présentes dans la base cible lorsqu'il y a correspondance (pas de doublons, mise à jour des éléments existants).
- La migration est **unidirectionnelle** : aucun retour en arrière, aucune synchronisation vers l'application client lourd.
- Un outil ou script de migration dédié devra être prévu le moment venu.

## Hébergement

- L'application est hébergée sur un **NAS Synology**.
- Le déploiement Docker doit être compatible avec l'environnement Docker fourni par Synology (Container Manager).

## Estimation de la valeur des éditions

- La valeur estimée est calculée par un modèle **Random Forest** (voir `fonctionnel.md`).
- Implémentation via **ML.NET**, embarquée dans le conteneur `api`.

## Compatibilité multi-supports

- L'application doit être **responsive** : utilisable sur PC, tablette et smartphone.
- Le rendu et la navigation doivent s'adapter à toutes les tailles d'écran (approche *mobile-first*).
- Implémentation via un framework CSS responsive intégré à Blazor (ex. Bootstrap ou MudBlazor — cf. choix de composants UI).

## Authentification

- L'accès à la partie Administration est protégé par un **compte administrateur unique** (login + mot de passe).
- Implémentation via **ASP.NET Core Cookie Authentication** (sans ASP.NET Core Identity — pas de gestion multi-utilisateurs).
- Pas d'authentification sur la partie Consultation (accès public).

## Gestion des taux de change

- Devises à taux **fixe** vis-à-vis de l'euro (ex. Franc français) : constantes en code.
- Devises à taux **variable** (ex. Dollar américain) : taux récupérés depuis l'API **[Frankfurter](https://www.frankfurter.app/)** (open source, gratuite, sans clé API). Les taux sont mis en cache côté `api` pour éviter les appels répétés.

## Stockage des visuels

- Les fichiers visuels (couvertures, planches, etc.) sont stockés sur un **volume Docker** monté sur le NAS.
- Le volume est accessible en écriture depuis `api` (upload) et en lecture depuis `frontend` (affichage).
- Le chemin de montage est configurable via variable d'environnement Docker Compose.
