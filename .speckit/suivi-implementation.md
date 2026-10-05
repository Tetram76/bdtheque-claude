# Suivi d'implémentation

Ce fichier décrit le plan d'implémentation de l'application, découpé en phases puis en chantiers — pas l'historique des décisions qui ont conduit à ce découpage.

Chaque ligne correspond à un chantier, c'est-à-dire à un ticket GitHub (et à une Pull Request) : elle en indique le **numéro** et le **titre**. Le contenu détaillé du chantier est porté par le ticket. L'**état** d'un chantier est celui de son ticket : le numéro est **barré** lorsque le ticket est fermé (cf. `gestion-projet.md` § « Suivi par tickets »).

---

## Phase 0 — Architecture

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

| Ticket | Titre |
| --- | --- |
| ~~#60~~ | Ajout des référentiels de base du domaine |
| ~~#61~~ | Ajout de l'entité Série |
| ~~#62~~ | Ajout de l'entité Album |
| ~~#63~~ | Ajout de l'entité Contribution |
| ~~#64~~ | Ajout de l'entité Édition |
| ~~#65~~ | Ajout de l'entité Visuel d'édition |
| ~~#66~~ | Ajout de l'entité Intention d'achat |

### Consolidation avant la Phase 2

Corrections issues de la revue complète de la Phase 1, à réaliser avant d'ouvrir la Phase 2.

| Ticket | Titre |
| --- | --- |
| ~~#67~~ | Tests de persistance sur PostgreSQL réel |
| ~~#68~~ | Passage à PostgreSQL 18 et regroupement des migrations |
| ~~#69~~ | Tri linguistique des colonnes texte en base |
| ~~#70~~ | Distinction des erreurs métier et techniques dans le domaine |
| ~~#71~~ | Encapsulation des collections de navigation |
| ~~#72~~ | Correction de la documentation relevée par la revue de la Phase 1 |

### Ajustement issu de la reprise des données

Répercussion dans le modèle de domaine des ajustements du `.speckit/` issus de la confrontation avec la base existante (ticket global #101), à réaliser avant les chantiers restants de la Phase 2.

| Ticket | Titre |
| --- | --- |
| ~~#102~~ | Ajustement du modèle de domaine issu de la reprise des données |

---

## Phase 2 — Contracts et API

Exposition du modèle de domaine via l'API : DTOs dans `Bdtheque.Contracts`, endpoints Minimal API dans `Bdtheque.Api` — administration (`/admin` : saisie, suppression selon `fonctionnel.md` § Suppression des entités) et consultation (`/catalog` : listes, navigation par initiale, recherche, fiches). Choix techniques : `choix-implementation.md` § Organisation de l'API et sections suivantes.

Hors périmètre (phases dédiées ci-dessous) : statistiques, conversion de devises, estimations.

| Ticket | Titre |
| --- | --- |
| ~~#73~~ | Socle des endpoints et des réponses d'erreur |
| ~~#74~~ | Entrée de navigation par initiale et clé de tri des auteurs |
| ~~#75~~ | Alignement des suppressions en base sur les règles de suppression |
| ~~#76~~ | Administration des genres et des univers |
| ~~#77~~ | Administration des éditeurs et de leurs collections |
| ~~#78~~ | Administration des auteurs |
| ~~#79~~ | Administration des séries |
| #103 | Ajustement de l'API des séries issu de la reprise des données |
| #80 | Administration des albums |
| #81 | Administration des éditions |
| #82 | Téléversement et gestion des visuels d'édition |
| #83 | Intentions d'achat |
| #84 | Listes et recherche de la consultation |
| #85 | Fiches détaillées de la consultation |

## Phase 3 — Frontend Blazor

Pages de consultation (public) et d'administration (authentifié), composants partagés, aide contextuelle.

Découpage précis à établir une fois la Phase 2 stabilisée ; probablement par écran (fiche album, fiche série, recherche, formulaires admin) plutôt que par entité.

## Phase 4 — Statistiques, devises et estimation de valeur

Dashboard public et statistiques (sur la collection uniquement) ; conversion des montants en euro (taux fixes, taux variables récupérés auprès de Frankfurter et mis en cache) ; valeur estimée des éditions (Random Forest, ML.NET), recalculée à partir de l'état courant de la base (`fonctionnel.md` § Calcul des estimations).

Découpage à établir à l'ouverture de la phase. La qualité de l'estimation de valeur ne peut être évaluée que sur des données réelles (collection issue de l'application existante).

## Phase 5 — Fonctionnalités de second plan

Identification des albums manquants d'une série ; estimation de sortie d'un nouvel album (`fonctionnel.md` § Fonctionnalités de second plan).

Découpage à établir à l'ouverture de la phase.

## Phase 6 — Outil pour agents IA

Serveur MCP exposant l'aide à la saisie, la recherche/consultation et les analyses et statistiques (`fonctionnel.md` § Interface pour agents IA ; `choix-implementation.md` § Outil pour agents IA). Dépend des API de consultation (Phase 2) et des statistiques (Phase 4).

Découpage à établir à l'ouverture de la phase, une fois le périmètre de l'aide à la saisie et le mécanisme d'authentification de l'agent précisés.
