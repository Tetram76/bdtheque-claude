---
type: Contrainte technique
title: 'Indépendance de l''outil pour agents IA'
description: 'Outil pour agents IA indépendant de l''API ; conteneur souhaité, exposition non souhaitée (non-contraintes).'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Indépendance de l'outil pour agents IA

L'implémentation de l'outil pour agents IA ([Interface pour agents IA](/fonctionnel/agents-ia/interface-pour-agents-ia.md)) doit être **indépendante de l'API** : déployer l'API ne doit pas impliquer de déployer l'outil pour agents IA.

**Non-contraintes** (ouvertures de réflexion explicites, laissées au choix de l'agent) :

- Le déploiement de l'outil sous forme de **conteneur**, comme le reste de l'application, est souhaitable mais **non imposé**.
- L'**exposition** de l'outil au-delà de `frontend` (seul conteneur exposé, cf. [Architecture des tiers](/contraintes-techniques/architecture-deploiement/architecture-des-tiers.md)) est **à envisager** si la contrainte d'exposition impose une surcharge disproportionnée, mais **non souhaitée** dans l'idéal.
