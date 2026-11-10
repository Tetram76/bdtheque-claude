---
type: Choix d'implémentation
title: 'Navigation par initiale : mise en œuvre'
description: 'Entrée de navigation calculée par le domaine selon la collation française, stockée et indexée ; clé de tri des auteurs.'
applies_to:
  - 'src/Bdtheque.Domain/Common/NavigationEntryCalculator.cs'
  - 'src/Bdtheque.Domain/Common/TitleSortKeyCalculator.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/NavigationEntryColumn.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Navigation par initiale : mise en œuvre

- L'**entrée de navigation** ([Entrées de la navigation par initiale](../../fonctionnel/presentation/tri-et-navigation-par-initiale.md#entrées-de-la-navigation-par-initiale)) est **calculée par le domaine** à partir de la clé de tri, et **stockée** à côté d'elle avec un index (entrée, clé de tri) : la navigation filtre sur l'entrée, puis trie sur la clé. Sa cohérence avec la clé de tri reste garantie par le domaine seul, une contrainte en base supposant de réécrire en SQL la collation française ([Cohérence entre tables](../persistance/coherence-entre-tables.md)).
  - **Alternative écartée (calcul en SQL à chaque requête)** : une seconde implémentation de la même normalisation, à maintenir identique, et aucun index possible.
- La **lettre de base** d'un caractère est celle que donne l'**ordre alphabétique français lui-même** (collation ICU française, sans casse ni diacritiques, celle des colonnes, [Collation des colonnes texte](../persistance/collation-des-colonnes-texte.md)) : la dernière lettre `A`–`Z` qui la précède ou l'égale. Une lettre se range ainsi exactement là où le tri de la liste la place.
  - **Alternative écartée (décomposition Unicode et table de translittération)** : la décomposition laisse intactes de nombreuses lettres latines (`Œ`, `Æ`, `Ø`, `Ł`, `Đ`…), une table écrite à la main n'est jamais exhaustive, et elle pourrait contredire l'ordre du tri.
  - La navigation **filtre toujours sur l'entrée stockée**, jamais sur une plage du tri : l'entrée `@` n'y est pas contiguë (la ponctuation se classe avant les chiffres, les autres alphabets après `Z`).
- Un album **sans titre** n'a ni clé de tri ni entrée stockées : la substitution par sa série ([Album](../../modele-metier/album.md), clé de tri) est résolue **à la lecture**.
  - **Alternative écartée (valeur de la série recopiée sur l'album)** : la copie deviendrait fausse à chaque modification du titre ou de la clé de tri de la série.
- Les **auteurs** reçoivent la même clé de tri stockée que les titres ([Artistes](../../fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes)), calculée par le domaine : sans elle, la base ne pourrait ni trier ni paginer les artistes.
