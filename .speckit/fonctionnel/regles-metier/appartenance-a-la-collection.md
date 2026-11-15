---
type: Règle métier
title: 'Appartenance à la collection'
description: 'Un album fait partie de la collection s''il a une édition possédée ; seules les éditions d''intention ne sont pas possédées ; trois états, de même sens partout où l''appartenance d''un album est présentée, comptée ou filtrée.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-15T10:15:00Z }
---

# Appartenance à la collection

Un album est considéré comme **faisant partie de la collection** uniquement s'il possède au moins une édition **possédée** (dont le `Mode d'acquisition` est renseigné). Un album sans édition possédée est un album catalogué mais non possédé — même s'il dispose d'une édition issue d'une intention d'achat non encore concrétisée.

Une édition peut exister dans la base sans être possédée (sans données d'acquisition). C'est le cas uniquement des éditions créées via une intention d'achat. Le `Mode d'acquisition` est optionnel en base, mais la saisie normale d'une édition continue à l'imposer : il n'est absent que lorsque l'édition est issue d'une intention d'achat non encore concrétisée.

## États d'un album

Un album est dans l'un de trois états, et dans un seul :

- **Dans la collection** : il a au moins une édition possédée, qu'il fasse ou non l'objet d'intentions d'achat ;
- **Intention d'achat** : il n'a aucune édition possédée, et fait l'objet d'une intention d'achat, sur lui-même ou sur l'une de ses éditions ([Intention d'achat](intention-d-achat.md)) ;
- **Hors collection** : il n'a ni édition possédée, ni intention d'achat.

Ces trois états, avec ce même sens, valent **partout** où l'appartenance d'un album est présentée, comptée ou filtrée (listes, fiches, décomptes, filtres), dans la consultation comme dans l'administration : « Hors collection » ne compte jamais un album visé par une intention d'achat (décision de l'utilisateur, #417).
