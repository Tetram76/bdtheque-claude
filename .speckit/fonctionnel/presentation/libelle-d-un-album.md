---
type: Règle métier
title: 'Libellé d''un album'
description: 'Libellé d''un album : série reprise selon que le contexte la rend évidente, tome présenté une seule fois sur un écran, tome selon le type, deux formats paramétrables, repli.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-15T10:00:00Z }
---

# Libellé d'un album

Le libellé d'un album utilise son titre en forme naturelle (cf. [Tri et navigation par initiale](tri-et-navigation-par-initiale.md)).

## Série reprise selon le contexte

> Quand le contexte de la série est évident, on ne reprend pas le nom de la série ; quand le contexte de la série n'est pas évident, il faut le nom de la série. (exigence de l'utilisateur)

- **Contexte qui rend la série évidente** : la fiche de l'album, sous le lien vers sa série ; « Et le reste de la série ? » de cette fiche ; la fiche de la série ; la bibliographie d'un auteur, où l'album est rangé sous sa série. L'album y est nommé par son **titre propre** ; sans titre propre, par son **tome**, écrit en entier (« Tome 3 », [Représentation du tome selon le type d'album](#représentation-du-tome-selon-le-type-dalbum)) ; sans l'un ni l'autre, par le repli (§ Fallback).
- **Contexte qui ne la rend pas évidente** : tout ce qui mêle des albums de plusieurs séries, ou présente un album sans sa série à côté (listes et pages de l'administration, recherche, dashboard, journal des modifications, titre de la page d'une fiche dans le navigateur). L'album y est nommé par son **libellé complet**, qui inclut le titre de la série, dans le format du paramétrage (§ Deux formats configurables) ; quand l'album n'a pas de titre propre, le titre de la série tient lieu de titre.

## Tome présenté une seule fois

> Sur sa fiche, avoir 2 fois l'information du numéro de tome, c'est une de trop ! (exigence de l'utilisateur)

Un même écran ne présente le tome d'un album **qu'une fois**. Un album nommé par son tome (sans titre propre, dans un contexte qui rend la série évidente) n'a donc, à côté de son nom, ni étiquette, ni colonne, ni mention de son tome ; un album qui a un titre propre présente son tome à part, en abrégé (« T. 3 »).

## Représentation du tome selon le type d'album

| Type | Hors-série | Format du tome (avec titre propre) | Format du tome (sans titre propre) |
| --- | --- | --- | --- |
| Régulier | non | `T. {N}` | `Tome {N}` |
| Régulier | oui | `HS[ {N}]` | `Hors-série[ {N}]` |
| Intégrale | non | `INT.[ - {N}][ [{Début} à {Fin}]]` | `Intégrale[ - {N}][ [{Début} à {Fin}]]` |
| Intégrale | oui | `INT.HS[ - {N}][ [{Début} à {Fin}]]` | `Intégrale hors-série[ - {N}][ [{Début} à {Fin}]]` |

Le numéro de tome n'apparaît que s'il est renseigné. Pour les intégrales, la séquence `[Début à Fin]` n'apparaît que si les deux bornes sont renseignées.

## Deux formats configurables (paramètre de l'application)

**Format 0 — `Album (Série - Tome)`** *(défaut)*

```text
{Titre} ({Série} - {Tome})
```

- Si pas de titre : `{Série} - {Tome}`
- Si pas de série : `{Titre} ({Tome})`
- Si pas de tome : `{Titre} ({Série})`
- Si pas de titre ni de tome : `{Série}`
- Si pas de série ni de tome : `{Titre}`

**Format 1 — `Tome - Album (Série)`**

```text
{Tome} - {Titre} ({Série})
```

- Si pas de titre : `{Tome} - {Série}`
- Si pas de série : `{Tome} - {Titre}`
- Si pas de tome : `{Titre} ({Série})`
- Si pas de titre ni de tome : `{Série}`
- Si pas de série ni de tome : `{Titre}`

## Fallback

Si toutes les composantes sont vides : `<Sans titre>`.

## Exemples (format 0, avec série)

| Album | Résultat |
| --- | --- |
| Titre + série + tome 5 | `Le Lotus bleu (Tintin - T. 5)` |
| Sans titre, série + tome 5 | `Tintin - T. 5` |
| Titre + hors-série 2 | `Titre (Série - HS 2)` |
| Titre + intégrale tomes 1 à 6 | `Titre (Série - INT. [1 à 6])` |
