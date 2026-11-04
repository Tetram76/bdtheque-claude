---
type: Modèle métier
title: 'Paramétrage de l''application'
description: 'Format du libellé des albums, culture d''affichage ; fiche unique.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/ApplicationSettings.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/ApplicationSettingsConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Paramétrage de l'application

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Format du libellé des albums | énuméré | oui | `Album (Série - Tome)` (défaut) / `Tome - Album (Série)` (voir [Libellé d'un album](/fonctionnel/presentation/libelle-d-un-album.md)). |
| Culture d'affichage | énuméré | oui | `Français (France)`, seule culture proposée (voir [Langue et culture d'affichage](/fonctionnel/presentation/langue-et-culture-d-affichage.md)). |

> **Contrainte :** le paramétrage est une fiche **unique**, toujours présente : il se modifie, mais ne se crée ni ne se supprime.
