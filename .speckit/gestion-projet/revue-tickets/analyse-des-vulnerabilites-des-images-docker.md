---
type: Gouvernance
title: 'Analyse des vulnérabilités des images Docker'
description: 'Analyse Docker Scout des images à chaque PR qui les modifie et avant publication ; traitement d''une vulnérabilité relevée.'
applies_to:
  - 'docker/**'
  - '.github/workflows/ci.yml'
  - '.github/workflows/docker-publish.yml'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-13T17:30:00Z }
---

# Analyse des vulnérabilités des images Docker

Les images Docker de l'application sont analysées par **Docker Scout** lors de la revue de chaque Pull Request qui modifie ce dont elles sont faites, par le CI (check requis, cf. [Intégration continue (CI)](../depot/integration-continue-ci.md)), et de nouveau avant toute publication. L'analyse échoue dès qu'une image contient une vulnérabilité **corrigeable** par le dépôt, nouvelle ou déjà présente dans l'image publiée : une image n'en contient aucune, et une image qui en contient une n'est jamais publiée ([Déploiement](../../contraintes-techniques/architecture-deploiement/deploiement.md) ; mise en œuvre : [Publication des images Docker](../../choix-implementation/deploiement/publication-des-images-docker.md)).

Une vulnérabilité ainsi relevée est **traitée dans la mesure du possible** : son correctif est appliqué (version corrigée de l'image de base ou de la dépendance concernée). Si ce correctif ne peut pas être appliqué (ex. il impose une montée de version incompatible avec l'application), la Pull Request reste bloquée : la vulnérabilité est évaluée dans le contexte réel de l'application (impact, probabilité de survenance) et soumise à la décision de l'utilisateur.
