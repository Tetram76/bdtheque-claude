---
type: Choix d'implémentation
title: 'Conventions de persistance (EF Core)'
description: 'Énumérations en entier à valeurs explicites et leur registre, chargement des collections en requêtes séparées.'
applies_to:
  - 'src/Bdtheque.Infrastructure/BdthequeDbContext.cs'
  - 'src/Bdtheque.Domain/Enums/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Conventions de persistance (EF Core)

- **Énumérations persistées en entier, à valeurs explicites** : réordonner ou ajouter un membre ne change jamais le sens des lignes déjà persistées. Un **registre append-only** des valeurs, vérifié par un test, garde la trace de toute valeur, même après le retrait de son membre : une unicité vérifiée sur les seuls membres déclarés ne détecterait pas la réaffectation d'une valeur retirée à un nouveau membre, qui réinterpréterait les lignes existantes. Chaque écriture d'une énumération rejette une valeur hors définition (ex. un entier arbitraire reçu par l'API).
  - **Alternative écartée (chaîne, nom du membre)** : sa lisibilité en base est réelle mais modeste. Elle couplerait le nom des membres à la donnée : un renommage deviendrait une migration de données implicite, sans filet, jusqu'aux contraintes CHECK du schéma qui citent un membre. Elle empêcherait aussi le tri SQL des énumérations dont l'ordre métier n'est pas alphabétique (ex. types de visuel), que l'entier, assigné dans l'ordre métier, permet. Elle n'allège en rien la reprise des données, qui traduit de toute façon chaque valeur de l'ancienne base.
  - **Renumérotation permise jusqu'au premier déploiement** consigné dans [`journal-evenements.md`](../../journal-evenements.md) (seuil de [Migrations EF Core](migrations-ef-core.md)) : aucune ligne n'existe encore, et les valeurs peuvent suivre l'ordre métier (ex. un état inséré entre deux autres) ; registre et énumérations des contrats sont alors renumérotés ensemble. Ensuite, un membre ajouté reçoit toujours une valeur nouvelle.
  - **Reprise des données** : chaque valeur de l'ancienne base est traduite par une correspondance explicite sur son **libellé**, jamais sur sa valeur numérique, sans rapport avec celle du nouveau modèle.
- **Collections chargées en requêtes séparées**, par défaut pour toute requête : un album est toujours chargé avec ses éditions, intentions et contributions ([Collections de navigation du domaine](collections-de-navigation-du-domaine.md)), qu'une requête unique joindrait en produit cartésien. Les écritures (racine verrouillée, [Concurrence d'accès](concurrence-d-acces.md)) et les lectures assemblées sous un même instantané restent cohérentes ; ailleurs, un enfant ajouté entre deux requêtes est sans conséquence pour un affichage, le formulaire portant la version de la racine. Les tests échouent sur une requête unique qui chargerait plusieurs collections.
  - **Alternative écartée (découpage requête par requête)** : chaque requête qui charge un album devrait y penser, même celles qui ne mentionnent pas ses collections.
