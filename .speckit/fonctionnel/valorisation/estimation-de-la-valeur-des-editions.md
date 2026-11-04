---
type: Règle métier
title: 'Estimation de la valeur des éditions'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Estimation de la valeur des éditions

- La **valeur estimée** d'une édition est l'estimation de son **prix d'acquisition** lorsqu'il n'est pas renseigné. Seules les éditions **possédées**, **non gratuites** et **sans prix d'acquisition** en ont une : une édition dont le prix d'acquisition est connu n'est jamais estimée.
- Elle sert à valoriser la collection : la **valeur totale estimée** est la somme des prix d'acquisition connus et des valeurs estimées.
- Elle suit la règle de [Calcul des estimations](/fonctionnel/valorisation/calcul-des-estimations.md) : elle est calculée à partir des données de la collection, et n'est pas stockée en base.
- Le modèle d'estimation retenu est un **Random Forest**.
