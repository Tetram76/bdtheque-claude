---
type: Gouvernance
title: 'Stratégie de branches'
description: 'Branches persistantes (main et branches de release seules), nommage des branches (feat/, fix/, chore/).'
tags: [socle]
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T16:08:46Z }
---

# Stratégie de branches

- `main` : branche stable, reflète l'état livrable du projet
- `feat/<sujet>` : développement de fonctionnalités
- `fix/<sujet>` : corrections de bugs
- `chore/<sujet>` : maintenance (config, refacto, outillage, CI)
- **Branches persistantes** : seules `main` et les branches de release le sont. Toute autre branche disparaît une fois sa Pull Request fusionnée ou abandonnée : un travail à conserver sans être fusionné l'est dans le dépôt lui-même (ex. un prototype dans `poc/`), jamais dans une branche.
