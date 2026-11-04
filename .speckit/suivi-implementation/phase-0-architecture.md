---
type: Plan d'implémentation
title: 'Phase 0 — Architecture'
description: 'Phase 0, architecture (v0.0.1).'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Phase 0 — Architecture

**Version** : `v0.0.1`, avec la mise en place du dépôt et du `.speckit/`.

Mise en place de la structure porteuse de l'application, indépendamment du modèle métier :

- Solution .NET découpée en 5 projets (`Bdtheque.Domain`, `Bdtheque.Contracts`, `Bdtheque.Infrastructure`, `Bdtheque.Api`, `Bdtheque.Frontend`).
- Architecture 3 conteneurs Docker (`frontend` / `api` / `db`), réseaux isolés, `docker-compose.yml`.
- Authentification cookie côté `frontend` (BFF) ; middleware de clé interne côté `api`.
- `DbContext` EF Core opérationnel (connexion PostgreSQL), sans entité métier.
- Endpoints `/health` sur `frontend` et `api` ; OpenAPI/Scalar en développement.
- Pipeline CI GitHub Actions (restauration, build, tests).
- Premiers tests d'intégration (health check, middleware de clé interne).

---
