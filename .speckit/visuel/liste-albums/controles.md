---
type: Référence visuelle
title: 'Contrôles'
description: 'Texte indicatif, focus clavier et croix d''effacement des zones de recherche, dans tous les thèmes.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-20T15:00:00Z }
---

# Contrôles

Dans tous les thèmes, quelle que soit la couleur de leur texte, le texte indicatif des champs se distingue du texte saisi et reste lisible, deux exigences qui priment : idéalement, sa couleur est celle du texte, atténuée, sans que l'atténuation nuise à l'une ou à l'autre, et jamais par une opacité (décision de l'utilisateur, #466). L'indicateur de focus clavier est un contour de la couleur du texte, détaché du contrôle par une bande de la couleur de la surface qui l'entoure : il contraste avec le contrôle, coloré ou non, comme avec cette surface ([`suivi-implementation/`](../../suivi-implementation/index.md), Phase 3, note sur le style des contrôles).

Toute zone de recherche, celle de l'en-tête, de la consultation comme de l'administration, celle d'une liste, celle de l'autre fiche dans le dialogue de fusion ou celle d'une édition par son ISBN, à l'accueil de l'administration, porte à son extrémité une **croix d'effacement** dessinée par l'application, de la taille et de la couleur de la loupe des zones de recherche, la même dans tous les navigateurs et dans tous les thèmes ; celle du navigateur n'apparaît jamais (décisions de l'utilisateur, #542) :

- elle ne paraît que lorsqu'un texte est saisi, jamais sur un champ vide ;
- dans une liste, elle ramène aussitôt la liste sans recherche, à sa première page, ses autres critères et l'entrée de la navigation par initiale restant ceux où l'on est, comme « Effacer la recherche » ([Contenu](contenu.md)) ; dans le dialogue de fusion, elle le ramène de même à son invite d'avant toute recherche ; dans l'en-tête et la recherche par ISBN, qui n'ont pas de liste derrière eux, elle vide le champ ;
- au clavier, elle suit immédiatement le champ dans l'ordre de tabulation, avec l'indicateur de focus des contrôles ; les technologies d'assistance l'annoncent « Effacer la recherche ».

La croix qui retire la fiche choisie dans le choix d'une fiche par son nom a le même visuel, l'action étant la même : réinitialiser le champ (décision de l'utilisateur, #542) ; la loupe de ce choix a de même la taille de celle des zones de recherche.
