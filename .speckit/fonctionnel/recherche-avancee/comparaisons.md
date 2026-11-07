---
type: Règle métier
title: 'Comparaisons'
description: 'Comparaisons par type, donnée absente, texte, dates, montants, comparaison entre deux données.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Comparaisons

Chaque critère compare une donnée à une valeur, ou à une autre donnée du même type (cf. ci-dessous), avec les méthodes de comparaison adaptées au type de la donnée :

| Type de donnée | Comparaisons |
| --- | --- |
| Texte | contient, ne contient pas, est égal à, est différent de, commence par |
| Nombre, date, montant | égal, différent, inférieur, inférieur ou égal, supérieur, supérieur ou égal, compris entre |
| Énuméré, booléen, fiche liée | est, n'est pas, est l'un de, n'est aucun de |
| Toute donnée optionnelle | est renseignée, n'est pas renseignée |

- Une comparaison n'est satisfaite que si les **données connues l'établissent**. Une donnée absente ne satisfait donc aucune comparaison, positive ou négative, hormis « n'est pas renseignée » : « état n'est pas Excellent » ne retient pas une édition dont l'état est absent. Le **NON** d'un critère (cf. [Combinaison des critères](combinaison-des-criteres.md)) retient au contraire tout ce que le critère n'établit pas : « NON (état est Excellent) » retient cette édition. Une comparaison négative et le NON de la comparaison positive ne sont donc pas équivalents : l'une exige que la donnée soit établie différente, l'autre seulement qu'elle ne soit pas établie égale.
- Un **texte** se compare par défaut sans tenir compte de la casse ni des accents : « contient epee » retient `L'Épée`. **Chaque critère** sur un texte peut, à l'image de la version de comparaison d'un montant, **respecter la casse et les accents**, pour toutes les comparaisons de texte : « contient Épée » ne retient alors ni `L'épée` ni `L'Epee`.
- Une **date** se compare à la précision choisie par l'utilisateur : jour, mois ou année (ex. « publiée tel mois », « publiée telle année »). Selon la règle ci-dessus, une date partielle ne satisfait un critère que si sa précision suffit à l'établir : une date connue à l'année seule ne satisfait ni « publiée en mars 2020 » ni « publiée en dehors de mars 2020 », mais « NON (publiée en mars 2020) » la retient.
- Un **montant** se compare dans la version que **chaque critère** choisit, si bien qu'une même recherche peut comparer un montant en euros du jour et un autre en euros courants ou dans sa devise d'origine (cf. [Gestion des devises](../valorisation/gestion-des-devises.md)) : **euros du jour** par défaut, **euros courants**, ou **devise d'origine**. En euros du jour comme en euros courants, un montant non convertible ne satisfait pas le critère. Dans la devise d'origine, la valeur du critère porte sa devise, et seuls les montants dans cette devise peuvent le satisfaire : « prix supérieur à 100 USD » ne retient aucun prix en francs, des montants de devises différentes ne se comparant qu'une fois convertis.
- Une donnée peut aussi être comparée à une **autre donnée du même type**, de la même fiche ou d'une fiche liée (ex. « édition dont l'année d'édition est postérieure à sa date d'acquisition, ou antérieure à la date de première publication de l'album »), par les comparaisons de son type qui portent sur une seule valeur. Comme pour une valeur, la comparaison n'est satisfaite que si les deux données sont renseignées et que les données connues l'établissent ; deux dates se comparent à la précision choisie par l'utilisateur, chacune couvrant toute la période où elle est connue (une date connue à l'année couvre, au mois, ses douze mois).
