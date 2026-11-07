---
type: Plan d'implémentation
title: 'Phase 1 — Modèle de domaine'
description: 'Phase 1, modèle de domaine (v0.0.2), consolidation et ajustement de la reprise.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Phase 1 — Modèle de domaine

**Version** : `v0.0.2`.

Implémentation des entités du modèle métier ([`modele-metier/`](../modele-metier/index.md)) dans `Bdtheque.Domain` et `Bdtheque.Infrastructure` (configurations EF Core + migrations), séquencée selon les dépendances entre entités. Le `DbContext` reçoit progressivement ses `DbSet`.

| Ticket | Titre |
| --- | --- |
| ~~#60~~ | Ajout des référentiels de base du domaine |
| ~~#61~~ | Ajout de l'entité Série |
| ~~#62~~ | Ajout de l'entité Album |
| ~~#63~~ | Ajout de l'entité Contribution |
| ~~#64~~ | Ajout de l'entité Édition |
| ~~#65~~ | Ajout de l'entité Visuel d'édition |
| ~~#66~~ | Ajout de l'entité Intention d'achat |

## Consolidation avant la Phase 2

Corrections issues de la revue complète de la Phase 1, à réaliser avant d'ouvrir la Phase 2.

| Ticket | Titre |
| --- | --- |
| ~~#67~~ | Tests de persistance sur PostgreSQL réel |
| ~~#68~~ | Passage à PostgreSQL 18 et regroupement des migrations |
| ~~#69~~ | Tri linguistique des colonnes texte en base |
| ~~#70~~ | Distinction des erreurs métier et techniques dans le domaine |
| ~~#71~~ | Encapsulation des collections de navigation |
| ~~#72~~ | Correction de la documentation relevée par la revue de la Phase 1 |

## Ajustement issu de la reprise des données

Répercussion dans le modèle de domaine des ajustements du `.speckit/` issus de la confrontation avec la base existante (ticket global #101), à réaliser avant les chantiers restants de la Phase 2.

| Ticket | Titre |
| --- | --- |
| ~~#102~~ | Ajustement du modèle de domaine issu de la reprise des données |

---
