---
type: Référence visuelle
title: 'Maquettes'
description: 'Maquettes des listes et leurs réglages.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-07T10:09:22Z }
---

# Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/listes/`](../maquettes/listes/index.html) (cf. [Maquettes](../charte-graphique/maquettes.md)). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [Maquettes](../administration/maquettes.md)).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `entite` | la liste présentée : `series` (par défaut), `editions`, `intentions`, `auteurs`, `editeurs`, `genres` ou `univers` |
| `filtres` | panneau des filtres ouvert (séries et éditions) |
| `actions` | menu des autres actions d'une ligne ouvert |
| `edition` | un genre en cours de modification dans sa ligne |
| `ajout` | cadre d'ajout d'un genre ouvert |
| `doublon` | un nouveau genre refusé, son libellé étant déjà pris (cadre d'ajout ouvert) |
| `panneau` | panneau d'un univers : `aucun` (par défaut), `modification`, `creation` ou `cycle` (parent refusé) |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives.
