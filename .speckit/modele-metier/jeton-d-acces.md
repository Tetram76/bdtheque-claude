---
type: Modèle métier
title: 'Jeton d''accès'
description: 'Attributs d''un jeton d''accès de l''outil pour agents IA (Phase 6).'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Jeton d'accès

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Nom | texte | oui | Identifie l'usage du jeton (ex. l'agent ou le poste qui l'utilise). |
| Accès à l'administration | booléen | oui | `false` : niveau d'accès public ; `true` : niveau du compte administrateur (voir [Accès par jeton](../fonctionnel/agents-ia/acces-par-jeton.md)). |
| Expiration | date et heure | non | À partir de laquelle le jeton est archivé, donc refusé. Absente : le jeton reste valable jusqu'à son archivage ou sa suppression. Fixée à la génération, elle ne change que par un réarmement, qui change aussi la valeur. Si elle est renseignée, elle est **postérieure** au moment de la génération ou du réarmement : une valeur ne naît jamais expirée. |
| Archivé | booléen | oui | `false` par défaut. Un jeton archivé est refusé, mais conservé avec son historique d'utilisation (voir [Accès par jeton](../fonctionnel/agents-ia/acces-par-jeton.md)). Un jeton expiré est archivé. |
| Valeur | secret | oui | Générée par l'application, jamais saisie ; présentée une seule fois, à la génération ou au réarmement du jeton, qui la remplace, et conservée sous une forme qui ne permet pas de la présenter de nouveau. |
