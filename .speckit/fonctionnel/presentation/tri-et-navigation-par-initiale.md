---
type: Règle métier
title: 'Tri et navigation par initiale'
description: 'Clé de tri des titres (article en suffixe) et des artistes, entrées A à Z, # et @, clé auto ou manuelle.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Tri et navigation par initiale

## Règle générale

Le tri alphabétique des **séries**, **albums** et **artistes** repose sur une **clé de tri** distincte de la valeur affichée. L'affichage respecte toujours la forme naturelle ; seule la clé de tri est transformée.

## Entrées de la navigation par initiale

La navigation par initiale propose les entrées **A** à **Z**, une entrée **#** et une entrée **@** :

- Une initiale qui est une lettre ayant une **lettre de base** de `A` à `Z` dans l'ordre alphabétique (lettre accentuée, en minuscule, ligature…) est rangée sous cette lettre de base en majuscule : `É`, `È`, `Ê` → **E** ; `à` → **A** ; `Ç` → **C** ; `Œ` → **O**. À l'intérieur d'une entrée, l'ordre de tri linguistique habituel s'applique.
- Une initiale **numérique** (chiffre) est rangée sous **#** (ex. `13`, `2001 Nights`).
- Toute **autre** initiale (ponctuation, symbole, lettre sans lettre de base de `A` à `Z`, comme celles d'un autre alphabet que le latin) est rangée sous **@** (ex. `...Et après`, `Ωmega`).

## Titres (séries et albums)

- La clé de tri d'un titre place le mot significatif en tête et reporte l'**article initial en suffixe**, entre crochets (ex. `Lotus bleu [Le]`), plutôt que de le supprimer : ceci garantit un ordre **déterministe** entre deux titres qui ne diffèrent que par leur article (ex. `Un Lotus bleu` et `Le Lotus bleu` ne doivent jamais se trouver à une position arbitraire l'un par rapport à l'autre).
- L'**initiale de navigation** (navigation par lettre) est la première lettre de la clé de tri, soit le premier caractère du mot significatif (**hors article**).
- L'affichage du titre reste toujours en forme naturelle, article inclus en tête.
- **Articles reconnus** : `L'`, `Le`, `La`, `Les`, `Un`, `Une`, `Des`.

Exemples :

| Titre affiché | Clé de tri | Initiale | Entrée de navigation |
| --- | --- | --- | --- |
| `Le Lotus bleu` | `Lotus bleu [Le]` | **L** | **L** |
| `Un Lotus bleu` | `Lotus bleu [Un]` | **L** | **L** |
| `Les Schtroumpfs` | `Schtroumpfs [Les]` | **S** | **S** |
| `L'Épervier` | `Épervier [L']` | **É** | **E** |
| `Tintin` | `Tintin` | **T** | **T** |
| `13` | `13` | **1** | **#** |
| `...Et après` | `...Et après` | **.** | **@** |

Ce traitement est **non configurable** : il est obligatoire pour que la navigation par initiale soit viable.

## Calcul et stockage de la clé de tri

La clé de tri des titres est **calculée automatiquement** à partir du titre et **stockée explicitement** en base. Elle est toujours visible dans le formulaire de saisie et modifiable par l'utilisateur.

La clé possède deux états :

- **Auto** : calculée depuis le titre, jamais modifiée manuellement. Recalculée automatiquement à chaque modification du titre (mise à jour en temps réel dans le formulaire). C'est l'état par défaut.
- **Manuelle** : l'utilisateur a explicitement saisi une valeur. Elle est conservée telle quelle même si le titre change. Un indicateur visuel signale cet état dans le formulaire ; un bouton "réinitialiser" permet de repasser en mode Auto.

La clé stockée fait foi pour le tri et la navigation par initiale.

## Artistes

L'**identifiant principal** d'un artiste est son pseudonyme s'il est renseigné, sinon `Prénom Nom` (ou `Nom` seul si le prénom est absent).

- **Affichage** : identifiant principal.
- **Clé de tri** : identifiant principal. Exception : si l'identifiant est `Prénom Nom`, la clé de tri est `Nom Prénom` pour un classement alphabétique par nom de famille.
- **Initiale de navigation** : première lettre de la clé de tri.

Exemples :

| Données | Affiché | Clé de tri | Initiale |
| --- | --- | --- | --- |
| Jean Van Hamme (sans pseudo) | `Jean Van Hamme` | `Van Hamme Jean` | **V** |
| Pseudonyme `Moebius` | `Moebius` | `Moebius` | **M** |
| Georges Remi + pseudo `Hergé` | `Hergé` | `Hergé` | **H** |
