---
type: Modèle métier
title: 'Collection éditeur'
description: 'Nom d''une collection éditeur, unique chez son éditeur sans casse ni accents.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/PublisherCollection.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/PublisherCollectionConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-14T12:00:00Z }
---

# Collection éditeur

| Attribut | Type | Obligatoire |
| --- | --- | --- |
| Nom | texte | oui |

> **Contrainte :** le nom d'une collection est **unique au sein de son éditeur, sans tenir compte de la casse ni des accents**, comme le nom d'un éditeur ([Éditeur](editeur.md)) : chez Dupuis, `Aire Libre` et `aire libre` désignent la même collection. Deux éditeurs peuvent en revanche avoir chacun une collection du même nom.
