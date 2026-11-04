---
type: Choix d'implémentation
title: 'Identifiants'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Identifiants

- Toute entité (`EntityBase`) reçoit son identifiant **dès sa construction, dans le domaine** : un `Guid` **version 7** (`Guid.CreateVersion7()`), jamais généré par EF Core ni par la base (`ValueGeneratedNever`, appliqué à toutes les entités par `BdthequeDbContext`). Un identifiant connu avant l'enregistrement permet au domaine de relier des entités entre elles (clés étrangères, comparaisons par identifiant) sans aller-retour en base ; la version 7, ordonnée dans le temps, évite la fragmentation d'index qu'engendrent des GUID aléatoires.
  - **Alternative écartée (entier auto-incrémenté par la base)** : l'identifiant n'existerait qu'après l'enregistrement, ce qui empêcherait le domaine de construire un agrégat complet (ex. un album et ses intentions d'achat) avant de le persister.
  - `ValueGeneratedNever` est indispensable : avec une clé réputée générée mais déjà renseignée, EF Core considère qu'un enfant découvert par la navigation d'un parent chargé existe déjà, et émet une mise à jour de zéro ligne au lieu d'une insertion.
