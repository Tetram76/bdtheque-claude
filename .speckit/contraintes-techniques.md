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

## Organisation du code

La solution .NET est découpée en projets par responsabilité, sous `src/` :

| Projet | Rôle |
| --- | --- |
| `Bdtheque.Domain` | Entités du modèle métier, enums, value objects. Aucune dépendance à EF Core ni à un framework web. |
| `Bdtheque.Infrastructure` | Implémentation EF Core / Npgsql : `DbContext`, configurations d'entités, migrations. |
| `Bdtheque.Contracts` | Contrats d'échange (DTOs) exposés par l'API et consommés par le frontend. Découplés des entités du domaine. |
| `Bdtheque.Api` | Conteneur `api` : endpoints Minimal API, règles applicatives, service de taux de change, estimation ML.NET. |
| `Bdtheque.Frontend` | Conteneur `frontend` : composants Blazor Server, authentification cookie, appels HTTP vers `Bdtheque.Api`. |

Chaque projet source a vocation à avoir son miroir sous `tests/` (ex. `Bdtheque.Api.Tests`), créé dès que son contenu justifie des tests — proportionnalité définie dans la règle de non-régression de `gestion-projet.md`.

## Conventions de persistance (EF Core)

- **Énumérations** : toute propriété de type `enum` est persistée sous forme de **chaîne** (nom du membre), jamais sous forme d'entier. Configuré une fois pour tout le modèle via `ConfigureConventions` sur `BdthequeDbContext` (`Properties<Enum>().HaveConversion<string>()`). Objectif : un ré-ordonnancement ou un ajout de membre dans un enum ne doit jamais changer silencieusement le sens des lignes déjà persistées, ce qui serait le cas avec un stockage par entier implicite.
  - **Alternative écartée (entier avec valeurs explicites)** : assigner une valeur numérique explicite à chaque membre (`= N`) neutraliserait aussi le risque de ré-ordonnancement, sans le surcoût de stockage/longueur d'une chaîne. Cette alternative est écartée car son seul point faible — la réutilisation d'un numéro retiré — est moins probable que le point faible symétrique du stockage en chaîne (un renommage de membre, qui invalide silencieusement les valeurs déjà stockées sous ce nom) : ce projet a déjà renommé plusieurs membres d'énumération en cours de développement pour en clarifier le nom anglais. Le format chaîne reste par ailleurs directement lisible en base, un atout pour un projet à maintenance solo et pour la future migration Firebird (cf. § Migration des données). Chaque setter d'énumération valide néanmoins la définition de la valeur reçue via `EnumGuard.EnsureDefined` (`Bdtheque.Domain.Common`), pour qu'une valeur hors plage (ex. liaison d'un entier arbitraire depuis l'API) ne soit jamais persistée telle quelle, quel que soit le mode de stockage retenu.
  - **Conséquence sur le tri** : ce stockage rend un `ORDER BY` direct sur la colonne **alphabétique**, pas conforme à l'ordre métier quand celui-ci ne l'est pas (ex. l'ordre fixe des types de visuel d'édition, cf. `fonctionnel.md`). Dans ce cas, le tri s'appuie explicitement sur le rang du membre (ordre de déclaration de l'enum), appliqué en mémoire pour les petites collections concernées plutôt que traduit en SQL.

## Déploiement

- Architecture **n-tiers avec isolation stricte** : chaque tier est déployé dans un **conteneur Docker dédié**.
- Un tier = un conteneur (pas de cohabitation de responsabilités dans un même conteneur).
- Orchestration via **Docker Compose**, compatible avec Synology Container Manager.
- Les migrations EF Core sont appliquées **automatiquement au démarrage** du conteneur `api` (`Database.Migrate()`), hors environnement `Testing` qui provisionne son propre schéma. Pas de conteneur ou d'étape d'initialisation dédiée : un déploiement neuf sur une base vide crée le schéma dès le premier démarrage.

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

## Internationalisation

- Le code (classes, fonctions, variables, commentaires, etc.) est écrit en **anglais** (cf. `gestion-projet.md`), indépendamment de la langue de l'utilisateur final.
- La **culture d'affichage** choisie par l'utilisateur (voir `fonctionnel.md`) est implémentée via les ressources de localisation ASP.NET Core (`IStringLocalizer`) pour la traduction des textes, combinées à la **culture .NET courante** (`CultureInfo`, positionnée par requête) pour le formatage des données et le tri linguistique. Aucun texte utilisateur n'est codé en dur dans le code applicatif.
- Le mode **globalization-invariant** de .NET est incompatible avec le tri linguistique et le formatage culturel requis par le fonctionnel : il ne doit pas être activé. Les images Docker utilisées (`aspnet:10.0`, base Ubuntu) embarquent déjà ICU, donc le support complet de la globalisation n'a aucun coût supplémentaire.

## Compatibilité multi-supports

- L'application doit être **responsive** : utilisable sur PC, tablette et smartphone.
- Le rendu et la navigation doivent s'adapter à toutes les tailles d'écran (approche *mobile-first*).
- Implémentation via un framework CSS responsive intégré à Blazor (ex. Bootstrap ou MudBlazor — cf. choix de composants UI).

## Authentification

- L'accès à la partie Administration est protégé par un **compte administrateur unique** (login + mot de passe).
- Implémentation via **ASP.NET Core Cookie Authentication** (sans ASP.NET Core Identity — pas de gestion multi-utilisateurs). Le cookie d'authentification est porté exclusivement par le conteneur `frontend` (Blazor Server), qui agit comme **BFF (Backend For Frontend)** : c'est lui qui affiche le formulaire de connexion, émet le cookie et protège ses propres pages/composants d'administration (`[Authorize]`).
- Pas d'authentification sur la partie Consultation (accès public).
- Le conteneur `api` n'est **jamais exposé publiquement** : il n'est joignable que par `frontend` via le réseau Docker interne. En défense en profondeur, `api` exige néanmoins un **secret interne partagé** (clé statique transmise via variable d'environnement, vérifiée par un middleware sur l'en-tête `X-Internal-Api-Key`) sur toutes ses requêtes. Ce secret n'est connu que de `frontend` et `api` ; il ne remplace pas l'authentification de l'utilisateur (qui reste du ressort de `frontend`), il empêche seulement qu'un appel direct à `api` contourne la protection applicative si l'isolation réseau venait à être mal configurée.

## Documentation de l'API

- L'API expose sa spécification via **OpenAPI** (génération native ASP.NET Core, `Microsoft.AspNetCore.OpenApi`).
- Une interface de documentation interactive (**Scalar**, open source MIT) est exposée par `api` en environnement de développement uniquement.

## Représentation des devises et validation de l'ISBN

- **Devise d'un montant** (ex. `Édition.Prix d'acquisition`) : stockée comme un **code ISO 4217 alpha-3** (`string`, 3 lettres majuscules), validé par le domaine sur sa seule **forme** (3 lettres majuscules), pas contre une liste fermée de devises. Une énumération C# figée aurait contredit l'exigence « n'importe quelle devise » de `fonctionnel.md` § Gestion des devises, qui n'est pas limitée aux quelques exemples cités (Franc français, Dollar américain) dans ce même fichier.
- **Validation de l'ISBN** : le contrôle du chiffre de vérification (ISBN-10 / ISBN-13) est isolé dans `Bdtheque.Domain.Common.IsbnChecksumValidator`, utilisable indépendamment de l'entité `Édition`. Conformément à `fonctionnel.md` § Validation de l'ISBN (contrôle non bloquant), `Edition.SetIsbn` ne rejette jamais une valeur incorrecte : c'est aux couches applicatives (API/Frontend) d'appeler ce validateur pour avertir l'utilisateur sans empêcher l'enregistrement.

## Gestion des taux de change

- Devises à taux **fixe** vis-à-vis de l'euro (ex. Franc français) : constantes en code.
- Devises à taux **variable** (ex. Dollar américain) : taux récupérés depuis l'API **[Frankfurter](https://www.frankfurter.app/)** (open source, gratuite, sans clé API). Les taux sont mis en cache côté `api` pour éviter les appels répétés.

## Stockage des visuels

- Les fichiers visuels (couvertures, planches, etc.) sont stockés sur un **volume Docker** monté sur le NAS.
- Le volume est accessible en écriture depuis `api` (upload) et en lecture depuis `frontend` (affichage).
- Le chemin de montage est configurable via variable d'environnement Docker Compose.
