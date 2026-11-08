---
type: Choix d'implémentation
title: 'Concurrence d''accès'
description: 'Concurrence optimiste par xmin sur les racines d''agrégat, verrou et marquage de la racine à toute écriture.'
applies_to:
  - 'src/Bdtheque.Infrastructure/AggregateWriteExtensions.cs'
  - 'src/Bdtheque.Domain/Entities/Common/IAggregateRoot.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Concurrence d'accès

- **Concurrence optimiste** : un formulaire d'administration peut rester ouvert longtemps, et deux onglets ou appareils ouverts sur la même fiche écraseraient sinon silencieusement la modification de l'autre. La version d'une fiche est transmise au client à la lecture et renvoyée avec toute modification ou suppression ; un écart est une erreur **fonctionnelle** (`409`).
- **Jeton de version : la colonne système `xmin`** de PostgreSQL, présente sur chaque table, sans colonne ni migration à ajouter.
  - **Alternative écartée (ETag / `If-Match`)** : plus conforme à HTTP, mais un champ des contrats est plus simple à porter, par un formulaire comme par un appel d'outil, pour les clients qui partagent ces contrats (`frontend`, `mcp`).
- **Portée : la racine d'agrégat**, que le domaine désigne sans rien savoir de la persistance. Seule sa version est transmise et vérifiée : toute écriture sur un agrégat, même sur un enfant seul, **verrouille la ligne de la racine** avant toute autre écriture (ordre partagé avec la suppression, [Suppression des entités : mise en œuvre](../ecritures/suppression-des-entites-mise-en-oeuvre.md)) et la **marque modifiée**, pour que sa version change à chaque modification de l'agrégat. Sans cela, deux modifications concurrentes de ses enfants ne seraient jamais détectées ; ainsi, elles ne peuvent aboutir toutes deux, ce qui protège aussi les règles de l'agrégat sur plusieurs lignes ([Intention d'achat : agrégat porté par l'album](../domaine/intention-d-achat-agregat-porte-par-l-album.md)).
- **Un point d'entrée unique** de toute écriture sur un agrégat existant, dans une transaction (sans elle, le verrou serait relâché dès la fin de la lecture) : verrou, comparaison de version, chargement, marquage.
  - La version est comparée **dès le verrou**, et non à l'enregistrement : un formulaire périmé est signalé comme tel, et non par la première règle métier que ses valeurs obsolètes enfreindraient.
  - Le verrou **ouvre l'unité de travail** : aucune entité n'est encore suivie, toute lecture préalable se faisant sans suivi. EF Core ne rafraîchit jamais une instance déjà suivie : un agrégat lu avant son verrou pourrait porter des valeurs plus anciennes que la ligne verrouillée, que le marquage de la racine réécrirait par-dessus une modification concurrente.
