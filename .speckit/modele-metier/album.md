---
type: Modèle métier
title: 'Album'
description: 'Attributs d''un album : titre, clé de tri, type, hors-série, tomes, date de première publication, résumé, notes, appréciation.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Album.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/AlbumConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Album

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Titre | texte | conditionnel | Obligatoire si l'album n'appartient à aucune série. Optionnel si une série est rattachée (la série + le tome peuvent suffire à identifier l'album). |
| Clé de tri | texte | non | Calculée automatiquement depuis le titre et stockée explicitement (règle de calcul : voir [Tri et navigation par initiale](/fonctionnel/presentation/tri-et-navigation-par-initiale.md)). Absente si le titre est absent ; dans ce cas, la clé de tri de la série rattachée est utilisée comme valeur de substitution pour le tri et la navigation par initiale. |
| Clé de tri manuelle | booléen | oui | `false` par défaut. Passe à `true` si l'utilisateur a explicitement modifié la clé de tri. Quand `false`, la clé est recalculée automatiquement à chaque modification du titre. |
| Type | énuméré | oui | `Régulier` (défaut) / `Intégrale`. |
| Hors-série | booléen | oui | `false` par défaut. Indépendant du type : une intégrale peut être hors-série. |
| Numéro de tome | entier | non | |
| Tome de début | entier | conditionnel | Intégrales uniquement : premier tome couvert. Doit être renseigné si et seulement si le tome de fin l'est. |
| Tome de fin | entier | conditionnel | Intégrales uniquement : dernier tome couvert. Doit être renseigné si et seulement si le tome de début l'est. |
| Date de première publication | date partielle | non | Granularité : année seule, ou mois + année. Jamais de date complète (jour inconnu). |
| Résumé | texte long | non | Résumé propre à l'album. |
| Notes personnelles | texte long | non | Annotations libres saisies par l'utilisateur. |
| Note | énuméré (1–5) | non | Appréciation de l'utilisateur : 1 = Très mauvais, 2 = Mauvais, 3 = Moyen, 4 = Bien, 5 = Très bien. |

> **Contrainte (intégrale) :** Les tomes de début et de fin sont solidaires : soit les deux sont renseignés, soit aucun des deux. De plus, `tome de début ≤ tome de fin` est obligatoire. Les tomes référencés par la séquence n'ont pas à exister en tant qu'albums dans la base : il n'y a aucune contrainte d'intégrité référentielle sur cette séquence.
