---
type: Modèle métier
title: 'Utilisation d''une fonction'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Utilisation d'une fonction

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Fonction | texte | oui | Écran affiché ou action déclenchée. |
| Fiche présentée | type de fiche + identifiant | conditionnel | Renseignée pour l'affichage d'un écran qui présente une fiche précise (ex. fiche d'un album), absente sinon. Conservée après la suppression de la fiche : aucune contrainte d'intégrité référentielle, comme pour le journal des modifications. |
| Paramètres | texte long | conditionnel | Renseignés pour une action qui en a (ex. critères d'une recherche, numéro de page, fiche enregistrée), absents sinon ; jamais un secret (mot de passe, valeur d'un jeton d'accès). |
| Date | date et heure | oui | Moment de l'utilisation. |

> Les indicateurs de la mesure d'audience (nombre d'utilisations et de visites, fiches et paramètres différents, utilisations répétées, fréquence, dernière utilisation, voir [Mesure d'audience de l'interface web](/fonctionnel/mesure-audience.md)) sont déduits de ces utilisations tant qu'elles sont conservées, puis des compteurs d'audience mensuels.
