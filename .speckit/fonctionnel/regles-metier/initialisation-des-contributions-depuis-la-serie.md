---
type: Règle métier
title: 'Initialisation des contributions depuis la série'
description: 'Contributions de la série recopiées sur un album rattaché qui n''en a pas, ou, à sa création, qui n''a que celles de la série choisie précédemment.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-09T15:00:00Z }
---

# Initialisation des contributions depuis la série

Lors du rattachement d'un album à une série, si l'album n'a encore aucune contribution saisie, les contributions de la série sont recopiées sur l'album comme point de départ.

À la **création** d'un album, tant que l'utilisateur n'a pas commencé à **personnaliser** ses auteurs, c'est-à-dire tant qu'ils sont encore exactement ceux de la série choisie précédemment, changer de série applique les contributions de la nouvelle série **en remplacement** des précédentes. Dès qu'il les a personnalisés (un auteur ajouté, retiré, ou changé de rôle), changer de série ne les touche plus.

En **modification** d'un album déjà enregistré, ses auteurs sont toujours considérés comme personnalisés, même identiques à ceux de sa série : changer de série ne les recopie que s'il n'en a aucun.

La recopie a lieu **dès que la série est choisie** dans la fiche de saisie de l'album, avant tout enregistrement : l'utilisateur voit ce qui est repris, et le corrige avant d'enregistrer. Enregistrer ne recopie rien : des auteurs retirés après la recopie ne reviennent pas.

L'album reste la source de vérité : ces contributions peuvent ensuite être modifiées ou supprimées indépendamment de la série.
