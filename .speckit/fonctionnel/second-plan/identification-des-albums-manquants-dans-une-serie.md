---
type: Règle métier
title: 'Identification des albums manquants dans une série'
description: 'Tomes manquants d''une série, intervalles, options sur les intégrales et les intentions.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Identification des albums manquants dans une série

Accessible en **mode consultation** (public). Permet de savoir quels albums d'une série ne sont pas encore présents dans la collection.

Un tome est **manquant** s'il est absent de la séquence théorique de tomes de la série (voir [Séquence théorique de tomes d'une série](/fonctionnel/regles-metier/sequence-theorique-de-tomes-d-une-serie.md)).

Les intégrales **couvrent** la plage `tome de début → tome de fin` de la séquence (chaque tome de la plage est considéré présent).

**Exclusions systématiques :**

- les séries avec l'attribut **Exclure des manquants**

**Affichage des manquants :**

Les tomes manquants consécutifs sont regroupés et affichés sous forme d'intervalle (ex. `T. 3 à 5` plutôt que `T. 3`, `T. 4`, `T. 5`).

**Options utilisateur :**

- **Exclure les intégrales** : les intégrales ne couvrent plus leur plage (pour posséder chaque tome en album individuel).
- **Exclure les intentions d'achat** : les intentions ne comblent plus les trous — affiche tous les manquants réels, intentions incluses.
