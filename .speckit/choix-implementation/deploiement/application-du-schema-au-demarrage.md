---
type: Choix d'implémentation
title: 'Application du schéma au démarrage'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Application du schéma au démarrage

- Les migrations EF Core sont appliquées **automatiquement au démarrage** du conteneur `api` (`Database.MigrateAsync()`). Pas de conteneur ou d'étape d'initialisation dédiée : un déploiement neuf sur une base vide crée le schéma dès le premier démarrage.
