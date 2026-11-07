---
type: Règle métier
title: 'Estimation de sortie d''un nouvel album'
description: 'Estimation de la sortie du prochain tome d''une série en cours, d''après le rythme de parution.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Estimation de sortie d'un nouvel album

Accessible en **mode consultation** (public). Permet d'estimer la date de sortie du prochain tome d'une série, sur la base du rythme de parution observé.
On parle d'**estimation** et non de prévision : le facteur humain rend toute prédiction précise impossible, et l'objectif est uniquement de donner un ordre de grandeur.

Comme toute estimation, elle suit la règle de [Calcul des estimations](../valorisation/calcul-des-estimations.md).

Règles de calcul :

1. L'estimation se fait **série par série**. Elle n'est calculée que pour les séries dont le statut est `En cours` — les séries `Terminée` (tous les albums prévus ont été publiés) et `Abandonnée` ne font l'objet d'aucune estimation — et qui n'ont pas l'attribut **Exclure des estimations de sortie**.
2. Elle se base sur les **dates de première publication** des albums réguliers de la série (les intégrales et hors-série sont exclus du calcul).
3. Il faut **au moins 2 albums réguliers** avec une date de première publication **passée** pour pouvoir produire une estimation. Les albums dont la date de première publication est strictement dans le futur sont exclus du calcul du rythme. La règle s'adapte à la granularité de la date saisie :

- **Mois + année** : le mois en cours est considéré comme passé. Seuls les mois strictement postérieurs au mois courant sont exclus.
- **Année seule** : l'année en cours est considérée comme passée. Seules les années strictement postérieures à l'année courante sont exclues.

4. Le délai attendu avant le prochain tome est déduit à partir des **délais entre les parutions précédentes**, en tenant compte des éventuels **trous dans la séquence de tomes** (un tome manquant dans la collection ne doit pas fausser le calcul du rythme).
5. Les **évolutions de rythme** (accélération ou décélération des parutions) doivent être prises en compte dans l'estimation.
