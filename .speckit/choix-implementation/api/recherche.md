---
type: Choix d'implémentation
title: 'Recherche'
description: 'Recherche simple par inclusion, texte recherché, filtres croisés des listes, ordre des listes, expressions partagées.'
applies_to:
  - 'src/Bdtheque.Api/Endpoints/CatalogFilters.cs'
  - 'src/Bdtheque.Api/Endpoints/CatalogExpressions.cs'
  - 'src/Bdtheque.Api/Endpoints/UniverseTree.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Recherche

- Recherche par **inclusion** (« contient »), insensible à la casse et aux accents, via la collation non déterministe `fr_case_accent_insensitive` ([Collation des colonnes texte](../persistance/collation-des-colonnes-texte.md)) appliquée à la requête : PostgreSQL 18 accepte `LIKE` sur une collation non déterministe. Aucun index dédié : un parcours séquentiel de quelques milliers de lignes reste instantané.
  - **Alternative écartée (`pg_trgm` / `unaccent`, recherche plein texte)** : extensions et index supplémentaires pour un gain nul à ce volume ; la recherche plein texte découpe en mots et racines, ce qui ne correspond pas à la recherche d'un fragment de titre ou de nom.
- **Texte recherché** : ce qui identifie chaque fiche dans les listes. Une série par son titre ; un album par son titre ou par celui de sa série, qui fait partie de son libellé ([Libellé d'un album](../../fonctionnel/presentation/libelle-d-un-album.md)) et seul identifie un album sans titre propre ; un auteur par son nom, son prénom, son pseudonyme ou son nom complet (`Prénom Nom`, et `Nom Prénom` de sa clé de tri) ; un éditeur, une collection éditeur, un genre ou un univers par son nom ; une édition par son ISBN, tirets et espaces ignorés de part et d'autre (un ISBN est saisi tel qu'imprimé, avec ou sans séparateurs) — ses autres composantes (éditeur, collection, album) se recherchent par les filtres croisés. Les caractères `%` et `_` saisis sont recherchés tels quels (échappés dans le motif `LIKE`).
- Les listes de consultation (`/catalog/…`) portent des **filtres croisés**, combinables entre eux et avec la recherche textuelle et la navigation par initiale, qui servent notamment aux fiches liées en nombre non borné ([Organisation de l'API](organisation-de-l-api.md), fiches de consultation) :
  - **albums** : d'une série, d'un auteur (ses contributions), d'un éditeur ou d'une collection éditeur (ses éditions), d'un genre ou d'un univers (genres et univers affichés, union avec ceux de la série, [Genres et univers d'un album](../../fonctionnel/regles-metier/genres-et-univers-d-un-album.md)) ;
  - **séries** : d'un auteur, d'un éditeur ou d'une collection éditeur, portés par la série elle-même (ses contributions, son éditeur et sa collection sont des données de la série autant que des templates, [Série — relations](../../modele-metier/relations-et-cardinalites.md#série--relations)) ou au travers de ses albums (leurs contributions, les éditeurs et collections de leurs éditions) ; d'un genre ou d'un univers (les siens) ;
  - **éditions** : d'un album, d'un éditeur, d'une collection éditeur ;
  - **collections éditeur** : d'un éditeur.
  - Un filtre sur un univers couvre ses sous-univers à tous les niveaux ([Hiérarchie des univers](../../fonctionnel/regles-metier/hierarchie-des-univers.md)), calculés en mémoire à partir du référentiel des univers chargé en entier (`UniverseTree`, qui fournit aussi les ancêtres d'un univers), comme pour son reparentage ([Organisation de l'API](organisation-de-l-api.md), chargement de la hiérarchie des univers), sans requête récursive.
  - Un filtre sur une fiche inexistante donne une liste vide, non une erreur : une liste filtrée n'adresse pas la fiche.
- Ces filtres croisés ne couvrent pas la **recherche avancée** ([Recherche avancée](../../fonctionnel/recherche-avancee/recherche-avancee.md)), mise en œuvre à part ([Recherche avancée : mise en œuvre](../recherche-avancee/recherche-avancee-mise-en-oeuvre.md)).
- La **navigation par initiale** ([Navigation par initiale : mise en œuvre](navigation-par-initiale-mise-en-oeuvre.md)) est un paramètre des listes des séries, des albums et des auteurs (`entry`) ; une entrée qui n'est pas l'une des 28 est une erreur technique, le frontend construisant la navigation.
- **Ordre des listes** : celui des clés de tri pour les séries, les albums (celle de la série tenant lieu de clé pour un album sans titre propre, puis numéro de tome) et les auteurs ; celui du nom pour les autres ; les éditions dans l'ordre de leurs albums, puis par année d'édition. Une liste d'albums filtrée sur une série reste une liste de résultats, dans cet ordre alphabétique : l'ordre des albums dans une série ([Ordre des albums dans une série](../../fonctionnel/regles-metier/ordre-des-albums-dans-une-serie.md)) est celui de la fiche de la série.
- Les **expressions partagées** par les listes et les fiches (libellés de l'album et de l'édition, appartenance à la collection, intention visant une édition, entrée d'un univers, contributions, ordre des albums) sont écrites une fois (`CatalogExpressions`, `Bdtheque.Api`) et insérées dans chaque requête par composition d'arbres d'expression, EF Core ne traduisant pas l'appel d'une méthode C#.
