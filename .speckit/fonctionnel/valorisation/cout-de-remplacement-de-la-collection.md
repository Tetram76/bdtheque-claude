---
type: Règle métier
title: 'Coût de remplacement de la collection'
description: 'Statistique envisagée, non statuée (YAGNI).'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Coût de remplacement de la collection

Statistique **envisagée** : rien n'est statué, elle n'est pas à implémenter tant qu'elle n'est pas décidée (YAGNI).

- Le **coût de remplacement** de la collection est ce que coûterait aujourd'hui l'acquisition de toutes ses éditions : la valeur de chaque édition y est remplacée par son **prix actuel**.
- **Problème non résolu** : obtenir ces prix actuels. Le volume de la collection rend impraticable une recherche du prix de chaque édition, et certaines éditions ne sont plus en vente.
- **Piste envisagée** : établir des **groupes d'éditions** selon des critères **hors dates** (par exemple à la manière d'un Random Forest), récupérer les prix actuels de quelques éditions de chaque groupe, puis **extrapoler** ces prix aux autres éditions du même groupe.
