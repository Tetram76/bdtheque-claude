---
type: Règle métier
title: 'Initialisation des contributions depuis la série'
description: 'Auteurs de la série repris sur un album qu''on y rattache, rôle par rôle, dès le choix de la série.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-09T15:00:00Z }
---

# Initialisation des contributions depuis la série

Lors du rattachement d'un album à une série, les auteurs de la série sont repris sur l'album comme point de départ, **rôle par rôle** : pour chaque rôle où l'album n'a encore aucun auteur, il reçoit ceux de la série pour ce rôle ; un rôle où l'album a déjà des auteurs garde les siens.

La reprise a lieu **dès que la série est choisie** dans la fiche de saisie de l'album, avant tout enregistrement : l'utilisateur voit ce qui est repris, et le corrige avant d'enregistrer. Enregistrer ne reprend rien : des auteurs retirés après la reprise ne reviennent pas.

L'album reste la source de vérité : ces contributions peuvent ensuite être modifiées ou supprimées indépendamment de la série.
