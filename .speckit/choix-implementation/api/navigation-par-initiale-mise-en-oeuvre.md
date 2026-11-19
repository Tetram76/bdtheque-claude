---
type: Choix d'implémentation
title: 'Navigation par initiale : mise en œuvre'
description: 'Entrée de navigation calculée par le domaine selon la collation française, stockée et indexée ; clé de tri des auteurs ; éditeurs et genres par leur nom, éditions et intentions d''acquisition par leur album.'
applies_to:
  - 'src/Bdtheque.Domain/Common/NavigationEntryCalculator.cs'
  - 'src/Bdtheque.Domain/Common/TitleSortKeyCalculator.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/NavigationEntryColumn.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-19T20:29:46Z }
---

# Navigation par initiale : mise en œuvre

- L'**entrée de navigation** ([Entrées de la navigation par initiale](../../fonctionnel/presentation/tri-et-navigation-par-initiale.md#entrées-de-la-navigation-par-initiale)) est **calculée par le domaine** à partir de la clé de tri, et **stockée** à côté d'elle avec un index (entrée, clé de tri) : la navigation filtre sur l'entrée, puis trie sur la clé. Sa cohérence avec la clé de tri reste garantie par le domaine seul, une contrainte en base supposant de réécrire en SQL la collation française ([Cohérence entre tables](../persistance/coherence-entre-tables.md)).
  - **Alternative écartée (calcul en SQL à chaque requête)** : une seconde implémentation de la même normalisation, à maintenir identique, et aucun index possible.
  - **Entrée stockée non recalculée quand l'ICU de l'application change** : elle suit l'ordre de l'ICU de l'image d'`api` au moment de son calcul, et la version de cet ordre n'est pas enregistrée avec elle. Entre ICU 74 et ICU 78, seules six lettres ajoutées par Unicode 16 (U+A7CB à U+A7CD, U+A7DA à U+A7DC) changent d'entrée, de `@` vers `E`, `S` ou `L` ; le recalcul des entrées stockées, au changement de la version du tri (`CompareInfo.Version`), est à prévoir quand l'image `aspnet` changera de version d'Ubuntu, seule occasion où cet ICU change ([Images de base des conteneurs](../deploiement/images-de-base-des-conteneurs.md)).
- La **lettre de base** d'un caractère est celle que donne l'**ordre alphabétique français lui-même** (collation ICU française, sans casse ni diacritiques, celle des colonnes, [Collation des colonnes texte](../persistance/collation-des-colonnes-texte.md)) : la dernière lettre `A`–`Z` qui la précède ou l'égale. Une lettre se range ainsi exactement là où le tri de la liste la place.
  - **Alternative écartée (décomposition Unicode et table de translittération)** : la décomposition laisse intactes de nombreuses lettres latines (`Œ`, `Æ`, `Ø`, `Ł`, `Đ`…), une table écrite à la main n'est jamais exhaustive, et elle pourrait contredire l'ordre du tri.
  - La navigation **filtre toujours sur l'entrée stockée**, jamais sur une plage de la clé de tri : dans l'ordre linguistique seul, l'entrée `@` n'est pas contiguë (la ponctuation se classe avant les chiffres, les autres alphabets après `Z`). Les listes la regroupent en triant d'abord sur le rang de l'entrée ([Recherche](recherche.md), ordre des listes).
- Un album **sans titre** n'a ni clé de tri ni entrée stockées : la substitution par sa série ([Album](../../modele-metier/album.md), clé de tri) est résolue **à la lecture**.
  - **Alternative écartée (valeur de la série recopiée sur l'album)** : la copie deviendrait fausse à chaque modification du titre ou de la clé de tri de la série.
- Les **auteurs** reçoivent la même clé de tri stockée que les titres ([Artistes](../../fonctionnel/presentation/tri-et-navigation-par-initiale.md#artistes)), calculée par le domaine : sans elle, la base ne pourrait ni trier ni paginer les artistes.
- Les **éditeurs** et les **genres**, triés par leur nom ou leur libellé tel quel, sans clé de tri, reçoivent la même **entrée stockée**, calculée par le domaine depuis ce nom ou ce libellé, et indexée avec lui ([Liste par liste](../../visuel/listes/liste-par-liste.md)).
  - **Alternative écartée (clé de tri propre aux éditeurs et aux genres)** : leur liste est triée par nom ; une clé à saisir ou à calculer n'aurait rien d'autre à porter que ce nom.
- Les **éditions** et les **intentions d'acquisition** n'ont pas d'entrée propre : elles sont rangées sous celle de leur album, lue à la requête par une **expression unique**, que partage la liste des albums, l'album sans titre prenant celle de sa série.
  - **Alternative écartée (entrée recopiée sur l'édition ou l'intention)** : la copie deviendrait fausse à chaque modification du titre de l'album ou de sa série, comme pour l'album sans titre.
