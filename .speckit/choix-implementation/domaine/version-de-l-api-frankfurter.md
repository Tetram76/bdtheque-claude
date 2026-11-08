---
type: Choix d'implémentation
title: 'Version de l''API Frankfurter'
description: 'Taux de change par l''API Frankfurter v2, taux fixes en constantes (Phase 4).'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Version de l'API Frankfurter

- Le service de taux de change, Frankfurter, est imposé ([Gestion des devises](../../fonctionnel/valorisation/gestion-des-devises.md)) ; sa **version 2** est un choix : la version courante du service.
  - **Alternative écartée (v1)** : dépréciée au profit de la v2, elle n'évoluera plus.
- Les **taux fixes** (ex. Franc français) sont des constantes du code : ils ne changent jamais, et un appel au service n'apporterait rien. Les taux variables sont mis en cache par `api` ([Sources de données externes](../../contraintes-techniques/architecture-deploiement/sources-de-donnees-externes.md)).
