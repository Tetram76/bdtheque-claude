---
type: Gouvernance
title: 'Lien entre Pull Requests et Issues'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Lien entre Pull Requests et Issues

- Une Pull Request a **zéro ou un** ticket de référence (cf. [Suivi par tickets](/gestion-projet/revue-tickets/suivi-par-tickets.md), correspondance), jamais plusieurs ; un ticket n'a qu'une seule PR, celle dont il est le ticket de référence. Les tickets associés au ticket de référence sont cités dans la description de la PR **sans mot-clé de fermeture**, qui fermerait aussi un ticket partiellement réalisé.
- Une PR qui réalise un chantier du plan d'implémentation est associée à **son ticket**, qu'elle ferme au merge. Hors plan d'implémentation, une PR peut n'avoir aucun ticket.
- Si une PR **répond** à un ticket (correction d'un bug tracé, implémentation d'une fonctionnalité tracée), elle **doit le référencer** (ex. mention `#<numéro>` dans la description).
- Le ticket de référence étant **entièrement traité** par sa PR, sa référence utilise un mot-clé de fermeture automatique GitHub (`Closes`, `Fixes`, `Resolves #<numéro>`), afin qu'il soit **automatiquement clôturé au merge** de la PR.
- Un ticket qui ne pourrait être traité que **partiellement** par la PR dont il est le ticket de référence est à découper en plusieurs tickets, chacun traité entièrement par sa propre PR, dès le démarrage de son chantier (cf. [Suivi par tickets](/gestion-projet/revue-tickets/suivi-par-tickets.md), découpage au démarrage d'un chantier).
