---
type: Référence visuelle
title: 'Maquettes'
description: 'Maquettes de la saisie d''un auteur.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-10T15:32:35Z }
---

# Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/saisie-auteur/`](../maquettes/saisie-auteur/index.html) (cf. [Maquettes](../charte-graphique/maquettes.md)). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [Maquettes](../administration/maquettes.md)).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `exemple` | `modification` (par défaut) : un auteur connu sous un pseudonyme ; `creation` : un nouvel auteur, vide |
| `erreur` | `aucune` (par défaut), `metier`, `fonctionnelle` ou `technique` : l'erreur présentée après un enregistrement refusé |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives ; les couvertures sont des illustrations de substitution.

Ces maquettes présentent encore la saisie sur une page dédiée, en blocs, avec un cadre en tirets de ce qui découle des noms et les dates sous le formulaire : elles ne restent la référence que de ses champs, le panneau qui les porte étant celui d'un univers ([Maquettes](../listes/maquettes.md) des listes, réglage `panneau`), sans ce cadre, et les dates en tête ([Contenu](contenu.md)).
