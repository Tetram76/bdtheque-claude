---
type: Choix d'implémentation
title: 'Navigation par initiale : mise en œuvre'
description: 'Entrée de navigation calculée par le domaine selon la collation française, stockée et indexée ; clé de tri des auteurs.'
applies_to:
  - 'src/Bdtheque.Domain/Common/NavigationEntryCalculator.cs'
  - 'src/Bdtheque.Domain/Common/TitleSortKeyCalculator.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/NavigationEntryColumn.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Navigation par initiale : mise en œuvre

- L'**entrée de navigation** (`A`–`Z`, `#` ou `@`, cf. [Entrées de la navigation par initiale](../../fonctionnel/presentation/tri-et-navigation-par-initiale.md#entrées-de-la-navigation-par-initiale)) est **calculée par le domaine** à partir de la clé de tri et **stockée** à côté d'elle, sur les séries, les albums et les auteurs, avec un index composite (entrée, clé de tri) : la navigation filtre sur l'entrée puis trie sur la clé. Une contrainte CHECK limite la colonne aux 28 entrées (`NavigationEntryCalculator.Entries`) ; sa cohérence avec la clé de tri, qui supposerait de réécrire en SQL la collation française, reste garantie par le domaine seul ([Cohérence entre tables](../persistance/coherence-entre-tables.md)). L'entrée suit strictement sa propre clé de tri : un album **sans titre** n'a ni clé de tri ni entrée stockées, et la substitution par la série ([Album](../../modele-metier/album.md), clé de tri) est résolue **à la lecture**, en combinant la valeur de l'album et celle de sa série dans la requête. La valeur de la série n'est jamais recopiée sur l'album : une telle copie deviendrait fausse à chaque modification du titre ou de la clé de tri de la série. Calcul de l'entrée à partir du premier caractère de la clé de tri, testé unitairement dans le domaine : la **lettre de base est celle que détermine l'ordre alphabétique français lui-même**, c'est-à-dire la collation ICU française (`CompareInfo` de la culture `fr-FR`, en ignorant casse et diacritiques — la même famille de collation que celle des colonnes, [Collation des colonnes texte](../persistance/collation-des-colonnes-texte.md)) :
  1. un chiffre décimal → `#` ;
  2. une lettre qui se classe entre `A` et la fin des mots en `Z` → la **dernière lettre `A`–`Z` qui la précède ou l'égale** dans cet ordre (vérifié : `É` → `E`, `Œ` → `O`, `Æ` → `A`, `Ø` → `O`, `Ł` → `L`, `Đ`/`Ð` → `D`, `Ŧ` → `T`, `Ħ` → `H`, `ß` → `S`, `ı` → `I`, `Ŋ` → `N`) ;
  3. tout le reste → `@` : ponctuation, symboles, et lettres que l'ordre alphabétique place après `Z` (autres alphabets, lettres latines sans lettre de base comme `Þ`).
  - Une lettre se range ainsi exactement là où le place le tri de la liste. La navigation **filtre toujours sur l'entrée stockée**, jamais sur une plage du tri : l'entrée `@` n'est pas contiguë dans la liste triée (la ponctuation se classe avant les chiffres et les lettres, les autres alphabets après `Z`).
  - **Alternative écartée (décomposition Unicode et table de translittération)** : la décomposition laisse intactes de nombreuses lettres latines (`Œ`, `Æ`, `Ø`, `Ł`, `Đ`, `Ŧ`…), et une table écrite à la main pour les compléter n'est jamais exhaustive ; elle pourrait en outre contredire l'ordre du tri.
  - **Alternative écartée (calcul en SQL à chaque requête)** : reproduirait en SQL une normalisation déjà écrite en C#, deux implémentations à maintenir identiques, et empêcherait l'usage d'un index.
- Les **auteurs** reçoivent la même clé de tri stockée que les titres ([Artistes](../../fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes)), calculée par le domaine à partir de l'identité, non modifiable manuellement : sans elle, le tri et la pagination des artistes ne pourraient pas être faits par la base.
