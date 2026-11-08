---
type: Choix d'implémentation
title: 'Identifiants'
description: 'Identifiants Guid v7 créés par le domaine.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Common/EntityBase.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Identifiants

- Toute entité reçoit son identifiant **dès sa construction, dans le domaine** : un `Guid` **version 7**, jamais généré par EF Core ni par la base. Connu avant l'enregistrement, il permet au domaine de relier des entités entre elles sans aller-retour en base ; ordonné dans le temps, il évite la fragmentation d'index des GUID aléatoires.
  - **Alternative écartée (entier auto-incrémenté par la base)** : l'identifiant n'existerait qu'après l'enregistrement, ce qui empêcherait le domaine de construire un agrégat complet (ex. un album et ses intentions d'achat) avant de le persister.
  - La clé est déclarée **jamais générée** (`ValueGeneratedNever`) : réputée générée mais déjà renseignée, EF Core croirait existant un enfant découvert par la navigation d'un parent chargé, et émettrait une mise à jour de zéro ligne au lieu d'une insertion.
