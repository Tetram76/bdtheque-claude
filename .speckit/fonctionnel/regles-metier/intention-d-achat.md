---
type: Règle métier
title: 'Intention d''achat'
description: 'Intention sur un album ou une édition, exclusions, conversion, édition non possédée indissociable, réalisation d''une intention.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Intention d'achat

Gestion en **mode administration** (authentifié). Consultation de la liste en **mode consultation** (public, lecture seule).

Deux cas d'usage distincts :

1. **Album** : l'utilisateur souhaite acquérir un album en particulier, sans contrainte sur l'édition. L'intention porte sur l'album ; n'importe quelle édition satisfera l'intention.
2. **Édition spécifique** : l'utilisateur souhaite acquérir une édition précise d'un album (éditeur, collection, année, etc.), qu'il possède déjà cet album dans une autre édition ou non.

Ces deux cas sont représentés par une entité **Intention d'achat** distincte, liée soit à un Album soit à une Édition.

Pour un même album, les deux cas sont **mutuellement exclusifs** : l'album est visé soit par une intention portant sur l'album lui-même, soit par des intentions portant sur une ou plusieurs de ses éditions (une intention par édition), jamais les deux à la fois.

Une intention ne peut porter sur l'**album** que tant qu'il n'a **aucune édition**, possédée ou visée par une intention : tant que l'album n'est pas dans la collection, l'utilisateur peut ne pas connaître les éditions disponibles ; mais dès qu'il en possède une ou en vise une en particulier, souhaiter acquérir une édition quelconque n'a plus de sens, et c'est une édition précise qu'il vise (cas d'usage 2).

Un cas d'usage permettra de **convertir** une intention portant sur l'album en intention portant sur une édition, et inversement, dans le respect de ces règles : une intention portant sur une édition ne se convertit en intention portant sur l'album que si cette édition est la **seule** de l'album, la conversion la supprimant (cf. ci-dessous).

Une intention d'achat est toujours **présentée comme une intention d'achat**, jamais comme un album ou une édition : une édition visée ne figure jamais parmi les éditions (d'un album, d'une liste, d'un décompte), mais parmi les intentions d'achat, avec ce qu'elle vise. Un album visé reste en revanche un album, dont l'appartenance à la collection signale qu'il fait l'objet d'une intention d'achat.

Une édition **non possédée** et l'intention qui la vise sont **indissociables** : l'une ne va pas sans l'autre. Supprimer l'une supprime l'autre, après confirmation (cf. [Suppression des entités](suppression-des-entites.md)) ; de même, convertir une intention portant sur une édition en intention portant sur l'album supprime l'édition visée, après confirmation. Une édition non possédée ne peut donc jamais exister sans intention d'achat.

## Réalisation d'une intention

La **confirmation d'un achat** est un **acte explicite** de l'utilisateur, effectué depuis un écran ou un menu dédié.

Une intention d'achat réalisée est **supprimée** :

- une intention portant sur l'**album** est réalisée, donc supprimée, par l'achat de n'importe laquelle de ses éditions (cf. cas d'usage 1) ;
- une intention portant sur une **édition** est réalisée, donc supprimée, par l'achat de cette édition. Lorsqu'un album fait l'objet de plusieurs intentions (sur plusieurs de ses éditions), **seules les intentions des éditions achetées** sont supprimées ; les autres sont conservées.

Une édition **déjà possédée** ne peut pas faire l'objet d'une intention d'achat : une édition achetée ne redevient jamais une intention. Pour acquérir un second exemplaire d'une même édition, l'utilisateur ajoute à l'album une **nouvelle édition** aux mêmes caractéristiques (une fonction de duplication d'édition pourra faciliter cette saisie) et place son intention sur cette nouvelle édition.

La saisie d'une intention d'achat présente le **minimum de champs** à renseigner : les caractéristiques propres à l'exemplaire possédé (gratuité, occasion, etc.) n'y figurent pas et ne font l'objet d'aucune règle particulière pour une édition non possédée.

Les autres règles métier sont à définir.
