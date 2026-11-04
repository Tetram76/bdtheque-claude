---
type: Gouvernance
title: 'Règles de travail de l''agent'
description: 'Contre-vérification par des sources externes avant toute décision technique ; titre de la conversation selon le chantier.'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Règles de travail de l'agent

- La base de connaissance de l'agent est considérée **toujours potentiellement obsolète**. Avant toute décision technique (choix de bibliothèque, version, API, configuration, bonne pratique), l'agent **doit contre-vérifier** ses connaissances via des sources externes reconnues et fiables (documentation officielle, dépôts GitHub officiels, etc.).
- Aucune décision technique ne peut reposer uniquement sur la mémoire de l'agent.
- **Titre de la conversation** : dès qu'un chantier est choisi pour être réalisé (chantier du plan, ticket hors plan, montée de version…), l'agent renomme la conversation pour que son titre corresponde à ce chantier (ex. le numéro et le titre de son ticket) : la liste des conversations dit ainsi, sans les ouvrir, ce que chacune réalise. Le choix d'un autre chantier dans la même conversation la renomme de même.
