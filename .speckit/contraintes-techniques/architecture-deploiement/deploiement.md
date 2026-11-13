---
type: Contrainte technique
title: 'Déploiement'
description: 'Isolation des tiers, Docker Compose, images publiées sur Docker Hub (nightly, à la demande), optimisées, sans aucune vulnérabilité corrigeable.'
applies_to:
  - 'docker-compose.yml'
  - 'docker/**'
  - '.github/workflows/docker-publish.yml'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-13T17:30:00Z }
---

# Déploiement

- Architecture **n-tiers avec isolation stricte** : chaque tier est déployé dans un **conteneur Docker dédié**.
- Un tier = un conteneur (pas de cohabitation de responsabilités dans un même conteneur).
- Orchestration via **Docker Compose**, compatible avec Synology Container Manager.
- Les **images Docker** de l'application sont publiées sur **Docker Hub**, sous le compte [`tetram76`](https://hub.docker.com/repositories/tetram76), avec pour nom de projet **`bdtheque`** (et non le nom du dépôt, `bdtheque-claude`). Les dépôts Docker Hub sont **privés**. Une image est publiée **quotidiennement** à partir de la branche `main`, sous le tag **`nightly`**, sauf si `main` n'a pas changé depuis la précédente publication quotidienne (aucune image n'est alors republiée) ; cette publication garantit qu'il n'existe qu'**une seule image `nightly`** sur Docker Hub. En dehors de cette publication quotidienne, les images ne sont publiées qu'**à la demande** (jamais à chaque fusion).
- Le contenu des images publiées est **optimisé pour la production**, en taille comme en contenu : elles ne contiennent que ce qui sert à l'exécution de l'application.
- **Vulnérabilités** : une image ne contient **aucune** vulnérabilité **corrigeable**, qu'elle soit nouvelle ou déjà présente dans l'image publiée, et une image qui en contient une n'est **jamais publiée**. Une vulnérabilité est corrigeable lorsque sa correction est mise à disposition et que le dépôt peut l'appliquer : version corrigée d'un package, version plus récente de l'image de base, image de base reconstruite par son éditeur sous le même tag. Tant que l'éditeur du composant n'a pas publié de correction que le dépôt peut appliquer, elle ne l'est pas, et ne bloque rien ([Suivi des versions et des vulnérabilités](../suivi-des-versions-et-des-vulnerabilites.md)). La surveillance des vulnérabilités découvertes après publication est assurée par **Docker Hub**, sur les images publiées.
