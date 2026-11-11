---
type: Gouvernance
title: 'Issues'
description: 'Usage des tickets GitHub : chantiers du plan, bugs et fonctionnalités hors plan, tickets de suivi des harnais, leur titre et leurs étiquettes (celle du harnais, `harness`) ; recherche d''un ticket existant avant toute création.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-11T13:22:47Z }
---

# Issues

Les Issues GitHub (« tickets ») servent à :

- tracer **chaque chantier** du plan d'implémentation (cf. [Suivi par tickets](suivi-par-tickets.md) ci-dessous) ;
- tracer ponctuellement des **bugs** à corriger ou des **fonctionnalités** à implémenter dans le futur, hors plan d'implémentation ;
- porter l'état d'un **harnais**, contrôle périodique de l'application ou du dépôt (§ Tickets de suivi des harnais).

## Tickets de suivi des harnais

Chaque harnais tient **un** ticket de suivi, ouvert tant qu'il a quelque chose à suivre, que ne réalise aucune Pull Request et qui n'a pas de jalon ([Versions et releases](../depot/versions-et-releases.md)). Son titre nomme le harnais, puis ce qu'il suit (« Harnais `<harnais>` : `<objet>` ») :

| Harnais | Ticket de suivi | Étiquette du harnais |
| --- | --- | --- |
| Sécurité et mises à jour ([Mises à jour des dépendances](../depot/mises-a-jour-des-dependances.md)) | « Harnais sécurité : Vulnérabilités connues » | `security-audit` |
| Fonctionnel ([Contrôle de cohérence fonctionnelle](controle-de-coherence-fonctionnelle.md)) | « Harnais fonctionnel : Cohérence ergonomique et fonctionnelle » | `ergonomy-audit` |
| Qualité | « Harnais qualité : Dette qualité » | `quality-audit` |

- **Étiquette du harnais** : elle réunit **tous** les tickets d'un harnais, son ticket de suivi comme les chantiers qui le créent ou le font évoluer (exigence de l'utilisateur).
- **Étiquette `harness`** : commune aux tickets de suivi, elle n'est portée **que** par eux (exigence de l'utilisateur), jamais par un chantier, fût-il celui d'un harnais : elle les réunit en une liste, à part des chantiers.
- **Ticket de suivi retrouvé par ses deux étiquettes** : un harnais, comme l'agent, le retrouve par l'étiquette du harnais **et** `harness`, jamais par la première seule, qui désigne aussi ses chantiers, ni par son titre, que l'utilisateur peut changer. Qui crée un ticket de suivi, harnais ou agent, lui pose les deux.

## Avant de créer un ticket

**Aucun ticket n'est créé sans avoir d'abord cherché s'il en existe déjà un** sur le même sujet, ouvert ou fermé : un doublon disperse le suivi d'un même problème entre plusieurs tickets, et oblige l'utilisateur à le relever.

- La recherche porte sur les tickets du dépôt, **ouverts et fermés**, par plusieurs mots-clés tirés du sujet : nom du test, du fichier ou du symbole en cause, symptôme, écran ou fonctionnalité, numéro du ticket ou de la Pull Request d'origine.
- Un ticket **ouvert** sur le sujet est complété (commentaire : nouvelle occurrence, élément nouveau de la cause ou de la correction) et cité là où le nouveau ticket l'aurait été, au lieu d'en créer un autre.
- Un ticket **fermé** sur le sujet est relu : un problème qui revient après sa correction est un ticket nouveau, qui le cite ; un problème écarté (*not planned*) n'est pas recréé sans élément nouveau.
- La recherche et ce qu'elle a trouvé sont signalés à l'utilisateur avec la création, ou le complément, du ticket.
