---
type: Gouvernance
title: 'Lien entre Pull Requests et Issues'
description: 'Référencement du ticket dans la PR, mot-clé de fermeture, tickets associés, tickets reliés tous fermés par la PR de leur chantier, étiquettes reprises du ticket.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-19T19:00:00Z }
---

# Lien entre Pull Requests et Issues

- Une Pull Request a **zéro ou un** ticket de référence (cf. [Suivi par tickets](suivi-par-tickets.md), correspondance), jamais plusieurs ; un ticket n'a qu'une seule PR, celle dont il est le ticket de référence. Les tickets associés au ticket de référence sont cités dans la description de la PR **sans mot-clé de fermeture**, qui fermerait aussi un ticket partiellement réalisé.
- **Tickets reliés** : seule exception, la PR d'un chantier de tickets reliés ([Suivi par tickets](suivi-par-tickets.md), tickets reliés) n'a pas de ticket de référence : elle réalise entièrement chacun de ces tickets, qui n'ont qu'elle, et sa description les cite **tous avec un mot-clé de fermeture** (`Closes #<numéro>` pour chacun), pour qu'ils soient tous clôturés à sa fusion.
- Une PR qui réalise un chantier du plan d'implémentation est associée à **son ticket**, qu'elle ferme au merge. Hors plan d'implémentation, une PR peut n'avoir aucun ticket.
- **Étiquettes** : une PR reprend les étiquettes du ticket qu'elle réalise, son ticket de référence, ou celles de chacun de ses tickets reliés, dès sa création, et suit celles qu'ils reçoivent ou perdent ensuite (exigence de l'utilisateur).
- Si une PR **répond** à un ticket (correction d'un bug tracé, implémentation d'une fonctionnalité tracée), elle **doit le référencer** (ex. mention `#<numéro>` dans la description).
- Le ticket de référence étant **entièrement traité** par sa PR, sa référence utilise un mot-clé de fermeture automatique GitHub (`Closes`, `Fixes`, `Resolves #<numéro>`), afin qu'il soit **automatiquement clôturé au merge** de la PR.
- Un ticket qui ne pourrait être traité que **partiellement** par la PR qui le réalise, dont il est le ticket de référence ou l'un des tickets reliés, est à découper en plusieurs tickets, chacun traité entièrement par sa propre PR, dès le démarrage de son chantier (cf. [Suivi par tickets](suivi-par-tickets.md), découpage au démarrage d'un chantier).
