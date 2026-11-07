---
type: Modèle métier
title: 'Session de l''outil pour agents IA'
description: 'Ouverture et expiration d''une session de l''outil (Phase 6).'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Session de l'outil pour agents IA

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Ouverture | date et heure | oui | Moment de l'ouverture de la session. |
| Expiration | date et heure | oui | Fin de validité du jeton de session, annoncée à l'agent à l'ouverture (voir [Suivi de l'utilisation](../fonctionnel/agents-ia/suivi-de-l-utilisation.md)). Un redémarrage de l'outil peut aussi invalider une session, sans que son expiration change. |
