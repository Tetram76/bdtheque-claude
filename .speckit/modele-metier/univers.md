---
type: Modèle métier
title: 'Univers'
description: 'Nom et description d''un univers, nom unique sous son parent sans casse ni accents.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Universe.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/UniverseConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-14T12:00:00Z }
---

# Univers

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Nom | texte | oui | |
| Description | texte long | non | |

> **Contrainte :** le nom d'un univers est **unique au sein de son parent, sans tenir compte de la casse ni des accents**, comme le nom d'un éditeur ([Éditeur](editeur.md)) : deux univers de même parent, ou tous deux sans parent, ne peuvent pas s'appeler `Star Wars` et `star wars`. Le même nom reste permis sous deux parents différents ([Univers — relations](relations-et-cardinalites.md#univers--relations)), un univers étant situé par ses ancêtres.
