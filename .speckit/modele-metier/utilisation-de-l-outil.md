---
type: Modèle métier
title: 'Utilisation de l''outil'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Utilisation de l'outil

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Point d'entrée | texte | oui | Outil appelé ou ressource lue. |
| Paramètres | texte long | oui | Paramètres tels que l'agent les a transmis (arguments de l'outil, adresse de la ressource) ; jamais le jeton d'accès. |
| Date | date et heure | oui | Moment de l'appel. |
| Durée | durée | oui | |
| Client | texte | non | Identité du logiciel client, s'il la transmet. |
| Issue | énuméré | oui | `Succès` / `Erreur métier` / `Erreur fonctionnelle` / `Erreur technique` (voir [Présentation des erreurs](/fonctionnel/presentation-des-erreurs.md)) / `Session refusée` (jeton de session absent, inconnu ou expiré). |
| Message d'erreur | texte long | non | Message renvoyé à l'agent, pour une utilisation en erreur. |
| Nombre de résultats | entier | non | Pour une utilisation qui renvoie des résultats. |
| Taille de la réponse | entier | oui | |

> L'indice d'efficacité et ses signaux (voir [Suivi de l'utilisation](/fonctionnel/agents-ia/suivi-de-l-utilisation.md)) sont déduits de ces utilisations, jamais stockés.
