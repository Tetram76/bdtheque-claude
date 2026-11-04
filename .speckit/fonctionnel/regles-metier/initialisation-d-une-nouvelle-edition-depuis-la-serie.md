---
type: Règle métier
title: 'Initialisation d''une nouvelle édition depuis la série'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Initialisation d'une nouvelle édition depuis la série

Lors de la création d'une nouvelle édition pour un album, si la série de l'album définit des valeurs template, les champs suivants sont pré-remplis :

- Éditeur et collection éditeur
- Catégorie d'édition, état, reliure, orientation, sens de lecture, format
- En couleur

Le template de la série fait foi : un champ sans valeur dans le template n'est pas pré-rempli. Pour un album **sans série**, les champs sont pré-remplis avec leur **valeur par défaut** (cf. [Édition](/modele-metier/edition.md)). Ce pré-remplissage ne concerne que le formulaire de saisie, jamais la base. Ces valeurs restent modifiables : l'édition est source de vérité, la série n'est qu'un point de départ.
