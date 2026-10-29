# Choix d'Implémentation

Ce dossier recense les choix d'implémentation retenus par l'agent **de sa propre initiative** (architecture, bibliothèque, algorithme non imposé, etc.), qu'il y ait eu ou non délibération explicite entre plusieurs options. `contraintes-techniques/` reste réservé aux contraintes **imposées** — par l'utilisateur, des normes/lois, ou une réalité externe non négociable : l'agent n'y ajoute rien de sa propre initiative.

Ces choix ne sont pas imposés : ils peuvent être remis en cause si une meilleure option apparaît ou si une nouvelle contrainte le justifie (cf. `gestion-projet/gouvernance.md` § « Prise de décision »).

| Fichier | Contenu |
| --- | --- |
| `index.md` | Organisation du code, versions et bibliothèques. |
| `socle.md` | Frontend, thèmes, internationalisation, authentification, documentation de l'API, journalisation. |
| `outillage.md` | Outillage de développement .NET : packages, tests, couverture, validation des commentaires, cohérence des migrations, pipeline CI. |
| `deploiement.md` | Images de base des conteneurs, frontend derrière le reverse proxy du NAS, publication des images Docker, version de PostgreSQL, application du schéma au démarrage, environnement de validation fonctionnelle. |
| `persistance.md` | Conventions EF Core, identifiants, cohérence entre tables, collation, migrations, concurrence d'accès, dates renseignées automatiquement, collections de navigation du domaine. |
| `api.md` | Organisation de l'API, navigation par initiale, recherche simple. |
| `ecritures.md` | Erreurs métier, fonctionnelles et techniques ; suppression des entités. |
| `domaine.md` | Intention d'achat, valeur d'une édition, devises, journal des modifications, paramétrage de l'application, édition « À la une », validation de l'ISBN, visuels. |
| `recherche-avancee.md` | Recherche avancée et recherches enregistrées. |
| `agents-ia.md` | Outil pour agents IA. |
| `mesure-audience.md` | Mesure d'audience de l'interface web : enregistrement, compteurs, conservation, indicateurs. |
| `reprise-donnees.md` | Accès à la base existante (Firebird), reprise des prix antérieurs à l'euro. Lu seulement pour la reprise des données (`AGENTS.md`). |

---

## Organisation du code

La solution .NET est découpée en projets par responsabilité, sous `src/` :

| Projet | Rôle |
| --- | --- |
| `Bdtheque.Domain` | Entités du modèle métier, enums, value objects. Aucune dépendance à EF Core ni à un framework web. |
| `Bdtheque.Infrastructure` | Implémentation EF Core / Npgsql : `DbContext`, configurations d'entités, migrations. |
| `Bdtheque.Contracts` | Contrats d'échange (DTOs) exposés par l'API et consommés par ses clients internes (`frontend`, `mcp`). Découplés des entités du domaine. |
| `Bdtheque.Api` | Conteneur `api` : endpoints Minimal API, règles applicatives, service de taux de change, estimation ML.NET. |
| `Bdtheque.Frontend` | Conteneur `frontend` : composants Blazor (rendu côté serveur), authentification cookie, appels HTTP vers `Bdtheque.Api`. |
| `Bdtheque.Mcp` | Conteneur `mcp` (Phase 6) : outils MCP pour agents IA, appels HTTP vers `Bdtheque.Api` via `Bdtheque.Contracts` (`choix-implementation/agents-ia.md` § Outil pour agents IA). Ne référence ni `Bdtheque.Api` ni `Bdtheque.Domain`. |
| `Bdtheque.HealthProbe` | Sonde du bilan de santé des images publiées (`HEALTHCHECK`), qui n'ont ni shell ni `curl` (`choix-implementation/deploiement.md` § Images de base des conteneurs) : interroge une URL et sort en `0` sur un succès, en `1` sinon. |
| `Bdtheque.ValidationData` | Chargement des données de l'environnement de validation fonctionnelle, au travers de l'API d'administration (`choix-implementation/deploiement.md` § Environnement de validation fonctionnelle). Ne référence que `Bdtheque.Contracts` ; jamais déployé. |

Chaque projet source a vocation à avoir son miroir sous `tests/` (ex. `Bdtheque.Api.Tests`), créé dès que son contenu justifie des tests — proportionnalité définie dans la règle de non-régression de `gestion-projet/`. S'y ajoutent les tests de bout en bout de l'application complète (`Bdtheque.EndToEnd.Tests`) et la bibliothèque que partagent les tests (`Bdtheque.Testing`), cf. `choix-implementation/outillage.md` § Outillage de développement .NET.

## Versions et bibliothèques

- **Aucune version n'est imposée** : `contraintes-techniques/` n'impose jamais un numéro de version. Les versions retenues sont des choix : **.NET 10** (LTS) et **EF Core 10**, alignés.
- **Versions LTS uniquement** pour la plateforme .NET (runtime, SDK, ASP.NET Core, EF Core, et le fournisseur Npgsql, aligné sur EF Core) : d'après la politique de support de .NET, une version majeure paraît chaque novembre, les versions paires étant LTS (trois ans de support) et les impaires STS (deux ans). Une STS ne prolonge pas le support — la 11 le perd en même temps que la 10 — et imposerait une montée de plus : la montée suivante est **.NET 12**, LTS, à sa parution (`gestion-projet/depot.md` § Mises à jour des dépendances). Les versions mineures et correctives sont suivies au fil de leur parution.
- **Le pilote PostgreSQL n'est pas imposé** (seule la base l'est) : le fournisseur EF Core retenu est **Npgsql** (`Npgsql.EntityFrameworkCore.PostgreSQL` 10).
- **Estimation de la valeur des éditions** : le modèle **Random Forest** est imposé comme règle métier (`fonctionnel/valorisation.md` § Estimation de la valeur des éditions). Sa bibliothèque d'implémentation, **ML.NET**, embarquée dans le conteneur `api`, est un choix : même langage et même runtime que le reste de l'application, sans service externe.
