---
type: Règle métier
title: 'Libellé d''une édition'
description: 'Libellé d''une édition : éditeur, collection, année, ISBN.'
generated: { by: claude-code/claude-opus-5-5, at: 2026-11-09T20:00:00Z }
---

# Libellé d'une édition

Le libellé d'une édition est construit dynamiquement selon le modèle suivant :

```text
{Éditeur}[ ({Collection})][ [{Année d'édition}]][ - ISBN {ISBN formaté}]
```

- La collection n'apparaît que si elle est renseignée.
- L'année n'apparaît que si elle est connue.
- L'ISBN n'apparaît que s'il est connu, préfixé de `ISBN ` et séparé par ` - `.
- L'ISBN est formaté avec ses tirets, placés selon les plages de l'Agence internationale de l'ISBN ; faute de pouvoir le formater (plages indisponibles ou illisibles, ISBN qu'aucune plage ne place), il est présenté tel qu'enregistré, jamais omis. Une indisponibilité des plages est signalée dans le journal de l'application.
- Un ISBN est d'un seul bloc : où qu'il soit affiché, dans un libellé avec sa mention `ISBN ` comme seul (détail d'une édition), il n'est jamais coupé en fin de ligne, ni de sa mention, ni à l'un de ses tirets.

**Exemples :**

- `Dargaud` *(aucune collection, année ni ISBN)*
- `Dargaud (Lucky Luke) [1978]`
- `Dargaud (Lucky Luke) [1978] - ISBN 978-2-205-01234-5`
- `Dargaud [1978] - ISBN 978-2-205-01234-5`
