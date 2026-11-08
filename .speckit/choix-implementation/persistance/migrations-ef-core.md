---
type: Choix d'implémentation
title: 'Migrations EF Core'
description: 'Migration unique avant le premier déploiement, append-only ensuite.'
applies_to:
  - 'src/Bdtheque.Infrastructure/Migrations/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Migrations EF Core

- **Avant le premier déploiement** consigné dans [`journal-evenements.md`](../../journal-evenements.md), le schéma tient en une **migration unique**, régénérée plutôt qu'enrichie : une migration intermédiaire ne protège alors aucune donnée, et n'ajoute que du code de conversion jamais exécuté sur des données réelles. Une régénération n'est acceptée que si le schéma produit est identique à celui des migrations qu'elle remplace (comparaison des `pg_dump --schema-only`).
- **Dès le premier déploiement**, les migrations sont **append-only** : chaque évolution du modèle en ajoute une, aucune existante n'est modifiée ni regroupée, une base déployée ayant déjà appliqué les précédentes.
