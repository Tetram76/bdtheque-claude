---
type: Gouvernance
title: 'Issues'
description: 'Usage des tickets GitHub : chantiers du plan, bugs et fonctionnalités hors plan ; recherche d''un ticket existant avant toute création.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-09T11:00:00Z }
---

# Issues

Les Issues GitHub (« tickets ») servent à :

- tracer **chaque chantier** du plan d'implémentation (cf. [Suivi par tickets](suivi-par-tickets.md) ci-dessous) ;
- tracer ponctuellement des **bugs** à corriger ou des **fonctionnalités** à implémenter dans le futur, hors plan d'implémentation.

## Avant de créer un ticket

**Aucun ticket n'est créé sans avoir d'abord cherché s'il en existe déjà un** sur le même sujet, ouvert ou fermé : un doublon disperse le suivi d'un même problème entre plusieurs tickets, et oblige l'utilisateur à le relever.

- La recherche porte sur les tickets du dépôt, **ouverts et fermés**, par plusieurs mots-clés tirés du sujet : nom du test, du fichier ou du symbole en cause, symptôme, écran ou fonctionnalité, numéro du ticket ou de la Pull Request d'origine.
- Un ticket **ouvert** sur le sujet est complété (commentaire : nouvelle occurrence, élément nouveau de la cause ou de la correction) et cité là où le nouveau ticket l'aurait été, au lieu d'en créer un autre.
- Un ticket **fermé** sur le sujet est relu : un problème qui revient après sa correction est un ticket nouveau, qui le cite ; un problème écarté (*not planned*) n'est pas recréé sans élément nouveau.
- La recherche et ce qu'elle a trouvé sont signalés à l'utilisateur avec la création, ou le complément, du ticket.
