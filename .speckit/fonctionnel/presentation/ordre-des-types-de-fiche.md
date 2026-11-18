---
type: Règle métier
title: 'Ordre des types de fiche'
description: 'Ordre unique des types de fiche partout où l''application cite les fiches liées à une fiche (suppression et son refus, fiche, liste, fusion) : du contenant au contenu.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-18T14:00:00Z }
---

# Ordre des types de fiche

Partout où l'application cite, pour une fiche, plusieurs types de fiches qui lui sont liées (impact d'une suppression et son refus, ce qui désigne une collection dans la saisie de son éditeur, colonnes et décomptes d'une liste, candidats d'une fusion…), ces types viennent **toujours dans le même ordre**, quel que soit le type de la fiche (décision de l'utilisateur, #418) :

**séries, albums, éditeurs, collections, éditions, intentions d'acquisition, auteurs, contributions, visuels, genres, univers, recherches enregistrées.**

L'ordre va du contenant au contenu, en suivant le modèle ([Relations et cardinalités](../../modele-metier/relations-et-cardinalites.md)) : la série avant ses albums, l'éditeur avant ses collections, l'édition et l'intention d'acquisition qui la vise, puis les auteurs et leurs contributions, avant les visuels ; viennent ensuite les référentiels qui classent les fiches (genres, univers), puis les recherches enregistrées.

Exemples : supprimer un album « supprime 2 éditions, 1 intention d'acquisition, 2 contributions et 6 visuels » ; un genre est retiré de « 3 séries et 12 albums » ; une collection est désignée par « 1 série · 2 éditions · 1 intention ».

Les compteurs du tableau de bord, qui dénombrent la collection sans citer les fiches liées à une fiche, n'en relèvent pas.
