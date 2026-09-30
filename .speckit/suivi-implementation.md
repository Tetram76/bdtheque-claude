# Suivi d'implémentation

Ce fichier suit l'avancement de l'implémentation de l'application, découpée en phases puis en Pull Requests. Il reflète l'**état courant** du plan d'implémentation (réalisé / en cours / à faire) — pas l'historique des décisions qui ont conduit à ce découpage.

Chaque ligne correspond à une Pull Request unitaire, respectant les conventions de branches et de commits définies dans `gestion-projet.md`.

---

## Phase 0 — Architecture

**Statut : réalisée.**

Mise en place de la structure porteuse de l'application, indépendamment du modèle métier :

- Solution .NET découpée en 5 projets (`Bdtheque.Domain`, `Bdtheque.Contracts`, `Bdtheque.Infrastructure`, `Bdtheque.Api`, `Bdtheque.Frontend`).
- Architecture 3 conteneurs Docker (`frontend` / `api` / `db`), réseaux isolés, `docker-compose.yml`.
- Authentification cookie côté `frontend` (BFF) ; middleware de clé interne côté `api`.
- `DbContext` EF Core opérationnel (connexion PostgreSQL), sans entité métier.
- Endpoints `/health` sur `frontend` et `api` ; OpenAPI/Scalar en développement.
- Pipeline CI GitHub Actions (restauration, build, tests).
- Premiers tests d'intégration (health check, middleware de clé interne).

---

## Phase 1 — Modèle de domaine

Implémentation des entités du modèle métier (`modele-metier.md`) dans `Bdtheque.Domain` et `Bdtheque.Infrastructure` (configurations EF Core + migrations), séquencée selon les dépendances entre entités. Le `DbContext` reçoit progressivement ses `DbSet`.

**Statut : réalisée.**

| # | Branche | Titre (commit) | Contenu | Statut |
| --- | --- | --- | --- | --- |
| 1 | `feat/domain-referentiels` | `feat(domain): ajout des référentiels de base` | Entités `Auteur`, `Éditeur`, `Collection éditeur`, `Genre`, `Univers` ; configurations EF ; migration ; tests unitaires sur les contraintes (Auteur : Nom ou Pseudonyme requis ; Univers : acyclicité ; Collection éditeur : rattachement obligatoire à un Éditeur) | Réalisée |
| 2 | `feat/domain-serie` | `feat(domain): ajout de l'entité Série` | Entité `Série` (statut, champs template) ; jointures Série↔Genre, Série↔Univers ; configurations EF ; migration ; tests (clé de tri auto/manuelle, cohérence des templates Éditeur/Collection éditeur) | Réalisée |
| 3 | `feat/domain-album` | `feat(domain): ajout de l'entité Album` | Entité `Album` (type, hors-série, tomes) ; jointures Album↔Genre, Album↔Univers ; configurations EF ; migration ; tests (titre conditionnel selon rattachement à une série, cohérence tome de début/fin pour les intégrales, clé de tri) | Réalisée |
| 4 | `feat/domain-contribution` | `feat(domain): ajout de l'entité Contribution` | Entité `Contribution` (rôle, artiste) reliant un Auteur à un Album **ou** une Série ; configuration EF de la contrainte d'exclusivité ; migration ; tests sur cette exclusivité | Réalisée |
| 5 | `feat/domain-edition` | `feat(domain): ajout de l'entité Édition` | Entité `Édition` (reliure, orientation, sens de lecture, format, catégorie, état, mode d'acquisition) ; configurations EF des contraintes d'intégrité (mode d'acquisition ↔ date/prix, gratuite ↔ prix nul) ; migration ; tests sur ces contraintes | Réalisée |
| 6 | `feat/domain-visuel-edition` | `feat(domain): ajout de l'entité Visuel d'édition` | Entité `Visuel d'édition` (type, ordre d'affichage) ; configuration EF du tri par type puis ordre ; migration ; tests associés | Réalisée |
| 7 | `feat/domain-intention-achat` | `feat(domain): ajout de l'entité Intention d'achat` | Entité `Intention d'achat`, portée par l'agrégat `Album` (intention sur l'album **ou** sur ses éditions, une intention par cible) ; configuration EF (index uniques) ; migration ; tests sur ces règles | Réalisée |

### Consolidation avant la Phase 2

Corrections issues de la revue complète de la Phase 1, à réaliser avant d'ouvrir la Phase 2.

**Statut : réalisée.**

| # | Branche | Titre (commit) | Contenu | Statut |
| --- | --- | --- | --- | --- |
| 1 | `chore/tests-postgresql` | `test(infrastructure): exécute les tests de persistance sur PostgreSQL réel` | Testcontainers à la place de SQLite (migrations réellement appliquées, base clonée par test) ; suppression du cas particulier `Testing` de l'API ; assertions sur le nom de la contrainte violée ; contrôle modèle ↔ migrations en CI | Réalisée |
| 2 | `chore/postgresql-18` | `chore(docker): passe à PostgreSQL 18 et regroupe les migrations` | Image `postgres:18-alpine` (production et tests) ; migrations regroupées en une `InitialCreate` unique, aucun déploiement n'ayant eu lieu | Réalisée |
| 3 | `fix/collation-tri` | `fix(infrastructure): tri linguistique des colonnes texte en base` | Collation ICU française sur toutes les colonnes texte (la collation par défaut de l'image trie en ordre binaire) | Réalisée |
| 4 | `refactor/domain-business-errors` | `refactor(domain): distingue les erreurs métier des erreurs techniques` | Exception dédiée aux violations de règles métier, porteuse d'un code de règle stable ; erreurs de programmation et données corrompues maintenues en exceptions techniques | Réalisée |
| 5 | `refactor/domain-collections` | `refactor(domain): encapsule les collections de navigation` | Collections de navigation en lecture seule (règles du domaine non contournables par le suivi de relations EF) ; ordre des visuels déterministe | Réalisée |
| 6 | `docs/phase1-revue` | `docs: corrige la documentation relevée par la revue de la Phase 1` | Commentaires faux ou périmés ; justification des contraintes laissées au seul domaine intégrée au `.speckit` ; en-tête de `contraintes-techniques.md` aligné sur `AGENTS.md` | Réalisée |

---

## Phase 2 — Contracts et API

Exposition du modèle de domaine via l'API : DTOs dans `Bdtheque.Contracts`, endpoints Minimal API dans `Bdtheque.Api` — administration (`/admin` : saisie, suppression selon `fonctionnel.md` § Suppression des entités) et consultation (`/catalog` : listes, navigation par initiale, recherche, fiches). Choix techniques : `choix-implementation.md` § Organisation de l'API et sections suivantes.

Hors périmètre (phases dédiées ci-dessous) : statistiques, conversion de devises, estimations.

**Statut : à faire.**

| # | Branche | Titre (commit) | Contenu | Statut |
| --- | --- | --- | --- | --- |
| 1 | `feat/api-socle` | `feat(api): socle des endpoints et des réponses d'erreur` | Réponses ProblemDetails par catégorie (métier / fonctionnelle / technique) via un gestionnaire d'exceptions unique et, pour les erreurs émises sans exception (liaison de requête, route inconnue, clé interne absente), les pages de code d'état ; traduction des violations de contraintes selon l'opération (`choix-implementation.md` § Erreurs métier, fonctionnelles et techniques) : index unique → erreur métier avec code de règle, clé étrangère lors d'une création ou modification → erreur fonctionnelle (la clé étrangère lors d'une suppression relève du mécanisme de suppression, PR 4) ; erreurs métier détectées par l'API signalées avec un code de `DomainRules` ; conventions des groupes `/admin` et `/catalog` ; sérialisation JSON (énumérations en chaîne) ; énumérations des contrats et test de parité avec le domaine ; jeton de concurrence `xmin` sur les racines d'agrégat, racine verrouillée en premier puis marquée modifiée à toute écriture sur l'agrégat (ordre de verrouillage racine → enfant) ; outillage des tests d'intégration de l'API | À faire |
| 2 | `feat/domain-navigation` | `feat(domain): entrée de navigation par initiale et clé de tri des auteurs` | Clé de tri stockée des auteurs ; entrée de navigation (`A`–`Z`, `#`, `@`) calculée et stockée pour séries, albums et auteurs ; index ; migration `InitialCreate` régénérée ; tests | À faire |
| 3 | `feat/infrastructure-suppression` | `feat(infrastructure): aligne les suppressions en base sur les règles de suppression` | `ON DELETE CASCADE` pour les compositions (album → éditions, contributions ; édition → visuels, intention ; série → contributions template), `RESTRICT` pour les références ; migration régénérée ; tests sur PostgreSQL | À faire |
| 4 | `feat/api-genres-univers` | `feat(api): administration des genres et des univers` | CRUD Genre et Univers (hiérarchie, acyclicité, unicité du libellé de genre) ; **mécanisme commun de suppression**, réutilisé par les PR suivantes (`choix-implementation.md` § Suppression des entités : mise en œuvre) : impact par type d'entité avec empreinte, verrou de ligne, recalcul et comparaison dans la transaction, refus métier des références (y compris clé étrangère levée à la suppression), erreur fonctionnelle si l'impact a changé | À faire |
| 5 | `feat/api-editeurs` | `feat(api): administration des éditeurs et de leurs collections` | CRUD Éditeur et Collection éditeur (unicités) ; impact et règles de suppression | À faire |
| 6 | `feat/api-auteurs` | `feat(api): administration des auteurs` | CRUD Auteur ; impact et règles de suppression | À faire |
| 7 | `feat/api-series` | `feat(api): administration des séries` | CRUD Série : clé de tri auto/manuelle, templates d'édition, genres, univers, contributions template ; impact et règles de suppression | À faire |
| 8 | `feat/api-albums` | `feat(api): administration des albums` | CRUD Album : rattachement à une série avec recopie des contributions de la série, contributions, genres, univers, clé de tri auto/manuelle, méthodes atomiques (type et plage de tomes) ; suppression avec ses compositions | À faire |
| 9 | `feat/api-editions` | `feat(api): administration des éditions` | Pré-remplissage depuis les templates de la série ; création d'une édition possédée via `Album.RecordAcquisition` ; méthodes atomiques (acquisition, éditeur et collection) ; refus de retirer le mode d'acquisition d'une édition possédée (le domaine l'autorise encore) ; contrôle non bloquant de l'ISBN ; suppression avec ses compositions | À faire |
| 10 | `feat/api-visuels` | `feat(api): téléversement et gestion des visuels d'édition` | Téléversement validé par décodage (fichier refusé : erreur métier), original et version d'affichage WebP (SkiaSharp), ordre d'affichage, suppression des fichiers après validation de la transaction, compensation d'une création échouée et réconciliation périodique des fichiers orphelins | À faire |
| 11 | `feat/api-intentions-achat` | `feat(api): intentions d'achat` | Intention sur un album ou sur une nouvelle édition (saisie minimale), confirmation d'achat, conversion album ↔ édition, suppression (une intention sur une édition et son édition non possédée sont indissociables : supprimer ou convertir l'intention supprime l'édition, avec confirmation), liste publique | À faire |
| 12 | `feat/api-catalogue-listes` | `feat(api): listes et recherche de la consultation` | Listes paginées et recherche couvrant **chaque entité consultable** (`fonctionnel.md` § Périmètre de la consultation) : séries, albums, éditions (y compris par ISBN), auteurs, éditeurs, collections éditeur, genres, univers ; navigation par initiale (un album sans titre suit la clé et l'entrée **courantes** de sa série, y compris après leur modification), indicateur d'appartenance à la collection, recherche insensible à la casse et aux accents, filtres croisés (recherche avancée) | À faire |
| 13 | `feat/api-catalogue-fiches` | `feat(api): fiches détaillées de la consultation` | Une fiche pour **chaque entité consultable** : album (genres et univers affichés : union avec la série ; intentions d'achat), série (ordre des albums), édition (visuels ordonnés, appartenance à la collection), auteur (bibliographie), éditeur (collections, éditions), collection éditeur (éditions), genre et univers (albums et séries rattachés ; hiérarchie des univers) ; navigation inter-entités. Les **visuels** et les **intentions d'achat** n'ont pas de fiche distincte, toutes leurs informations étant présentées par la fiche qui les porte : un visuel n'a pour données que son type, son rang et son média (présentés dans la fiche de l'édition, avec accès à l'image originale), une intention que sa cible (présentée dans la fiche de l'album ou de l'édition visée, et dans la liste publique des intentions, qui mène à cette fiche) | À faire |

## Phase 3 — Frontend Blazor

Pages de consultation (public) et d'administration (authentifié), composants partagés, aide contextuelle.

**Statut : à faire.** Découpage précis à établir une fois la Phase 2 stabilisée ; probablement par écran (fiche album, fiche série, recherche, formulaires admin) plutôt que par entité.

## Phase 4 — Statistiques, devises et estimation de valeur

Dashboard public et statistiques (sur la collection uniquement) ; conversion des montants en euro (taux fixes, taux variables récupérés auprès de Frankfurter et mis en cache) ; valeur estimée des éditions (Random Forest, ML.NET), recalculée à partir de l'état courant de la base (`fonctionnel.md` § Calcul des estimations).

**Statut : à faire.** Découpage à établir à l'ouverture de la phase. La qualité de l'estimation de valeur ne peut être évaluée que sur des données réelles (collection issue de l'application existante).

## Phase 5 — Fonctionnalités de second plan

Identification des albums manquants d'une série ; estimation de sortie d'un nouvel album (`fonctionnel.md` § Fonctionnalités de second plan).

**Statut : à faire.** Découpage à établir à l'ouverture de la phase.
