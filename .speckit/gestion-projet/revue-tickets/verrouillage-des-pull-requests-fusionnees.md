---
type: Gouvernance
title: 'Verrouillage des Pull Requests fusionnées'
description: 'Verrouillage « Resolved » des commentaires d''une PR fusionnée.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Verrouillage des Pull Requests fusionnées

Une fois une Pull Request fusionnée, ses commentaires sont **verrouillés** avec la raison **« Resolved »** (`lock_reason: resolved` de l'API GitHub, qui traite une PR comme un ticket), la PR étant résolue. Le verrouillage vient **après** la dernière contribution à la PR. Il s'applique à toute PR fusionnée, qu'elle soit associée ou non à un ticket ; le verrouillage du ticket qu'elle ferme suit sa propre règle (cf. [Suivi par tickets](/gestion-projet/revue-tickets/suivi-par-tickets.md)).
