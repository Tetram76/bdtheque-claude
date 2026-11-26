---
type: Choix d'implémentation
title: 'Intention d''acquisition : agrégat porté par l''album'
description: 'Intentions d''acquisition portées par l''agrégat Album : acquisition, conversion, édition non possédée indissociable de son intention, fusion de deux albums et de deux éditions.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Album.cs'
  - 'src/Bdtheque.Domain/Entities/PurchaseIntent.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/PurchaseIntentConfiguration.cs'
  - 'src/Bdtheque.Api/Endpoints/PurchaseIntentEndpoints.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-26T10:30:00Z }
---

# Intention d'acquisition : agrégat porté par l'album

- **Les intentions d'un album appartiennent à l'agrégat Album** : ses règles ([Intention d'acquisition](../../modele-metier/intention-d-acquisition.md) : une intention « album », une par édition, un album visé en entier sans aucune édition) portent sur plusieurs lignes et deux tables, et seul l'album les voit toutes. Elles sont toujours chargées avec lui, pour qu'aucune règle ne soit évaluée sur un agrégat partiellement chargé ([Collections de navigation du domaine](../persistance/collections-de-navigation-du-domaine.md)).
  - **Alternative écartée (garde côté édition, l'édition connaissant son intention)** : elle ne voit pas l'intention sur l'album entier, et dépend du chargement de l'intention avec l'édition.
- **L'album est renseigné pour les deux sortes d'intention** (celui de l'édition visée pour une intention « édition ») : toutes les intentions d'un album tiennent sous une seule clé, l'agrégat se charge en une requête, et la base garantit la partie mono-ligne des règles par deux index uniques.
- **L'album est le seul moyen de rendre une édition possédée** (acquisition confirmée, nouvelle édition possédée, fusion de deux éditions), car seul il voit l'intention qu'une acquisition réalise : il la supprime et laisse les autres, après avoir tout contrôlé, si bien qu'un refus laisse l'agrégat intact. Une édition possédée ne perd jamais son mode d'acquisition : elle ne redevient jamais une intention ([Intention d'acquisition](../../fonctionnel/regles-metier/intention-d-acquisition.md)), sauf quand la fusion de deux éditions lui fait garder l'acquisition d'une édition visée, qu'a choisie l'utilisateur ([Fusion de deux fiches](../../fonctionnel/regles-metier/fusion-des-fiches.md)).
- **Conversion album ↔ édition par remplacement** de l'intention, plutôt que par modification de sa cible : la conversion vers l'album supprime l'édition visée, dont la suppression emporte en base, par cascade, les intentions qui la visent encore.
- **Édition non possédée et intention indissociables** : retirer une intention sur une édition supprime l'édition, dans la même transaction, et supprimer l'édition emporte son intention par cascade. L'application ne produit ainsi jamais d'édition non possédée sans intention.
- **Album visé en entier** : l'agrégat refuse une intention sur l'album, ou une conversion vers lui, dès que l'album a une édition ; l'acquisition et la conversion vers une édition le font sortir de cet état.
  - **Reprise des données** : une intention sur l'album, pour un album qui a des éditions, serait refusée par le domaine ([Cohérence entre tables](../persistance/coherence-entre-tables.md)) ; elle est soumise à l'arbitrage de l'utilisateur, comme un prix au doute persistant ([Reprise des prix antérieurs à l'euro : devise retenue](../reprise-donnees/reprise-des-prix-anterieurs-a-l-euro-devise-retenue.md)).
  - **Alternative écartée (correction automatique à la reprise)** : supprimer l'intention perdrait un souhait de l'utilisateur, et la porter sur une nouvelle édition en inventerait les caractéristiques.
- **Fusion de deux albums** ([Fusion de deux fiches](../../fonctionnel/regles-metier/fusion-des-fiches.md)) : l'album qui reste prend les éditions de l'autre et leurs intentions, puis applique à ses intentions sur l'album entier la règle de l'album visé en entier, que la même opération désigne pour l'écran avant la fusion : retirées dès qu'il a une édition, gardées une fois sinon.
- **Fusion de deux éditions** ([Fusion de deux fiches](../../fonctionnel/regles-metier/fusion-des-fiches.md)) : opérée par les albums des deux éditions (`Album.MergeEditions`), qui voient leurs intentions ; l'édition qui reste possédée perd les intentions des deux éditions, celle qui reste non possédée en garde une, la sienne, ou celle de l'autre édition, qui la vise alors, et passe avec elle à l'album retenu, que son autre édition empêche d'être visé en entier.
- **Alternative écartée (exclusion garantie aussi en base : contrainte d'exclusion, table de verrou, clé étrangère composite)** : arbitrage général de [Cohérence entre tables](../persistance/coherence-entre-tables.md). Les écritures concurrentes sur les intentions d'un album sont de toute façon sérialisées par la version de l'album ([Concurrence d'accès](../persistance/concurrence-d-acces.md)) : de deux transactions parties de la même version, la seconde échoue entièrement.
