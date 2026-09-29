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

**Statut : en cours.**

| # | Branche | Titre (commit) | Contenu | Statut |
| --- | --- | --- | --- | --- |
| 1 | `chore/tests-postgresql` | `test(infrastructure): exécute les tests de persistance sur PostgreSQL réel` | Testcontainers à la place de SQLite (migrations réellement appliquées, base clonée par test) ; suppression du cas particulier `Testing` de l'API ; assertions sur le nom de la contrainte violée ; contrôle modèle ↔ migrations en CI | En cours |
| 2 | `chore/postgresql-18` | `chore(docker): passe à PostgreSQL 18 et regroupe les migrations` | Image `postgres:18-alpine` (production et tests) ; migrations regroupées en une `InitialCreate` unique, aucun déploiement n'ayant eu lieu | À faire |
| 3 | `fix/collation-tri` | `fix(infrastructure): tri linguistique des clés de tri en base` | Collation ICU française sur les colonnes de tri (la collation par défaut de l'image trie en ordre binaire) | À faire |
| 4 | `refactor/domain-consolidation` | `refactor(domain): encapsule les collections et distingue les erreurs métier` | Collections de navigation en lecture seule (règles du domaine non contournables par le suivi de relations EF) ; type d'exception dédié aux violations de règles métier ; ordre des visuels déterministe ; jeton de concurrence optimiste | À faire |
| 5 | `docs/phase1-revue` | `docs: corrige la documentation relevée par la revue de la Phase 1` | Commentaires faux ou périmés ; justification des contraintes laissées au seul domaine intégrée au `.speckit` ; en-tête de `contraintes-techniques.md` aligné sur `AGENTS.md` | À faire |

---

## Phase 2 — Contracts et API

Exposition du modèle de domaine via l'API : DTOs dans `Bdtheque.Contracts`, endpoints Minimal API dans `Bdtheque.Api` (CRUD administration + lecture consultation, règles applicatives).

**Statut : à faire.** Découpage précis à établir une fois la Phase 1 stabilisée (dépend des décisions prises sur la forme exacte des entités). Séquencement prévisionnel par groupe d'entités, dans le même ordre que la Phase 1.

## Phase 3 — Frontend Blazor

Pages de consultation (public) et d'administration (authentifié), composants partagés, aide contextuelle.

**Statut : à faire.** Découpage précis à établir une fois la Phase 2 stabilisée ; probablement par écran (fiche album, fiche série, recherche, dashboard, formulaires admin) plutôt que par entité.
