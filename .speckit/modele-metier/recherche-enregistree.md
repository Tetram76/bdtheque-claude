---
type: Modèle métier
title: 'Recherche enregistrée'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Recherche enregistrée

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Nom | texte | oui | Nom sous lequel la recherche est proposée en consultation. |
| Entité recherchée | énuméré | oui | Type des entités que la recherche renvoie (voir [Entités recherchées](/fonctionnel/recherche-avancee/entites-recherchees.md)). |
| Critères | expression | oui | Combinaison des critères (ET, OU, NON, groupes imbriqués), chacun avec la donnée interrogée, le chemin suivi depuis l'entité recherchée, la comparaison et sa valeur — constante ou variable (voir [Recherche avancée](/fonctionnel/recherche-avancee/recherche-avancee.md)). |
| Variables | liste | non | Constantes des critères remplacées par une variable, chacune avec sa **valeur par défaut**, dont la valeur est demandée au rejeu de la recherche. |

> **Contrainte :** le nom d'une recherche enregistrée est **unique sans tenir compte de la casse ni des accents**, comme le libellé d'un genre : les recherches ne se distinguent en consultation que par leur nom.
