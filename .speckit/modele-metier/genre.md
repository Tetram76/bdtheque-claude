---
type: Modèle métier
title: 'Genre'
description: 'Libellé d''un genre, unique sans casse ni accents.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Genre.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/GenreConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Genre

| Attribut | Type | Obligatoire |
| --- | --- | --- |
| Libellé | texte | oui |

> **Contrainte :** le libellé d'un genre est **unique sans tenir compte de la casse ni des accents** : `Aventure`, `aventure` et `AVENTURE` désignent le même genre, de même que `Épopée` et `Epopee`.
