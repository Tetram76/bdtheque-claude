# Contraintes Techniques

Ce fichier recense les **contraintes techniques imposées** à l'application et à son déploiement — par l'utilisateur, des normes/lois, ou une réalité externe non négociable (système existant à migrer, environnement d'hébergement donné, etc.). Il peut aussi mentionner, explicitement, des **non-contraintes** (ouvertures de réflexion voulues par l'utilisateur). L'agent n'y ajoute rien de sa propre initiative : ses propres choix techniques, délibérés entre plusieurs options ou non, sont documentés dans `choix-implementation.md`.
Il ne concerne pas non plus les aspects gestion de projet (repo, branches, processus de développement, etc.) — ceux-ci relèvent de `gestion-projet.md`.

---

## Application existante (référence)

| Élément | Valeur |
| --- | --- |
| Langage | Delphi 10.3 Rio (10.3.3) |
| Base de données | Firebird 2.5 |
| Connexion à la base (production) | Firebird embarqué (*embedded*) |
| Type | Client lourd (desktop) |
| Dépôt des sources | <https://github.com/Tetram76/tetram> (dossier `bdtheque/`) |

Cette application est la **référence fonctionnelle** : le périmètre de la réécriture web doit couvrir ses fonctionnalités.

## Stack cible

| Élément | Valeur |
| --- | --- |
| Langage | C# / .NET |
| Frontend | Interface web (technologie : cf. `choix-implementation.md` § Frontend) |
| Backend API | ASP.NET Core (style d'API : cf. `choix-implementation.md` § Organisation de l'API) |
| ORM | EF Core |
| Base de données | PostgreSQL |

## Architecture des tiers

L'application repose sur un **socle de 3 conteneurs Docker**, fondation à laquelle d'autres conteneurs peuvent s'ajouter (ex. l'outil pour agents IA, cf. § Indépendance de l'outil pour agents IA) :

| Conteneur | Rôle |
| --- | --- |
| `frontend` | UI et rendu des pages |
| `api` | API ASP.NET Core — logique métier, accès données, ML |
| `db` | PostgreSQL — persistance |

Les images de base des conteneurs sont des choix d'implémentation : cf. `choix-implementation.md` § Images de base des conteneurs.

Seul le conteneur `frontend` est exposé : `api` n'est jamais exposé, le `frontend` l'appelle via HTTP interne (réseau Docker). Le conteneur `api` est le seul à accéder à `db`.

## Indépendance de l'outil pour agents IA

L'implémentation de l'outil pour agents IA (`fonctionnel.md` § Interface pour agents IA) doit être **indépendante de l'API** : déployer l'API ne doit pas impliquer de déployer l'outil pour agents IA.

**Non-contraintes** (ouvertures de réflexion explicites, laissées au choix de l'agent) :

- Le déploiement de l'outil sous forme de **conteneur**, comme le reste de l'application, est souhaitable mais **non imposé**.
- L'**exposition** de l'outil au-delà de `frontend` (seul conteneur exposé, cf. § Architecture des tiers) est **à envisager** si la contrainte d'exposition impose une surcharge disproportionnée, mais **non souhaitée** dans l'idéal.

## Déploiement

- Architecture **n-tiers avec isolation stricte** : chaque tier est déployé dans un **conteneur Docker dédié**.
- Un tier = un conteneur (pas de cohabitation de responsabilités dans un même conteneur).
- Orchestration via **Docker Compose**, compatible avec Synology Container Manager.

## Licences

- **Aucune licence payante** autorisée, pour quelque composant que ce soit (frameworks, bibliothèques, outils, bases de données, images Docker, etc.).
- Licences acceptées : **open source** (MIT, Apache 2.0, GPL, etc.) et **Community Edition** gratuites.
- Pour les projets open source : n'utiliser que des projets **reconnus, activement maintenus et largement adoptés** par la communauté. Exclure les projets confidentiels, abandonnés ou à faible adoption.

## Migration des données

- Des données existantes sont stockées dans la base Firebird 2.5 de l'application client lourd.
- Une **migration de données** depuis Firebird vers la base cible devra être possible lorsque la nouvelle application sera suffisamment mature.
- La conception du modèle de données cible doit tenir compte de cette migration future : préserver la sémantique des données existantes et ne pas rendre la migration inutilement complexe.
- La migration pourra être **incrémentale** (imports successifs et partiels) : l'outil de migration devra **fusionner** les données importées avec les données déjà présentes dans la base cible lorsqu'il y a correspondance (pas de doublons, mise à jour des éléments existants).
- La migration est **unidirectionnelle** : aucun retour en arrière, aucune synchronisation vers l'application client lourd.
- Un outil ou script de migration dédié devra être prévu le moment venu.

### Base existante

Caractéristiques de la base Firebird de l'application existante, que la migration doit prendre en compte (moyens d'accès retenus : cf. `choix-implementation.md` § Accès à la base existante (Firebird)) :

| Élément | Valeur |
| --- | --- |
| Fichier | Copie de la base de production, fournie seule et hors dépôt, sous un nom non fixé (ex. `BD.GDB`), éventuellement accompagnée de `BDT_UDF.dll` |
| Format | ODS 11.2 (Firebird 2.5), pages de 16 Ko |
| Tables | `ALBUMS`, `ALBUMS_UNIVERS`, `AUTEURS`, `AUTEURS_PARABD`, `AUTEURS_SERIES`, `COLLECTIONS`, `CONVERSIONS`, `COTES`, `COTES_PARABD`, `COUVERTURES`, `CRITERES`, `EDITEURS`, `EDITIONS`, `EMPRUNTEURS`, `GENRES`, `GENRESERIES`, `IMPORT_ASSOCIATIONS`, `LISTES`, `OPTIONS`, `OPTIONS_SCRIPTS`, `PARABD`, `PARABD_UNIVERS`, `PERSONNES`, `PHOTOS`, `SERIES`, `SERIES_UNIVERS`, `STATUT`, `SUPPRESSIONS`, `UNIVERS` |
| Texte | Jeu de caractères `UTF8` |

- **Collations propres à la base** : `UTF8_FR`, `UTF8_FR_CI` et `UTF8_FR_CI_AI`, dérivées de `UNICODE` avec `LOCALE=fr_FR`, portent l'attribut `COLL-VERSION=58.0.6.50`, celui d'**ICU 52**. L'application existante livre cet ICU 52 avec Firebird (dossier `bdtheque/delphi/trunk/deploy/` du dépôt des sources ; ses DLL 64 bits ont la taille de celles du build officiel ICU4C 52.1 Win64 msvc10), et son `intl/fbintl.conf` le déclare pour le module `builtin` (`icu_versions 5.2`). Avec l'ICU 3.0 des kits officiels Firebird 2.5.9, Firebird refuse toute requête sur une table qui utilise ces collations (`COLLATION … is not installed`).
- **Fonctions externes (UDF)** : la base déclare 20 UDF de la bibliothèque propre à l'application, `BDT_UDF.dll` (dépôt des sources : `bdtheque/delphi/trunk/src/BDT_UDF.DLL/`, sans binaire compilé ; la DLL peut être fournie avec la base). Certaines agissent sur le système de fichiers (`UDF_DELETEFILE`, `UDF_SAVEBLOBTOFILE`, `UDF_LOADBLOBFROMFILE`, `UDF_FINDFILEFIRST`…).
- **Vues et procédures stockées** : 20 vues (`VW_*`) et des procédures stockées qui portent une partie de la logique de l'application (listes par initiale, albums manquants, prévisions de sorties…). Certaines procédures construisent leur requête en concaténant un paramètre `FILTRE` ; d'autres enveloppent les UDF de fichiers (`DELETEFILE`, `SAVEBLOBTOFILE`, `LOADBLOBFROMFILE`, `DIRECTORYCONTENT`, `SEARCHFILENAME`).
- **Titres stockés sous forme de tri** (`ALBUMS.TITREALBUM`, `SERIES.TITRESERIE`) : l'article initial est reporté en suffixe entre crochets (`fils d'Asterix [Le]`, `étoile du désert [L']`), selon la même convention que la clé de tri de `fonctionnel.md` § Titres (séries et albums). La forme affichée est reconstituée par l'UDF `UDF_FORMATTITLE` (`Le fils d'Asterix`).
- **Initiale stockée** (`ALBUMS.INITIALETITREALBUM`, `CHAR(1)` en `UTF8`) : initiale **brute** du titre, non normalisée — casse et accents conservés (`É` et `é` sont deux valeurs distinctes), chaque chiffre est une valeur à part entière, `#` figure parmi les valeurs, et la colonne est vide (`NULL`) pour un album sans titre propre, que l'application range sous l'initiale de sa série (`coalesce(initialetitrealbum, initialetitreserie)`, procédure `INITIALES_ALBUMS`). Elle ne correspond donc pas aux entrées de navigation de `fonctionnel.md` § Entrées de la navigation par initiale.

## Hébergement

- L'application est hébergée sur un **NAS Synology**.
- Le déploiement Docker doit être compatible avec l'environnement Docker fourni par Synology (Container Manager).

## Compatibilité multi-supports

- L'application doit être **responsive** : utilisable sur PC, tablette et smartphone.
- Le rendu et la navigation doivent s'adapter à toutes les tailles d'écran (approche *mobile-first*).

## Authentification

- L'accès à la partie Administration est protégé par un **compte administrateur unique** (login + mot de passe).
- Pas d'authentification sur la partie Consultation (accès public).

## Stockage des visuels

- Les fichiers visuels (couvertures, planches, etc.) sont stockés sur un **volume Docker** monté sur le NAS.
- Le chemin de montage est configurable via variable d'environnement Docker Compose.
