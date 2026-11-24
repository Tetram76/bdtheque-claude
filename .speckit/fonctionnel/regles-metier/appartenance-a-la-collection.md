---
type: Règle métier
title: 'Appartenance à la collection'
description: 'Un album fait partie de la collection s''il a une édition possédée ; seules les éditions d''intention ne sont pas possédées ; trois états (Possédé, Intention, Catalogué), de même nom et de même sens partout où la possession d''un album est présentée, comptée ou filtrée.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-24T09:45:00Z }
---

# Appartenance à la collection

Un album est considéré comme **faisant partie de la collection** uniquement s'il possède au moins une édition **possédée** (dont le `Mode d'acquisition` est renseigné). Un album sans édition possédée est un album catalogué mais non possédé — même s'il dispose d'une édition issue d'une intention d'acquisition non encore concrétisée.

Une édition peut exister dans la base sans être possédée (sans données d'acquisition). C'est le cas uniquement des éditions créées via une intention d'acquisition. Le `Mode d'acquisition` est optionnel en base, mais la saisie normale d'une édition continue à l'imposer : il n'est absent que lorsque l'édition est issue d'une intention d'acquisition non encore concrétisée.

## États d'un album

Un album est dans l'un de trois états, et dans un seul :

- **Possédé** : il a au moins une édition possédée, qu'il fasse ou non l'objet d'intentions d'acquisition ;
- **Intention** : il n'a aucune édition possédée, et fait l'objet d'une intention d'acquisition, sur lui-même ou sur l'une de ses éditions ([Intention d'acquisition](intention-d-acquisition.md)) ;
- **Catalogué** : il n'a ni édition possédée, ni intention d'acquisition ; il n'est que catalogué. Cet état ne se nomme pas « Non possédé » : un album visé par une intention n'est pas possédé non plus (décision de l'utilisateur, #604).

Ces trois états, avec ce même nom et ce même sens, valent **partout** où l'appartenance d'un album est présentée, comptée ou filtrée (listes, fiches, décomptes, filtres), dans la consultation comme dans l'administration : « Catalogué » ne compte jamais un album visé par une intention d'acquisition (décision de l'utilisateur, #417). Un filtre ou une colonne qui la présente s'intitule **« Possession »**, et l'état d'un album visé s'y nomme « Intention », plus court que le nom de la fiche, « Intention d'acquisition » (décision de l'utilisateur, #534) ; ni ces états ni cet intitulé n'emploient le mot « collection », que les libellés réservent à la collection d'un éditeur ([Collection éditeur](../../modele-metier/collection-editeur.md)), pour que les deux notions se distinguent au premier coup d'œil (décision de l'utilisateur, #425). Une phrase qui parle de la collection de l'utilisateur garde le mot (« 7 albums dans la collection », « Complète dans la collection »).
