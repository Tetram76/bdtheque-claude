---
type: Modèle métier
title: 'Compteur d''audience mensuel'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Compteur d'audience mensuel

| Attribut | Type | Obligatoire | Remarques |
| --- | --- | --- | --- |
| Fonction | texte | oui | Écran ou action compté. |
| Mois | date partielle (mois + année) | oui | Mois du début des visites comptées : toutes les utilisations d'une visite sont comptées au mois de son début. |
| Administrateur | booléen | oui | Visites de l'administrateur ou des visiteurs publics. |
| Type d'appareil | énuméré | oui | Comme pour la visite. |
| Moteur de rendu | énuméré | oui | Comme pour la visite. |
| Version du moteur | texte | non | Comme pour la visite. |
| Nombre d'utilisations | entier | oui | |
| Nombre de visites | entier | oui | Visites au cours desquelles la fonction a été utilisée. |
| Nombre d'utilisations répétées | entier | oui | Utilisations qui répètent aussitôt, dans leur visite, la précédente sur la même fiche ou avec les mêmes paramètres. |
| Dernière utilisation | date et heure | oui | |
| Objets | liste | non | Pour une fonction qui présente une fiche ou porte des paramètres : chaque fiche présentée ou chaque jeu de paramètres employé, avec son nombre d'utilisations. |

> **Contraintes :** un compteur est unique pour une fonction, un mois et une combinaison d'administrateur, de type d'appareil et de moteur de rendu avec sa version. Il est établi à partir des visites du mois au plus tard avant la suppression de leur détail, n'est jamais modifié ensuite, et est conservé sans limite. Ce n'est pas une fiche : son enregistrement n'est pas consigné au journal des modifications.
>
> Tout indicateur se recompose à partir des compteurs, quels que soient les compartiments et les mois réunis (ex. visiteurs et administrateur ensemble, tous moteurs, une année) : les nombres d'utilisations, de visites et d'utilisations répétées s'additionnent, une visite n'appartenant qu'à un compteur par fonction (un seul mois, un seul compartiment) ; la dernière utilisation est la plus récente ; le nombre de fiches ou de paramètres différents est celui des objets distincts des compteurs réunis, qui ne s'additionne pas — d'où la liste des objets plutôt qu'un nombre.
