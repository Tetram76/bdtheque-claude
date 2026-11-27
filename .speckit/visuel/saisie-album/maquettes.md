---
type: Référence visuelle
title: 'Maquettes'
description: 'Maquettes de la saisie d''un album.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-27T18:00:00Z }
---

# Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/saisie-album/`](../maquettes/saisie-album/index.html) (cf. [Maquettes](../charte-graphique/maquettes.md)). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [Maquettes](../administration/maquettes.md)).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `exemple` | `modification` (par défaut) : un album d'une série, tel qu'il a été lu ; `modifiee` : le même album, des données changées depuis sa lecture, marquées « Modifié », un auteur et un genre ajoutés, un genre retiré ; `rechargee` : le même album rechargé après une modification faite entre-temps, ce que l'utilisateur avait saisi présenté sous chaque donnée ; `creation` : un nouvel album, vide ; `integrale` : une intégrale sans titre propre, dont les auteurs viennent d'être repris de sa série |
| `cleManuelle` | clé de tri saisie à la main (exemple `modification`) |
| `erreur` | `aucune` (par défaut), `metier`, `fonctionnelle` ou `technique` : l'erreur présentée après un enregistrement refusé |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives ; les couvertures sont des illustrations de substitution.
