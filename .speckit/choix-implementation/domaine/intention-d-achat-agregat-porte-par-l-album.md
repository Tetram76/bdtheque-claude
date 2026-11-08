---
type: Choix d'implémentation
title: 'Intention d''achat : agrégat porté par l''album'
description: 'Intentions d''achat portées par l''agrégat Album : acquisition, conversion, édition non possédée indissociable de son intention.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Album.cs'
  - 'src/Bdtheque.Domain/Entities/PurchaseIntent.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/PurchaseIntentConfiguration.cs'
  - 'src/Bdtheque.Api/Endpoints/PurchaseIntentEndpoints.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Intention d'achat : agrégat porté par l'album

- **Les intentions d'un album appartiennent à l'agrégat Album** : ses règles ([Intention d'achat](../../modele-metier/intention-d-achat.md) : une intention « album », une par édition, un album visé en entier sans aucune édition) portent sur plusieurs lignes et deux tables, et seul l'album les voit toutes. Elles sont toujours chargées avec lui, pour qu'aucune règle ne soit évaluée sur un agrégat partiellement chargé ([Collections de navigation du domaine](../persistance/collections-de-navigation-du-domaine.md)).
  - **Alternative écartée (garde côté édition, l'édition connaissant son intention)** : elle ne voit pas l'intention sur l'album entier, et dépend du chargement de l'intention avec l'édition.
- **L'album est renseigné pour les deux sortes d'intention** (celui de l'édition visée pour une intention « édition ») : toutes les intentions d'un album tiennent sous une seule clé, l'agrégat se charge en une requête, et la base garantit la partie mono-ligne des règles par deux index uniques.
- **L'album est le seul moyen de rendre une édition possédée** (achat confirmé, nouvelle édition possédée), car seul il voit l'intention qu'une acquisition réalise : il la supprime et laisse les autres, après avoir tout contrôlé, si bien qu'un refus laisse l'agrégat intact. Une édition possédée ne perd jamais son mode d'acquisition : elle ne redevient jamais une intention ([Intention d'achat](../../fonctionnel/regles-metier/intention-d-achat.md)).
- **Conversion album ↔ édition par remplacement** de l'intention, plutôt que par modification de sa cible : la conversion vers l'album supprime l'édition visée, dont la suppression emporte en base, par cascade, les intentions qui la visent encore.
- **Édition non possédée et intention indissociables** : retirer une intention sur une édition supprime l'édition, dans la même transaction, et supprimer l'édition emporte son intention par cascade. L'application ne produit ainsi jamais d'édition non possédée sans intention.
- **Album visé en entier** : l'agrégat refuse une intention sur l'album, ou une conversion vers lui, dès que l'album a une édition ; l'acquisition et la conversion vers une édition le font sortir de cet état.
  - **Reprise des données** : une intention sur l'album, pour un album qui a des éditions, serait refusée par le domaine ([Cohérence entre tables](../persistance/coherence-entre-tables.md)) ; elle est soumise à l'arbitrage de l'utilisateur, comme un prix au doute persistant ([Reprise des prix antérieurs à l'euro : devise retenue](../reprise-donnees/reprise-des-prix-anterieurs-a-l-euro-devise-retenue.md)).
  - **Alternative écartée (correction automatique à la reprise)** : supprimer l'intention perdrait un souhait de l'utilisateur, et la porter sur une nouvelle édition en inventerait les caractéristiques.
- **Alternative écartée (exclusion garantie aussi en base : contrainte d'exclusion, table de verrou, clé étrangère composite)** : arbitrage général de [Cohérence entre tables](../persistance/coherence-entre-tables.md). Les écritures concurrentes sur les intentions d'un album sont de toute façon sérialisées par la version de l'album ([Concurrence d'accès](../persistance/concurrence-d-acces.md)) : de deux transactions parties de la même version, la seconde échoue entièrement.
