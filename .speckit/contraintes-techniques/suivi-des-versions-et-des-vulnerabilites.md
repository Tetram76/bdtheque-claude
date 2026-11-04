---
type: Contrainte technique
title: 'Suivi des versions et des vulnérabilités'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Suivi des versions et des vulnérabilités

Ces règles valent pour **toutes les technologies utilisées** : bibliothèques, SDK, images Docker, base de données, outils, actions du CI…

- **Suivi des versions** : les nouvelles versions de chaque technologie sont suivies.
- **Surveillance active des vulnérabilités** : les alertes de sécurité de chaque technologie sont surveillées activement, que le dépôt fasse ou non l'objet de travaux.
- **Correction au plus tôt** : une vulnérabilité signalée est corrigée dès que possible, c'est-à-dire dès qu'une correction est disponible.
- **Pas de blocage** : une vulnérabilité qu'aucune correction disponible ne permet encore de corriger ne bloque pas le projet ni la poursuite du développement.
- **Pas de mise à jour pour elle-même** : appliquer une mise à jour impose la revue de l'existant ([Exploitation des technologies retenues](/contraintes-techniques/exploitation-des-technologies-retenues.md)), pour que le dépôt profite de la nouvelle version.
- **Souhait** : aucune Pull Request de mise à jour n'est ouverte automatiquement hors brouillon.
