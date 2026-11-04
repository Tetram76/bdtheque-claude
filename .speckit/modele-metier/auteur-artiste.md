---
type: Modèle métier
title: 'Auteur / Artiste'
description: 'Attributs d''un auteur, au moins un nom ou un pseudonyme.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Author.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/AuthorConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Auteur / Artiste

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Nom | texte | non | |
| Prénom | texte | non | |
| Pseudonyme | texte | non | |
| Biographie | texte long | non | |
| Nationalité | texte | non | |

> **Contraintes :** Au moins un des champs Nom ou Pseudonyme doit être renseigné. Un auteur peut représenter un duo ou collectif (ex. deux auteurs créditant sous un nom commun).
