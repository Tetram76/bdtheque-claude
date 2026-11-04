---
type: Modèle métier
title: 'Intention d''achat'
description: 'Cible album ou édition d''une intention d''achat et ses contraintes.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/PurchaseIntent.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/PurchaseIntentConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Intention d'achat

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Cible album | référence | conditionnel | Album visé. Renseigné si l'intention porte sur un album (toute édition). Exclusif avec "Cible édition". |
| Cible édition | référence | conditionnel | Édition visée. Renseignée si l'intention porte sur une édition spécifique. Exclusive avec "Cible album". |

> **Contraintes :** Exactement l'un des deux champs "Cible album" ou "Cible édition" doit être renseigné. Une même cible (album ou édition) ne peut être visée que par une seule intention d'achat. Un album et l'une de ses éditions ne peuvent pas être visés simultanément. Un album visé par une intention portant sur lui-même n'a aucune édition. Une édition possédée (`Mode d'acquisition` renseigné) ne peut pas être visée.
