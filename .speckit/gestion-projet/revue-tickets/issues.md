---
type: Gouvernance
title: 'Issues'
description: 'Usage des tickets GitHub : chantiers du plan, bugs et fonctionnalités hors plan, type de tout ticket (Bug, Feature, Task), tickets de suivi des harnais, leurs étiquettes (celle du harnais, `harness`) et leur description, état de ce qui reste à corriger et non historique ; recherche d''un ticket existant avant toute création.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-13T18:30:00Z }
---

# Issues

Les Issues GitHub (« tickets ») servent à :

- tracer **chaque chantier** du plan d'implémentation (cf. [Suivi par tickets](suivi-par-tickets.md) ci-dessous) ;
- tracer ponctuellement des **bugs** à corriger ou des **fonctionnalités** à implémenter dans le futur, hors plan d'implémentation ;
- porter l'état d'un **harnais**, contrôle périodique de l'application ou du dépôt (§ Tickets de suivi des harnais).

## Type d'un ticket

Tout ticket reçoit, dès sa création, un **type** GitHub (*issue type*), parmi les trois du dépôt (exigence de l'utilisateur) :

- **`Bug`** : un élément de l'**application** déjà réalisé se comporte ou s'affiche autrement qu'attendu, au regard du `.speckit/`, des maquettes, du reste de l'application (ex. un formulaire qui ne pose pas le focus que posent les autres) ou de ce que l'utilisateur en attend sans avoir à le demander. Le ticket décrit un constat ou une cause, sans changement d'exigence.
  - **Le type suit le problème, pas la solution** : un défaut reste un `Bug` même si sa correction ajoute un mécanisme ou retravaille un texte.
  - Qu'un ticket soit une demande de l'utilisateur n'en fait pas une `Feature` : seule compte la nature du travail.
  - `Bug` est réservé à l'application : un défaut de l'outillage du dépôt (test instable, workflow, script) est une `Task`.
- **`Feature`** : un comportement ou une capacité de l'application, visible de l'utilisateur, que le `.speckit/` ne prévoit pas encore : un changement d'exigence, que l'utilisateur décide et qui est écrit dans `fonctionnel/` ou `visuel/` avant sa réalisation ([Gouvernance documentaire](../gouvernance/gouvernance-documentaire.md#qui-décide-du-contenu)).
- **`Task`** : tout ce qui ne change pas le comportement visible de l'application : ticket de suivi d'un harnais, toujours ; chantier technique (outillage, CI, harnais, déploiement, exploitation, sécurité, montée de version) ; travail technique interne, comme celui de la reprise des données.

Un chantier fonctionnel du plan est un `Bug` ou une `Feature` selon la nature du travail à faire ; un chantier technique du plan est une `Task`. Un sous-ticket reçoit le type de son propre contenu, qui peut différer de celui de son ticket parent.

## Tickets de suivi des harnais

Chaque harnais tient **un** ticket de suivi, ouvert tant qu'il a quelque chose à suivre, que ne réalise aucune Pull Request et qui n'a pas de jalon ([Versions et releases](../depot/versions-et-releases.md)). Il est désigné par ses étiquettes, jamais par son titre, que l'utilisateur peut changer :

| Harnais | Ce que suit son ticket | Étiquette du harnais |
| --- | --- | --- |
| Sécurité et mises à jour ([Mises à jour des dépendances](../depot/mises-a-jour-des-dependances.md)) | vulnérabilités connues | `security-audit` |
| Fonctionnel ([Contrôle de cohérence fonctionnelle](controle-de-coherence-fonctionnelle.md)) | état du contrôle de cohérence fonctionnelle | `ergonomy-audit` |
| Qualité ([Contrôle qualité hebdomadaire](../depot/controle-qualite-hebdomadaire.md)) | dette qualité du code de `main` | `quality-audit` |

- **Étiquette du harnais** : elle réunit **tous** les tickets d'un harnais, son ticket de suivi comme les chantiers qui le créent ou le font évoluer (exigence de l'utilisateur).
  - **Tickets ouverts par un harnais** : tout ticket qu'ouvre un harnais, ou l'agent qui traite ce qu'il relève (suite d'un contrôle de cohérence, correction d'une dette qualité ou d'une vulnérabilité reportée, montée de version, chantier d'amélioration issu d'une revue de l'existant), reçoit l'étiquette de ce harnais dès sa création, quel que soit le harnais (exigence de l'utilisateur) : le ticket de suivi et ses suites se retrouvent ensemble, sans que l'étiquette `harness` leur soit posée.
- **Étiquette `harness`** : commune aux tickets de suivi, elle n'est portée **que** par eux (exigence de l'utilisateur), jamais par un chantier, fût-il celui d'un harnais : elle les réunit en une liste, à part des chantiers.
- **Un état, pas un historique** (exigences de l'utilisateur) : la description d'un ticket de suivi ne porte que **ce qui reste à corriger**, et une seule ligne sur son traitement, la date de sa dernière prise en compte par l'agent (`Dernière prise en compte : AAAA-MM-JJ`), qui dit si le traitement est dû. Rien d'autre : ni compte rendu, ni justification, ni Pull Request, ni entrée déjà traitée. La raison d'un rejet est portée par le mécanisme de l'outil qui l'a relevé (ex. commentaire du marquage dans SonarQube Cloud), celle d'une correction par sa Pull Request ou son ticket.
  - **Entrée traitée, aussitôt retirée** : une entrée corrigée ou rejetée n'est plus à traiter. Dès que son traitement est acquis, c'est-à-dire dès que le contrôle ne la relèverait plus (marquage dans l'outil, fusion sur `main` de la Pull Request qui la corrige ou porte son rejet dans le code), l'agent la retire de la description, sans attendre le contrôle suivant ; ou, quand le contrôle seul sait ce qui reste (ex. vulnérabilités d'une image après une montée de version), il relance le contrôle, qui réécrit la liste, et remet la ligne de sa prise en compte. Retirée plus tôt, une entrée encore relevée reviendrait au contrôle suivant comme nouvelle, et le ferait échouer pour elle.
- **Ticket de suivi retrouvé par ses deux étiquettes** : un harnais, comme l'agent, le retrouve par l'étiquette du harnais **et** `harness`, jamais par la première seule, qui désigne aussi ses chantiers. Qui crée un ticket de suivi, harnais ou agent, lui pose les deux.

## Avant de créer un ticket

**Aucun ticket n'est créé sans avoir d'abord cherché s'il en existe déjà un** sur le même sujet, ouvert ou fermé : un doublon disperse le suivi d'un même problème entre plusieurs tickets, et oblige l'utilisateur à le relever.

- La recherche porte sur les tickets du dépôt, **ouverts et fermés**, par plusieurs mots-clés tirés du sujet : nom du test, du fichier ou du symbole en cause, symptôme, écran ou fonctionnalité, numéro du ticket ou de la Pull Request d'origine.
- Un ticket **ouvert** sur le sujet est complété (commentaire : nouvelle occurrence, élément nouveau de la cause ou de la correction) et cité là où le nouveau ticket l'aurait été, au lieu d'en créer un autre.
- Un ticket **fermé** sur le sujet est relu : un problème qui revient après sa correction est un ticket nouveau, qui le cite ; un problème écarté (*not planned*) n'est pas recréé sans élément nouveau.
- La recherche et ce qu'elle a trouvé sont signalés à l'utilisateur avec la création, ou le complément, du ticket.
