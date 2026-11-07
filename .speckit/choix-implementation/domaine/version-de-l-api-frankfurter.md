---
type: Choix d'implémentation
title: 'Version de l''API Frankfurter'
description: 'Taux de change par l''API Frankfurter v2, taux fixes en constantes (Phase 4).'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Version de l'API Frankfurter

- Le service de taux de change (Frankfurter) est imposé ([Gestion des devises](../../fonctionnel/valorisation/gestion-des-devises.md)) ; sa version est un choix : **v2** (`https://api.frankfurter.dev/v2/…`), version courante du service.
  - **Alternative écartée (v1)** : dépréciée au profit de la v2 ; elle reste disponible, mais n'évoluera plus.
- Les taux **variables** sont mis en cache côté `api` ([Sources de données externes](../../contraintes-techniques/architecture-deploiement/sources-de-donnees-externes.md)) ; les taux **fixes** (ex. Franc français) sont des constantes en code.
