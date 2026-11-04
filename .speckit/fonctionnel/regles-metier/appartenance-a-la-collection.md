---
type: Règle métier
title: 'Appartenance à la collection'
description: 'Un album fait partie de la collection s''il a une édition possédée ; seules les éditions d''intention ne sont pas possédées.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Appartenance à la collection

Un album est considéré comme **faisant partie de la collection** uniquement s'il possède au moins une édition **possédée** (dont le `Mode d'acquisition` est renseigné). Un album sans édition possédée est un album catalogué mais non possédé — même s'il dispose d'une édition issue d'une intention d'achat non encore concrétisée.

Une édition peut exister dans la base sans être possédée (sans données d'acquisition). C'est le cas uniquement des éditions créées via une intention d'achat. Le `Mode d'acquisition` est optionnel en base, mais la saisie normale d'une édition continue à l'imposer : il n'est absent que lorsque l'édition est issue d'une intention d'achat non encore concrétisée.
