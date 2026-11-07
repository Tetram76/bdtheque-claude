---
type: Modèle métier
title: 'Série'
description: 'Attributs d''une série : titre, clé de tri, statut, tomes prévus, indicateurs, valeurs template.'
applies_to:
  - 'src/Bdtheque.Domain/Entities/Series.cs'
  - 'src/Bdtheque.Infrastructure/Configurations/SeriesConfiguration.cs'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Série

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Titre | texte | oui | |
| Clé de tri | texte | oui (auto) | Calculée automatiquement depuis le titre et stockée explicitement (règle de calcul : voir [Tri et navigation par initiale](../fonctionnel/presentation/tri-et-navigation-par-initiale.md)). Toujours présente car le titre est obligatoire. |
| Clé de tri manuelle | booléen | oui | `false` par défaut. Passe à `true` si l'utilisateur a explicitement modifié la clé de tri. Quand `false`, la clé est recalculée automatiquement à chaque modification du titre. |
| Statut | énuméré | non | `En cours` / `Terminée` / `Abandonnée`. `Terminée` signifie que tous les albums prévus par les auteurs ont été publiés. |
| Nombre de tomes numérotés (théorique) | entier | non | Nombre de tomes numérotés attendus dans la séquence principale de la série, selon l'utilisateur. Ne compte pas les hors-série ni les albums sans numéro de tome. Non calculé depuis la base — sert de borne supérieure pour la détection des albums manquants (queue théorique) et à évaluer la complétude de la collection. |
| Complète | booléen | oui | `false` par défaut. Choix explicite de l'utilisateur, indépendant du nombre d'albums réellement présents dans la collection. |
| Exclure des manquants | booléen | oui | `false` par défaut. Si `true`, la série est ignorée lors de la recherche des albums manquants. |
| Exclure des estimations de sortie | booléen | oui | `false` par défaut. Si `true`, la série ne fait l'objet d'aucune estimation de sortie, quel que soit son statut. |
| Résumé | texte long | non | Résumé propre à la série. |
| Notes personnelles | texte long | non | Annotations libres saisies par l'utilisateur. |
| *(template)* Reliure | énuméré | non | Valeur par défaut pour les nouvelles éditions. |
| *(template)* Orientation | énuméré | non | Valeur par défaut pour les nouvelles éditions. |
| *(template)* Sens de lecture | énuméré | non | Valeur par défaut pour les nouvelles éditions. |
| *(template)* Format | énuméré | non | Valeur par défaut pour les nouvelles éditions. |
| *(template)* Catégorie d'édition | énuméré | non | Valeur par défaut pour les nouvelles éditions. |
| *(template)* État | énuméré | non | Valeur par défaut pour les nouvelles éditions. |
| *(template)* En couleur | booléen | non | Valeur par défaut pour les nouvelles éditions. |

> Le formulaire de création d'une série pré-remplit ses valeurs template avec les **valeurs par défaut** des attributs correspondants de l'édition (cf. [Édition](edition.md)) ; elles restent modifiables, et ne sont pas portées par la base (aucune clause `DEFAULT`).
