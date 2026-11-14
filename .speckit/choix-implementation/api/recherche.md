---
type: Choix d'implémentation
title: 'Recherche'
description: 'Recherche simple par inclusion, texte recherché, filtres croisés des listes, ordre des listes, expressions partagées.'
applies_to:
  - 'src/Bdtheque.Api/Endpoints/CatalogFilters.cs'
  - 'src/Bdtheque.Api/Endpoints/CatalogExpressions.cs'
  - 'src/Bdtheque.Api/Endpoints/UniverseTree.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-14T12:00:00Z }
---

# Recherche

- **Recherche par inclusion** (« contient »), insensible à la casse et aux accents par la collation non déterministe ([Collation des colonnes texte](../persistance/collation-des-colonnes-texte.md)), **appliquée à la recherche** et non déduite des colonnes, dont la plupart gardent la collation française déterministe ; **sans index dédié** : un parcours de quelques milliers de lignes reste instantané.
  - **Alternative écartée (`pg_trgm` / `unaccent`, recherche plein texte)** : extensions et index de plus pour un gain nul à ce volume ; le plein texte découpe en mots et en racines, ce qui ne correspond pas à la recherche d'un fragment de titre ou de nom.
- **Texte recherché : ce qui identifie chaque fiche dans les listes** (titre, nom, nom complet d'un auteur dans ses deux ordres ; titre de la série pour un album, qu'elle identifie dans son libellé, [Libellé d'un album](../../fonctionnel/presentation/libelle-d-un-album.md)). Une édition se recherche par son ISBN, séparateurs ignorés de part et d'autre (ISBN cherché et ISBN saisi), un ISBN étant saisi tel qu'imprimé, et souvent collé depuis une application bureautique : espaces et tirets, insécables compris (espace insécable, espace fine, trait d'union insécable, tiret demi-cadratin…), dans les listes comme dans la recherche avancée ; ses autres composantes, par les filtres croisés.
- **Filtres croisés des listes de consultation**, combinables avec la recherche et la navigation par initiale : ils servent aux fiches liées en nombre non borné ([Organisation de l'API](organisation-de-l-api.md), fiches de consultation).
  - Une série est filtrée par ses propres auteurs, éditeur et collection **comme par ceux de ses albums** : ce sont des données de la série autant que des templates ([Série — relations](../../modele-metier/relations-et-cardinalites.md#série--relations)).
  - Un filtre sur un univers couvre ses sous-univers ([Hiérarchie des univers](../../fonctionnel/regles-metier/hierarchie-des-univers.md)), calculés en mémoire sur le référentiel des univers chargé en entier, comme pour son reparentage, sans requête récursive.
  - Un filtre sur une fiche inexistante donne une liste vide, et non une erreur : une liste filtrée n'adresse pas la fiche.
  - La recherche avancée est mise en œuvre à part ([Recherche avancée : mise en œuvre](../recherche-avancee/recherche-avancee-mise-en-oeuvre.md)).
- **Ordre des listes** : clé de tri (titres, auteurs), nom sinon. Une liste d'albums filtrée sur une série reste une liste de résultats, dans l'ordre alphabétique : l'ordre des albums dans la série ([Ordre des albums dans une série](../../fonctionnel/regles-metier/ordre-des-albums-dans-une-serie.md)) est celui de sa fiche.
- **Expressions partagées** par les listes et les fiches (libellés, appartenance à la collection, contributions, ordre des albums…) : écrites une fois, et insérées dans chaque requête par composition d'arbres d'expression, EF Core ne traduisant pas l'appel d'une méthode C#.
