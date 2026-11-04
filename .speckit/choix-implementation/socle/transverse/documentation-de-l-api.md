---
type: Choix d'implémentation
title: 'Documentation de l''API'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Documentation de l'API

- L'API expose sa spécification via **OpenAPI** (génération native ASP.NET Core, `Microsoft.AspNetCore.OpenApi`).
- Une interface de documentation interactive (**Scalar**, open source MIT) est exposée par `api` en environnement de développement et dans l'environnement de validation fonctionnelle ([Environnement de validation fonctionnelle](/choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md)) uniquement, jamais en production.
