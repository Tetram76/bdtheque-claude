---
type: Choix d'implémentation
title: 'Application du schéma au démarrage'
description: 'Migrations appliquées au démarrage d''api.'
applies_to:
  - 'src/Bdtheque.Api/Program.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Application du schéma au démarrage

- Les migrations EF Core sont appliquées **au démarrage d'`api`** : un déploiement neuf, sur une base vide, crée son schéma dès le premier démarrage, sans autre étape.
  - **Alternative écartée (conteneur ou étape d'initialisation dédiés)** : un élément de plus à orchestrer au déploiement, pour le même résultat.
