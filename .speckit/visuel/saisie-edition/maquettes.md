---
type: Référence visuelle
title: 'Maquettes'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Maquettes

Toutes les déclinaisons sont présentées côte à côte par la visionneuse [`maquettes/saisie-edition/`](/visuel/maquettes/saisie-edition/index.html) (cf. [Maquettes](/visuel/charte-graphique/maquettes.md)). Elles ont une source commune, `mockup.html`, que chacune affiche avec son thème, choisi par le paramètre `theme` (mêmes valeurs que pour la page d'accueil de l'administration, [Maquettes](/visuel/administration/maquettes.md)).

Réglages de la source commune :

| Réglage | Valeurs |
| --- | --- |
| `exemple` | `modification` (par défaut) : une édition possédée, avec onze visuels ; `creation` : une nouvelle édition, pré-remplie d'après la série de l'album ; `nouvelleIntention` : une nouvelle édition visée par une intention d'achat, créée avec elle ; `intention` : une édition visée par une intention d'achat, donc non possédée |
| `mode` | `achat` (par défaut) ou `offerte` : le mode d'acquisition, dont suivent les libellés de la date et du montant |
| `gratuite` | édition gratuite (avec le mode `offerte`) |
| `isbn` | `valide` (par défaut) ou `incoherent` : chiffre de contrôle incohérent |
| `televersement` | `aucun` (par défaut), `encours` (un envoi en cours, un autre en attente) ou `refus` (deux fichiers refusés) |
| `visuelMenu` | menu des actions d'un visuel ouvert |
| `erreur` | `aucune` (par défaut), `metier`, `fonctionnelle` ou `technique` |
| `menuOpen` | menu de l'administration ouvert |

Les données sont fictives ; les visuels sont des illustrations de substitution.
