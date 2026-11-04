---
type: Contrainte technique
title: 'Déploiement'
description: 'Isolation des tiers, Docker Compose, images publiées sur Docker Hub (nightly, à la demande), optimisées, sans vulnérabilité corrigeable introduite.'
applies_to:
  - 'docker-compose.yml'
  - 'docker/**'
  - '.github/workflows/docker-publish.yml'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Déploiement

- Architecture **n-tiers avec isolation stricte** : chaque tier est déployé dans un **conteneur Docker dédié**.
- Un tier = un conteneur (pas de cohabitation de responsabilités dans un même conteneur).
- Orchestration via **Docker Compose**, compatible avec Synology Container Manager.
- Les **images Docker** de l'application sont publiées sur **Docker Hub**, sous le compte [`tetram76`](https://hub.docker.com/repositories/tetram76), avec pour nom de projet **`bdtheque`** (et non le nom du dépôt, `bdtheque-claude`). Les dépôts Docker Hub sont **privés**. Une image est publiée **quotidiennement** à partir de la branche `main`, sous le tag **`nightly`**, sauf si `main` n'a pas changé depuis la précédente publication quotidienne (aucune image n'est alors republiée) ; cette publication garantit qu'il n'existe qu'**une seule image `nightly`** sur Docker Hub. En dehors de cette publication quotidienne, les images ne sont publiées qu'**à la demande** (jamais à chaque fusion).
- Le contenu des images publiées est **optimisé pour la production**, en taille comme en contenu : elles ne contiennent que ce qui sert à l'exécution de l'application.
- **Vulnérabilités** : une modification n'introduit dans les images aucune vulnérabilité **déjà connue et corrigeable**, et surtout une image qui en introduit une n'est **jamais publiée**. La surveillance des vulnérabilités découvertes après publication est assurée par **Docker Hub**, sur les images publiées.
