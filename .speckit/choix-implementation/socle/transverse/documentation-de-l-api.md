---
type: Choix d'implémentation
title: 'Documentation de l''API'
description: 'OpenAPI et Scalar, servis hors production.'
applies_to:
  - 'src/Bdtheque.Api/Program.cs'
  - 'src/Bdtheque.Api/Hosting/**'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-08T17:00:00Z }
---

# Documentation de l'API

- La spécification **OpenAPI** de l'API est produite par la génération native d'ASP.NET Core, sans bibliothèque tierce.
  - **Alternative écartée (Swashbuckle)** : ASP.NET Core ne l'inclut plus depuis .NET 9, la génération native l'ayant remplacée.
- Son interface interactive, **Scalar** (open source, MIT), n'est servie qu'en développement et dans l'[Environnement de validation fonctionnelle](../../deploiement/environnement-de-validation-fonctionnelle.md), là où l'on explore l'API ; jamais en production, où elle ne servirait personne.
