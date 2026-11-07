---
type: Règle métier
title: 'Données interrogées'
description: 'Données interrogeables (publiques, de gestion, déduites), données techniques exclues.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Données interrogées

- Toute donnée du modèle métier peut servir de critère, qu'elle soit **publique** (ex. titre, année de parution, genre), **de gestion** (ex. intention d'achat, date de création, date de dernière modification) ou **déduite** (ex. nombre d'éditions, clé de tri, initiale, appartenance à la collection, genres et univers affichés d'un album).
- Les données techniques, sans signification pour l'utilisateur, en sont exclues : données d'optimisation, s'il en existe, et référence du fichier d'un visuel d'édition ([Visuel d'édition](../../modele-metier/visuel-d-edition.md)).
- La recherche ne propose pas, dans un premier temps, de **regroupement** défini par l'utilisateur (somme, moyenne, minimum, maximum) ; les données déduites que l'application calcule elle-même (ex. nombre d'éditions) restent interrogeables, de même que le **nombre d'entités liées** qui remplissent des critères (cf. [Liens entre entités](liens-entre-entites.md)).
- Les règles de rattachement s'appliquent aux critères comme au reste de la consultation : un critère sur un univers couvre ses sous-univers (cf. [Hiérarchie des univers](../regles-metier/hierarchie-des-univers.md)).
