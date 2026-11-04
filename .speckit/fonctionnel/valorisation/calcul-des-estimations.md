---
type: Règle métier
title: 'Calcul des estimations'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Calcul des estimations

Toute **estimation** produite par l'application (valeur estimée d'une édition, estimation de sortie d'un nouvel album) est **toujours calculée dynamiquement**, à partir de l'**état courant** de la base : elle n'est jamais stockée, ni figée à partir d'un état antérieur des données.

Objectif : tirer parti de l'enrichissement progressif de la base pour **affiner les estimations au fil du temps**, dans la mesure où les données ajoutées le permettent.
