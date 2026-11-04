---
type: Modèle métier
title: 'Éditeur'
description: 'Attributs d''un éditeur, nom unique tel que saisi.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Publisher.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/PublisherConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Éditeur

| Attribut | Type | Obligatoire |
| --- | --- | --- |
| Nom | texte | oui |
| Site web | URL | non |

> **Contrainte :** le nom d'un éditeur est **unique tel que saisi** : deux éditeurs ne peuvent pas porter exactement le même nom, mais des noms ne différant que par la casse (ex. `Dargaud` et `dargaud`) restent distincts.
