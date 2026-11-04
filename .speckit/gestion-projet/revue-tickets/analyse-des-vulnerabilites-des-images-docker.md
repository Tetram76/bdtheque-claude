---
type: Gouvernance
title: 'Analyse des vulnérabilités des images Docker'
description: 'Analyse Docker Scout des images à chaque PR et avant publication ; traitement d''une vulnérabilité relevée.'
applies_to:
  - 'docker/**'
  - '.github/workflows/ci.yml'
  - '.github/workflows/docker-publish.yml'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Analyse des vulnérabilités des images Docker

Les images Docker de l'application sont analysées par **Docker Scout** lors de la revue de chaque Pull Request, par le CI (check requis, cf. [Intégration continue (CI)](/gestion-projet/depot/integration-continue-ci.md)), et de nouveau avant toute publication. L'analyse échoue dès qu'une image ajoute une vulnérabilité **corrigeable** par rapport à l'image publiée, qu'elle ne doit ni introduire ni voir publiée ([Déploiement](/contraintes-techniques/architecture-deploiement/deploiement.md)).

Une vulnérabilité ainsi relevée est **traitée dans la mesure du possible** : son correctif est appliqué (version corrigée de l'image de base ou de la dépendance concernée). Si ce correctif ne peut pas être appliqué (ex. il impose une montée de version incompatible avec l'application), la Pull Request reste bloquée : la vulnérabilité est évaluée dans le contexte réel de l'application (impact, probabilité de survenance) et soumise à la décision de l'utilisateur.
