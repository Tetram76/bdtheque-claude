---
type: Règle métier
title: 'Libellé d''un album'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-04T12:00:00Z }
---

# Libellé d'un album

Le libellé d'un album utilise son titre en forme naturelle (cf. [Tri et navigation par initiale](/fonctionnel/presentation/tri-et-navigation-par-initiale.md)).

Il peut optionnellement inclure le **titre de la série**, utilisé quand le contexte ne rend pas la série évidente (ex. résultat de recherche multi-séries). Quand la série est incluse et que l'album n'a pas de titre propre, le titre de la série tient lieu de titre.

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
