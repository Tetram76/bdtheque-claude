---
type: Plan d'implémentation
title: 'Phase 6 — Outil pour agents IA'
description: 'Phase 6, outil pour agents IA, et ses notes.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Phase 6 — Outil pour agents IA

Serveur MCP exposant l'aide à la saisie, la recherche/consultation (recherche avancée comprise) et les analyses et statistiques ; accès par jeton, avec la gestion des jetons dans l'administration ; suivi de l'utilisation par jeton et par outil ou ressource ([Interface pour agents IA](/fonctionnel/agents-ia/interface-pour-agents-ia.md) ; [Outil pour agents IA](/choix-implementation/agents-ia.md)). Dépend des API de consultation (Phase 2), des statistiques (Phase 4) et de l'administration du frontend (Phase 3), qui accueille la gestion des jetons et le suivi de l'utilisation.

Découpage et version à établir à l'ouverture de la phase, une fois le périmètre de l'aide à la saisie précisé.

- **Note — modèle de domaine et journal des modifications** : la phase ajoute au modèle de domaine et à l'API les entités du suivi de l'outil ([`modele-metier/`](/modele-metier/index.md) : jeton d'accès, session, objectif de session, utilisation de l'outil, retour écrit), sans modifier les entités existantes, et étend le journal des modifications au jeton d'accès (type de fiche, correspondance de ses attributs, valeur consignée sans contenu pour son réarmement, [Journal des modifications : mise en œuvre](/choix-implementation/domaine/journal-des-modifications-mise-en-oeuvre.md)).
- **Note — image du conteneur `mcp`** : le conteneur dédié `mcp` ([Outil pour agents IA](/choix-implementation/agents-ia.md)) doit avoir son image, comme `api` et `frontend` : image de base ([Images de base des conteneurs](/choix-implementation/deploiement/images-de-base-des-conteneurs.md)), image publiée `tetram76/bdtheque-mcp`, intégrée à la paire d'images construites toutes avant d'en publier aucune, à l'analyse des vulnérabilités et au job `docker-build` du CI ([Publication des images Docker](/choix-implementation/deploiement/publication-des-images-docker.md) ; [Intégration continue (CI)](/gestion-projet/depot/integration-continue-ci.md), Ruleset compris). Sans elle, le déploiement sans les sources ne peut pas démarrer `mcp`.
