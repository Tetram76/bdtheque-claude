---
type: Choix d'implémentation
title: 'Migrations EF Core'
description: 'Migration unique avant le premier déploiement, append-only ensuite.'
applies_to:
  - 'src/Bdtheque.Infrastructure/Migrations/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Migrations EF Core

- **Tant qu'aucune base n'a été déployée** (aucun déploiement consigné dans [`journal-evenements.md`](/journal-evenements.md)), le schéma est porté par une **migration unique** (`InitialCreate`), régénérée plutôt qu'enrichie de migrations successives : une migration intermédiaire ne protège alors aucune donnée et ne fait qu'ajouter du code de conversion jamais exécuté sur des données réelles. Une régénération n'est acceptée que si le schéma produit est **identique** à celui des migrations qu'elle remplace (comparaison des `pg_dump --schema-only`).
- **Dès le premier déploiement consigné**, les migrations deviennent **append-only** : chaque évolution du modèle ajoute une migration, aucune migration existante n'est modifiée ni regroupée.
