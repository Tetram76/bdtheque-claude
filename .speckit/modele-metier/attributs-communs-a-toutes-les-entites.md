---
type: Modèle métier
title: 'Attributs communs à toutes les entités'
description: 'Dates de création et de dernière modification, renseignées automatiquement ; celles d''une fiche provisionnée au déploiement.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Common/EntityBase.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-23T20:45:00Z }
---

# Attributs communs à toutes les entités

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Date de création | date et heure | oui | Renseignée automatiquement à la création de la fiche, jamais saisie. |
| Date de dernière modification | date et heure | oui | Renseignée automatiquement à chaque modification de la fiche, jamais saisie. |

> **Fiche provisionnée au déploiement** (ex. le [Paramétrage de l'application](parametrage-de-l-application.md)) : personne ne la crée, si bien que sa date de création n'a aucun sens pour l'utilisateur, et n'est jamais présentée. Sa date de dernière modification ne l'est qu'une fois la fiche modifiée : jusque-là, elle n'est que celle du déploiement (exigence de l'utilisateur). Seule est concernée une fiche que l'application provisionne elle-même au déploiement : les données chargées explicitement pour la validation fonctionnelle ([Environnement de validation fonctionnelle](../choix-implementation/deploiement/environnement-de-validation-fonctionnelle.md)), créées comme toute fiche, n'en sont pas (exigence de l'utilisateur).
