---
type: Modèle métier
title: 'Contribution *(relation Album ↔ Auteur/Artiste)*'
description: 'Rôle et artiste d''une contribution.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Contribution.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/ContributionConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Contribution *(relation Album ↔ Auteur/Artiste)*

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Rôle | énuméré | oui | `Scénariste` / `Dessinateur` / `Coloriste`. |
| Auteur/Artiste | référence | oui | Toute contribution est nécessairement associée à un artiste identifié. |

> Un artiste peut être associé à plusieurs rôles sur le même album (ex. scénariste et dessinateur). Chaque combinaison album + rôle + artiste constitue une ligne de contribution distincte.
