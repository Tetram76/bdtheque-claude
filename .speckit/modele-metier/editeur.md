---
type: Modèle métier
title: 'Éditeur'
description: 'Attributs d''un éditeur, nom unique sans casse ni accents.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Publisher.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/PublisherConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-14T12:00:00Z }
---

# Éditeur

| Attribut | Type | Obligatoire |
| --- | --- | --- |
| Nom | texte | oui |
| Site web | URL | non |

> **Contrainte :** le nom d'un éditeur est **unique sans tenir compte de la casse ni des accents**, comme le libellé d'un genre ([Genre](genre.md)) : `Dargaud`, `dargaud` et `DARGAUD` désignent le même éditeur, de même que `Éditions Delcourt` et `Editions Delcourt`. Deux noms qui ne diffèrent que par la casse ou les accents sont une saisie en double, que la fusion devrait ensuite réparer.
